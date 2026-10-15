. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
`$buttons = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$cond)
foreach (`$b in `$buttons) {
    if (`$b.Current.Name -match 'Layout|Details|Grid|Columns|View|Display|Detalhes|Grade|Colunas' -or `$b.Current.AutomationId -match 'Layout|View|Grid|Column|Details') {
        Write-Output ("Button: '{0}' AutomationId: '{1}'" -f `$b.Current.Name, `$b.Current.AutomationId)
    }
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
