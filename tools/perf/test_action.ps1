. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

# Find focused element
`$focused = [System.Windows.Automation.AutomationElement]::FocusedElement
if (`$focused) {
    Write-Output ("Focused: '{0}' Type: {1} AutoId: '{2}'" -f `$focused.Current.Name, `$focused.Current.ControlType.ProgrammaticName, `$focused.Current.AutomationId)
    `$textPattern = `$focused.GetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern) -as [System.Windows.Automation.TextPattern]
    if (`$textPattern) {
        Write-Output "Has TextPattern!"
        `$selRanges = `$textPattern.GetSelection()
        foreach (`$r in `$selRanges) {
            Write-Output ("Selected text: '{0}'" -f `$r.GetText(-1))
        }
    }
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
