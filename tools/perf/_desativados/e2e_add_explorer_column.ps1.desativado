. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient

`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::RootElement

`$code = @'
using System;
using System.Runtime.InteropServices;
public static class MouseHelper {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    public static void RightClick(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(100);
        mouse_event(0x08, 0, 0, 0, 0); // RIGHTDOWN
        System.Threading.Thread.Sleep(100);
        mouse_event(0x10, 0, 0, 0, 0); // RIGHTUP
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

# Step 1: Right-click Name header to open context menu
`$condName = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Name')
`$nameEl = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condName)
if (!`$nameEl) {
    Write-Output "Name element not found"
    exit 1
}

`$r = `$nameEl.Current.BoundingRectangle
`$clickX = [int](`$r.Left + 50)
`$clickY = [int](`$r.Top + 10)
Write-Output ("Right clicking on Name header at {0}, {1}" -f `$clickX, `$clickY)
[MouseHelper]::RightClick(`$clickX, `$clickY)
Start-Sleep -Seconds 1

# Step 2: Click 'More...' menu item
`$condMore = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'MoreColumnsMenuFlyoutItem')
`$moreItem = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condMore)
if (!`$moreItem) {
    Write-Output "MoreColumnsMenuFlyoutItem not found"
    exit 1
}

Write-Output "Invoking MoreColumnsMenuFlyoutItem..."
`$invMore = `$moreItem.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
if (`$invMore) {
    `$invMore.Invoke()
} else {
    Write-Output "InvokePattern not supported on MoreColumnsMenuFlyoutItem"
    exit 1
}
Start-Sleep -Seconds 2

# Step 3: Find Dimensões in the dialog
`$condDim = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Dimensões')
`$dimText = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condDim)
if (!`$dimText) {
    Write-Output "Dimensões text not found in dialog"
    exit 1
}

Write-Output ("Found Dimensões Text: Rect={0}" -f `$dimText.Current.BoundingRectangle)

# Find the CheckBox associated with this item:
# In the tree, the CheckBox is either a sibling or in the same container.
# Let's walk up to parent container and find CheckBox descendant
`$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
`$parent = `$walker.GetParent(`$dimText)
`$targetCb = `$null

if (`$parent) {
    `$condCb = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::CheckBox)
    `$targetCb = `$parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condCb)
}

if (!`$targetCb) {
    # Fallback: search all checkboxes and find the one nearest in Y
    `$condCb = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::CheckBox)
    `$allCbs = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$condCb)
    `$dimY = `$dimText.Current.BoundingRectangle.Top
    foreach (`$cb in `$allCbs) {
        `$cbRect = `$cb.Current.BoundingRectangle
        if ([Math]::Abs(`$cbRect.Top - `$dimY) -lt 20 -and `$cbRect.Left -lt `$dimText.Current.BoundingRectangle.Left) {
            `$targetCb = `$cb
            break
        }
    }
}

if (!`$targetCb) {
    Write-Output "CheckBox for Dimensões not found!"
    exit 1
}

Write-Output ("Found target CheckBox: Rect={0}" -f `$targetCb.Current.BoundingRectangle)
`$toggle = `$targetCb.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern) -as [System.Windows.Automation.TogglePattern]
if (`$toggle) {
    Write-Output ("Current toggle state: {0}. Toggling..." -f `$toggle.Current.ToggleState)
    `$toggle.Toggle()
    Start-Sleep -Milliseconds 500
    Write-Output ("New toggle state: {0}" -f `$toggle.Current.ToggleState)
} else {
    Write-Output "TogglePattern not available on CheckBox"
}

# Step 4: Click PrimaryButton (OK)
`$condPrimary = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'PrimaryButton')
`$primaryBtn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condPrimary)
if (`$primaryBtn) {
    Write-Output ("Found PrimaryButton: Rect={0}" -f `$primaryBtn.Current.BoundingRectangle)
    `$invBtn = `$primaryBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
    if (`$invBtn) {
        Write-Output "Invoking PrimaryButton..."
        `$invBtn.Invoke()
    }
} else {
    Write-Output "PrimaryButton not found"
}

Start-Sleep -Seconds 3
Write-Output "Dialog confirmed, verifying dynamic columns..."

# Step 5: Verify headers
`$headers = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)))
foreach (`$h in `$headers) {
    `$name = `$h.Current.Name
    if (`$name -match '^(Name|Tag|Date modified|Date created|Type|Size|Status|Dimensões|Dimensions)$') {
        Write-Output ("Header present: '{0}', Rect={1}" -f `$name, `$h.Current.BoundingRectangle)
    }
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture screen
Capture-FilesWindow -OutFile 'docs\releases\evidence\f010_added_column.png'
