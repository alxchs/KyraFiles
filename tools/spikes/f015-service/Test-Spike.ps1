<#
.SYNOPSIS
    Testa o spike F015-T1 sem elevação: sc query + ping pelo named pipe.

.DESCRIPTION
    Spike descartável — F015 T1. Não eleva.
    1. Verifica o estado do serviço com sc query.
    2. Faz ping pelo named pipe e imprime a identidade retornada.
    3. Grava saída em log para o Claude ler.

.NOTES
    Pode ser chamado pelo Claude (sem RunAs) após Install-Spike.ps1 ter rodado elevado.
#>

$LogDir  = "$env:TEMP\claude\desenv\utils\KyraFiles\f015"
$LogFile = Join-Path $LogDir "Test-Spike-$(Get-Date -f 'yyyyMMdd_HHmmss').log"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Log([string]$msg) {
    $line = "[$(Get-Date -f 'yyyy-MM-dd HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

Log "=== Test-Spike.ps1 iniciado ==="
Log "Log: $LogFile"

# --- 1. Estado do serviço
Log "`n--- sc query ---"
$queryOut = sc.exe query "FilesCustomIndexServiceSpike" 2>&1
Log $queryOut

$running = $queryOut -match "RUNNING"
if (-not $running) {
    Log "[AVISO] Serviço não está RUNNING. O ping pode falhar."
}

# --- 2. Localizar o cliente
$ScriptDir  = $PSScriptRoot
$ClientExe  = Join-Path $ScriptDir "IndexClientSpike\bin\x64\Release\net10.0-windows\IndexClientSpike.exe"
if (-not (Test-Path $ClientExe)) {
    $ClientExe = Join-Path $ScriptDir "IndexClientSpike\bin\x64\Debug\net10.0-windows\IndexClientSpike.exe"
}
if (-not (Test-Path $ClientExe)) {
    $ClientExe = Join-Path $ScriptDir "IndexClientSpike\bin\Release\net10.0-windows\IndexClientSpike.exe"
}
if (-not (Test-Path $ClientExe)) {
    $ClientExe = Join-Path $ScriptDir "IndexClientSpike\bin\Debug\net10.0-windows\IndexClientSpike.exe"
}
if (-not (Test-Path $ClientExe)) {
    Log "ERRO: IndexClientSpike.exe não encontrado. Compile antes: dotnet build IndexClientSpike"
    exit 1
}
Log "Cliente: $ClientExe"

# --- 3. Ping
Log "`n--- Executando ping ---"
$pingOut = & $ClientExe 2>&1
$exitCode = $LASTEXITCODE
$pingOut | ForEach-Object { Log $_ }
Log "Exit code: $exitCode"

if ($exitCode -eq 0) {
    Log "`n[RESULTADO] OK — pipe respondeu. Verifique 'pong:NT AUTHORITY\SYSTEM' acima."
} else {
    Log "`n[RESULTADO] FALHOU — veja o log completo em $LogFile"
}

Log "=== Test-Spike.ps1 encerrado ==="
Write-Host "Log gravado em: $LogFile"
exit $exitCode
