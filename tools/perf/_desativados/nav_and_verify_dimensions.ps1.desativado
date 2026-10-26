. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$env:FILES_HWND_VAL = $hwnd.ToInt64().ToString()

$script = @'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Windows.Forms

$code = "using System; using System.Runtime.InteropServices; public static class NavHelper { [DllImport(`"user32.dll`")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo); [DllImport(`"user32.dll`")] public static extern bool SetForegroundWindow(IntPtr hWnd); public static void PressCtrlL() { keybd_event(0x11, 0, 0, 0); keybd_event(0x4C, 0, 0, 0); System.Threading.Thread.Sleep(50); keybd_event(0x4C, 0, 2, 0); keybd_event(0x11, 0, 2, 0); } }"
Add-Type -TypeDefinition $code -ErrorAction SilentlyContinue

$hwndVal = [Int64]::Parse($env:FILES_HWND_VAL)
$hwnd = [IntPtr]$hwndVal
[NavHelper]::SetForegroundWindow($hwnd)
Start-Sleep -Milliseconds 200

# Focus address bar via Ctrl+L
[NavHelper]::PressCtrlL()
Start-Sleep -Milliseconds 500

# Send address
[System.Windows.Forms.SendKeys]::SendWait('c:\desenv\utils\FilesApp\docs\releases\evidence{ENTER}')
Start-Sleep -Seconds 3

Write-Output "Navigated to evidence directory"

$root = [System.Windows.Automation.AutomationElement]::RootElement
$condText = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
$texts = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condText)
foreach ($t in $texts) {
    $r = $t.Current.BoundingRectangle
    if ($r.Top -gt 600 -and $r.Top -lt 1000 -and $r.Left -gt 2700 -and $r.Left -lt 3200) {
        Write-Output ("Dimensions Column Cell: Name='{0}', Rect={1}" -f $t.Current.Name, $r)
    }
}
'@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture screen showing image files and their dimensions
$outPath = 'c:\desenv\utils\FilesApp\docs\releases\evidence\f010_dimensions_populated.png'
Capture-FilesWindow -OutFile $outPath
Write-Output "Captured evidence to $outPath"
