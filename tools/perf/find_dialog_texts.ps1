. "$PSScriptRoot\SmokeTest-Helper.ps1"

$script = @'
Add-Type -AssemblyName UIAutomationClient
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
$texts = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
foreach ($t in $texts) {
    $r = $t.Current.BoundingRectangle
    if ($r.Left -gt 1200 -and $r.Left -lt 1800 -and $r.Top -gt 700 -and $r.Top -lt 1500) {
        Write-Output ("Text: Name='{0}', Rect={1}" -f $t.Current.Name, $r)
    }
}
'@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
