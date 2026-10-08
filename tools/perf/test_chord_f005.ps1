# Test chord sending for F005
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

public static class ChordSender {
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
        if (curFore != hwnd) {
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
    }

    public static void SendChord(IntPtr hwnd, ushort key, ushort m1 = 0, ushort m2 = 0) {
        Focus(hwnd);

        if (m1 != 0) {
            INPUT[] mod1Down = new INPUT[1];
            mod1Down[0].type = 1; mod1Down[0].ki.wVk = m1;
            SendInput(1, mod1Down, 40);
            System.Threading.Thread.Sleep(50);
        }

        if (m2 != 0) {
            INPUT[] mod2Down = new INPUT[1];
            mod2Down[0].type = 1; mod2Down[0].ki.wVk = m2;
            SendInput(1, mod2Down, 40);
            System.Threading.Thread.Sleep(50);
        }

        INPUT[] keyPress = new INPUT[2];
        keyPress[0].type = 1; keyPress[0].ki.wVk = key;
        keyPress[1].type = 1; keyPress[1].ki.wVk = key; keyPress[1].ki.dwFlags = 2; // keyup
        SendInput(2, keyPress, 40);
        System.Threading.Thread.Sleep(80);

        if (m2 != 0) {
            INPUT[] mod2Up = new INPUT[1];
            mod2Up[0].type = 1; mod2Up[0].ki.wVk = m2; mod2Up[0].ki.dwFlags = 2;
            SendInput(1, mod2Up, 40);
            System.Threading.Thread.Sleep(50);
        }

        if (m1 != 0) {
            INPUT[] mod1Up = new INPUT[1];
            mod1Up[0].type = 1; mod1Up[0].ki.wVk = m1; mod1Up[0].ki.dwFlags = 2;
            SendInput(1, mod1Up, 40);
            System.Threading.Thread.Sleep(50);
        }
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

[ChordSender]::Focus(`$hwnd)

# Select FilesUXLab tab
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'FilesUXLab')
`$tab = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$tab) {
    `$sel = `$tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
    if (`$sel) { `$sel.Select() }
}
Start-Sleep -Milliseconds 200

# Send Alt+P (Key=0x50, M1=0x12)
Write-Output "Sending Alt+P..."
[ChordSender]::SendChord(`$hwnd, 0x50, 0x12, 0)
Start-Sleep -Milliseconds 800
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
Capture-FilesWindow -OutFile docs/agents/evidence/f005/test_chord_alt_p.png
