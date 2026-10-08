# tools/perf/Run-F001-Validation.ps1
# Automates and captures evidence for F001 Rename UX across Details, Grid, and Column layouts.

. "$PSScriptRoot\SmokeTest-Helper.ps1"

$evidenceDir = "C:\desenv\utils\FilesApp\docs\agents\evidence\f001"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Force $evidenceDir | Out-Null
}

$p = Get-Process Files -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) {
    throw "Process Files not running!"
}
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

Write-Host "Found Files window: $hwndVal" -ForegroundColor Cyan

# Ensure window is active and on C:\FilesUXLab
files-dev.exe C:\FilesUXLab
Start-Sleep -Seconds 1

$testRunnerCode = @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public static class TestRunner {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int nIndex);

    [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public MOUSEKEYBDHARDWAREINPUT mkhi; }
    [StructLayout(LayoutKind.Explicit)] public struct MOUSEKEYBDHARDWAREINPUT {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
    }
    [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
    [DllImport("user32.dll")] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static void Focus(IntPtr hwnd) {
        IntPtr curFore = GetForegroundWindow();
        uint curPid;
        uint foreThread = GetWindowThreadProcessId(curFore, out curPid);
        uint appThread = GetCurrentThreadId();
        AttachThreadInput(appThread, foreThread, true);
        if (IsIconic(hwnd)) ShowWindow(hwnd, 9);
        ShowWindow(hwnd, 5);
        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);
        AttachThreadInput(appThread, foreThread, false);
        Thread.Sleep(150);
    }

    public static void Click(int x, int y, bool right = false) {
        int sw = GetSystemMetrics(0);
        int sh = GetSystemMetrics(1);
        int absX = (int)((double)x * 65535.0 / (sw - 1));
        int absY = (int)((double)y * 65535.0 / (sh - 1));

        uint downFlag = right ? 0x0008u : 0x0002u;
        uint upFlag   = right ? 0x0010u : 0x0004u;

        INPUT[] inp = new INPUT[3];
        inp[0].type = 0; inp[0].mkhi.mi.dx = absX; inp[0].mkhi.mi.dy = absY; inp[0].mkhi.mi.dwFlags = 0x0001 | 0x8000;
        inp[1].type = 0; inp[1].mkhi.mi.dx = absX; inp[1].mkhi.mi.dy = absY; inp[1].mkhi.mi.dwFlags = 0x0001 | 0x8000 | downFlag;
        inp[2].type = 0; inp[2].mkhi.mi.dx = absX; inp[2].mkhi.mi.dy = absY; inp[2].mkhi.mi.dwFlags = 0x0001 | 0x8000 | upFlag;
        SendInput(3, inp, Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(150);
    }

    public static void SendKey(ushort vk) {
        ushort scan = (ushort)MapVirtualKey(vk, 0);
        INPUT[] inp = new INPUT[2];
        inp[0].type = 1; inp[0].mkhi.ki.wVk = vk; inp[0].mkhi.ki.wScan = scan;
        inp[1].type = 1; inp[1].mkhi.ki.wVk = vk; inp[1].mkhi.ki.wScan = scan; inp[1].mkhi.ki.dwFlags = 2;
        SendInput(2, inp, Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(100);
    }

    public static void SendModifiedKey(ushort vk, ushort modVk) {
        ushort sMod = (ushort)MapVirtualKey(modVk, 0);
        ushort sVk = (ushort)MapVirtualKey(vk, 0);
        INPUT[] inp = new INPUT[4];
        inp[0].type = 1; inp[0].mkhi.ki.wVk = modVk; inp[0].mkhi.ki.wScan = sMod;
        inp[1].type = 1; inp[1].mkhi.ki.wVk = vk; inp[1].mkhi.ki.wScan = sVk;
        inp[2].type = 1; inp[2].mkhi.ki.wVk = vk; inp[2].mkhi.ki.wScan = sVk; inp[2].mkhi.ki.dwFlags = 2;
        inp[3].type = 1; inp[3].mkhi.ki.wVk = modVk; inp[3].mkhi.ki.wScan = sMod; inp[3].mkhi.ki.dwFlags = 2;
        SendInput(4, inp, Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(100);
    }

    public static void SendCtrlShiftKey(ushort vk) {
        ushort sCtrl = (ushort)MapVirtualKey(0x11, 0);
        ushort sShift = (ushort)MapVirtualKey(0x10, 0);
        ushort sVk = (ushort)MapVirtualKey(vk, 0);
        INPUT[] inp = new INPUT[6];
        inp[0].type = 1; inp[0].mkhi.ki.wVk = 0x11; inp[0].mkhi.ki.wScan = sCtrl;
        inp[1].type = 1; inp[1].mkhi.ki.wVk = 0x10; inp[1].mkhi.ki.wScan = sShift;
        inp[2].type = 1; inp[2].mkhi.ki.wVk = vk; inp[2].mkhi.ki.wScan = sVk;
        inp[3].type = 1; inp[3].mkhi.ki.wVk = vk; inp[3].mkhi.ki.wScan = sVk; inp[3].mkhi.ki.dwFlags = 2;
        inp[4].type = 1; inp[4].mkhi.ki.wVk = 0x10; inp[4].mkhi.ki.wScan = sShift; inp[4].mkhi.ki.dwFlags = 2;
        inp[5].type = 1; inp[5].mkhi.ki.wVk = 0x11; inp[5].mkhi.ki.wScan = sCtrl; inp[5].mkhi.ki.dwFlags = 2;
        SendInput(6, inp, Marshal.SizeOf(typeof(INPUT)));
        Thread.Sleep(200);
    }
}
"@

function Step-Action {
    param([string]$ScriptBlockText)
    $wrapped = @"
Add-Type -AssemblyName UIAutomationClient, System.Windows.Forms
$testRunnerCode
`$hwnd = [IntPtr]$hwndVal
[TestRunner]::Focus(`$hwnd)
$ScriptBlockText
"@
    Invoke-OnDefault -PowerShellScript $wrapped | Out-Null
    Start-Sleep -Milliseconds 400
}

function Take-Evidence {
    param([string]$Name)
    $path = Join-Path $evidenceDir "$Name.png"
    Capture-FilesWindow -OutFile $path | Out-Null
    Write-Host "Captured: $Name.png" -ForegroundColor Green
}

# Ensure Details Layout (Ctrl+Shift+1)
Write-Host "Ensuring Details layout..." -ForegroundColor Yellow
Step-Action "[TestRunner]::SendCtrlShiftKey(0x31)" # '1'
Take-Evidence "01_details_initial"

# -------------------------------------------------------------
# 1. .gitignore (initial selection & width check)
# -------------------------------------------------------------
Write-Host "Testing .gitignore..." -ForegroundColor Yellow
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, '.gitignore')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
}
"@
Take-Evidence "02_details_rename_gitignore"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc to cancel
Start-Sleep -Milliseconds 200

