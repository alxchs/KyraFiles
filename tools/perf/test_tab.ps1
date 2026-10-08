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
public static class WinHelper {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT {
        public uint type;
        public MOUSEKEYBDHARDWAREINPUT mkhi;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct MOUSEKEYBDHARDWAREINPUT {
        [FieldOffset(0)] public KEYBDINPUT ki;
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

[WinHelper]::SetForegroundWindow(`$hwnd)
Start-Sleep -Milliseconds 200

# Select arquivo.txt
`$condItem = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'arquivo.txt')
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

# Click Rename button
`$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'InnerNavigationToolbarRenameButton')
`$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
`$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
`$inv.Invoke()

Start-Sleep -Milliseconds 400

# Find TextBox
`$condEdit = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ItemNameTextBox')
`$tb = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condEdit)
`$textPat = `$tb.GetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern) -as [System.Windows.Automation.TextPattern]
`$valPat = `$tb.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]

Write-Output ("Initial: Val='{0}' Sel='{1}'" -f `$valPat.Current.Value, `$textPat.GetSelection()[0].GetText(-1))

# Ensure $tb has focus
`$tb.SetFocus()
Start-Sleep -Milliseconds 100

# Send Tab
[WinHelper]::SendKey(0x09) # VK_TAB
Start-Sleep -Milliseconds 400
Write-Output ("After Tab: Val='{0}' Sel='{1}'" -f `$valPat.Current.Value, `$textPat.GetSelection()[0].GetText(-1))

# Send Shift+Tab
# VK_TAB = 0x09, VK_SHIFT = 0x10
[WinHelper]::SendKey(0x09, 0x10)
Start-Sleep -Milliseconds 400
Write-Output ("After Shift+Tab: Val='{0}' Sel='{1}'" -f `$valPat.Current.Value, `$textPat.GetSelection()[0].GetText(-1))

# Cancel
[WinHelper]::SendKey(0x1B) # VK_ESCAPE
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
