. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

# Search for Header elements or DataGridHeader
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Header)
`$headers = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$cond)
Write-Output ("Found {0} Header elements" -f `$headers.Count)
foreach (`$h in `$headers) {
    Write-Output ("Header: Name='{0}', AutoId='{1}', Rect='{2}'" -f `$h.Current.Name, `$h.Current.AutomationId, `$h.Current.BoundingRectangle)
}

# Also search for elements named 'Name' or 'Nome'
`$condName = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'NameHeader')
`$nameHeader = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condName)
if (`$nameHeader) {
    Write-Output ("Found NameHeader: Rect={0}" -f `$nameHeader.Current.BoundingRectangle)
} else {
    Write-Output "NameHeader not found by AutomationId"
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
