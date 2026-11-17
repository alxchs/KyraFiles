<#
.SYNOPSIS
    F015 T2c — Verifica se listar C:\Google\Drive\disk baixa dados do Google Drive.

.DESCRIPTION
    Sem admin. Mede bytes recebidos pelo processo do Google Drive antes e depois
    de listar C:\Google\Drive\disk inteiro (sem abrir arquivos).
    Usa Win32_PerfFormattedData_PerfProc_Process (contadores de I/O do processo).

.NOTES
    Execute: pwsh tools\perf\f015_gdrive_probe.ps1
    Resultado gravado em log para o Claude ler.
#>

$ErrorActionPreference = 'Stop'
$LogDir  = "$env:TEMP\claude\desenv\utils\KyraFiles\f015"
$LogFile = Join-Path $LogDir "f015_gdrive_probe-$(Get-Date -f 'yyyyMMdd_HHmmss').log"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Log([string]$msg) {
    $line = "[$(Get-Date -f 'HH:mm:ss.fff')] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

Log "=== f015_gdrive_probe.ps1 ==="
Log "Log: $LogFile"

$GDrivePath = "C:\Google\Drive\disk"
if (-not (Test-Path $GDrivePath)) {
    Log "ERRO: '$GDrivePath' não encontrado. O Google Drive está montado?"
    exit 1
}
Log "Pasta do Google Drive: $GDrivePath"

# Localizar processo do Google Drive
$gdriveProcs = Get-Process | Where-Object {
    $_.ProcessName -match 'googledrivefs|googledrive|DriveFS' -or
    ($_.MainModule -and $_.MainModule.FileName -match 'Google')
} -ErrorAction SilentlyContinue

if (-not $gdriveProcs) {
    Log "[AVISO] Processo do Google Drive não encontrado. Medição de rede pode ser imprecisa."
}

# Função para ler bytes de I/O do processo (via WMI)
function Get-ProcessIOBytes([string]$procName) {
    try {
        $q = Get-CimInstance -ClassName Win32_Process -Filter "Name LIKE '%$procName%'" -ErrorAction SilentlyContinue
        if (-not $q) { return $null }
        # Win32_Process tem ReadOperationCount e ReadTransferCount (bytes lidos do I/O)
        $totBytes = ($q | Measure-Object -Property ReadTransferCount -Sum).Sum
        return $totBytes
    } catch { return $null }
}

# Capturar métricas de rede do processo pelo contador de Performance
function Get-GDriveNetBytes {
    try {
        $procs = @('googledrivefs', 'googledrive', 'DriveFS')
        foreach ($pn in $procs) {
            $p = Get-Process -Name $pn -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($p) {
                # Usar Get-Counter para I/O Read Bytes Total do processo
                $counter = "\Process($($p.ProcessName))\IO Read Bytes/sec"
                $sample  = Get-Counter -Counter $counter -SampleInterval 1 -MaxSamples 1 -ErrorAction SilentlyContinue
                if ($sample) {
                    return [pscustomobject]@{
                        ProcessName = $p.ProcessName
                        PID         = $p.Id
                        # Bytes totais de I/O (network incluso) via Process object
                        WorkingSetMB = [Math]::Round($p.WorkingSet64 / 1MB, 1)
                    }
                }
                return [pscustomobject]@{ ProcessName = $p.ProcessName; PID = $p.Id; WorkingSetMB = [Math]::Round($p.WorkingSet64 / 1MB, 1) }
            }
        }
        return $null
    } catch { return $null }
}

# Leitura de bytes de rede recebidos via netstat/WMI — alternativa simples
function Get-ProcessNetworkBytesReceived([int]$pid) {
    # Lemos o campo de I/O do processo via WMI (inclui rede + disco)
    try {
        $wmi = Get-CimInstance Win32_Process -Filter "ProcessId=$pid" -ErrorAction SilentlyContinue
        if ($wmi) { return $wmi.ReadTransferCount }
    } catch {}
    return 0
}

# Identificar PID do GoogleDriveFS
$gdriveProc = Get-Process | Where-Object {
    $_.ProcessName -match 'googledrivefs|GoogleDriveFS|DriveFS'
} | Select-Object -First 1

Log ""
if ($gdriveProc) {
    Log "Processo Google Drive: $($gdriveProc.ProcessName) PID=$($gdriveProc.Id)"
} else {
    Log "[AVISO] googledrivefs não encontrado. Tentando por nome genérico."
    $gdriveProc = Get-Process | Where-Object { $_.ProcessName -match 'google' } | Select-Object -First 1
    if ($gdriveProc) { Log "Processo 'google*' encontrado: $($gdriveProc.ProcessName) PID=$($gdriveProc.Id)" }
}

$pidGD = if ($gdriveProc) { $gdriveProc.Id } else { $null }

# --- Bytes de I/O antes ---
$bytesBefore = if ($pidGD) { Get-ProcessNetworkBytesReceived $pidGD } else { $null }
Log "I/O Read bytes ANTES (PID $pidGD): $($bytesBefore ?? 'N/A')"

# --- Listar o diretório inteiro sem abrir arquivos ---
Log ""
Log "Iniciando listagem de $GDrivePath (Get-ChildItem -Recurse -Force -File -ErrorAction SilentlyContinue)..."
$sw = [Diagnostics.Stopwatch]::StartNew()

$count = 0
Get-ChildItem -LiteralPath $GDrivePath -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
    $count++
}

$sw.Stop()
Log "Listagem concluída: $count itens em $($sw.Elapsed.TotalSeconds.ToString('F2')) s"

# Pequena pausa para dar tempo de bytes de rede serem registrados
Start-Sleep -Seconds 2

# --- Bytes de I/O depois ---
$bytesAfter = if ($pidGD) { Get-ProcessNetworkBytesReceived $pidGD } else { $null }
Log "I/O Read bytes DEPOIS (PID $pidGD): $($bytesAfter ?? 'N/A')"

if ($bytesBefore -ne $null -and $bytesAfter -ne $null) {
    $delta = $bytesAfter - $bytesBefore
    $deltaMB = [Math]::Round($delta / 1MB, 2)
    Log ""
    Log "Delta de I/O Read do processo Google Drive: $delta bytes ($deltaMB MB)"
    Log ""
    if ($delta -gt 10MB) {
        Log "[RESULTADO] ATENÇÃO — o Google Drive recebeu mais de 10 MB durante a listagem."
        Log "            Isso sugere download de dados. Verificar se o volume deve ser excluído do índice por padrão."
    } elseif ($delta -gt 1MB) {
        Log "[RESULTADO] O Google Drive recebeu ~$deltaMB MB. Pode ser metadados; avaliar com o Claude."
    } else {
        Log "[RESULTADO] OK — delta pequeno ($deltaMB MB). Provavelmente só metadados de diretório, sem download de conteúdo."
    }
} else {
    Log "[RESULTADO] Não foi possível medir o delta de I/O (processo não identificado ou sem dados WMI)."
    Log "            Verificação manual: observe o ícone do Google Drive na bandeja durante a execução."
}

Log ""
Log "Comando: Get-ChildItem -LiteralPath '$GDrivePath' -Recurse -Force -ErrorAction SilentlyContinue"
Log "Log completo: $LogFile"
Log "=== Concluído ==="
Write-Host "Log: $LogFile"
