# tools/perf/f092_sidebar_bench.ps1
# Medição de baseline de serviços e tempo de carregamento da barra lateral (F092 / T0)

[CmdletBinding()]
param(
    [string]$AppPath = (Join-Path $env:LOCALAPPDATA "Programs\KyraFiles\KyraFiles.exe"),
    [int]$Iterations = 5,
    [switch]$IncludeUI
)

$ErrorActionPreference = 'Stop'

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  F092 - Medição de Baseline de Abertura e Sidebar   " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# 1. Medição Lógica Silenciosa (Zero Foco, 100% seguro em qualquer horário)
Write-Host "[1] Medição de Latência de Enumeração de Dispositivos e Shell" -ForegroundColor Yellow

$shellTimes = @()
$driveInfoTimes = @()

for ($i = 1; $i -le $Iterations; $i++) {
    # Mede DriveInfo.GetDrives() (.NET padrão hoje)
    $swDrives = [System.Diagnostics.Stopwatch]::StartNew()
    $drives = [System.IO.DriveInfo]::GetDrives()
    $swDrives.Stop()
    $driveInfoTimes += $swDrives.Elapsed.TotalMilliseconds

    # Mede Shell.Application NameSpace(17) (ssfDRIVES / This PC)
    $swShell = [System.Diagnostics.Stopwatch]::StartNew()
    $sh = New-Object -ComObject Shell.Application
    $ns17 = $sh.NameSpace(17)
    $items = @()
    if ($null -ne $ns17) {
        foreach ($it in $ns17.Items()) {
            $items += $it.Path
        }
    }
    $swShell.Stop()
    $shellTimes += $swShell.Elapsed.TotalMilliseconds
}

$avgDrives = ($driveInfoTimes | Measure-Object -Average).Average
$avgShell = ($shellTimes | Measure-Object -Average).Average

Write-Host "  Média DriveInfo.GetDrives(): $([math]::Round($avgDrives, 2)) ms (Itens: $($drives.Count))" -ForegroundColor Gray
Write-Host "  Média Shell ssfDRIVES COM:   $([math]::Round($avgShell, 2)) ms (Itens: $($items.Count))" -ForegroundColor Gray

# 2. Medição de Interface (UI)
$now = Get-Date
$isAfterLateHours = ($now.Hour -ge 23 -and $now.Minute -ge 59) -or ($now.Hour -lt 6)

if ($IncludeUI -and $isAfterLateHours) {
    Write-Host "[2] Medição de Startup da UI (Autorizada pós-23:59)" -ForegroundColor Yellow
    if (Test-Path -LiteralPath $AppPath) {
        $uiTimes = @()
        for ($i = 1; $i -le $Iterations; $i++) {
            Stop-Process -Name "KyraFiles", "Files" -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500

            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $proc = Start-Process -FilePath $AppPath -WindowStyle Minimized -PassThru
            
            $timeout = [System.Diagnostics.Stopwatch]::StartNew()
            while ($timeout.ElapsedMilliseconds -lt 15000) {
                $proc.Refresh()
                if ($proc.MainWindowHandle -ne [IntPtr]::Zero) {
                    break
                }
                Start-Sleep -Milliseconds 50
            }
            $sw.Stop()
            $uiTimes += $sw.ElapsedMilliseconds
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 500
        }
        $avgUi = ($uiTimes | Measure-Object -Average).Average
        Write-Host "  Média Abertura UI: $([math]::Round($avgUi, 2)) ms" -ForegroundColor Green
    } else {
        Write-Warning "App executável não encontrado para teste de UI."
    }
} else {
    Write-Host "[2] Medição de UI: NOT TESTED (Horário comercial/pré-23:59 - Regra Zero de Preservação de Foco)" -ForegroundColor Yellow
}

Write-Host "=====================================================" -ForegroundColor Cyan
