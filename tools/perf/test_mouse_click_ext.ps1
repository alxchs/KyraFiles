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
public static class MouseTester {
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

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

    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(80);
        mouse_event(0x02, 0, 0, 0, 0); // Down
        System.Threading.Thread.Sleep(80);
        mouse_event(0x04, 0, 0, 0, 0); // Up
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

# 1. Select arquivo.txt
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

# 2. Invoke Rename
`$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'InnerNavigationToolbarRenameButton')
`$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
`$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
`$inv.Invoke()
Start-Sleep -Milliseconds 500

# 3. Find TextBox and its bounding rect
`$condEdit = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ItemNameTextBox')
`$tb = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condEdit)
`$r = `$tb.Current.BoundingRectangle
Write-Output ("TextBox rect: Left={0}, Top={1}, Width={2}, Height={3}" -f `$r.Left, `$r.Top, `$r.Width, `$r.Height)

# Click on .txt: x = Left + 85, y = Top + Height/2
`$targetX = [int](`$r.Left + 85)
`$targetY = [int](`$r.Top + (`$r.Height / 2))
Write-Output ("Clicking at: {0}, {1}" -f `$targetX, `$targetY)
[MouseTester]::Click(`$targetX, `$targetY)
Start-Sleep -Milliseconds 500
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\09_arquivo_txt_click_extension.png'

# Send Escape to cancel
$scriptEsc = @"
`$hwnd = [IntPtr]$hwndVal
[MouseTester]::SendKey(0x1B)
"@
Invoke-OnDefault -PowerShellScript $scriptEsc | Out-Null
