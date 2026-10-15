. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

`$code = @'
using System;
using System.Runtime.InteropServices;
public static class FocusHelper {
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public MOUSEKEYBDHARDWAREINPUT mkhi; }
    [StructLayout(LayoutKind.Explicit)] public struct MOUSEKEYBDHARDWAREINPUT { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [DllImport("user32.dll")] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static bool EnsureFocus(IntPtr hwnd) {
        IntPtr curFore = GetForegroundWindow();
        if (curFore == hwnd) return true;
        uint curPid;
        uint foreThread = GetWindowThreadProcessId(curFore, out curPid);
        uint appThread = GetCurrentThreadId();

        AttachThreadInput(appThread, foreThread, true);
        BringWindowToTop(hwnd);
        ShowWindow(hwnd, 5);
        bool ok = SetForegroundWindow(hwnd);
        AttachThreadInput(appThread, foreThread, false);
        System.Threading.Thread.Sleep(200);
        return ok;
    }

    public static void SendKey(ushort key, ushort m1 = 0) {
        if (m1 == 0) {
            INPUT[] inp = new INPUT[2];
            ushort s = (ushort)MapVirtualKey(key, 0);
            inp[0].type = 1; inp[0].mkhi.ki.wVk = key; inp[0].mkhi.ki.wScan = s;
            inp[1].type = 1; inp[1].mkhi.ki.wVk = key; inp[1].mkhi.ki.wScan = s; inp[1].mkhi.ki.dwFlags = 2;
            SendInput(2, inp, Marshal.SizeOf(typeof(INPUT)));
        } else {
            INPUT[] inp = new INPUT[4];
            ushort smod = (ushort)MapVirtualKey(m1, 0);
            ushort skey = (ushort)MapVirtualKey(key, 0);
            inp[0].type = 1; inp[0].mkhi.ki.wVk = m1; inp[0].mkhi.ki.wScan = smod;
            inp[1].type = 1; inp[1].mkhi.ki.wVk = key; inp[1].mkhi.ki.wScan = skey;
            inp[2].type = 1; inp[2].mkhi.ki.wVk = key; inp[2].mkhi.ki.wScan = skey; inp[2].mkhi.ki.dwFlags = 2;
            inp[3].type = 1; inp[3].mkhi.ki.wVk = m1; inp[3].mkhi.ki.wScan = smod; inp[3].mkhi.ki.dwFlags = 2;
            SendInput(4, inp, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
'@
Add-Type -TypeDefinition `$code

`$foreBefore = [FocusHelper]::GetForegroundWindow()
`$focusOk = [FocusHelper]::EnsureFocus(`$hwnd)
`$foreAfter = [FocusHelper]::GetForegroundWindow()
Write-Output ("Foreground before: {0}, after: {1}, target: {2}, focusOk: {3}" -f `$foreBefore, `$foreAfter, `$hwnd, `$focusOk)
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
