# tools/perf/verify_no_index_service.ps1
# Verificação mandatória de contenção de recursos e ausência do IndexService (F021 / NFR-001 / NFR-002)
# Pré-requisito obrigatório para AGY e Claude considerarem qualquer tarefa concluída no Files Custom.

[CmdletBinding()]
param(
    [string]$InstallPath = (Join-Path $env:LOCALAPPDATA "Programs\FilesCustom\Files.exe"),
    [int]$WaitSeconds = 10,
    [switch]$KeepOpen = $false
)

$ErrorActionPreference = 'Stop'

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  F021 — Verificacao Mandatoria: Ausencia do IndexService e CPU   " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Valida se o executável oficial instalado existe
if (-not (Test-Path -LiteralPath $InstallPath)) {
    Write-Error "ERRO: O executavel oficial instalado nao foi encontrado em: $InstallPath"
    exit 1
}

# 2. Garante que processos anteriores estão fechados
Stop-Process -Name "Files.App.IndexService" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "Files" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# 3. Inicia o Files Custom oficial instalado
Write-Host "[1/4] Iniciando Files Custom instalado ($InstallPath)..." -ForegroundColor Yellow
$proc = Start-Process -FilePath $InstallPath -PassThru
if (-not $proc -or $proc.HasExited) {
    Write-Error "ERRO: Falha ao iniciar $InstallPath"
    exit 1
}

Write-Host "[2/4] Aguardando $WaitSeconds segundos para estabilizacao de startup e background..." -ForegroundColor Yellow
Start-Sleep -Seconds $WaitSeconds

# 4. Verifica se o Files.App.IndexService foi gerado
Write-Host "[3/4] Verificando se Files.App.IndexService tentou subir..." -ForegroundColor Yellow
$indexProcs = @(Get-Process -Name "Files.App.IndexService" -ErrorAction SilentlyContinue)

if ($indexProcs.Count -gt 0) {
    Write-Host "FALHA CRITICA: Processo Files.App.IndexService DETECTADO em execucao!" -ForegroundColor Red
    $indexProcs | Format-Table Id, ProcessName, WorkingSet64, TotalProcessorTime
    if (-not $KeepOpen) {
        Stop-Process -Name "Files", "Files.App.IndexService" -Force -ErrorAction SilentlyContinue
    }
    exit 1
} else {
    Write-Host "SUCESSO: Files.App.IndexService NAO esta rodando (0 processos). Bloqueio F021 confirmado!" -ForegroundColor Green
}

# 5. Mede o consumo de CPU e RAM do Files em repouso
Write-Host "[4/4] Medindo CPU e RAM do Files.exe em repouso (amostra de 5s)..." -ForegroundColor Yellow
$cpuSampleStart = $proc.TotalProcessorTime.TotalSeconds
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Start-Sleep -Seconds 5
$sw.Stop()
$cpuSampleEnd = $proc.TotalProcessorTime.TotalSeconds

$cpuDelta = [math]::Max(0, ($cpuSampleEnd - $cpuSampleStart))
$cpuPercent = ($cpuDelta / ($sw.Elapsed.TotalSeconds * [Environment]::ProcessorCount)) * 100
$wsMb = [math]::Round($proc.WorkingSet64 / 1MB, 2)

Write-Host "Files PID: $($proc.Id)" -ForegroundColor Gray
Write-Host "RAM Working Set: $wsMb MB" -ForegroundColor Gray
Write-Host "CPU em repouso: $([math]::Round($cpuPercent, 2))%" -ForegroundColor Gray

if ($cpuPercent -gt 5.0) {
    Write-Warning "AVISO: Consumo de CPU ($([math]::Round($cpuPercent, 2))%) acima do esperado para repouso (< 5%)."
} else {
    Write-Host "SUCESSO: CPU em repouso calmíssima (< 5%)." -ForegroundColor Green
}

# 6. Verifica higiene de pastas auxiliares no repositório
$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$debugArtifacts = Get-ChildItem -Path "$repoRoot\src\Files.App\bin\x64\Debug" -ErrorAction SilentlyContinue
if ($debugArtifacts) {
    Write-Warning "AVISO DE HIGIENE: Foram encontrados artefatos em bin\x64\Debug. Lembre-se da regra do Alexandre de manter apenas a Release instalada."
}

if (-not $KeepOpen) {
    Write-Host "Fechando processo de teste..." -ForegroundColor Gray
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  TESTE F021 CONCLUIDO COM SUCESSO! GATE VALIDADO.                " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
exit 0

