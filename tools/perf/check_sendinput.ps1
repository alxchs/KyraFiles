. "$PSScriptRoot\SmokeTest-Helper.ps1"

$script = @"
`$code = @'
using System;
using System.Runtime.InteropServices;
public static class RawCheck2 {
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct INPUT {
        [FieldOffset(0)] public uint type;
        [FieldOffset(8)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public static string TestKey(ushort key) {
        INPUT[] inp = new INPUT[2];
        inp[0].type = 1; inp[0].ki.wVk = key;
        inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
        uint res = SendInput(2, inp, Marshal.SizeOf(typeof(INPUT)));
        int err = Marshal.GetLastWin32Error();
        return string.Format("Res={0}, Err={1}, SizeOfInput={2}", res, err, Marshal.SizeOf(typeof(INPUT)));
    }
}
'@
Add-Type -TypeDefinition `$code
[RawCheck2]::TestKey(0x09)
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
