. "$PSScriptRoot\SmokeTest-Helper.ps1"
$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$val = $hwnd.ToInt64()
$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$val
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'No')
`$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$btn) {
    `$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    if (`$inv) { `$inv.Invoke(); Write-Output 'Clicked No button' }
} else {
    Write-Output 'No button not found'
}
"@
$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
