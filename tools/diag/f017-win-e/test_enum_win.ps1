Add-Type @'
using System; using System.Runtime.InteropServices; using System.Text;
public class WinEnum {
  public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
}
'@

$filesProc = Get-Process Files -ErrorAction SilentlyContinue
if (-not $filesProc) {
    Write-Host "Files process not running."
    exit
}
$targetPid = $filesProc.Id
Write-Host "Target PID: $targetPid"

[WinEnum]::EnumWindows({
    param($hwnd, $lparam)
    $wPid = 0
    [WinEnum]::GetWindowThreadProcessId($hwnd, [ref]$wPid)
    if ($wPid -eq $targetPid) {
        $sb = New-Object System.Text.StringBuilder 256
        [WinEnum]::GetWindowText($hwnd, $sb, 256)
        $vis = [WinEnum]::IsWindowVisible($hwnd)
        Write-Host "HWND: $hwnd, Visible: $vis, Title: '$($sb.ToString())'"
    }
    return $true
}, [IntPtr]::Zero)

