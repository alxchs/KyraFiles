<#
.SYNOPSIS
    Mede o consumo de memória (Working Set / Private Bytes) e a latência de restauração do KyraFiles.exe.
    Rastreia conformidade com a Spec F063 (P0 - Zero Regressão).
    Atende à decisão Q12 (29/10/2026): utiliza instância própria e controlada de teste,
    nunca sequestrando nem roubando o foco da janela em uso do usuário.
#>

param(
    [int]$WaitAfterMinimizeSec = 4,
    [switch]$SkipRestore = $false,
    [int]$TargetPid = 0,
    [switch]$UseInstalledRelease = $true
)

$ErrorActionPreference = "Stop"

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

$proc = $null
$isDedicatedTestInstance = $false

if ($TargetPid -gt 0) {
    $proc = Get-Process -Id $TargetPid -ErrorAction SilentlyContinue
    if (-not $proc) {
        Write-Error "Processo com PID $TargetPid não encontrado."
        exit 1
    }
} else {
    # Q12: NUNCA sequestrar a janela ativa do usuário silenciosamente.
    # Dispara uma instância própria de teste em Release instalada.
    $installedExe = Join-Path $env:LOCALAPPDATA "Programs\KyraFiles\KyraFiles.exe"
    if (-not (Test-Path $installedExe)) {
        $installedExe = Join-Path $env:LOCALAPPDATA "Programs\Files\KyraFiles.exe"
    }

    if (-not (Test-Path $installedExe)) {
        Write-Error "KyraFiles.exe instalado não foi encontrado para execução de instância de teste."
        exit 1
    }

    Write-Host "Iniciando instância própria e isolada de teste: $installedExe" -ForegroundColor Cyan
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $installedExe
    $psi.UseShellExecute = $true

    $proc = [System.Diagnostics.Process]::Start($psi)
    $isDedicatedTestInstance = $true

    # Aguarda inicialização da janela
    Write-Host "Aguardando inicialização da janela da instância de teste..."
    $timeout = [System.Diagnostics.Stopwatch]::StartNew()
    $hwnd = [IntPtr]::Zero
    while ($timeout.ElapsedMilliseconds -lt 15000 -and $hwnd -eq [IntPtr]::Zero) {
        Start-Sleep -Milliseconds 500
        $proc.Refresh()
        $hwnd = [Win32WindowHelper]::FindWinUIWindow($proc.Id)
        if ($hwnd -eq [IntPtr]::Zero) {
            $hwnd = $proc.MainWindowHandle
        }
    }

    if ($hwnd -eq [IntPtr]::Zero) {
        Write-Warning "Instância de teste iniciada, mas HWND principal não foi obtido a tempo."
    }
}

try {
    $pidNum = $proc.Id
    if (-not $hwnd) {
        $hwnd = [Win32WindowHelper]::FindWinUIWindow($pidNum)
        if ($hwnd -eq [IntPtr]::Zero) {
            $hwnd = $proc.MainWindowHandle
        }
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
        IsDedicatedInstance = $isDedicatedTestInstance
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
}
finally {
    # Se foi uma instância de teste dedicada criada pelo script, fecha com segurança
    if ($isDedicatedTestInstance -and $proc -and -not $proc.HasExited) {
        Write-Host "Fechando instância dedicada de teste (PID: $($proc.Id))..." -ForegroundColor Yellow
        try {
            $proc.CloseMainWindow() | Out-Null
            Start-Sleep -Seconds 1
            if (-not $proc.HasExited) {
                $proc.Kill()
            }
        } catch { }
    }
}
