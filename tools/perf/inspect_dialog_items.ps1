. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$root = [System.Windows.Automation.AutomationElement]::RootElement

# Find CheckBoxes
`$condCb = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::CheckBox)
`$cbs = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$condCb)
Write-Output ("Total CheckBoxes on desktop: {0}" -f `$cbs.Count)

foreach (`$cb in `$cbs) {
    Write-Output ("CheckBox: Name='{0}', AutoId='{1}', Rect={2}" -f `$cb.Current.Name, `$cb.Current.AutomationId, `$cb.Current.BoundingRectangle)
}

# Find Dimensões Text element
`$condDim = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Dimensões')
`$dim = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condDim)
if (`$dim) {
    Write-Output ("Found Dimensões Text: Rect={0}" -f `$dim.Current.BoundingRectangle)
}

# Find Buttons in KyraFiles
`$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
`$condPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, [int]`$p.Id)
`$cond = New-Object System.Windows.Automation.AndCondition(@(`$condBtn, `$condPid))
`$btns = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$cond)
foreach (`$b in `$btns) {
    Write-Output ("Files Button: Name='{0}', AutoId='{1}', Rect={2}" -f `$b.Current.Name, `$b.Current.AutomationId, `$b.Current.BoundingRectangle)
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
