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
public static class UndoTester2 {
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);

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
    public static void Focus(IntPtr hwnd) {
        IntPtr curFore = GetForegroundWindow();
        if (curFore == hwnd) return;
        uint curPid;
        uint foreThread = GetWindowThreadProcessId(curFore, out curPid);
        uint appThread = GetCurrentThreadId();

        AttachThreadInput(appThread, foreThread, true);
        BringWindowToTop(hwnd);
        ShowWindow(hwnd, 5);
        SetForegroundWindow(hwnd);
        AttachThreadInput(appThread, foreThread, false);
        System.Threading.Thread.Sleep(150);
    }
    public static void SendKey(ushort key, ushort m1 = 0) {
        if (m1 == 0) {
            INPUT[] inp = new INPUT[2];
            inp[0].type = 1; inp[0].ki.wVk = key;
            inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
            SendInput(2, inp, 40);
        } else {
            INPUT[] inp = new INPUT[4];
            inp[0].type = 1; inp[0].ki.wVk = m1;
            inp[1].type = 1; inp[1].ki.wVk = key;
            inp[2].type = 1; inp[2].ki.wVk = key; inp[2].ki.dwFlags = 2;
            inp[3].type = 1; inp[3].ki.wVk = m1; inp[3].ki.dwFlags = 2;
            SendInput(4, inp, 40);
        }
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

[UndoTester2]::Focus(`$hwnd)
# Send Ctrl+Z
[UndoTester2]::SendKey(0x5A, 0x11)
Start-Sleep -Milliseconds 1500
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture undone state
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\14_rename_undone.png' | Out-Null
$origPath = "C:\FilesUXLab\activate_test.txt"
Write-Output ("Disk check after Undo: orig exists = " + (Test-Path $origPath))
