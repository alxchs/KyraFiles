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
public static class MouseHelper {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    public static void RightClick(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        mouse_event(0x08, 0, 0, 0, 0); // RIGHTDOWN
        System.Threading.Thread.Sleep(100);
        mouse_event(0x10, 0, 0, 0, 0); // RIGHTUP
    }

    public static void LeftClick(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        mouse_event(0x02, 0, 0, 0, 0); // LEFTDOWN
        System.Threading.Thread.Sleep(100);
        mouse_event(0x04, 0, 0, 0, 0); // LEFTUP
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

# Find 'Name' header
`$condText = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Name')
`$nameEl = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condText)
if (!`$nameEl) {
    Write-Output "Name element not found"
    exit 1
}

`$r = `$nameEl.Current.BoundingRectangle
`$clickX = [int](`$r.Left + 50)
`$clickY = [int](`$r.Top + 10)
Write-Output ("Right clicking on Name header at {0}, {1}" -f `$clickX, `$clickY)

[MouseHelper]::RightClick(`$clickX, `$clickY)
Start-Sleep -Seconds 1
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture context menu
Capture-FilesWindow -OutFile 'docs\releases\evidence\f010_context_menu.png'
