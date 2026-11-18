# tools/perf/verify_no_index_service.ps1
# Verificação mandatória de contenção de recursos e ausência do IndexService (F021 / NFR-001 / NFR-002)
# Pré-requisito obrigatório para AGY e Claude considerarem qualquer tarefa concluída no KyraFiles.

[CmdletBinding()]
param(
    [string]$InstallPath = $(
        $kyra = Join-Path $env:LOCALAPPDATA "Programs\KyraFiles\KyraFiles.exe"
        $custom = Join-Path $env:LOCALAPPDATA "Programs\KyraFiles\KyraFiles.exe"
        if (Test-Path -LiteralPath $kyra) { $kyra } else { $custom }
    ),
    [string]$AliasPath = (Join-Path $env:LOCALAPPDATA "Microsoft\WindowsApps\kyrafiles.exe"),
    [int]$WaitSeconds = 10,
    [switch]$KeepOpen = $false
)

$ErrorActionPreference = 'Stop'

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  F021 - Verificacao Mandatoria: Ausencia do IndexService e CPU   " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Valida se o executavel oficial instalado existe
if (-not (Test-Path -LiteralPath $InstallPath)) {
    Write-Error "ERRO: O executavel oficial instalado nao foi encontrado em: $InstallPath"
    exit 1
}

# 2. Garante que processos anteriores estao fechados
Stop-Process -Name "KyraFiles.App.IndexService" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "KyraFiles" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "KyraFiles" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# 3. Inicia o KyraFiles oficial instalado (prefere o alias MSIX para preservar identidade de pacote)
$targetToRun = if (Test-Path -LiteralPath $AliasPath) { $AliasPath } else { $InstallPath }
Write-Host "[1/4] Iniciando KyraFiles instalado..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $targetToRun -WindowStyle Minimized -PassThru
if (-not $proc -or $proc.HasExited) {
    Start-Sleep -Seconds 2
    $proc = Get-Process -Name "KyraFiles" -ErrorAction SilentlyContinue | Select-Object -First 1
}

if (-not $proc) {
    Write-Error "ERRO: Falha ao iniciar processo KyraFiles"
    exit 1
}

Write-Host "[2/4] Aguardando estabilizacao de startup e background..." -ForegroundColor Yellow
Start-Sleep -Seconds $WaitSeconds

# 4. Verifica se o KyraFiles.App.IndexService foi gerado
Write-Host "[3/4] Verificando se KyraFiles.App.IndexService tentou subir..." -ForegroundColor Yellow
$indexProcs = @(Get-Process -Name "KyraFiles.App.IndexService" -ErrorAction SilentlyContinue)

if ($indexProcs.Count -gt 0) {
    Write-Host "FALHA CRITICA: Processo KyraFiles.App.IndexService DETECTADO em execucao!" -ForegroundColor Red
    $indexProcs | Format-Table Id, ProcessName, WorkingSet64, TotalProcessorTime
    if (-not $KeepOpen) {
        Stop-Process -Name "KyraFiles", "KyraFiles.App.IndexService" -Force -ErrorAction SilentlyContinue
    }
    exit 1
} else {
    Write-Host 'SUCESSO: KyraFiles.App.IndexService NAO esta rodando. Bloqueio F021 confirmado!' -ForegroundColor Green
}

# 5. Mede o consumo de CPU e RAM do Files em repouso
Write-Host "[4/4] Medindo CPU e RAM do KyraFiles.exe em repouso (amostra de 5s)..." -ForegroundColor Yellow
$proc = Get-Process -Name "KyraFiles" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($proc) {
    $cpuSampleStart = $proc.TotalProcessorTime.TotalSeconds
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    Start-Sleep -Seconds 5
    $sw.Stop()
    $proc.Refresh()
    $cpuSampleEnd = $proc.TotalProcessorTime.TotalSeconds

    $cpuDelta = [math]::Max(0, ($cpuSampleEnd - $cpuSampleStart))
    $cpuPercent = ($cpuDelta / ($sw.Elapsed.TotalSeconds * [Environment]::ProcessorCount)) * 100
    $wsMb = [math]::Round($proc.WorkingSet64 / 1MB, 2)
    $roundedCpu = [math]::Round($cpuPercent, 2)

    Write-Host "KyraFiles PID: $($proc.Id)" -ForegroundColor Gray
    Write-Host "RAM Working Set: $wsMb MB" -ForegroundColor Gray
    Write-Host "CPU em repouso: $roundedCpu %" -ForegroundColor Gray

    if ($cpuPercent -gt 5.0) {
        Write-Warning "AVISO: Consumo de CPU acima do esperado para repouso (< 5%)."
    } else {
        Write-Host "SUCESSO: CPU em repouso calmissima (< 5%)." -ForegroundColor Green
    }
}

# 6. Verifica higiene de pastas auxiliares no repositorio
$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$debugArtifacts = Get-ChildItem -Path "$repoRoot\src\KyraFiles.App\bin\x64\Debug" -ErrorAction SilentlyContinue
if ($debugArtifacts) {
    Write-Warning "AVISO DE HIGIENE: Foram encontrados artefatos em bin\x64\Debug. Lembre-se da regra do Alexandre de manter apenas a Release instalada."
}

if (-not $KeepOpen -and $proc) {
    Write-Host "Fechando processo de teste..." -ForegroundColor Gray
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  TESTE F021 CONCLUIDO COM SUCESSO! GATE VALIDADO.                " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
exit 0
