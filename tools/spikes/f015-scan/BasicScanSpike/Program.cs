using System.Runtime.InteropServices;
using System.Diagnostics;

// Spike descartável — F015 T2b. Não entra em src/.
// Varredura de volume com FindFirstFileExW(FIND_FIRST_EX_LARGE_FETCH).
// Não desce em pontos de reanálise, não abre arquivo.
// Uso: BasicScanSpike.exe <raiz1> [raiz2 ...]

if (args.Length == 0)
{
    Console.Error.WriteLine("Uso: BasicScanSpike.exe <raiz1> [raiz2 ...]");
    Console.Error.WriteLine("Ex.:  BasicScanSpike.exe C:\\ D:\\ R:\\ S:\\");
    return 1;
}

foreach (string root in args)
{
    string normalizedRoot = root.TrimEnd('\\', '/') + "\\";
    if (!Directory.Exists(normalizedRoot))
    {
        Console.WriteLine($"[SKIP] {normalizedRoot} — não existe ou sem acesso");
        continue;
    }

    long fileCount = 0, dirCount = 0;
    long wsBefore = Process.GetCurrentProcess().WorkingSet64;

    var sw = Stopwatch.StartNew();
    try
    {
        Scanner.ScanDirectory(normalizedRoot, ref fileCount, ref dirCount);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ERRO {normalizedRoot}] {ex.Message}");
    }
    sw.Stop();

    // Forçar leitura do working set atualizado
    long wsAfter = Process.GetCurrentProcess().PeakWorkingSet64;
    long ramMB   = wsAfter / (1024 * 1024);
    long deltaMB = (wsAfter - wsBefore) / (1024 * 1024);

    Console.WriteLine(
        $"Volume:  {normalizedRoot}\n" +
        $"  Arquivos:   {fileCount,12:N0}\n" +
        $"  Pastas:     {dirCount,12:N0}\n" +
        $"  Total:      {fileCount + dirCount,12:N0}\n" +
        $"  Tempo:      {sw.Elapsed.TotalSeconds,8:F2} s\n" +
        $"  RAM pico:   {ramMB,8:N0} MB (delta: +{deltaMB} MB)\n");
}

return 0;

// ─────────────────────────────────────────────────────────────────────────────

internal static class Scanner
{
    // FILE_ATTRIBUTE_*
    private const uint FA_DIRECTORY     = 0x10;
    private const uint FA_REPARSE_POINT = 0x400;

    // FindFirstFileEx flags/enums
    private const int FindExInfoBasic     = 1;
    private const int FindExSearchNameMatch = 0;
    private const uint FIND_FIRST_EX_LARGE_FETCH = 2; // batch enumeration hint
    private const uint FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY = 4; // skip cloud/virtual items

    private static readonly IntPtr INVALID_HANDLE = new IntPtr(-1);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WIN32_FIND_DATA
    {
        public uint     dwFileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
        public uint     nFileSizeHigh;
        public uint     nFileSizeLow;
        public uint     dwReserved0;
        public uint     dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindFirstFileExW(
        string lpFileName,
        int    fInfoLevelId,
        out WIN32_FIND_DATA lpFindFileData,
        int    fSearchOp,
        IntPtr lpSearchFilter,
        uint   dwAdditionalFlags);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool FindNextFileW(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FindClose(IntPtr hFindFile);

    public static void ScanDirectory(string path, ref long fileCount, ref long dirCount)
    {
        var stack = new Stack<string>();
        stack.Push(path.TrimEnd('\\'));

        while (stack.Count > 0)
        {
            string dir = stack.Pop();
            string pattern = dir + "\\*";

            IntPtr h = FindFirstFileExW(
                pattern,
                FindExInfoBasic,
                out WIN32_FIND_DATA fd,
                FindExSearchNameMatch,
                IntPtr.Zero,
                FIND_FIRST_EX_LARGE_FETCH | FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY);

            if (h == INVALID_HANDLE)
                continue; // acesso negado ou erro — pula

            try
            {
                do
                {
                    string name = fd.cFileName;
                    if (name == "." || name == "..") continue;

                    bool isDir     = (fd.dwFileAttributes & FA_DIRECTORY)     != 0;
                    bool isReparse = (fd.dwFileAttributes & FA_REPARSE_POINT) != 0;

                    if (isDir)
                    {
                        dirCount++;
                        // Não desce em ponto de reanálise (link, junção, mount point)
                        if (!isReparse)
                            stack.Push(dir + "\\" + name);
                    }
                    else
                    {
                        fileCount++;
                    }
                }
                while (FindNextFileW(h, out fd));
            }
            finally
            {
                FindClose(h);
            }
        }
    }
}
