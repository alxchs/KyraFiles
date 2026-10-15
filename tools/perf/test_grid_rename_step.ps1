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
public static class GridTester2 {
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
    public static void SendKey(ushort key) {
        INPUT[] inp = new INPUT[2];
        inp[0].type = 1; inp[0].ki.wVk = key;
        inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
        SendInput(2, inp, 40);
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

[GridTester2]::Focus(`$hwnd)

# 1. Trigger rename
`$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'InnerNavigationToolbarRenameButton')
`$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
if (`$btn -and `$btn.Current.IsEnabled) {
    `$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    `$inv.Invoke()
} else {
    [GridTester2]::SendKey(0x71)
}
Start-Sleep -Milliseconds 600
"@

Invoke-OnDefault -PowerShellScript $script | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\16_layout_grid_rename_initial.png' | Out-Null
Write-Output "Initial rename captured"

$scriptTab = @"
`$hwnd = [IntPtr]$hwndVal
`$code = @'
using System;
using System.Runtime.InteropServices;
public static class GridTester3 {
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
    public static void SendKey(ushort key) {
        INPUT[] inp = new INPUT[2];
        inp[0].type = 1; inp[0].ki.wVk = key;
        inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
        SendInput(2, inp, 40);
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue
[GridTester3]::SendKey(0x09)
Start-Sleep -Milliseconds 500
"@

Invoke-OnDefault -PowerShellScript $scriptTab | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\17_layout_grid_rename_tab.png' | Out-Null
Write-Output "Tab rename captured"

$scriptEsc = @"
`$hwnd = [IntPtr]$hwndVal
`$code = @'
using System;
using System.Runtime.InteropServices;
public static class GridTester4 {
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
    public static void SendKey(ushort key) {
        INPUT[] inp = new INPUT[2];
        inp[0].type = 1; inp[0].ki.wVk = key;
        inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
        SendInput(2, inp, 40);
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue
[GridTester4]::SendKey(0x1B)
Start-Sleep -Milliseconds 400
"@

Invoke-OnDefault -PowerShellScript $scriptEsc | Out-Null
Write-Output "Escaped rename"
