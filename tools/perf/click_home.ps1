# tools/perf/click_home.ps1
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

# Procura o item Home ou Início
`$condHome = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Home')
`$homeElement = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condHome)
if (!`$homeElement) {
    `$condHome = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Início')
    `$homeElement = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condHome)
}
if (`$homeElement) {
    `$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    `$cur = `$homeElement
    while (`$cur -and `$cur.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) {
        `$cur = `$walker.GetParent(`$cur)
    }
    if (`$cur) {
        `$sel = `$cur.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
        if (`$sel) { `$sel.Select() }
        else {
            `$inv = `$cur.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
            if (`$inv) { `$inv.Invoke() }
        }
    }
}
Start-Sleep -Seconds 2
"@

Invoke-OnDefault -PowerShellScript $script
Capture-FilesWindow -OutFile (Join-Path $PSScriptRoot "..\..\docs\agents\evidence\f009\f009_files_separate_font.png")
