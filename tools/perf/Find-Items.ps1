. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files -ErrorAction SilentlyContinue | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
`$tbs = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$cond)
foreach (`$t in `$tbs) {
    if (`$t.Current.Name -match 'arquivo|\.git|Folder|shortcut') {
        `$r = `$t.Current.BoundingRectangle
        Write-Output "Found: '`$(`$t.Current.Name)' at X=`$(`$r.X), Y=`$(`$r.Y), W=`$(`$r.Width), H=`$(`$r.Height)"
    }
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
