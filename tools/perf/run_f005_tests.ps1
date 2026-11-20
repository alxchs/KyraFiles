# tools/perf/run_f005_tests.ps1
. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
if (-not $p) { Write-Error "Files not running"; exit 1 }

$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @'
Add-Type -AssemblyName UIAutomationClient
param([int64]$HwndVal)
$hwnd = [IntPtr]$HwndVal
$root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)

# 1. Focus window
[System.Windows.Automation.AutomationElement]::RootElement.SetFocus()
$root.SetFocus()

# 2. Select KyraFilesUXLab tab
$condTab = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'KyraFilesUXLab')
$tab = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condTab)
if ($tab) {
    $sel = $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
    if ($sel) { $sel.Select() }
    Write-Output "Tab KyraFilesUXLab selected"
}

# 3. Select sample.jpg
$condItem = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'sample.jpg')
$item = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condItem)
if ($item) {
    # Walk up to ListItem
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $cur = $item
    while ($cur -and $cur.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) {
        $cur = $walker.GetParent($cur)
    }
    if ($cur) {
        $selItem = $cur.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
        if ($selItem) { $selItem.Select() }
        $cur.SetFocus()
        Write-Output "sample.jpg selected and focused"
    }
}

# 4. Check PreviewPane button
$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'PreviewPane')
$btn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condBtn)
if ($btn) {
    Write-Output "Found PreviewPane button: Name='$($btn.Current.Name)'"
    $toggle = $btn.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern) -as [System.Windows.Automation.TogglePattern]
    if ($toggle) {
        Write-Output "Current ToggleState: $($toggle.Current.ToggleState)"
    }
}
'@

$out = Invoke-OnDefault -PowerShellScript "$script -HwndVal $hwndVal"
Write-Output $out
