<#
.SYNOPSIS
    M2-T3 (NFR-002, M2-AC-1): Medição de pico de memória e tempo para o Editor Hexadecimal (HexFileBuffer e HexFileSaver).
.DESCRIPTION
    Gera arquivos determinísticos de 256 MB e 4 GB em %TEMP%\agy\<GUID>\desenv\utils\FilesApp-f060-m2\Files\,
    e mede em processos separados por fase:
    (a) Abrir com HexFileBuffer e ler o primeiro bloco;
    (b) Ir ao fim (ler os últimos 256 bytes);
    (c) Alterar 3 bytes (começo, meio e fim) e salvar com HexFileSaver (blocos alterados + diário);
    (d) Verificar de forma independente: SHA-256 em streaming do arquivo salvo comparado ao original
        com os 3 bytes substituídos, e conferir exclusão do diário.
    Amostra PeakWorkingSet64 e PrivateMemorySize64 a cada 100 ms.
    Gera evidência CSV com timestamp em docs/agents/evidence/F060/.
#>

[CmdletBinding()]
param(
    [string]$OutputDir = "$PSScriptRoot/../../docs/agents/evidence/F060",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================="
Write-Host "F060 M2-T3 (NFR-002, M2-AC-1): Medicao de Desempenho e Memoria Hex Editor"
Write-Host "================================================================="

# 1. Verificacao de Espaco Livre em Disco (minimo 12 GB)
$tempDriveName = [System.IO.Path]::GetPathRoot($env:TEMP).TrimEnd(":\")
if ([string]::IsNullOrWhiteSpace($tempDriveName)) {
    $tempDriveName = "C"
}

$driveInfo = Get-PSDrive -Name $tempDriveName -ErrorAction SilentlyContinue
if ($null -eq $driveInfo) {
    throw "Nao foi possivel obter informacoes da unidade de disco '${tempDriveName}:' para %TEMP%."
}

$freeBytes = $driveInfo.Free
$minBytes = 12L * 1024 * 1024 * 1024
$freeGB = [math]::Round($freeBytes / 1GB, 2)
$minGB = [math]::Round($minBytes / 1GB, 2)

Write-Host "Espaco livre na unidade $tempDriveName`: $freeGB GB (minimo necessario: $minGB GB)"

if ($freeBytes -lt $minBytes) {
    throw "ABORTADO: Espaco livre insuficiente na unidade $tempDriveName`: ($freeGB GB livre, minimo $minGB GB)."
}

# 2. Verificacao de Carga de CPU (se > 50%, aguardar cair)
Write-Host "`nVerificando carga de CPU antes do inicio..."
$cpuLoad = 0
try {
    $cpuLoad = [int]((Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average)
} catch {
    $cpuLoad = 0
}

Write-Host "Carga atual de CPU: $cpuLoad%"
while ($cpuLoad -gt 50) {
    Write-Host "Carga de CPU acima de 50% ($cpuLoad%). Aguardando 3 segundos para estabilizacao..."
    Start-Sleep -Seconds 3
    try {
        $cpuLoad = [int]((Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average)
    } catch {
        $cpuLoad = 0
    }
}
Write-Host "CPU estavel em $cpuLoad% (abaixo do limiar de 50%). Prosseguindo."

# 3. Compilacao do Harness .NET
$harnessDir = "$PSScriptRoot/HexEditorHarness"
$harnessCsproj = "$harnessDir/HexEditorHarness.csproj"
$harnessExe = "$harnessDir/bin/x64/Release/net10.0/HexEditorHarness.exe"

if (-not $SkipBuild -or -not (Test-Path $harnessExe)) {
    Write-Host "`nCompilando HexEditorHarness (Release x64)..."
    $buildOutput = dotnet build "$harnessCsproj" -c Release -p:Platform=x64 -v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao compilar HexEditorHarness. Codigo de saida: $LASTEXITCODE`n$buildOutput"
    }
    Write-Host "HexEditorHarness compilado com sucesso."
}

if (-not (Test-Path $harnessExe)) {
    throw "Executavel do harness nao encontrado: $harnessExe"
}

# 4. Configuracao do Diretorio Temporario
$guid = [guid]::NewGuid().ToString()
$tempRoot = [System.IO.Path]::Combine($env:TEMP, "agy", $guid, "desenv", "utils", "FilesApp-f060-m2", "Files")
Write-Host "`nDiretorio temporario da sessao: $tempRoot"
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Invoke-HarnessPhase {
    param(
        [string]$PhaseName,
        [string]$FileSizeLabel,
        [long]$FileSizeBytes,
        [int]$CpuPercentage,
        [string[]]$HarnessArgs
    )

    $exactCommand = "$harnessExe $($HarnessArgs -join ' ')"
    Write-Host "  -> [$PhaseName] Iniciando fase ($FileSizeLabel)..."

    $pinfo = New-Object System.Diagnostics.ProcessStartInfo
    $pinfo.FileName = $harnessExe
    foreach ($arg in $HarnessArgs) {
        $pinfo.ArgumentList.Add($arg)
    }
    $pinfo.RedirectStandardOutput = $true
    $pinfo.RedirectStandardError = $true
    $pinfo.UseShellExecute = $false
    $pinfo.CreateNoWindow = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $pinfo

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $proc.Start() | Out-Null

    $maxPws = 0L
    $maxPriv = 0L

    while (-not $proc.HasExited) {
        try {
            $proc.Refresh()
            $pws = $proc.PeakWorkingSet64
            $priv = $proc.PrivateMemorySize64
            if ($pws -gt $maxPws) { $maxPws = $pws }
            if ($priv -gt $maxPriv) { $maxPriv = $priv }
        } catch {}
        Start-Sleep -Milliseconds 100
    }

    $stdoutTask = $proc.StandardOutput.ReadToEndAsync()
    $stderrTask = $proc.StandardError.ReadToEndAsync()
    $proc.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()

    try {
        $proc.Refresh()
        $pws = $proc.PeakWorkingSet64
        $priv = $proc.PrivateMemorySize64
        if ($pws -gt $maxPws) { $maxPws = $pws }
        if ($priv -gt $maxPriv) { $maxPriv = $priv }
    } catch {}

    $sw.Stop()
    $durationMs = $sw.ElapsedMilliseconds
    $durationSec = [math]::Round($durationMs / 1000.0, 3)

    if ($proc.ExitCode -ne 0) {
        throw "Erro na execucao da fase '$PhaseName' (exit code $($proc.ExitCode)):`n$stderr`n$stdout"
    }

    # Analise de saida estruturada
    $shaOriginal = ""
    $shaSaved = ""
    $shaExpected = ""
    $shaMatch = ""
    $journalDeleted = ""
    $verified = ""

    $outputLines = $stdout -split "`r?`n"
    foreach ($line in $outputLines) {
        if ($line -match "PHASE_OUTPUT:\s*(.+)") {
            $pairs = $matches[1] -split "\|"
            foreach ($pair in $pairs) {
                if ($pair -match "^\s*([^=]+)=(.*)$") {
                    $k = $matches[1].Trim()
                    $v = $matches[2].Trim()
                    if ($k -eq "Sha256") { $shaOriginal = $v }
                    if ($k -eq "Sha256Saved") { $shaSaved = $v }
                    if ($k -eq "Sha256Expected") { $shaExpected = $v }
                    if ($k -eq "HashMatch") { $shaMatch = $v }
                    if ($k -eq "JournalDeleted") { $journalDeleted = $v }
                    if ($k -eq "Verified") { $verified = $v }
                }
            }
        }
    }

    $pwsMB = [math]::Round($maxPws / 1MB, 2)
    $privMB = [math]::Round($maxPriv / 1MB, 2)

    Write-Host "     Tempo: $durationSec s | PeakWorkingSet64: $pwsMB MB | PrivateMemorySize64: $privMB MB"

    $row = [PSCustomObject]@{
        Timestamp = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
        Command = $exactCommand
        FileSizeLabel = $FileSizeLabel
        FileSizeBytes = $FileSizeBytes
        Phase = $PhaseName
        DurationMs = $durationMs
        DurationSec = $durationSec
        PeakWorkingSet64_MB = $pwsMB
        PeakWorkingSet64_Bytes = $maxPws
        PeakPrivateMemorySize64_MB = $privMB
        PeakPrivateMemorySize64_Bytes = $maxPriv
        CpuLoadPercent = $CpuPercentage
        Sha256Original = $shaOriginal
        Sha256Saved = $shaSaved
        Sha256Expected = $shaExpected
        Sha256Match = $shaMatch
        JournalDeleted = $journalDeleted
        Status = "PASS"
    }

    return $row
}

try {
    # -------------------------------------------------------------
    # BATERIA 1: Rodada de Referencia (256 MB)
    # -------------------------------------------------------------
    $size256MB = 256L * 1024 * 1024
    $dir256MB = Join-Path $tempRoot "test_256MB"
    $file256MB = Join-Path $dir256MB "hex_test_256mb.bin"

    Write-Host "`n>>> Executando Bateria de 256 MB (Referencia de Linha de Base)..."

    # Fase: Generate
    $rGen256 = Invoke-HarnessPhase -PhaseName "Generate" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -CpuPercentage $cpuLoad -HarnessArgs @("generate", $file256MB, $size256MB.ToString(), "42")
    $results.Add($rGen256)
    $shaOriginal256 = $rGen256.Sha256Original

    # Fase (a): Open
    $rOpen256 = Invoke-HarnessPhase -PhaseName "Open" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -CpuPercentage $cpuLoad -HarnessArgs @("open", $file256MB)
    $results.Add($rOpen256)

    # Fase (b): SeekEnd
    $rSeek256 = Invoke-HarnessPhase -PhaseName "SeekEnd" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -CpuPercentage $cpuLoad -HarnessArgs @("seek-end", $file256MB)
    $results.Add($rSeek256)

    # Fase (c): EditAndSave
    $rEdit256 = Invoke-HarnessPhase -PhaseName "EditAndSave" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -CpuPercentage $cpuLoad -HarnessArgs @("edit-save", $file256MB, "42")
    $results.Add($rEdit256)

    # Fase (d): Verify
    $rVerify256 = Invoke-HarnessPhase -PhaseName "Verify" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -CpuPercentage $cpuLoad -HarnessArgs @("verify", $file256MB, "42")
    $results.Add($rVerify256)

    if ($rVerify256.Sha256Match -ne "True" -or $rVerify256.JournalDeleted -ne "True") {
        throw "FALHA: Verificacao independente de 256 MB reprovou (HashMatch=$($rVerify256.Sha256Match), JournalDeleted=$($rVerify256.JournalDeleted))!"
    }
    Write-Host "     Verificacao independente 256 MB: APROVADA (SHA-256 salvo: $($rVerify256.Sha256Saved), diario apagado: True)"

    # Limpar arquivo de 256 MB para economizar espaco antes de gerar 4 GB
    Remove-Item -LiteralPath $dir256MB -Recurse -Force

    # -------------------------------------------------------------
    # BATERIA 2: Rodada Alvo NFR-002 (4 GB)
    # -------------------------------------------------------------
    $size4GB = 4L * 1024 * 1024 * 1024 # 4 GB = 4.294.967.296 bytes
    $dir4GB = Join-Path $tempRoot "test_4GB"
    $file4GB = Join-Path $dir4GB "hex_test_4gb.bin"

    Write-Host "`n>>> Executando Bateria de 4 GB (Alvo NFR-002 / M2-AC-1)..."

    # Fase: Generate
    $rGen4GB = Invoke-HarnessPhase -PhaseName "Generate" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -CpuPercentage $cpuLoad -HarnessArgs @("generate", $file4GB, $size4GB.ToString(), "42")
    $results.Add($rGen4GB)
    $shaOriginal4GB = $rGen4GB.Sha256Original

    # Fase (a): Open
    $rOpen4GB = Invoke-HarnessPhase -PhaseName "Open" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -CpuPercentage $cpuLoad -HarnessArgs @("open", $file4GB)
    $results.Add($rOpen4GB)

    # Fase (b): SeekEnd
    $rSeek4GB = Invoke-HarnessPhase -PhaseName "SeekEnd" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -CpuPercentage $cpuLoad -HarnessArgs @("seek-end", $file4GB)
    $results.Add($rSeek4GB)

    # Fase (c): EditAndSave
    $rEdit4GB = Invoke-HarnessPhase -PhaseName "EditAndSave" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -CpuPercentage $cpuLoad -HarnessArgs @("edit-save", $file4GB, "42")
    $results.Add($rEdit4GB)

    # Fase (d): Verify
    $rVerify4GB = Invoke-HarnessPhase -PhaseName "Verify" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -CpuPercentage $cpuLoad -HarnessArgs @("verify", $file4GB, "42")
    $results.Add($rVerify4GB)

    if ($rVerify4GB.Sha256Match -ne "True" -or $rVerify4GB.JournalDeleted -ne "True") {
        throw "FALHA: Verificacao independente de 4 GB reprovou (HashMatch=$($rVerify4GB.Sha256Match), JournalDeleted=$($rVerify4GB.JournalDeleted))!"
    }
    Write-Host "     Verificacao independente 4 GB: APROVADA (SHA-256 salvo: $($rVerify4GB.Sha256Saved), diario apagado: True)"

    # -------------------------------------------------------------
    # 5. Avaliacao do Criterio NFR-002 (Crescimento de Memoria)
    # -------------------------------------------------------------
    # Picos das fases do editor (Open, SeekEnd, EditAndSave)
    $maxEditor256_Pws = [math]::Max($rOpen256.PeakWorkingSet64_MB, [math]::Max($rSeek256.PeakWorkingSet64_MB, $rEdit256.PeakWorkingSet64_MB))
    $maxEditor4GB_Pws = [math]::Max($rOpen4GB.PeakWorkingSet64_MB, [math]::Max($rSeek4GB.PeakWorkingSet64_MB, $rEdit4GB.PeakWorkingSet64_MB))

    $maxEditor256_Priv = [math]::Max($rOpen256.PeakPrivateMemorySize64_MB, [math]::Max($rSeek256.PeakPrivateMemorySize64_MB, $rEdit256.PeakPrivateMemorySize64_MB))
    $maxEditor4GB_Priv = [math]::Max($rOpen4GB.PeakPrivateMemorySize64_MB, [math]::Max($rSeek4GB.PeakPrivateMemorySize64_MB, $rEdit4GB.PeakPrivateMemorySize64_MB))

    $growthRatioPws = [math]::Round($maxEditor4GB_Pws / [math]::Max(1.0, $maxEditor256_Pws), 2)
    $growthRatioPriv = [math]::Round($maxEditor4GB_Priv / [math]::Max(1.0, $maxEditor256_Priv), 2)

    Write-Host "`n================================================================="
    Write-Host "AVALIACAO DO CRITERIO NFR-002 (M2 Editor Hexadecimal):"
    Write-Host "Crescimento de tamanho do arquivo: 16.0x (256 MB -> 4 GB)"
    Write-Host "Pico WorkingSet (Editor): 256 MB = $maxEditor256_Pws MB | 4 GB = $maxEditor4GB_Pws MB (razao: ${growthRatioPws}x)"
    Write-Host "Pico PrivateMemory (Editor): 256 MB = $maxEditor256_Priv MB | 4 GB = $maxEditor4GB_Priv MB (razao: ${growthRatioPriv}x)"

    $criterionPassed = ($growthRatioPws -lt 2.0) -and ($growthRatioPriv -lt 2.0)
    if ($criterionPassed) {
        Write-Host "VEREDITO: APROVADO (NFR-002 ATENDIDO)" -ForegroundColor Green
        Write-Host "O consumo de memoria permaneceu constante (buffer LRU fixo e gravacao de blocos)."
    } else {
        Write-Host "VEREDITO: REPROVADO (BUG: Pico de memoria cresceu substancialmente com o arquivo)" -ForegroundColor Red
    }
    Write-Host "================================================================="

    # -------------------------------------------------------------
    # 6. Gravacao da Evidencia CSV
    # -------------------------------------------------------------
    $evidenceDir = [System.IO.Path]::GetFullPath($OutputDir)
    if (-not (Test-Path $evidenceDir)) {
        [System.IO.Directory]::CreateDirectory($evidenceDir) | Out-Null
    }

    $csvTimestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $csvFileName = "measure_hex_editor_$csvTimestamp.csv"
    $csvPath = Join-Path $evidenceDir $csvFileName

    # Preencher linha de sumario comparativo
    $summaryRow = [PSCustomObject]@{
        Timestamp = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
        Command = "NFR-002 Comparison (Hex Editor)"
        FileSizeLabel = "Comparison (4GB vs 256MB)"
        FileSizeBytes = $size4GB
        Phase = "NFR002_Ratio"
        DurationMs = 0
        DurationSec = 0
        PeakWorkingSet64_MB = $growthRatioPws
        PeakWorkingSet64_Bytes = 0
        PeakPrivateMemorySize64_MB = $growthRatioPriv
        PeakPrivateMemorySize64_Bytes = 0
        CpuLoadPercent = $cpuLoad
        Sha256Original = $shaOriginal4GB
        Sha256Saved = $rVerify4GB.Sha256Saved
        Sha256Expected = $rVerify4GB.Sha256Expected
        Sha256Match = $rVerify4GB.Sha256Match
        JournalDeleted = $rVerify4GB.JournalDeleted
        Status = if ($criterionPassed) { "PASS" } else { "BUG" }
    }
    $results.Add($summaryRow)

    $results | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
    Write-Host "`nEvidencia CSV gravada em: $csvPath"

    # Exibir tabela resumida
    Write-Host "`nTabela de Resultados:"
    $results | Format-Table FileSizeLabel, Phase, DurationSec, PeakWorkingSet64_MB, PeakPrivateMemorySize64_MB, Sha256Match, JournalDeleted, Status -AutoSize

}
finally {
    # -------------------------------------------------------------
    # 7. Limpeza Rigorosa de Temporarios
    # -------------------------------------------------------------
    Write-Host "`nExecutando limpeza de temporarios..."
    $guidBaseDir = [System.IO.Path]::Combine($env:TEMP, "agy", $guid)
    if (Test-Path $guidBaseDir) {
        Remove-Item -LiteralPath $guidBaseDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $agyRoot = [System.IO.Path]::Combine($env:TEMP, "agy")
    if (Test-Path $agyRoot) {
        $remainingItems = Get-ChildItem -Path $agyRoot -Force -ErrorAction SilentlyContinue
        if ($remainingItems.Count -eq 0) {
            Write-Host "Verificacao de %TEMP%\agy: VAZIO (100% limpo)."
            Remove-Item -LiteralPath $agyRoot -Force -ErrorAction SilentlyContinue
        } else {
            Write-Warning "Atencao: %TEMP%\agy contem $($remainingItems.Count) item(ns) remanescente(s):"
            $remainingItems | ForEach-Object { Write-Warning "  - $($_.FullName)" }
        }
    }
}