# -------------------------------------------------------------
# 2. arquivo.txt (initial selection & Ctrl+A & End+Backspace)
# -------------------------------------------------------------
Write-Host "Testing arquivo.txt (initial & Ctrl+A & End+Backspace)..." -ForegroundColor Yellow
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'arquivo.txt')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
}
"@
Take-Evidence "03_details_rename_arquivo_txt_initial"

# Test Ctrl+A
Step-Action "[TestRunner]::SendModifiedKey(0x41, 0x11)" # Ctrl+A
Take-Evidence "04_details_rename_arquivo_txt_ctrl_a"

# Test End + Backspace
Step-Action @"
[TestRunner]::SendKey(0x23); // End
Thread.Sleep(100);
[TestRunner]::SendKey(0x08); // Backspace
"@
Take-Evidence "05_details_rename_arquivo_txt_end_backspace"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc to cancel
Start-Sleep -Milliseconds 200

# -------------------------------------------------------------
# 3. arquivo.txt (Tab and Shift+Tab)
# -------------------------------------------------------------
Write-Host "Testing Tab and Shift+Tab on arquivo.txt..." -ForegroundColor Yellow
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'arquivo.txt')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x09); // Tab
}
"@
Take-Evidence "06_details_rename_tab_to_extension"

# Shift+Tab back to name
Step-Action "[TestRunner]::SendModifiedKey(0x09, 0x10)" # Shift+Tab
Take-Evidence "07_details_rename_shift_tab_to_name"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc to cancel
Start-Sleep -Milliseconds 200

# -------------------------------------------------------------
# 4. Click inside extension text
# -------------------------------------------------------------
Write-Host "Testing mouse click inside extension on arquivo.txt..." -ForegroundColor Yellow
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'arquivo.txt')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
    Thread.Sleep(300);
    // Click on '.txt' part (roughly 60px to the right of text start)
    [TestRunner]::Click((int)(`$r.X + 65), (int)(`$r.Y + 10));
}
"@
Take-Evidence "08_details_rename_click_inside_extension"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc
Start-Sleep -Milliseconds 200

