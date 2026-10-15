# tools/perf/click_new_tab.ps1
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

`$condAdd = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'AddButton')
`$addBtn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condAdd)
if (`$addBtn) {
    `$inv = `$addBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    if (`$inv) { `$inv.Invoke() }
}
Start-Sleep -Seconds 2
"@

Invoke-OnDefault -PowerShellScript $script
Capture-FilesWindow -OutFile (Join-Path $PSScriptRoot "..\..\docs\agents\evidence\f009\f009_files_separate_font.png")
