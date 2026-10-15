. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)
$hwndVal = $hwnd.ToInt64()

$script = @"
Add-Type -AssemblyName UIAutomationClient

`$code = @'
using System;
using System.Runtime.InteropServices;
public static class NavBack {
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);

    public static void PressAltLeft() {
        keybd_event(0x12, 0, 0, 0); // ALT down
        keybd_event(0x25, 0, 0, 0); // LEFT down
        System.Threading.Thread.Sleep(50);
        keybd_event(0x25, 0, 2, 0); // LEFT up
        keybd_event(0x12, 0, 2, 0); // ALT up
    }
}
'@
Add-Type -TypeDefinition `$code -ErrorAction SilentlyContinue

`$hwnd = [IntPtr]$hwndVal
[NavBack]::SetForegroundWindow(`$hwnd)
Start-Sleep -Milliseconds 200

[NavBack]::PressAltLeft()
Start-Sleep -Seconds 3

Write-Output "Navigated back"
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res

# Capture screen showing D:\ again
$outPath = 'c:\desenv\utils\FilesApp\docs\releases\evidence\f010_persisted_d_drive.png'
Capture-FilesWindow -OutFile $outPath
Write-Output "Captured evidence to $outPath"
