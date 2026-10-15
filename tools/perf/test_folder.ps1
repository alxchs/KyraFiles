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
public static class Helper {
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

function Select-And-Rename(`$name) {
    `$condItem = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, `$name)
    `$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condItem)
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

    `$condEdit = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ItemNameTextBox')
    `$tb = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condEdit)
    if (`$tb) {
        `$valPat = `$tb.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
        `$textPat = `$tb.GetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern) -as [System.Windows.Automation.TextPattern]
        `$v = if (`$valPat) { `$valPat.Current.Value } else { "" }
        `$s = if (`$textPat -and `$textPat.GetSelection().Length -gt 0) { `$textPat.GetSelection()[0].GetText(-1) } else { "" }
        return "Val='$v' Sel='$s'"
    }
    return "Not found"
}

Write-Output ("Folder A: " + (Select-And-Rename 'Folder A'))
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\11_folder_rename_selection.png'

# Send Escape to cancel
$scriptEsc = @"
[Helper]::SendKey(0x1B)
"@
Invoke-OnDefault -PowerShellScript $scriptEsc | Out-Null
