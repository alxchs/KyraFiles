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
public static class LayoutTester {
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
    public static void SendKey(ushort key, ushort m1 = 0, ushort m2 = 0) {
        if (m1 == 0 && m2 == 0) {
            INPUT[] inp = new INPUT[2];
            inp[0].type = 1; inp[0].ki.wVk = key;
            inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
            SendInput(2, inp, 40);
        } else if (m2 == 0) {
            INPUT[] inp = new INPUT[4];
            inp[0].type = 1; inp[0].ki.wVk = m1;
            inp[1].type = 1; inp[1].ki.wVk = key;
            inp[2].type = 1; inp[2].ki.wVk = key; inp[2].ki.dwFlags = 2;
            inp[3].type = 1; inp[3].ki.wVk = m1; inp[3].ki.dwFlags = 2;
            SendInput(4, inp, 40);
        } else {
            INPUT[] inp = new INPUT[6];
            inp[0].type = 1; inp[0].ki.wVk = m1;
            inp[1].type = 1; inp[1].ki.wVk = m2;
            inp[2].type = 1; inp[2].ki.wVk = key;
            inp[3].type = 1; inp[3].ki.wVk = key; inp[3].ki.dwFlags = 2;
            inp[4].type = 1; inp[4].ki.wVk = m2; inp[4].ki.dwFlags = 2;
            inp[5].type = 1; inp[5].ki.wVk = m1; inp[5].ki.dwFlags = 2;
            SendInput(6, inp, 40);
        }
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

[LayoutTester]::Focus(`$hwnd)

function Start-Rename-Item(`$name) {
    `$condItem = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, `$name)
    `$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condItem)
    if (-not `$el) { return `$false }
    `$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    `$cur = `$el
    while (`$cur -and `$cur.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) {
        `$cur = `$walker.GetParent(`$cur)
    }
    if (`$cur) {
        `$sel = `$cur.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
        if (`$sel) { `$sel.Select() }
        `$cur.SetFocus()
        Start-Sleep -Milliseconds 300
    }

    `$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'InnerNavigationToolbarRenameButton')
    `$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
    `$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    `$inv.Invoke()
    Start-Sleep -Milliseconds 500
    return `$true
}
"@

# ==================== 1. Switch to Grid layout (Ctrl+Shift+4) ====================
# VK_4 = 0x34, VK_CONTROL = 0x11, VK_SHIFT = 0x10
$scriptGridSwitch = $script + @"
[LayoutTester]::SendKey(0x34, 0x11, 0x10)
Start-Sleep -Milliseconds 1000
"@
Invoke-OnDefault -PowerShellScript $scriptGridSwitch | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\15_layout_grid_view.png' | Out-Null

# Rename arquivo.txt in Grid layout
$scriptGridRename = $script + @"
Start-Rename-Item 'arquivo.txt'
"@
Invoke-OnDefault -PowerShellScript $scriptGridRename | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\16_layout_grid_rename_initial.png' | Out-Null

# Send Tab in Grid layout
$scriptGridTab = $script + @"
[LayoutTester]::SendKey(0x09)
Start-Sleep -Milliseconds 400
"@
Invoke-OnDefault -PowerShellScript $scriptGridTab | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\17_layout_grid_rename_tab.png' | Out-Null

# Escape in Grid layout
Invoke-OnDefault -PowerShellScript ($script + "[LayoutTester]::SendKey(0x1B); Start-Sleep -Milliseconds 500") | Out-Null

# ==================== 2. Switch to Column layout (Ctrl+Shift+5) ====================
# VK_5 = 0x35
$scriptColSwitch = $script + @"
[LayoutTester]::SendKey(0x35, 0x11, 0x10)
Start-Sleep -Milliseconds 1000
"@
Invoke-OnDefault -PowerShellScript $scriptColSwitch | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\18_layout_column_view.png' | Out-Null

# Rename arquivo.txt in Column layout
$scriptColRename = $script + @"
Start-Rename-Item 'arquivo.txt'
"@
Invoke-OnDefault -PowerShellScript $scriptColRename | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\19_layout_column_rename_initial.png' | Out-Null

# Send Tab in Column layout
$scriptColTab = $script + @"
[LayoutTester]::SendKey(0x09)
Start-Sleep -Milliseconds 400
"@
Invoke-OnDefault -PowerShellScript $scriptColTab | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\20_layout_column_rename_tab.png' | Out-Null

# Escape in Column layout
Invoke-OnDefault -PowerShellScript ($script + "[LayoutTester]::SendKey(0x1B); Start-Sleep -Milliseconds 500") | Out-Null

# ==================== 3. Restore Details layout (Ctrl+Shift+1) ====================
# VK_1 = 0x31
$scriptDetSwitch = $script + @"
[LayoutTester]::SendKey(0x31, 0x11, 0x10)
Start-Sleep -Milliseconds 1000
"@
Invoke-OnDefault -PowerShellScript $scriptDetSwitch | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\21_layout_details_restored.png' | Out-Null

Write-Output "Layout validations complete!"
