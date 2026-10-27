// F036 spike T8: pausa e retomada no meio do arquivo. NÃO é código de produto. Fora do app, sem entrada global.
// Comandos: gen | hash | chunk | resume | filecopy | cf2 | meta | stamp
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

static partial class P
{
    const int Mi = 1024 * 1024;

    static int Main(string[] a)
    {
        try
        {
            switch (a[0])
            {
                case "gen": Gen(a[1], long.Parse(a[2])); return 0;
                case "hash": Console.WriteLine(Hash(a[1])); return 0;
                case "chunk": return Chunk(a);
                case "resume": return Resume(a);
                case "filecopy": return FileCopy(a);
                case "cf2": return Cf2(a);
                case "meta": Meta(a[1]); return 0;
                case "cf2b": return Cf2b(a);
                case "mksparse": MkSparse(a[1], long.Parse(a[2])); return 0;
                case "size": Size(a[1]); return 0;
                case "fsinfo": Fsinfo(a[1]); return 0;
                case "stamp": Stamp(a[1]); return 0;
            }
        }
        catch (Exception e) { Console.Error.WriteLine("ERRO: " + e); return 2; }
        return 1;
    }

    static string Arg(string[] a, string name, string def = "")
    { int i = Array.IndexOf(a, name); return i >= 0 && i + 1 < a.Length ? a[i + 1] : def; }

