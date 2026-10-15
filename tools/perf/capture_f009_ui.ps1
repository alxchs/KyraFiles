# tools/perf/capture_f009_ui.ps1
. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) {
    Write-Error "Files not running"
    exit 1
}

$hwnd = [VC]::FindMainWindow($p.Id)
$val = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$val
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

`$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'SettingsButton')
`$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
if (`$btn) {
    `$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    if (`$inv) { `$inv.Invoke() }
}
Start-Sleep -Seconds 2

`$condApp = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Aparência')
`$item = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condApp)
if (!`$item) {
    `$condApp = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Appearance')
    `$item = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condApp)
}
if (`$item) {
    `$item.SetFocus()
    `$inv = `$item.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    if (`$inv) { `$inv.Invoke() }
    `$sel = `$item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
    if (`$sel) { `$sel.Select() }
}
Start-Sleep -Seconds 1
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Host "Result: $res"
Send-KeyToFiles -Key 0x0D -WaitAfterMs 1500
Send-KeyToFiles -Key 0x20 -WaitAfterMs 1500
Capture-FilesWindow -OutFile (Join-Path $PSScriptRoot "..\..\docs\agents\evidence\f009\f009_settings_fonts.png")
