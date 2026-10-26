. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

`$codeForce = @'
using System;
using System.Runtime.InteropServices;
public static class ForceSend2 {
    [DllImport("user32.dll")] public static extern uint SendInput(uint nInputs, DesktopLauncher.INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint uCode, uint uMapType);
    [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public MOUSEKEYBDHARDWAREINPUT mkhi; }
    [StructLayout(LayoutKind.Explicit)] public struct MOUSEKEYBDHARDWAREINPUT { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

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
}
'@
Add-Type -TypeDefinition `$codeForce -ErrorAction SilentlyContinue

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
        }
    }
    return @{ Found = `$false }
}

# 1. Select arquivo.txt and trigger rename
Select-ItemByName 'arquivo.txt' | Out-Null
Start-Sleep -Milliseconds 300
Invoke-Rename | Out-Null
Start-Sleep -Milliseconds 400

`$step1 = Get-RenameInfo
Write-Output ("STEP 1 Initial: Text='{0}' Sel='{1}'" -f `$step1.Text, `$step1.Selection)
"@

$res1 = Invoke-OnDefault -PowerShellScript $script
Write-Output $res1
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\02_arquivo_txt_initial.png'