# -------------------------------------------------------------
# 5. arquivo -> arquivo.exe (dialog verification)
# -------------------------------------------------------------
Write-Host "Testing arquivo -> arquivo.exe dialog..." -ForegroundColor Yellow
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'arquivo')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
    Thread.Sleep(200);
    // Tab in file without extension should be no-op
    [TestRunner]::SendKey(0x09);
    Thread.Sleep(100);
    // Add .exe to the end
    [TestRunner]::SendKey(0x23); // End
    Thread.Sleep(100);
    [System.Windows.Forms.SendKeys]::SendWait('.exe{ENTER}');
}
"@
Start-Sleep -Milliseconds 600
Take-Evidence "09_details_rename_arquivo_warning_dialog"

# Dismiss dialog (press Esc or 'No')
Step-Action "[TestRunner]::SendKey(0x1B)"
Start-Sleep -Milliseconds 300

# -------------------------------------------------------------
# 6. Folder & Shortcut
# -------------------------------------------------------------
Write-Host "Testing Folder A and shortcut..." -ForegroundColor Yellow
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'Folder A')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
}
"@
Take-Evidence "10_details_rename_folder_a"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc
Start-Sleep -Milliseconds 200

Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'shortcut')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
}
"@
Take-Evidence "11_details_rename_shortcut"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc
Start-Sleep -Milliseconds 200

# -------------------------------------------------------------
# 7. Rename commit + Undo
# -------------------------------------------------------------
Write-Host "Testing Rename Commit and Undo..." -ForegroundColor Yellow
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'activate_test.txt')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
    Thread.Sleep(200);
    [System.Windows.Forms.SendKeys]::SendWait('activate_renamed{ENTER}');
}
"@
Start-Sleep -Milliseconds 600
Take-Evidence "12_details_renamed_committed"

# Undo with Ctrl+Z
Step-Action "[TestRunner]::SendModifiedKey(0x5A, 0x11)" # Ctrl+Z
Start-Sleep -Milliseconds 600
Take-Evidence "13_details_rename_undo"

# -------------------------------------------------------------
# 8. Grid Layout (Ctrl+Shift+4)
# -------------------------------------------------------------
Write-Host "Testing Grid Layout (Ctrl+Shift+4)..." -ForegroundColor Yellow
Step-Action "[TestRunner]::SendCtrlShiftKey(0x34)" # '4'
Start-Sleep -Milliseconds 600
Take-Evidence "14_grid_layout_view"

# Test arquivo.txt in Grid Layout
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'arquivo.txt')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 20))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
}
"@
Take-Evidence "15_grid_rename_arquivo_txt"

# Tab in Grid Layout
Step-Action "[TestRunner]::SendKey(0x09)" # Tab
Take-Evidence "16_grid_rename_tab"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc
Start-Sleep -Milliseconds 200

# .gitignore in Grid Layout
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, '.gitignore')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 20))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
}
"@
Take-Evidence "17_grid_rename_gitignore"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc
Start-Sleep -Milliseconds 200

# -------------------------------------------------------------
# 9. Column Layout (Ctrl+Shift+5)
# -------------------------------------------------------------
Write-Host "Testing Column Layout (Ctrl+Shift+5)..." -ForegroundColor Yellow
Step-Action "[TestRunner]::SendCtrlShiftKey(0x35)" # '5'
Start-Sleep -Milliseconds 600
Take-Evidence "18_column_layout_view"

# Test arquivo.txt in Column Layout
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'arquivo.txt')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
}
"@
Take-Evidence "19_column_rename_arquivo_txt"

# Tab in Column Layout
Step-Action "[TestRunner]::SendKey(0x09)" # Tab
Take-Evidence "20_column_rename_tab"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc
Start-Sleep -Milliseconds 200

# .gitignore in Column Layout
Step-Action @"
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)
`$cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, '.gitignore')
`$el = `$root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, `$cond)
if (`$el) {
    `$r = `$el.Current.BoundingRectangle
    [TestRunner]::Click((int)(`$r.X + 20), (int)(`$r.Y + 10))
    Thread.Sleep(200);
    [TestRunner]::SendKey(0x71); // F2
}
"@
Take-Evidence "21_column_rename_gitignore"
Step-Action "[TestRunner]::SendKey(0x1B)" # Esc
Start-Sleep -Milliseconds 200

# Switch back to Details Layout (Ctrl+Shift+1)
Write-Host "Returning to Details Layout..." -ForegroundColor Yellow
Step-Action "[TestRunner]::SendCtrlShiftKey(0x31)" # '1'
Take-Evidence "22_final_details_view"

Write-Host "Validation suite complete! All evidence captured in $evidenceDir" -ForegroundColor Green
