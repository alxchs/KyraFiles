#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Instala o spike F015-T1 como serviço do Windows (opção b: instalador elevado).

.DESCRIPTION
    Spike descartável — F015 T1. Fora de src/.
    Copia IndexServiceSpike.exe para %ProgramFiles%\FilesCustom\IndexService\
    e registra o serviço com 'sc create'.
    Grava saída em log para o Claude ler.

.NOTES
    Executar elevado. O Alexandre clica "Sim" no UAC;
    o Claude dispara com: Start-Process pwsh -Verb RunAs -ArgumentList "-NoProfile -File Install-Spike.ps1"
#>

$LogDir = "$env:TEMP\claude\desenv\utils\FilesApp\f015"
$LogFile = Join-Path $LogDir "Install-Spike-$(Get-Date -f 'yyyyMMdd_HHmmss').log"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Log([string]$msg) {
    $line = "[$(Get-Date -f 'yyyy-MM-dd HH:mm:ss')] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

Log "=== Install-Spike.ps1 iniciado ==="
Log "Log: $LogFile"

# Localizar o exe compilado
$ScriptDir  = $PSScriptRoot
$ExePath    = Join-Path $ScriptDir "IndexServiceSpike\bin\x64\Release\net10.0-windows\IndexServiceSpike.exe"
if (-not (Test-Path $ExePath)) {
    $ExePath = Join-Path $ScriptDir "IndexServiceSpike\bin\x64\Debug\net10.0-windows\IndexServiceSpike.exe"
}
if (-not (Test-Path $ExePath)) {
    $ExePath = Join-Path $ScriptDir "IndexServiceSpike\bin\Release\net10.0-windows\IndexServiceSpike.exe"
}
if (-not (Test-Path $ExePath)) {
    $ExePath = Join-Path $ScriptDir "IndexServiceSpike\bin\Debug\net10.0-windows\IndexServiceSpike.exe"
}
if (-not (Test-Path $ExePath)) {
    Log "ERRO: exe não encontrado. Compile antes: dotnet build IndexServiceSpike -c Release"
    exit 1
}
Log "Exe encontrado: $ExePath"

# Destino
$Dest = "$env:ProgramFiles\FilesCustom\IndexService"
New-Item -ItemType Directory -Force -Path $Dest | Out-Null
$DestExe = Join-Path $Dest "IndexServiceSpike.exe"

# Copiar exe (e dependências .NET se necessário)
$SrcDir = Split-Path $ExePath
Get-ChildItem $SrcDir -File | ForEach-Object {
    $target = Join-Path $Dest $_.Name
    Copy-Item -LiteralPath $_.FullName -Destination $target -Force
    Log "Copiado: $($_.Name)"
}

# Remover serviço antigo se existir
$existing = Get-Service -Name "FilesCustomIndexServiceSpike" -ErrorAction SilentlyContinue
if ($existing) {
    Log "Serviço já existe; removendo antes de reinstalar..."
    Stop-Service "FilesCustomIndexServiceSpike" -Force -ErrorAction SilentlyContinue
    sc.exe delete "FilesCustomIndexServiceSpike" | Out-Null
    Start-Sleep -Seconds 1
}

# Criar serviço
Log "Criando serviço..."
$scOut = sc.exe create "FilesCustomIndexServiceSpike" `
    binPath= "`"$DestExe`" --service" `
    start= demand `
    obj= LocalSystem `
    DisplayName= "Files Custom Index Service (Spike F015)" 2>&1
Log "sc create: $scOut"

# Iniciar
Log "Iniciando serviço..."
$startOut = sc.exe start "FilesCustomIndexServiceSpike" 2>&1
Log "sc start: $startOut"
Start-Sleep -Seconds 2

# Verificar
$queryOut = sc.exe query "FilesCustomIndexServiceSpike" 2>&1
Log "sc query:`n$queryOut"

if ($queryOut -match "RUNNING") {
    Log "[OK] Serviço instalado e rodando."
    exit 0
} else {
    Log "[AVISO] Serviço não está RUNNING. Verifique o log."
    exit 1
}
