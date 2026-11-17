#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Remove o spike F015-T1 (serviço + arquivos em %ProgramFiles%).

.NOTES
    Executar elevado. O Alexandre clica "Sim" no UAC;
    o Claude dispara com: Start-Process pwsh -Verb RunAs -ArgumentList "-NoProfile -File Uninstall-Spike.ps1"
#>

$LogDir = "$env:TEMP\claude\desenv\utils\KyraFiles\f015"
$LogFile = Join-Path $LogDir "Uninstall-Spike-$(Get-Date -f 'yyyyMMdd_HHmmss').log"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Log([string]$msg) {
    $line = "[$(Get-Date -f 'yyyy-MM-dd HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

Log "=== Uninstall-Spike.ps1 iniciado ==="
Log "Log: $LogFile"

# Parar serviço
$svc = Get-Service -Name "FilesCustomIndexServiceSpike" -ErrorAction SilentlyContinue
if ($svc) {
    Log "Parando serviço..."
    Stop-Service "FilesCustomIndexServiceSpike" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    $stopOut = sc.exe delete "FilesCustomIndexServiceSpike" 2>&1
    Log "sc delete: $stopOut"
} else {
    Log "Serviço não encontrado (já removido ou nunca instalado)."
}

# Remover arquivos
$Dest = "$env:ProgramFiles\KyraFiles\IndexService"
if (Test-Path $Dest) {
    Remove-Item -Recurse -Force $Dest
    Log "Diretório removido: $Dest"
} else {
    Log "Diretório não encontrado: $Dest"
}

# Verificar
$queryOut = sc.exe query "FilesCustomIndexServiceSpike" 2>&1
Log "sc query após remoção: $queryOut"

if ($queryOut -match "1060") {
    Log "[OK] Serviço removido com sucesso (código 1060 = não existe)."
    exit 0
} else {
    Log "[AVISO] Serviço ainda pode existir. Verifique manualmente."
    exit 1
}
