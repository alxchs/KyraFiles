<#
.SYNOPSIS
    M1-T3 (NFR-002): Medição de pico de memória e tempo para Dividir (SplitEngine) e Juntar (CombineEngine).
.DESCRIPTION
    Executa medições de 256 MB e 4 GB em blocos, amostrando PeakWorkingSet64 e PrivateMemorySize64
    a cada 100 ms, conferindo a integridade por SHA-256 e gerando evidência CSV com timestamp.
#>

[CmdletBinding()]
param(
    [string]$OutputDir = "$PSScriptRoot/../../docs/agents/evidence/F060",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

Write-Host "================================================================="
Write-Host "F060 M1-T3 (NFR-002): Medicao de Desempenho e Memoria Split/Combine"
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

# 2. Compilacao do Harness .NET
$harnessDir = "$PSScriptRoot/SplitCombineHarness"
$harnessCsproj = "$harnessDir/SplitCombineHarness.csproj"
$harnessExe = "$harnessDir/bin/x64/Release/net10.0/SplitCombineHarness.exe"

if (-not $SkipBuild -or -not (Test-Path $harnessExe)) {
    Write-Host "`nCompilando SplitCombineHarness (Release x64)..."
    $buildOutput = dotnet build "$harnessCsproj" -c Release -p:Platform=x64 -v:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao compilar SplitCombineHarness. Codigo de saida: $LASTEXITCODE`n$buildOutput"
    }
    Write-Host "SplitCombineHarness compilado com sucesso."
}

if (-not (Test-Path $harnessExe)) {
    throw "Executavel do harness nao encontrado: $harnessExe"
}

# 3. Configuracao do Diretorio Temporario
$guid = [guid]::NewGuid().ToString()
$tempRoot = [System.IO.Path]::Combine($env:TEMP, "agy", $guid, "desenv", "utils", "FilesApp-f060-m1", "Files")
Write-Host "`nDiretorio temporario da sessao: $tempRoot"
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Invoke-HarnessPhase {
    param(
        [string]$PhaseName,
        [string]$FileSizeLabel,
        [long]$FileSizeBytes,
        [long]$PartSizeBytes,
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

    # Analisar saida estruturada
    $sha256 = ""
    $shaRecombined = ""
    $shaMatch = ""
    $outputLines = $stdout -split "`r?`n"
    foreach ($line in $outputLines) {
        if ($line -match "PHASE_OUTPUT:\s*(.+)") {
            $pairs = $matches[1] -split "\|"
            foreach ($pair in $pairs) {
                if ($pair -match "^\s*([^=]+)=(.*)$") {
                    $k = $matches[1].Trim()
                    $v = $matches[2].Trim()
                    if ($k -eq "Sha256" -or $k -eq "Sha256Original") { $sha256 = $v }
                    if ($k -eq "Sha256Recombined") { $shaRecombined = $v }
                    if ($k -eq "Matched") { $shaMatch = $v }
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
        PartSizeBytes = $PartSizeBytes
        Phase = $PhaseName
        DurationMs = $durationMs
        DurationSec = $durationSec
        PeakWorkingSet64_MB = $pwsMB
        PeakWorkingSet64_Bytes = $maxPws
        PeakPrivateMemorySize64_MB = $privMB
        PeakPrivateMemorySize64_Bytes = $maxPriv
        Sha256Original = $sha256
        Sha256Recombined = $shaRecombined
        Sha256Match = $shaMatch
        Status = "PASS"
    }

    return $row
}

try {
    # -------------------------------------------------------------
    # BATERIA 1: Rodada de Referencia (256 MB)
    # -------------------------------------------------------------
    $size256MB = 256L * 1024 * 1024
    $partSize256MB = 64L * 1024 * 1024 # 4 partes de 64 MB
    $dir256MB = Join-Path $tempRoot "test_256MB"
    $orig256MB = Join-Path $dir256MB "original.bin"
    $parts256MB = Join-Path $dir256MB "parts"
    $manifest256MB = Join-Path $parts256MB "original.bin.manifest.json"
    $combined256MB = Join-Path $dir256MB "combined"
    $recombined256MB = Join-Path $combined256MB "recombined.bin"

    Write-Host "`n>>> Executando Bateria de 256 MB (Referencia de Linha de Base)..."

    # Fase 1: Generate
    $rGen256 = Invoke-HarnessPhase -PhaseName "Generate" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -PartSizeBytes $partSize256MB -HarnessArgs @("generate", $orig256MB, $size256MB.ToString(), "42")
    $results.Add($rGen256)
    $shaOriginal256 = $rGen256.Sha256Original

    # Fase 2: Split
    $rSplit256 = Invoke-HarnessPhase -PhaseName "Split" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -PartSizeBytes $partSize256MB -HarnessArgs @("split", $orig256MB, $partSize256MB.ToString(), $parts256MB)
    $results.Add($rSplit256)

    # Fase 3: Combine
    $rCombine256 = Invoke-HarnessPhase -PhaseName "Combine" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -PartSizeBytes $partSize256MB -HarnessArgs @("combine", $manifest256MB, $combined256MB, "recombined.bin")
    $results.Add($rCombine256)

    # Fase 4: VerifyHash
    $rVerify256 = Invoke-HarnessPhase -PhaseName "VerifyHash" -FileSizeLabel "256 MB" -FileSizeBytes $size256MB -PartSizeBytes $partSize256MB -HarnessArgs @("verify-hash", $orig256MB, $recombined256MB)
    $results.Add($rVerify256)

    if ($rVerify256.Sha256Match -ne "True") {
        throw "FALHA: SHA-256 do arquivo recombinado de 256 MB diverge do original!"
    }
    Write-Host "     SHA-256 256 MB conferido com sucesso: $shaOriginal256"

    # Limpar arquivos de 256 MB para liberar espaco
    Remove-Item -LiteralPath $dir256MB -Recurse -Force

    # -------------------------------------------------------------
    # BATERIA 2: Rodada Alvo NFR-002 (4 GB)
    # -------------------------------------------------------------
    $size4GB = 4L * 1024 * 1024 * 1024 # 4 GB = 4.294.967.296 bytes
    $partSize4GB = 256L * 1024 * 1024 # partes de 256 MB = 16 partes
    $dir4GB = Join-Path $tempRoot "test_4GB"
    $orig4GB = Join-Path $dir4GB "original_4gb.bin"
    $parts4GB = Join-Path $dir4GB "parts"
    $manifest4GB = Join-Path $parts4GB "original_4gb.bin.manifest.json"
    $combined4GB = Join-Path $dir4GB "combined"
    $recombined4GB = Join-Path $combined4GB "recombined.bin"

    Write-Host "`n>>> Executando Bateria de 4 GB (Alvo NFR-002)..."

    # Fase 1: Generate
    $rGen4GB = Invoke-HarnessPhase -PhaseName "Generate" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -PartSizeBytes $partSize4GB -HarnessArgs @("generate", $orig4GB, $size4GB.ToString(), "42")
    $results.Add($rGen4GB)
    $shaOriginal4GB = $rGen4GB.Sha256Original

    # Fase 2: Split
    $rSplit4GB = Invoke-HarnessPhase -PhaseName "Split" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -PartSizeBytes $partSize4GB -HarnessArgs @("split", $orig4GB, $partSize4GB.ToString(), $parts4GB)
    $results.Add($rSplit4GB)

    # Fase 3: Combine
    $rCombine4GB = Invoke-HarnessPhase -PhaseName "Combine" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -PartSizeBytes $partSize4GB -HarnessArgs @("combine", $manifest4GB, $combined4GB, "recombined.bin")
    $results.Add($rCombine4GB)

    # Fase 4: VerifyHash
    $rVerify4GB = Invoke-HarnessPhase -PhaseName "VerifyHash" -FileSizeLabel "4 GB" -FileSizeBytes $size4GB -PartSizeBytes $partSize4GB -HarnessArgs @("verify-hash", $orig4GB, $recombined4GB)
    $results.Add($rVerify4GB)

    if ($rVerify4GB.Sha256Match -ne "True") {
        throw "FALHA: SHA-256 do arquivo recombinado de 4 GB diverge do original!"
    }
    Write-Host "     SHA-256 4 GB conferido com sucesso: $shaOriginal4GB"

    # -------------------------------------------------------------
    # 4. Avaliacao do Criterio NFR-002 (Crescimento de Memoria)
    # -------------------------------------------------------------
    $maxSplitCombine256_Pws = [math]::Max($rSplit256.PeakWorkingSet64_MB, $rCombine256.PeakWorkingSet64_MB)
    $maxSplitCombine4GB_Pws = [math]::Max($rSplit4GB.PeakWorkingSet64_MB, $rCombine4GB.PeakWorkingSet64_MB)

    $maxSplitCombine256_Priv = [math]::Max($rSplit256.PeakPrivateMemorySize64_MB, $rCombine256.PeakPrivateMemorySize64_MB)
    $maxSplitCombine4GB_Priv = [math]::Max($rSplit4GB.PeakPrivateMemorySize64_MB, $rCombine4GB.PeakPrivateMemorySize64_MB)

    $growthRatioPws = [math]::Round($maxSplitCombine4GB_Pws / [math]::Max(1.0, $maxSplitCombine256_Pws), 2)
    $growthRatioPriv = [math]::Round($maxSplitCombine4GB_Priv / [math]::Max(1.0, $maxSplitCombine256_Priv), 2)

    Write-Host "`n================================================================="
    Write-Host "AVALIACAO DO CRITERIO NFR-002:"
    Write-Host "Crescimento de tamanho do arquivo: 16.0x (256 MB -> 4 GB)"
    Write-Host "Pico WorkingSet (Split/Combine): 256 MB = $maxSplitCombine256_Pws MB | 4 GB = $maxSplitCombine4GB_Pws MB (razao: ${growthRatioPws}x)"
    Write-Host "Pico PrivateMemory (Split/Combine): 256 MB = $maxSplitCombine256_Priv MB | 4 GB = $maxSplitCombine4GB_Priv MB (razao: ${growthRatioPriv}x)"

    $criterionPassed = ($growthRatioPws -lt 2.0) -and ($growthRatioPriv -lt 2.0)
    if ($criterionPassed) {
        Write-Host "VEREDITO: APROVADO (NFR-002 ATENDIDO)" -ForegroundColor Green
        Write-Host "O consumo de memoria permaneceu constante (streaming em buffers fixos)."
    } else {
        Write-Host "VEREDITO: REPROVADO (BUG: Pico de memoria cresceu substancialmente com o arquivo)" -ForegroundColor Red
    }
    Write-Host "================================================================="

    # -------------------------------------------------------------
    # 5. Gravacao da Evidencia CSV
    # -------------------------------------------------------------
    $evidenceDir = [System.IO.Path]::GetFullPath($OutputDir)
    if (-not (Test-Path $evidenceDir)) {
        [System.IO.Directory]::CreateDirectory($evidenceDir) | Out-Null
    }

    $csvTimestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $csvFileName = "measure_split_combine_$csvTimestamp.csv"
    $csvPath = Join-Path $evidenceDir $csvFileName

    # Preencher linha de sumario comparativo
    $summaryRow = [PSCustomObject]@{
        Timestamp = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
        Command = "NFR-002 Comparison"
        FileSizeLabel = "Comparison (4GB vs 256MB)"
        FileSizeBytes = $size4GB
        PartSizeBytes = $partSize4GB
        Phase = "NFR002_Ratio"
        DurationMs = 0
        DurationSec = 0
        PeakWorkingSet64_MB = $growthRatioPws
        PeakWorkingSet64_Bytes = 0
        PeakPrivateMemorySize64_MB = $growthRatioPriv
        PeakPrivateMemorySize64_Bytes = 0
        Sha256Original = $shaOriginal4GB
        Sha256Recombined = $shaOriginal4GB
        Sha256Match = "True"
        Status = if ($criterionPassed) { "PASS" } else { "BUG" }
    }
    $results.Add($summaryRow)

    $results | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
    Write-Host "`nEvidencia CSV gravada em: $csvPath"

    # Exibir tabela resumida
    Write-Host "`nTabela de Resultados:"
    $results | Format-Table FileSizeLabel, Phase, DurationSec, PeakWorkingSet64_MB, PeakPrivateMemorySize64_MB, Sha256Match, Status -AutoSize

}
finally {
    # -------------------------------------------------------------
    # 6. Limpeza Rigorosa de Temporarios
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
        } else {
            Write-Warning "Atencao: %TEMP%\agy contem $($remainingItems.Count) item(ns) remanescente(s):"
            $remainingItems | ForEach-Object { Write-Warning "  - $($_.FullName)" }
        }
    }
}
