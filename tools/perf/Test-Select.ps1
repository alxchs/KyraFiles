. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, '.gitignore')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    Write-Output "Found .gitignore: `$(`$el.Current.Name)"
    `$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    `$cur = `$el
    while (`$cur -and `$cur.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) {
        `$cur = `$walker.GetParent(`$cur)
    }
    if (`$cur) {
        Write-Output "Found parent ListItem!"
        `$sel = `$cur.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
        if (`$sel) {
            Write-Output "Has SelectionItemPattern! Calling Select()..."
            `$sel.Select()
        }
        `$cur.SetFocus()
        Write-Output "SetFocus called."
    }
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture to verify
Capture-FilesWindow -OutFile 'tools\perf\captures\f001\test_select_result.png'
