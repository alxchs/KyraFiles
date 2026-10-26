. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

`$code = @'
using System;
using System.Runtime.InteropServices;
public static class DialogTester {
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public struct INPUT {
        [FieldOffset(0)] public uint type;
        [FieldOffset(8)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    public static void Focus(IntPtr hwnd) {
        IntPtr curFore = GetForegroundWindow();
        if (curFore == hwnd) return;
        uint curPid;
        uint foreThread = GetWindowThreadProcessId(curFore, out curPid);
        uint appThread = GetCurrentThreadId();

        AttachThreadInput(appThread, foreThread, true);
        BringWindowToTop(hwnd);
        ShowWindow(hwnd, 5);
        SetForegroundWindow(hwnd);
        AttachThreadInput(appThread, foreThread, false);
        System.Threading.Thread.Sleep(150);
    }

    public static void SendKey(ushort key) {
        INPUT[] inp = new INPUT[2];
        inp[0].type = 1; inp[0].ki.wVk = key;
        inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
        SendInput(2, inp, 40);
    }

    public static void SendText(string text) {
        INPUT[] inp = new INPUT[text.Length * 2];
        for (int i = 0; i < text.Length; i++) {
            inp[i * 2].type = 1;
            inp[i * 2].ki.wScan = (ushort)text[i];
            inp[i * 2].ki.dwFlags = 4;
            inp[i * 2 + 1].type = 1;
            inp[i * 2 + 1].ki.wScan = (ushort)text[i];
            inp[i * 2 + 1].ki.dwFlags = 4 | 2;
        }
        SendInput((uint)inp.Length, inp, 40);
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

[DialogTester]::Focus(`$hwnd)

# 1. Select 'arquivo' (file without extension)
`$condItem = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'arquivo')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condItem)
`$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
`$cur = `$el
while (`$cur -and `$cur.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) {
    `$cur = `$walker.GetParent(`$cur)
}
`$sel = `$cur.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
if (`$sel) { `$sel.Select() }
`$cur.SetFocus()
Start-Sleep -Milliseconds 300

# 2. Invoke Rename
`$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'InnerNavigationToolbarRenameButton')
`$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
`$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
`$inv.Invoke()
Start-Sleep -Milliseconds 500

# 3. Test Tab is no-op
[DialogTester]::SendKey(0x09)
Start-Sleep -Milliseconds 200

# 4. Press Right Arrow to unselect, then append .exe
[DialogTester]::SendKey(0x27) # VK_RIGHT (caret to end)
Start-Sleep -Milliseconds 150
[DialogTester]::SendText(".exe")
Start-Sleep -Milliseconds 300

# 5. Press Enter (VK 0x0D) to trigger commit and dialog
[DialogTester]::SendKey(0x0D)
Start-Sleep -Milliseconds 800
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture the dialog!
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\10_arquivo_to_exe_dialog.png'

# Check if dialog is visible via UIAutomation
$scriptCheck = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Window)
`$wins = `$root.FindAll([System.Windows.Automation.TreeScope]::Descendants, `$cond)
foreach (`$w in `$wins) {
    Write-Output ("Child window: '{0}' class: '{1}'" -f `$w.Current.Name, `$w.Current.ClassName)
}
"@
$resCheck = Invoke-OnDefault -PowerShellScript $scriptCheck
Write-Output "Dialog check:"
Write-Output $resCheck

# Dismiss the dialog by sending Escape (VK 0x1B) or finding 'No' button
$dismissScript = @"
`$hwnd = [IntPtr]$hwndVal
[DialogTester]::SendKey(0x1B)
Start-Sleep -Milliseconds 400
"@
Invoke-OnDefault -PowerShellScript $dismissScript | Out-Null
