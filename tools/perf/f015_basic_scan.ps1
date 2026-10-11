<#
.SYNOPSIS
    F015 T2b — Varredura básica por volume sem admin.
    Usa o BasicScanSpike (FileIdBothDirectoryInfo, sem abrir arquivo, sem descer em reparse).

.DESCRIPTION
    Mede por volume (C:, D:, R:, S:): tempo, itens (arquivos + pastas) e pico de RAM do processo.
    Não precisa de administrador.
    Grava resultado em log e imprime no console.

.NOTES
    Compile o spike antes:
        dotnet build tools\spikes\f015-scan\BasicScanSpike -c Release
    Execute:
        pwsh tools\perf\f015_basic_scan.ps1
#>

$ErrorActionPreference = 'Stop'
$LogDir  = "$env:TEMP\claude\desenv\utils\FilesApp\f015"
$LogFile = Join-Path $LogDir "f015_basic_scan-$(Get-Date -f 'yyyyMMdd_HHmmss').log"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Log([string]$msg) {
    $line = "[$(Get-Date -f 'HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

Log "=== f015_basic_scan.ps1 ==="
Log "Log: $LogFile"

# Localizar o exe
$SpikeDir = Join-Path $PSScriptRoot "..\spikes\f015-scan\BasicScanSpike"
$SpikeDir = (Resolve-Path $SpikeDir).Path
$ExePath  = Join-Path $SpikeDir "bin\x64\Release\net10.0-windows\BasicScanSpike.exe"
if (-not (Test-Path $ExePath)) {
    $ExePath = Join-Path $SpikeDir "bin\x64\Debug\net10.0-windows\BasicScanSpike.exe"
}
if (-not (Test-Path $ExePath)) {
    # Fallback sem plataforma
    $ExePath = Join-Path $SpikeDir "bin\Release\net10.0-windows\BasicScanSpike.exe"
}
if (-not (Test-Path $ExePath)) {
    Log "Compilando BasicScanSpike..."
    Push-Location $SpikeDir
    dotnet build -c Release --nologo -v quiet 2>&1 | ForEach-Object { Log $_ }
    Pop-Location
    $ExePath = Join-Path $SpikeDir "bin\x64\Release\net10.0-windows\BasicScanSpike.exe"
}
if (-not (Test-Path $ExePath)) {
    Log "ERRO: exe não encontrado após build."
    exit 1
}
Log "Exe: $ExePath"

# Volumes a varrer
$Volumes = @('C:\', 'D:\', 'R:\', 'S:\') | Where-Object { Test-Path $_ }

Log ""
Log "Volumes encontrados: $($Volumes -join ', ')"
Log ""

# Executar (um processo por rodada para capturar RAM limpa)
$Results = @()
foreach ($vol in $Volumes) {
    Log "--- Varrendo $vol ---"
    $proc = Start-Process -FilePath $ExePath `
        -ArgumentList $vol `
        -NoNewWindow -PassThru -Wait `
        -RedirectStandardOutput "$LogDir\scan_stdout_$($vol[0]).txt" `
        -RedirectStandardError  "$LogDir\scan_stderr_$($vol[0]).txt"

    $stdout = Get-Content "$LogDir\scan_stdout_$($vol[0]).txt" -Raw
    $stderr = Get-Content "$LogDir\scan_stderr_$($vol[0]).txt" -Raw -ErrorAction SilentlyContinue

    Log $stdout
    if ($stderr) { Log "[STDERR] $stderr" }
    $Results += $stdout
}

Log ""
Log "=== RESUMO ==="
$Results | ForEach-Object { Log $_ }

Log ""
Log "Comando de medição: $ExePath C:\ D:\ R:\ S:\"
Log "Log completo: $LogFile"
Log "=== Concluído ==="
Write-Host "Log: $LogFile"
