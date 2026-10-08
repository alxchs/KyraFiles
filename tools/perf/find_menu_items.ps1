. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$root = [System.Windows.Automation.AutomationElement]::RootElement

`$condMenu = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::MenuItem)
`$items = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$condMenu)
Write-Output ("Total MenuItem elements on desktop: {0}" -f `$items.Count)

foreach (`$item in `$items) {
    Write-Output ("MenuItem: Name='{0}', AutoId='{1}', Rect={2}" -f `$item.Current.Name, `$item.Current.AutomationId, `$item.Current.BoundingRectangle)
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
