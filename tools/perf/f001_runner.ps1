. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

`$codeKey = @'
using System;
using System.Runtime.InteropServices;
public static class RawInput {
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT {
        public uint type;
        public MOUSEKEYBDHARDWAREINPUT mkhi;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct MOUSEKEYBDHARDWAREINPUT {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [DllImport("user32.dll")] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint uCode, uint uMapType);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

    public static void SendKey(ushort key, ushort m1 = 0, ushort m2 = 0) {
        if (m1 == 0 && m2 == 0) {
            INPUT[] inp = new INPUT[2];
            ushort s = (ushort)MapVirtualKey(key, 0);
            inp[0].type = 1; inp[0].mkhi.ki.wVk = key; inp[0].mkhi.ki.wScan = s;
            inp[1].type = 1; inp[1].mkhi.ki.wVk = key; inp[1].mkhi.ki.wScan = s; inp[1].mkhi.ki.dwFlags = 2;
            SendInput(2, inp, Marshal.SizeOf(typeof(INPUT)));
        } else if (m2 == 0) {
            INPUT[] inp = new INPUT[4];
            ushort smod = (ushort)MapVirtualKey(m1, 0);
            ushort skey = (ushort)MapVirtualKey(key, 0);
            inp[0].type = 1; inp[0].mkhi.ki.wVk = m1; inp[0].mkhi.ki.wScan = smod;
            inp[1].type = 1; inp[1].mkhi.ki.wVk = key; inp[1].mkhi.ki.wScan = skey;
            inp[2].type = 1; inp[2].mkhi.ki.wVk = key; inp[2].mkhi.ki.wScan = skey; inp[2].mkhi.ki.dwFlags = 2;
            inp[3].type = 1; inp[3].mkhi.ki.wVk = m1; inp[3].mkhi.ki.wScan = smod; inp[3].mkhi.ki.dwFlags = 2;
            SendInput(4, inp, Marshal.SizeOf(typeof(INPUT)));
        } else {
            INPUT[] inp = new INPUT[6];
            ushort s1 = (ushort)MapVirtualKey(m1, 0);
            ushort s2 = (ushort)MapVirtualKey(m2, 0);
            ushort sk = (ushort)MapVirtualKey(key, 0);
            inp[0].type = 1; inp[0].mkhi.ki.wVk = m1; inp[0].mkhi.ki.wScan = s1;
            inp[1].type = 1; inp[1].mkhi.ki.wVk = m2; inp[1].mkhi.ki.wScan = s2;
            inp[2].type = 1; inp[2].mkhi.ki.wVk = key; inp[2].mkhi.ki.wScan = sk;
            inp[3].type = 1; inp[3].mkhi.ki.wVk = key; inp[3].mkhi.ki.wScan = sk; inp[3].mkhi.ki.dwFlags = 2;
            inp[4].type = 1; inp[4].mkhi.ki.wVk = m2; inp[4].mkhi.ki.wScan = s2; inp[4].mkhi.ki.dwFlags = 2;
            inp[5].type = 1; inp[5].mkhi.ki.wVk = m1; inp[5].mkhi.ki.wScan = s1; inp[5].mkhi.ki.dwFlags = 2;
            SendInput(6, inp, Marshal.SizeOf(typeof(INPUT)));
        }
    }

    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(50);
        mouse_event(0x02, (uint)x, (uint)y, 0, 0);
        System.Threading.Thread.Sleep(50);
        mouse_event(0x04, (uint)x, (uint)y, 0, 0);
    }
}
'@
Add-Type -TypeDefinition `$codeKey

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
        if (`$inv) { `$inv.Invoke(); return `$true }
    }
    return `$false
}

function Get-RenameInfo() {
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
            Rect = `$tb.Current.BoundingRectangle
        }
    }
    return @{ Found = `$false }
}

# 1. Select arquivo.txt
Select-ItemByName 'arquivo.txt' | Out-Null
Start-Sleep -Milliseconds 300
Invoke-Rename | Out-Null
Start-Sleep -Milliseconds 400

`$info = Get-RenameInfo
Write-Output ("STEP 1 (Initial): Text='{0}' Sel='{1}'" -f `$info.Text, `$info.Selection)

# 2. Press End (VK 0x23)
[RawInput]::SendKey(0x23)
Start-Sleep -Milliseconds 300
`$info = Get-RenameInfo
Write-Output ("STEP 2 (After End): Text='{0}' Sel='{1}'" -f `$info.Text, `$info.Selection)

# 3. Press Backspace (VK 0x08)
[RawInput]::SendKey(0x08)
Start-Sleep -Milliseconds 300
`$info = Get-RenameInfo
Write-Output ("STEP 3 (After Backspace): Text='{0}' Sel='{1}'" -f `$info.Text, `$info.Selection)

# 4. Press Ctrl+A (VK 0x41 with Mod 0x11)
[RawInput]::SendKey(0x41, 0x11)
Start-Sleep -Milliseconds 300
`$info = Get-RenameInfo
Write-Output ("STEP 4 (After Ctrl+A): Text='{0}' Sel='{1}'" -f `$info.Text, `$info.Selection)

# 5. Press Tab (VK 0x09)
[RawInput]::SendKey(0x09)
Start-Sleep -Milliseconds 300
`$info = Get-RenameInfo
Write-Output ("STEP 5 (After Tab): Text='{0}' Sel='{1}'" -f `$info.Text, `$info.Selection)

# 6. Press Shift+Tab (VK 0x09 with Mod 0x10)
[RawInput]::SendKey(0x09, 0x10)
Start-Sleep -Milliseconds 300
`$info = Get-RenameInfo
Write-Output ("STEP 6 (After Shift+Tab): Text='{0}' Sel='{1}'" -f `$info.Text, `$info.Selection)

# 7. Cancel with Escape (VK 0x1B)
[RawInput]::SendKey(0x1B)
Start-Sleep -Milliseconds 300
Write-Output "STEP 7: Sent Escape"
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
