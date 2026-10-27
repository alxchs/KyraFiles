using System.Text;
using System.Runtime.InteropServices;
static partial class P
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetVolumeInformationW(string root, StringBuilder name, int nl, out uint serial, out uint maxComp, out uint flags, StringBuilder fs, int fl);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool GetDiskFreeSpaceExW(string dir, out ulong avail, out ulong total, out ulong free);
    static void Fsinfo(string p)
    {
        var fs = new StringBuilder(64); var vn = new StringBuilder(64);
        bool ok = GetVolumeInformationW(p, vn, 64, out _, out var maxc, out var fl, fs, 64);
        Console.WriteLine($"fsinfo,ok={ok},erro={Marshal.GetLastWin32Error()},fs={fs},maxcomp={maxc},flags=0x{fl:X},fluxos={(fl & 0x40000) != 0},esparso={(fl & 0x40) != 0},compressao={(fl & 0x10) != 0},acl={(fl & 0x8) != 0},criptografia={(fl & 0x20000) != 0}");
        if (GetDiskFreeSpaceExW(p, out var a, out var t, out _)) Console.WriteLine($"fsinfo,livre_gib={a / 1073741824.0:F1},total_gib={t / 1073741824.0:F1}");
    }
}
