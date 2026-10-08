. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

# Function to select an item by name
function Select-ItemByName(`$name) {
    `$condItem = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, `$name)
    `$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condItem)
    if (-not `$el) { return `$false }
    `$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    `$cur = `$el
    while (`$cur -and `$cur.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) {
        `$cur = `$walker.GetParent(`$cur)
    }
    if (`$cur) {
        `$sel = `$cur.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
        if (`$sel) { `$sel.Select() }
        `$cur.SetFocus()
        return `$true
    }
    return `$false
}

function Invoke-Rename() {
    `$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'InnerNavigationToolbarRenameButton')
    `$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
    if (`$btn) {
        `$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
        if (`$inv) {
            `$inv.Invoke()
            return `$true
        }
    }
    return `$false
}

function Get-RenameInfo() {
    Start-Sleep -Milliseconds 400
    `$condEdit = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'ItemNameTextBox')
    `$tb = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condEdit)
    if (`$tb) {
        `$valPat = `$tb.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
        `$textPat = `$tb.GetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern) -as [System.Windows.Automation.TextPattern]
        `$allText = if (`$valPat) { `$valPat.Current.Value } else { "N/A" }
        `$selText = ""
        if (`$textPat) {
            `$ranges = `$textPat.GetSelection()
            if (`$ranges -and `$ranges.Length -gt 0) {
                `$selText = `$ranges[0].GetText(-1)
            }
        }
        return @{
            Found = `$true
            Text = `$allText
            Selection = `$selText
            BoundingRect = `$tb.Current.BoundingRectangle
        }
    }
    return @{ Found = `$false }
}

`$ok = Select-ItemByName '.gitignore'
Write-Output "Selected .gitignore: `$ok"
Start-Sleep -Milliseconds 300
`$okRename = Invoke-Rename
Write-Output "Invoked Rename: `$okRename"
`$info = Get-RenameInfo
Write-Output ("TextBox Text: '{0}' | Selection: '{1}' | Rect: {2}" -f `$info.Text, `$info.Selection, `$info.BoundingRect)
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\01_gitignore_full_selection.png'

# Send Escape to cancel
Send-KeyToFiles -Key 0x1B -WaitAfterMs 400
