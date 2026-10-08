. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)

$script = @"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

`$code = @'
using System;
using System.Runtime.InteropServices;
public static class Clicker {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        mouse_event(0x02, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        mouse_event(0x04, 0, 0, 0, 0);
        System.Threading.Thread.Sleep(100);
    }

    public static void PressEnter() {
        keybd_event(0x0D, 0, 0, 0);
        System.Threading.Thread.Sleep(50);
        keybd_event(0x0D, 0, 2, 0);
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

`$root = [System.Windows.Automation.AutomationElement]::RootElement

# 1. Find Dimensões Text element and its neighboring CheckBox
`$condDim = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Dimensões')
`$dim = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condDim)
if (`$dim) {
    Write-Output ("Found Dimensões Text: Rect={0}" -f `$dim.Current.BoundingRectangle)
    # The CheckBox is to the left of the text, around X - 40
    `$r = `$dim.Current.BoundingRectangle
    `$clickX = [int](`$r.Left - 40)
    `$clickY = [int](`$r.Top + 12)
    Write-Output ("Clicking CheckBox at X={0}, Y={1}" -f `$clickX, `$clickY)
    [Clicker]::Click(`$clickX, `$clickY)
    Start-Sleep -Milliseconds 500
} else {
    Write-Output "Dimensões Text not found!"
}

# 2. Find PrimaryButton (OK)
`$condPrimary = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'PrimaryButton')
`$primaryBtn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condPrimary)
if (`$primaryBtn) {
    Write-Output ("Found PrimaryButton: Rect={0}" -f `$primaryBtn.Current.BoundingRectangle)
    `$inv = `$primaryBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    if (`$inv) {
        `$inv.Invoke()
    } else {
        `$r = `$primaryBtn.Current.BoundingRectangle
        [Clicker]::Click([int](`$r.Left + `$r.Width / 2), [int](`$r.Top + `$r.Height / 2))
    }
} else {
    Write-Output "PrimaryButton not found by ID, trying Enter key..."
    [Clicker]::PressEnter()
}

Start-Sleep -Seconds 3
Write-Output "Finished interaction"
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture screen
$scriptCapture = @"
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

`$bounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
`$bmp = New-Object System.Drawing.Bitmap `$bounds.Width, `$bounds.Height
`$g = [System.Drawing.Graphics]::FromImage(`$bmp)
`$g.CopyFromScreen(`$bounds.Location, [System.Drawing.Point]::Empty, `$bounds.Size)
`$g.Dispose()

`$outPath = 'c:\desenv\utils\FilesApp\docs\releases\evidence\f010_added_column.png'
`$bmp.Save(`$outPath, [System.Drawing.Imaging.ImageFormat]::Png)
`$bmp.Dispose()
Write-Output "Saved evidence to `$outPath"
"@

$capRes = Invoke-OnDefault -PowerShellScript $scriptCapture
Write-Output $capRes
