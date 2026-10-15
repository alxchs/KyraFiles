. "$PSScriptRoot\SmokeTest-Helper.ps1"

$script = @'
Add-Type -AssemblyName UIAutomationClient
$root = [System.Windows.Automation.AutomationElement]::RootElement

# Find CheckBoxes
$condCb = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::CheckBox)
$cbs = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condCb)

$firstCb = $null
foreach ($cb in $cbs) {
    $r = $cb.Current.BoundingRectangle
    if ($r.Left -gt 1200 -and $r.Left -lt 1400 -and $r.Top -gt 800 -and $r.Top -lt 920) {
        $firstCb = $cb
        break
    }
}

if ($firstCb) {
    Write-Output ("Found First CheckBox: Rect={0}" -f $firstCb.Current.BoundingRectangle)
    $toggle = $firstCb.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern) -as [System.Windows.Automation.TogglePattern]
    if ($toggle) {
        Write-Output ("Current ToggleState={0}, toggling..." -f $toggle.Current.ToggleState)
        $toggle.Toggle()
        Start-Sleep -Milliseconds 300
        Write-Output ("New ToggleState={0}" -f $toggle.Current.ToggleState)
    }
} else {
    Write-Output "First CheckBox not found!"
    exit 1
}

# Click OK
$condPrimary = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'PrimaryButton')
$primaryBtn = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condPrimary)
if ($primaryBtn) {
    Write-Output ("Found PrimaryButton: Rect={0}. Invoking..." -f $primaryBtn.Current.BoundingRectangle)
    $invBtn = $primaryBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    if ($invBtn) {
        $invBtn.Invoke()
    }
} else {
    Write-Output "PrimaryButton not found!"
    exit 1
}

Start-Sleep -Seconds 3

# Verify headers
$condText = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
$texts = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condText)
foreach ($t in $texts) {
    $r = $t.Current.BoundingRectangle
    if ($r.Top -gt 500 -and $r.Top -lt 700 -and $r.Left -gt 800 -and $r.Left -lt 3000) {
        Write-Output ("Header Text: Name='{0}', Rect={1}" -f $t.Current.Name, $r)
    }
}
'@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture screen
$outPath = 'c:\desenv\utils\FilesApp\docs\releases\evidence\f010_added_column.png'
Capture-FilesWindow -OutFile $outPath
Write-Output "Captured evidence to $outPath"
