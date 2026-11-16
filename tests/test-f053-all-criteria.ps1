# test-f053-all-criteria.ps1
# Script de validação completa de critérios F053 (Codificação e Fim de Linha)

param (
    [string]$SharedDll = "c:\desenv\utils\KyraFiles\src\KyraFiles.Shared\bin\x64\Debug\net10.0-windows10.0.26100.0\KyraFiles.Shared.dll"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Bateria de Testes F053: Codificação e Fim de Linha (EOL)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (-not (Test-Path $SharedDll)) {
    # Tenta achar a dll compilada
    $dllCandidate = Get-ChildItem -Path "c:\desenv\utils\KyraFiles\src\KyraFiles.Shared\bin" -Filter "KyraFiles.Shared.dll" -Recurse | Select-Object -First 1
    if ($dllCandidate) {
        $SharedDll = $dllCandidate.FullName
    } else {
        Write-Error "KyraFiles.Shared.dll não encontrada em $SharedDll"
    }
}

Write-Host "Carregando assembly: $SharedDll" -ForegroundColor Gray
Add-Type -Path $SharedDll

$tempDir = Join-Path $env:TEMP ("kyrafiles_f053_test_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Record-Result {
    param($Id, $Desc, $Passed, $Details)
    $statusStr = if ($Passed) { "PASS" } else { "FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "[$statusStr] ${Id}: $Desc - $Details" -ForegroundColor $color
    $results.Add([PSCustomObject]@{
        Id = $Id
        Description = $Desc
        Status = $statusStr
        Details = $Details
    })
}

try {
    # ----------------------------------------------------
    # AC-1: Detecção de UTF-8, UTF-8 BOM, CRLF e Binários
    # ----------------------------------------------------
    $utf8CrlfFile = Join-Path $tempDir "utf8_crlf.txt"
    $utf8Bytes = [System.Text.Encoding]::UTF8.GetBytes("Olá Mundo!`r`nSegunda Linha`r`n")
    [System.IO.File]::WriteAllBytes($utf8CrlfFile, $utf8Bytes)

    $bomLfFile = Join-Path $tempDir "bom_lf.txt"
    $bomBytes = [System.Text.Encoding]::UTF8.GetPreamble() + [System.Text.Encoding]::UTF8.GetBytes("Header com BOM`nSegunda linha LF`n")
    [System.IO.File]::WriteAllBytes($bomLfFile, $bomBytes)

    $binFile = Join-Path $tempDir "binary.bin"
    $binBytes = [byte[]](0x00, 0x01, 0x02, 0xFF, 0xFE, 0x00, 0x55)
    [System.IO.File]::WriteAllBytes($binFile, $binBytes)

    $info1 = [KyraFiles.Shared.Helpers.TextEncodingHelper]::GetFileInfo($utf8CrlfFile)
    $ac1_1 = ($info1.Encoding -eq "UTF-8") -and ($info1.LineEnding -eq "CRLF") -and ($info1.IsText -eq $true)

    $info2 = [KyraFiles.Shared.Helpers.TextEncodingHelper]::GetFileInfo($bomLfFile)
    $ac1_2 = ($info2.Encoding.StartsWith("UTF-8 BOM")) -and ($info2.LineEnding -eq "LF") -and ($info2.IsText -eq $true)

    $info3 = [KyraFiles.Shared.Helpers.TextEncodingHelper]::GetFileInfo($binFile)
    $ac1_3 = ($info3.IsText -eq $false)

    Record-Result "AC-1" "Detecção de Encodings e Linhas" ($ac1_1 -and $ac1_2 -and $ac1_3) "UTF-8/CRLF: $ac1_1, BOM/LF: $ac1_2, Binário: $ac1_3"

    # ----------------------------------------------------
    # AC-2: Detecção de quebras mistas -> Misto
    # ----------------------------------------------------
    $mixedFile = Join-Path $tempDir "mixed.txt"
    $mixedBytes = [System.Text.Encoding]::UTF8.GetBytes("Linha 1`r`nLinha 2`nLinha 3`r`n")
    [System.IO.File]::WriteAllBytes($mixedFile, $mixedBytes)

    $infoMixed = [KyraFiles.Shared.Helpers.TextEncodingHelper]::GetFileInfo($mixedFile)
    $ac2 = ($infoMixed.LineEnding -eq "Misto") -and ($infoMixed.IsText -eq $true)
    Record-Result "AC-2" "Detecção de quebras mistas" $ac2 "LineEnding: $($infoMixed.LineEnding)"

    # ----------------------------------------------------
    # AC-3: Conversão para CRLF e LF preservando o texto
    # ----------------------------------------------------
    $convFile = Join-Path $tempDir "convert_test.txt"
    [System.IO.File]::WriteAllBytes($convFile, $mixedBytes)

    # Converte para CRLF
    $taskCrlf = [KyraFiles.Shared.Helpers.BatchTextConverter]::ConvertToCrlfAsync([string[]]@($convFile))
    $resCrlf = $taskCrlf.GetAwaiter().GetResult()
    $infoAfterCrlf = [KyraFiles.Shared.Helpers.TextEncodingHelper]::GetFileInfo($convFile)
    $isCrlf = ($infoAfterCrlf.LineEnding -eq "CRLF") -and ($resCrlf.ConvertedCount -eq 1)

    # Converte para LF
    $taskLf = [KyraFiles.Shared.Helpers.BatchTextConverter]::ConvertToLfAsync([string[]]@($convFile))
    $resLf = $taskLf.GetAwaiter().GetResult()
    $infoAfterLf = [KyraFiles.Shared.Helpers.TextEncodingHelper]::GetFileInfo($convFile)
    $isLf = ($infoAfterLf.LineEnding -eq "LF") -and ($resLf.ConvertedCount -eq 1)

    # Verifica se o texto sobreviveu íntegro
    $content = [System.IO.File]::ReadAllText($convFile)
    $textPreserved = $content.Contains("Linha 1") -and $content.Contains("Linha 2") -and $content.Contains("Linha 3")

    Record-Result "AC-3" "Conversão bidirecional CRLF / LF" ($isCrlf -and $isLf -and $textPreserved) "CRLF: $isCrlf, LF: $isLf, Conteúdo íntegro: $textPreserved"

    # ----------------------------------------------------
    # AC-4: Arquivo bloqueado pulado sem interrupção
    # ----------------------------------------------------
    $lockedFile = Join-Path $tempDir "locked.txt"
    $freeFile = Join-Path $tempDir "free.txt"
    [System.IO.File]::WriteAllText($lockedFile, "Linha travada`n")
    [System.IO.File]::WriteAllText($freeFile, "Linha livre`n")

    $lockStream = [System.IO.File]::Open($lockedFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $taskLock = [KyraFiles.Shared.Helpers.BatchTextConverter]::ConvertToCrlfAsync([string[]]@($lockedFile, $freeFile))
        $resLock = $taskLock.GetAwaiter().GetResult()
        $ac4 = ($resLock.ConvertedCount -eq 1) -and ($resLock.FailedCount -eq 1)
        Record-Result "AC-4" "Tolerância a arquivo bloqueado/em uso" $ac4 "Convertido: $($resLock.ConvertedCount), Falhou: $($resLock.FailedCount)"
    } finally {
        $lockStream.Dispose()
    }

    # ----------------------------------------------------
    # AC-5: Reversão byte a byte via desfazer
    # ----------------------------------------------------
    $undoFile = Join-Path $tempDir "undo_test.txt"
    $origContent = "Texto original com CRLF`r`nSegunda Linha`r`nTerceira Linha`r`n"
    [System.IO.File]::WriteAllText($undoFile, $origContent)
    $origSha = (Get-FileHash -Path $undoFile -Algorithm SHA256).Hash

    # Converte para LF
    $tConv = [KyraFiles.Shared.Helpers.BatchTextConverter]::ConvertToLfAsync([string[]]@($undoFile))
    $tConv.GetAwaiter().GetResult() | Out-Null

    $afterConvSha = (Get-FileHash -Path $undoFile -Algorithm SHA256).Hash
    $changed = ($origSha -ne $afterConvSha)

    # Executa Desfazer
    $tUndo = [KyraFiles.Shared.Helpers.BatchTextConverter]::UndoLastConversionAsync()
    $restoredCount = $tUndo.GetAwaiter().GetResult()
    $revertedSha = (Get-FileHash -Path $undoFile -Algorithm SHA256).Hash
    $ac5 = $changed -and ($restoredCount -eq 1) -and ($origSha -eq $revertedSha)

    Record-Result "AC-5" "Desfazer conversão byte a byte (Undo)" $ac5 "Alterou antes: $changed, Restaurou: $restoredCount, Hash idêntico: $($origSha -eq $revertedSha)"

    # ----------------------------------------------------
    # AC-6: Performance e Cache para milhares de itens
    # ----------------------------------------------------
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($i = 0; $i -lt 1000; $i++) {
        [KyraFiles.Shared.Helpers.TextEncodingHelper]::GetFileInfo($utf8CrlfFile) | Out-Null
    }
    $sw.Stop()
    $ac6 = ($sw.ElapsedMilliseconds -lt 100) # 1000 consultas no cache < 100ms
    Record-Result "AC-6" "Performance com Cache (1000 chamadas)" $ac6 "Tempo: $($sw.ElapsedMilliseconds) ms"

    # ----------------------------------------------------
    # AC-7: Rejeição explicada para arquivos não-texto
    # ----------------------------------------------------
    $tRej = [KyraFiles.Shared.Helpers.BatchTextConverter]::ConvertToCrlfAsync([string[]]@($binFile))
    $resRej = $tRej.GetAwaiter().GetResult()
    $ac7 = ($resRej.ConvertedCount -eq 0) -and ($resRej.SkippedCount -eq 1) -and ($resRej.SkippedReasons.Count -gt 0)
    Record-Result "AC-7" "Rejeição explicada de binários/não-texto" $ac7 "Skipped: $($resRej.SkippedCount), Motivo: $($resRej.SkippedReasons[0])"

    # ----------------------------------------------------
    # NFR-001: Streaming / buffer de 4 KB para arquivo grande
    # ----------------------------------------------------
    $largeFile = Join-Path $tempDir "large_test.txt"
    # Cria arquivo de 5 MB
    $lineChunk = "Linha de teste repetida para encher o arquivo rapidamente.`r`n"
    $sb = [System.Text.StringBuilder]::new(5 * 1024 * 1024)
    while ($sb.Length -lt (5 * 1024 * 1024)) {
        $sb.Append($lineChunk) | Out-Null
    }
    [System.IO.File]::WriteAllText($largeFile, $sb.ToString())

    $swLarge = [System.Diagnostics.Stopwatch]::StartNew()
    $largeInfo = [KyraFiles.Shared.Helpers.TextEncodingHelper]::GetFileInfo($largeFile)
    $swLarge.Stop()

    $nfr1 = ($swLarge.ElapsedMilliseconds -lt 50) -and ($largeInfo.LineEnding -eq "CRLF") -and ($largeInfo.IsText -eq $true)
    Record-Result "NFR-001" "Análise streaming (4 KB) em arquivo de 5 MB" $nfr1 "Tempo: $($swLarge.ElapsedMilliseconds) ms, LineEnding: $($largeInfo.LineEnding)"

} finally {
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "==========================================================" -ForegroundColor Cyan
$failedCount = ($results | Where-Object { $_.Status -eq "FAIL" }).Count
if ($failedCount -eq 0) {
    Write-Host "RESULTADO: TODOS OS CRITÉRIOS PASSARAM COM SUCESSO! (Total: $($results.Count))" -ForegroundColor Green
    exit 0
} else {
    Write-Host "RESULTADO: $failedCount CRITÉRIO(S) FALHARAM!" -ForegroundColor Red
    exit 1
}
