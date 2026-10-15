. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

# Find all Text elements
`$condText = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
`$texts = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$condText)
Write-Output ("Total Text elements: {0}" -f `$texts.Count)

foreach (`$t in `$texts) {
    `$name = `$t.Current.Name
    if (`$name -match '^(Name|Tag|Date modified|Date created|Type|Size|Status|Nome|Data|Tipo|Tamanho|Dimensões|Dimensions)$') {
        Write-Output ("Header Text: Name='{0}', Rect={1}" -f `$name, `$t.Current.BoundingRectangle)
    }
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
