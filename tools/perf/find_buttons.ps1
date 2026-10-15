. "$PSScriptRoot\SmokeTest-Helper.ps1"

$script = @'
Add-Type -AssemblyName UIAutomationClient
$root = [System.Windows.Automation.AutomationElement]::RootElement
$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
$btns = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condBtn)
foreach ($b in $btns) {
    $r = $b.Current.BoundingRectangle
    if ($r.Left -gt 1200 -and $r.Left -lt 2200 -and $r.Top -gt 1200 -and $r.Top -lt 1650) {
        Write-Output ("Button: Name='{0}', AutoId='{1}', Rect={2}" -f $b.Current.Name, $b.Current.AutomationId, $r)
    }
}
'@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
