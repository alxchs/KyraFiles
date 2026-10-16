Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, 31716)
$windows = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $condition)

Write-Host "Windows count for PID 31716: $($windows.Count)"
foreach ($w in $windows) {
    Write-Host "Name: '$($w.Current.Name)', ClassName: '$($w.Current.ClassName)', BoundingRectangle: $($w.Current.BoundingRectangle)"
}

