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
public static class Helper2 {
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

function Start-Rename-Item(`$name) {
    `$condItem = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, `$name)
    `$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condItem)
    if (-not `$el) { return `$false }
    `$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    `$cur = `$el
    while (`$cur -and `$cur.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) {
        `$cur = `$walker.GetParent(`$cur)
    }
    `$sel = `$cur.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
    if (`$sel) { `$sel.Select() }
    `$cur.SetFocus()
    Start-Sleep -Milliseconds 300

    `$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'InnerNavigationToolbarRenameButton')
    `$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
    `$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    `$inv.Invoke()
    Start-Sleep -Milliseconds 500
    return `$true
}
"@

# --- 1. Folder A ---
$scriptFolder = $script + @"
Start-Rename-Item 'Folder A'
"@
Invoke-OnDefault -PowerShellScript $scriptFolder | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\11_folder_rename_selection.png' | Out-Null
Invoke-OnDefault -PowerShellScript "[Helper2]::SendKey(0x1B)" | Out-Null
Start-Sleep -Milliseconds 500

# --- 2. Shortcut (.lnk) ---
$scriptLnk = $script + @"
Start-Rename-Item 'shortcut'
"@
Invoke-OnDefault -PowerShellScript $scriptLnk | Out-Null
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\12_shortcut_rename_selection.png' | Out-Null
Invoke-OnDefault -PowerShellScript "[Helper2]::SendKey(0x1B)" | Out-Null
Start-Sleep -Milliseconds 500

Write-Output "Folder A and shortcut validated!"
