Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$AE = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]
$p = Get-Process -Name "Files" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) {
    Write-Output "Files process not running"
    exit
}
Write-Output "Found Files PID $($p.Id)"
$condPid = New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $p.Id)
$win = $AE::RootElement.FindFirst($Scope::Children, $condPid)
if ($win) {
    Write-Output "Window Name: '$($win.Current.Name)'"
    Write-Output "NativeWindowHandle: $($win.Current.NativeWindowHandle)"
    Write-Output "BoundingRectangle: $($win.Current.BoundingRectangle)"
} else {
    Write-Output "No UIA window found"
}

