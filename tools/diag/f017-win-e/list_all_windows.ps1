Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$AE = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]

$windows = $AE::RootElement.FindAll($Scope::Children, [System.Windows.Automation.Condition]::TrueCondition)
Write-Output "Total top-level windows: $($windows.Count)"
foreach ($w in $windows) {
    if ($w.Current.Name -or $w.Current.ProcessId -eq 6728) {
        Write-Output "PID: $($w.Current.ProcessId) | Class: '$($w.Current.ClassName)' | Name: '$($w.Current.Name)'"
    }
}

