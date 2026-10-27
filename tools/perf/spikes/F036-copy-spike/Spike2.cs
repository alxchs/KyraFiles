// F036 spike 2: paridade do CopyFile2 (pausa dupla, cancelar na pausa, esparso, compactado). NÃO é código de produto.
using System.Diagnostics;
using System.Runtime.InteropServices;

static partial class P
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern uint GetCompressedFileSizeW(string f, out uint hi);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(IntPtr h, uint code, IntPtr inb, uint inl, IntPtr outb, uint outl, out uint ret, IntPtr ov);

    static long PhysSize(string p) { uint lo = GetCompressedFileSizeW(p, out uint hi); return ((long)hi << 32) | lo; }

    static void Size(string p)
        => Console.WriteLine($"size,{Path.GetFileName(p)},logico={new FileInfo(p).Length},fisico={PhysSize(p)},attr={File.GetAttributes(p)}");

    static void MkSparse(string path, long mib)
    {
        var rnd = new byte[Mi];
        Random.Shared.NextBytes(rnd);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        DeviceIoControl(fs.SafeFileHandle.DangerousGetHandle(), 0x900C4 /* FSCTL_SET_SPARSE */, IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
        fs.Write(rnd, 0, rnd.Length);
        fs.SetLength(mib * Mi);
        fs.Seek(-Mi, SeekOrigin.End);
        fs.Write(rnd, 0, rnd.Length);
    }

    static int Cf2b(string[] a)
    {
        string src = a[1], dst = a[2];
        long p1 = long.Parse(Arg(a, "--pause-after-mib", "-1")), p2 = long.Parse(Arg(a, "--pause2-after-mib", "-1"));
        int forMs = int.Parse(Arg(a, "--pause-for-ms", "1000"));
        bool cancel = a.Contains("--cancel-after-pause");
        uint baseFlags = Convert.ToUInt32(Arg(a, "--flags", "0"), 16);
        var cb = new ProgressRoutine(Cb);
        var ptr = Marshal.GetFunctionPointerForDelegate(cb);
        var ev = new List<string>();
        var sw = Stopwatch.StartNew();
        long next = p1; uint flags = baseFlags; int pauses = 0;
        while (true)
        {
            cf2Paused = 0; cf2LastTotal = 0; cf2PauseAfterBytes = next >= 0 ? next * Mi : -1;
            var p = new COPYFILE2_EXTENDED_PARAMETERS { dwSize = (uint)Marshal.SizeOf<COPYFILE2_EXTENDED_PARAMETERS>(), dwCopyFlags = flags, pProgressRoutine = ptr };
            var t = Stopwatch.StartNew();
            int hr = CopyFile2(src, dst, ref p);
            if (cf2Paused == 1 && hr == unchecked((int)0x80070BEA))
            {
                pauses++;
                long dl = File.Exists(dst) ? new FileInfo(dst).Length : -1;
                ev.Add($"pausa{pauses}: apos {t.Elapsed.TotalMilliseconds:F0}ms dst={dl / Mi}MiB total_do_callback={cf2LastTotal / Mi}MiB");
                if (cancel)
                {
                    try { File.Delete(dst); ev.Add("cancelar(apagar parcial)=ok"); }
                    catch (Exception e) { ev.Add("cancelar=ERRO " + e.GetType().Name); }
                    cf2PauseAfterBytes = -1; cf2Paused = 0;
                    var p3 = new COPYFILE2_EXTENDED_PARAMETERS { dwSize = p.dwSize, dwCopyFlags = baseFlags, pProgressRoutine = ptr };
                    int hr3 = CopyFile2(src, dst, ref p3);
                    ev.Add($"recopia_limpa_hr=0x{hr3:X8}");
                    break;
                }
                Thread.Sleep(forMs);
                flags = baseFlags | 0x00004000; // COPY_FILE_RESUME_FROM_PAUSE
                next = pauses == 1 ? p2 : -1;
                continue;
            }
            ev.Add($"fim hr=0x{hr:X8} em {t.Elapsed.TotalMilliseconds:F0}ms");
            break;
        }
        Console.WriteLine("cf2b," + string.Join(" | ", ev) + $",total_ms={sw.Elapsed.TotalMilliseconds - (pauses * forMs):F0},peak_ws_mb={Ws()}");
        return 0;
    }
}
