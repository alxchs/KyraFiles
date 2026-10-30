<#
.SYNOPSIS
    Mede o consumo de memória (Working Set / Private Bytes) e a latência de restauração do Files.exe.
    Rastreia conformidade com a Spec F063 (P0 - Zero Regressão).
#>

param(
    [int]$WaitAfterMinimizeSec = 4,
    [switch]$SkipRestore = $false
)

$code = @"
using System;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

public class Win32WindowHelper
{
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpfn, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetThreadDesktop(IntPtr hDesktop);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool CloseDesktop(IntPtr hDesktop);

    [DllImport("user32.dll")]
    public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumWindowsProc lpfn, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr h, out int pid);

    [DllImport("user32.dll")]
    public static extern int GetClassName(IntPtr h, StringBuilder sb, int n);

    public const int SW_MINIMIZE = 6;
    public const int SW_RESTORE = 9;

    public static IntPtr FindWinUIWindow(int targetPid)
    {
        IntPtr found = IntPtr.Zero;
        var t = new Thread(() =>
        {
            IntPtr hDesk = OpenDesktop("Default", 0, false, 0x01FF);
            if (hDesk != IntPtr.Zero)
            {
                SetThreadDesktop(hDesk);
                EnumDesktopWindows(hDesk, (h, l) =>
                {
                    int p;
                    GetWindowThreadProcessId(h, out p);
                    if (p == targetPid)
                    {
                        var sb = new StringBuilder(256);
                        GetClassName(h, sb, 256);
                        if (sb.ToString() == "WinUIDesktopWin32WindowClass")
                        {
                            found = h;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
                CloseDesktop(hDesk);
            }
        });
        t.Start();
        t.Join();
        return found;
    }
}
"@

if (-not ([System.Management.Automation.PSTypeName]'Win32WindowHelper').Type) {
    Add-Type -TypeDefinition $code
}

$proc = Get-Process -Name "Files" -ErrorAction SilentlyContinue | Where-Object { [Win32WindowHelper]::FindWinUIWindow($_.Id) -ne [IntPtr]::Zero } | Select-Object -First 1

if (-not $proc) {
    $proc = Get-Process -Name "Files" -ErrorAction SilentlyContinue | Select-Object -First 1
}

if (-not $proc) {
    Write-Error "Files.exe não está em execução."
    exit 1
}

$pidNum = $proc.Id
$hwnd = [Win32WindowHelper]::FindWinUIWindow($pidNum)
if ($hwnd -eq [IntPtr]::Zero) {
    $hwnd = $proc.MainWindowHandle
}

$proc.Refresh()
$wsInitial = [math]::Round($proc.WorkingSet64 / 1MB, 2)
$privInitial = [math]::Round($proc.PrivateMemorySize64 / 1MB, 2)

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "Medição de Memória e Latência (Spec F063) - PID: $pidNum" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "1. Estado Inicial (Ativo):"
Write-Host "   Working Set: $wsInitial MB"
Write-Host "   Private Bytes: $privInitial MB"

$wsMin = $null
$privMin = $null
$latencyMs = $null
$wsRestored = $null
$privRestored = $null

if ($hwnd -ne [IntPtr]::Zero) {
    Write-Host "`n2. Minimizando janela (HWND: $hwnd)..."
    [Win32WindowHelper]::ShowWindow($hwnd, [Win32WindowHelper]::SW_MINIMIZE) | Out-Null
    
    Start-Sleep -Seconds $WaitAfterMinimizeSec
    
    $proc.Refresh()
    $wsMin = [math]::Round($proc.WorkingSet64 / 1MB, 2)
    $privMin = [math]::Round($proc.PrivateMemorySize64 / 1MB, 2)
    Write-Host "   Working Set minimizado (com trim suave): $wsMin MB"
    Write-Host "   Private Bytes minimizado: $privMin MB"

    if (-not $SkipRestore) {
        Write-Host "`n3. Medindo latência de restauração..."
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        [Win32WindowHelper]::ShowWindow($hwnd, [Win32WindowHelper]::SW_RESTORE) | Out-Null
        
        while ([Win32WindowHelper]::IsIconic($hwnd) -and $sw.ElapsedMilliseconds -lt 2000) {
            [System.Threading.Thread]::Sleep(1)
        }
        $sw.Stop()
        $latencyMs = $sw.ElapsedMilliseconds
        Write-Host "   Latência de Restauração: $latencyMs ms"

        Start-Sleep -Seconds 1
        $proc.Refresh()
        $wsRestored = [math]::Round($proc.WorkingSet64 / 1MB, 2)
        $privRestored = [math]::Round($proc.PrivateMemorySize64 / 1MB, 2)
        Write-Host "   Working Set restaurado: $wsRestored MB"
        Write-Host "   Private Bytes restaurado: $privRestored MB"
    }
} else {
    Write-Warning "Nenhuma janela WinUI encontrada para o processo $pidNum."
}

$result = [PSCustomObject]@{
    Timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    PID = $pidNum
    ActiveWorkingSetMB = $wsInitial
    ActivePrivateBytesMB = $privInitial
    MinimizedWorkingSetMB = $wsMin
    MinimizedPrivateBytesMB = $privMin
    RestoreLatencyMs = $latencyMs
    RestoredWorkingSetMB = $wsRestored
    RestoredPrivateBytesMB = $privRestored
}

Write-Host "`n=====================================================" -ForegroundColor Cyan
Write-Host "Resultado Consolidado:" -ForegroundColor Green
$result | Format-List
Write-Host "=====================================================" -ForegroundColor Cyan

return $result
