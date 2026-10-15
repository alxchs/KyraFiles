. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()
$evidenceDir = (Resolve-Path "docs\agents\evidence\f001").Path

# Script that runs ON the default desktop so it never loses focus to external processes
$script = @"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName System.Drawing

`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$evidenceDir = '$evidenceDir'

`$code = @'
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

public static class UnifiedTester {
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    public struct RECT { public int L, T, R, B; }

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

    public static void SendKey(ushort key, ushort m1 = 0, ushort m2 = 0) {
        if (m1 == 0 && m2 == 0) {
            INPUT[] inp = new INPUT[2];
            inp[0].type = 1; inp[0].ki.wVk = key;
            inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
            SendInput(2, inp, 40);
        } else if (m2 == 0) {
            INPUT[] inp = new INPUT[4];
            inp[0].type = 1; inp[0].ki.wVk = m1;
            inp[1].type = 1; inp[1].ki.wVk = key;
            inp[2].type = 1; inp[2].ki.wVk = key; inp[2].ki.dwFlags = 2;
            inp[3].type = 1; inp[3].ki.wVk = m1; inp[3].ki.dwFlags = 2;
            SendInput(4, inp, 40);
        } else {
            INPUT[] inp = new INPUT[6];
            inp[0].type = 1; inp[0].ki.wVk = m1;
            inp[1].type = 1; inp[1].ki.wVk = m2;
            inp[2].type = 1; inp[2].ki.wVk = key;
            inp[3].type = 1; inp[3].ki.wVk = key; inp[3].ki.dwFlags = 2;
            inp[4].type = 1; inp[4].ki.wVk = m2; inp[4].ki.dwFlags = 2;
            inp[5].type = 1; inp[5].ki.wVk = m1; inp[5].ki.dwFlags = 2;
            SendInput(6, inp, 40);
        }
    }

    public static void Capture(IntPtr hwnd, string path) {
        SetProcessDpiAwarenessContext((IntPtr)(-4));
        RECT r;
        GetClientRect(hwnd, out r);
        int w = r.R - r.L;
        int h = r.B - r.T;
        if (w <= 0 || h <= 0) return;
        using (Bitmap bmp = new Bitmap(w, h)) {
            using (Graphics g = Graphics.FromImage(bmp)) {
                IntPtr hdc = g.GetHdc();
                PrintWindow(hwnd, hdc, 2);
                g.ReleaseHdc(hdc);
            }
            bmp.Save(path, ImageFormat.Png);
        }
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

[UnifiedTester]::Focus(`$hwnd)

function Select-And-Rename(`$name) {
    # Find item
    `$condItem = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, `$name)
    `$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condItem)
    if (-not `$el) {
        Write-Output "Item not found: `$name"
        return `$false
    }
    `$walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    `$cur = `$el
    while (`$cur -and `$cur.Current.ControlType -ne [System.Windows.Automation.ControlType]::ListItem) {
        `$cur = `$walker.GetParent(`$cur)
    }
    if (`$cur) {
        `$sel = `$cur.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern) -as [System.Windows.Automation.SelectionItemPattern]
        if (`$sel) { `$sel.Select() }
        `$cur.SetFocus()
        Start-Sleep -Milliseconds 300
    }

    # Trigger rename
    `$condBtn = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'InnerNavigationToolbarRenameButton')
    `$btn = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$condBtn)
    if (`$btn -and `$btn.Current.IsEnabled) {
        `$inv = `$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern) -as [System.Windows.Automation.InvokePattern]
        `$inv.Invoke()
    } else {
        [UnifiedTester]::SendKey(0x71) # F2
    }
    Start-Sleep -Milliseconds 500
    return `$true
}

# --- 1. Grid Layout ---
Write-Output "Step 1: Grid Layout"
[UnifiedTester]::Focus(`$hwnd)
[UnifiedTester]::SendKey(0x1B) # Esc any active rename
Start-Sleep -Milliseconds 300
# Switch to Grid view: Ctrl+Shift+4
[UnifiedTester]::SendKey(0x34, 0x11, 0x10)
Start-Sleep -Milliseconds 1200
[UnifiedTester]::Capture(`$hwnd, (Join-Path `$evidenceDir "15_layout_grid_view.png"))

# Rename arquivo.txt
Select-And-Rename "arquivo.txt"
[UnifiedTester]::Capture(`$hwnd, (Join-Path `$evidenceDir "16_layout_grid_rename_initial.png"))

# Tab to extension
[UnifiedTester]::SendKey(0x09)
Start-Sleep -Milliseconds 400
[UnifiedTester]::Capture(`$hwnd, (Join-Path `$evidenceDir "17_layout_grid_rename_tab.png"))

# Esc to cancel rename
[UnifiedTester]::SendKey(0x1B)
Start-Sleep -Milliseconds 500

# --- 2. Column Layout ---
Write-Output "Step 2: Column Layout"
# Switch to Column view: Ctrl+Shift+5
[UnifiedTester]::SendKey(0x35, 0x11, 0x10)
Start-Sleep -Milliseconds 1500
[UnifiedTester]::Capture(`$hwnd, (Join-Path `$evidenceDir "18_layout_column_view.png"))

# Rename arquivo.txt
Select-And-Rename "arquivo.txt"
[UnifiedTester]::Capture(`$hwnd, (Join-Path `$evidenceDir "19_layout_column_rename_initial.png"))

# Tab to extension
[UnifiedTester]::SendKey(0x09)
Start-Sleep -Milliseconds 400
[UnifiedTester]::Capture(`$hwnd, (Join-Path `$evidenceDir "20_layout_column_rename_tab.png"))

# Esc to cancel rename
[UnifiedTester]::SendKey(0x1B)
Start-Sleep -Milliseconds 500

# --- 3. Details Layout (Restore) ---
Write-Output "Step 3: Restore Details Layout"
# Switch to Details view: Ctrl+Shift+1
[UnifiedTester]::SendKey(0x31, 0x11, 0x10)
Start-Sleep -Milliseconds 1500
[UnifiedTester]::Capture(`$hwnd, (Join-Path `$evidenceDir "21_layout_details_restored.png"))

Write-Output "ALL_LAYOUTS_COMPLETE"
"@

$result = Invoke-OnDefault -PowerShellScript $script
Write-Output $result
