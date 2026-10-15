. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

# Helper script to execute a single key or click action on Default desktop
function Invoke-Action([string]$actionCode) {
    $script = @"
Add-Type -AssemblyName UIAutomationClient
`$hwnd = [IntPtr]$hwndVal
`$root = [System.Windows.Automation.AutomationElement]::FromHandle(`$hwnd)

`$code = @'
using System;
using System.Runtime.InteropServices;
public static class InputDriver {
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

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

    public static void SendKey(ushort key, ushort m1 = 0) {
        if (m1 == 0) {
            INPUT[] inp = new INPUT[2];
            inp[0].type = 1; inp[0].ki.wVk = key;
            inp[1].type = 1; inp[1].ki.wVk = key; inp[1].ki.dwFlags = 2;
            SendInput(2, inp, 40);
        } else {
            INPUT[] inp = new INPUT[4];
            inp[0].type = 1; inp[0].ki.wVk = m1;
            inp[1].type = 1; inp[1].ki.wVk = key;
            inp[2].type = 1; inp[2].ki.wVk = key; inp[2].ki.dwFlags = 2;
            inp[3].type = 1; inp[3].ki.wVk = m1; inp[3].ki.dwFlags = 2;
            SendInput(4, inp, 40);
        }
    }

    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        System.Threading.Thread.Sleep(50);
        mouse_event(0x02, 0, 0, 0, 0); // Down
        System.Threading.Thread.Sleep(50);
        mouse_event(0x04, 0, 0, 0, 0); // Up
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

[InputDriver]::Focus(`$hwnd)
$actionCode
"@
    return Invoke-OnDefault -PowerShellScript $script
}

Write-Output "--- 1. Initial State (already in rename) ---"
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\02_arquivo_txt_initial.png' | Out-Null

Write-Output "--- 2. Sending End key (VK 0x23) ---"
Invoke-Action "[InputDriver]::SendKey(0x23)" | Out-Null
Start-Sleep -Milliseconds 400
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\03_arquivo_txt_end_key.png' | Out-Null

Write-Output "--- 3. Sending Backspace (VK 0x08) ---"
Invoke-Action "[InputDriver]::SendKey(0x08)" | Out-Null
Start-Sleep -Milliseconds 400
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\04_arquivo_txt_backspace.png' | Out-Null

Write-Output "--- 4. Sending Ctrl+A (VK 0x41 + 0x11) ---"
Invoke-Action "[InputDriver]::SendKey(0x41, 0x11)" | Out-Null
Start-Sleep -Milliseconds 400
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\05_arquivo_txt_ctrl_a.png' | Out-Null

Write-Output "--- 5. Sending Tab (VK 0x09) ---"
Invoke-Action "[InputDriver]::SendKey(0x09)" | Out-Null
Start-Sleep -Milliseconds 400
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\06_arquivo_txt_tab.png' | Out-Null

Write-Output "--- 6. Sending Shift+Tab (VK 0x09 + 0x10) ---"
Invoke-Action "[InputDriver]::SendKey(0x09, 0x10)" | Out-Null
Start-Sleep -Milliseconds 400
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\07_arquivo_txt_shift_tab.png' | Out-Null

Write-Output "--- 7. Sending Escape (VK 0x1B) to cancel ---"
Invoke-Action "[InputDriver]::SendKey(0x1B)" | Out-Null
Start-Sleep -Milliseconds 400
Capture-FilesWindow -OutFile 'docs\agents\evidence\f001\08_arquivo_txt_escaped.png' | Out-Null

Write-Output "Done sequence!"