    static void Gen(string path, long mib)
    {
        var buf = new byte[4 * Mi];
        ulong s = 0x9E3779B97F4A7C15UL;
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, Mi);
        long left = mib * Mi;
        while (left > 0)
        {
            for (int i = 0; i < buf.Length; i += 8) { s ^= s << 13; s ^= s >> 7; s ^= s << 17; BitConverter.TryWriteBytes(buf.AsSpan(i), s); }
            int n = (int)Math.Min(left, buf.Length);
            fs.Write(buf, 0, n); left -= n;
        }
    }

    static string Hash(string path)
    {
        using var sha = SHA256.Create();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4 * Mi, FileOptions.SequentialScan);
        return Convert.ToHexString(sha.ComputeHash(fs));
    }

    static string Ws() => (Process.GetCurrentProcess().PeakWorkingSet64 / (double)Mi).ToString("F0");

    // cópia por blocos com pausa pedida por outra thread
    static int Chunk(string[] a)
    {
        string src = a[1], dst = a[2];
        int chunkMi = int.Parse(Arg(a, "--chunk", "4"));
        int pauseAtMs = int.Parse(Arg(a, "--pause-at-ms", "-1"));
        int pauseForMs = int.Parse(Arg(a, "--pause-for-ms", "0"));
        int killAtMs = int.Parse(Arg(a, "--kill-at-ms", "-1"));
        int fixedBlock = chunkMi * Mi;
        var buf = new byte[fixedBlock];
        long pauseRequestedTicks = 0; int pauseRequested = 0;
        double ackMs = -1; double wsPausedMb = -1;
        var sw = Stopwatch.StartNew();
        if (pauseAtMs >= 0)
            new Thread(() => { Thread.Sleep(pauseAtMs); pauseRequestedTicks = Stopwatch.GetTimestamp(); Volatile.Write(ref pauseRequested, 1); }) { IsBackground = true }.Start();
        if (killAtMs >= 0)
            new Thread(() => { Thread.Sleep(killAtMs); Environment.FailFast("kill de teste"); }) { IsBackground = true }.Start();

        using var inp = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, Mi, FileOptions.SequentialScan);
        using var outp = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None, Mi);
        long total = inp.Length, done = 0;
        while (done < total)
        {
            if (Volatile.Read(ref pauseRequested) == 1)
            {
                // reconhece a pausa no limite do bloco; para de emitir E/S
                ackMs = (Stopwatch.GetTimestamp() - pauseRequestedTicks) * 1000.0 / Stopwatch.Frequency;
                outp.Flush();
                Thread.Sleep(pauseForMs);
                wsPausedMb = Process.GetCurrentProcess().WorkingSet64 / (double)Mi;
                Volatile.Write(ref pauseRequested, 0);
            }
            int n = inp.Read(buf, 0, (int)Math.Min(buf.Length, total - done));
            if (n <= 0) break;
            outp.Write(buf, 0, n); done += n;
        }
        outp.Flush(true);
        sw.Stop();
        Console.WriteLine($"chunk,{chunkMi}MiB,{done},{sw.Elapsed.TotalMilliseconds - (pauseAtMs >= 0 ? pauseForMs : 0):F0},{(done / (double)Mi) / ((sw.Elapsed.TotalMilliseconds - (pauseAtMs >= 0 ? pauseForMs : 0)) / 1000.0):F0},ack_ms={ackMs:F1},ws_pause_mb={wsPausedMb:F0},peak_ws_mb={Ws()}");
        return 0;
    }

    // retoma uma cópia interrompida (processo morto) a partir do tamanho do destino, arredondado ao bloco
    static int Resume(string[] a)
    {
        string src = a[1], dst = a[2];
        int chunkMi = int.Parse(Arg(a, "--chunk", "4"));
        long block = (long)chunkMi * Mi;
        var buf = new byte[block];
        var sw = Stopwatch.StartNew();
        long start = new FileInfo(dst).Length / block * block;   // descarta o bloco possivelmente rasgado
        using var inp = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, Mi, FileOptions.SequentialScan);
        using var outp = new FileStream(dst, FileMode.Open, FileAccess.Write, FileShare.None, Mi);
        outp.SetLength(start); inp.Seek(start, SeekOrigin.Begin); outp.Seek(start, SeekOrigin.Begin);
        long total = inp.Length, done = start;
        while (done < total) { int n = inp.Read(buf, 0, (int)Math.Min(buf.Length, total - done)); if (n <= 0) break; outp.Write(buf, 0, n); done += n; }
        outp.Flush(true); sw.Stop();
        Console.WriteLine($"resume,{chunkMi}MiB,retomou_em={start / Mi}MiB,total={done},ms={sw.Elapsed.TotalMilliseconds:F0}");
        return 0;
    }

    static int FileCopy(string[] a)
    {
        var sw = Stopwatch.StartNew();
        File.Copy(a[1], a[2], true);
        sw.Stop();
        long len = new FileInfo(a[1]).Length;
        Console.WriteLine($"filecopy,-,{len},{sw.Elapsed.TotalMilliseconds:F0},{(len / (double)Mi) / (sw.Elapsed.TotalMilliseconds / 1000.0):F0},peak_ws_mb={Ws()}");
        return 0;
    }

    // ---- CopyFile2 com pausa pelo callback de progresso ----
    [StructLayout(LayoutKind.Sequential)]
    struct COPYFILE2_EXTENDED_PARAMETERS { public uint dwSize; public uint dwCopyFlags; public IntPtr pfCancel; public IntPtr pProgressRoutine; public IntPtr pvCallbackContext; }
    delegate uint ProgressRoutine(IntPtr message, IntPtr context);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    static extern int CopyFile2(string src, string dst, ref COPYFILE2_EXTENDED_PARAMETERS p);

    static long cf2PauseAfterBytes = -1; static Stopwatch cf2Sw = new(); static double cf2AckMs = -1; static long cf2Paused;
    static long cf2LastTotal; static long cf2ReqTicks;

    static uint Cb(IntPtr msg, IntPtr ctx)
    {
        int type = Marshal.ReadInt32(msg, 0);          // 1=CHUNK_STARTED 2=CHUNK_FINISHED
        if (type == 2)
        {
            cf2LastTotal = Marshal.ReadInt64(msg, 72); // uliTotalBytesTransferred (x64)
            if (cf2PauseAfterBytes >= 0 && cf2LastTotal >= cf2PauseAfterBytes && cf2Paused == 0)
            {
                cf2Paused = 1; cf2ReqTicks = Stopwatch.GetTimestamp();
                return 4; // COPYFILE2_PROGRESS_PAUSE
            }
        }
        return 0;
    }

    static int Cf2(string[] a)
    {
        string src = a[1], dst = a[2];
        long pauseAfterMiB = long.Parse(Arg(a, "--pause-after-mib", "-1"));
        int pauseForMs = int.Parse(Arg(a, "--pause-for-ms", "0"));
        cf2PauseAfterBytes = pauseAfterMiB >= 0 ? pauseAfterMiB * Mi : -1;
        var cb = new ProgressRoutine(Cb);
        var ptr = Marshal.GetFunctionPointerForDelegate(cb);
        var p = new COPYFILE2_EXTENDED_PARAMETERS { dwSize = (uint)Marshal.SizeOf<COPYFILE2_EXTENDED_PARAMETERS>(), dwCopyFlags = 0, pProgressRoutine = ptr };
        var sw = Stopwatch.StartNew();
        int hr = CopyFile2(src, dst, ref p);
        double firstMs = sw.Elapsed.TotalMilliseconds;
        string first = $"hr=0x{hr:X8}";
        double ws = Process.GetCurrentProcess().WorkingSet64 / (double)Mi;
        if (cf2Paused == 1 && hr != 0)
        {
            cf2AckMs = (Stopwatch.GetTimestamp() - cf2ReqTicks) * 1000.0 / Stopwatch.Frequency;
            long dstLen = File.Exists(dst) ? new FileInfo(dst).Length : -1;
            Thread.Sleep(pauseForMs);
            cf2PauseAfterBytes = -1;
            var p2 = new COPYFILE2_EXTENDED_PARAMETERS { dwSize = p.dwSize, dwCopyFlags = 0x00004000 /* COPY_FILE_RESUME_FROM_PAUSE */, pProgressRoutine = ptr };
            var sw2 = Stopwatch.StartNew();
            int hr2 = CopyFile2(src, dst, ref p2);
            Console.WriteLine($"cf2,pause,{first},dst_na_pausa_mib={dstLen / Mi},pausa_ack_ms={cf2AckMs:F1},ws_pausa_mb={ws:F0},retomada_hr=0x{hr2:X8},retomada_ms={sw2.Elapsed.TotalMilliseconds:F0},total_antes_pausa_ms={firstMs:F0},peak_ws_mb={Ws()}");
            return 0;
        }
        long len = new FileInfo(src).Length;
        Console.WriteLine($"cf2,direto,{first},{len},ms={firstMs:F0},mbps={(len / (double)Mi) / (firstMs / 1000.0):F0},peak_ws_mb={Ws()}");
        return 0;
    }

    // ---- metadados ----
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr FindFirstStreamW(string file, int infoLevel, out WIN32_FIND_STREAM_DATA d, int flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool FindNextStreamW(IntPtr h, out WIN32_FIND_STREAM_DATA d);
    [DllImport("kernel32.dll")] static extern bool FindClose(IntPtr h);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct WIN32_FIND_STREAM_DATA { public long StreamSize; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)] public string StreamName; }

    static List<string> Streams(string path)
    {
        var l = new List<string>();
        var h = FindFirstStreamW(path, 0, out var d, 0);
        if (h == new IntPtr(-1)) return l;
        do { l.Add($"{d.StreamName}({d.StreamSize})"); } while (FindNextStreamW(h, out d));
        FindClose(h); return l;
    }

    static void Stamp(string path)
    {
        File.SetCreationTimeUtc(path, new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(path, new DateTime(2021, 6, 7, 8, 9, 10, DateTimeKind.Utc));
        File.SetAttributes(path, FileAttributes.Archive | FileAttributes.Hidden);
        File.WriteAllText(path + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\n");
        var fi = new FileInfo(path);
        var sec = fi.GetAccessControl();
        sec.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Read, AccessControlType.Allow));
        sec.SetAccessRuleProtection(true, true);   // protegida: não herda do pai, para o ACL ser comparável
        fi.SetAccessControl(sec);
    }

    static void Meta(string path)
    {
        var fi = new FileInfo(path);
        string sddl = fi.GetAccessControl().GetSecurityDescriptorSddlForm(AccessControlSections.Access);
        Console.WriteLine($"meta,{Path.GetFileName(path)},attr={fi.Attributes},criado={fi.CreationTimeUtc:u},gravado={fi.LastWriteTimeUtc:u},streams={string.Join('|', Streams(path))},aclprotegido={fi.GetAccessControl().AreAccessRulesProtected},acl_len={sddl.Length}");
    }
}


