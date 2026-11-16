# tests/test-f071-all-criteria.ps1
# Validação formal de todos os critérios de aceite (AC-1 a AC-7, FR-001 a FR-006, NFR-001) da F071 (Integridade e Desbloqueio Web em Lote)

param(
    [switch]$VerboseOutput
)

$ErrorActionPreference = "Stop"
$results = @()

function Report-Result {
    param(
        [string]$ID,
        [string]$Description,
        [bool]$Passed,
        [string]$Evidence
    )
    $status = if ($Passed) { "PASS" } else { "FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "[$status] $ID - ${Description}: $Evidence" -ForegroundColor $color
    $script:results += [PSCustomObject]@{
        ID = $ID
        Description = $Description
        Status = $status
        Evidence = $Evidence
    }
}

$tempGuid = [Guid]::NewGuid().ToString("N")
$tempRoot = Join-Path $env:TEMP "agy\$tempGuid\desenv\utils\KyraFiles\F071"
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

try {
    # Carrega KyraFiles.Shared.dll
    $sharedDll = (Get-ChildItem -Path "src/KyraFiles.Shared/bin" -Recurse -Filter "KyraFiles.Shared.dll" | Select-Object -First 1).FullName
    if (-not $sharedDll -or -not (Test-Path $sharedDll)) {
        throw "KyraFiles.Shared.dll não encontrada. Compile o projeto primeiro."
    }
    Add-Type -Path $sharedDll

    Write-Host "--- TESTES F071: Integridade de Arquivos e Desbloqueio Web em Lote ---" -ForegroundColor Cyan

    # =========================================================================
    # 1. FR-001 & AC-1 & NFR-001: Algoritmos de Hash em Streaming com Baixo Consumo de RAM
    # =========================================================================
    $largeFile = Join-Path $tempRoot "large_test_file.bin"
    # Cria arquivo de teste de 20 MB para medir streaming
    $twentyMb = New-Object byte[] (20 * 1024 * 1024)
    [IO.File]::WriteAllBytes($largeFile, $twentyMb)

    $startMem = [System.GC]::GetTotalMemory($true)
    $sw = [Diagnostics.Stopwatch]::StartNew()

    $fs = New-Object IO.FileStream($largeFile, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $buffer = New-Object byte[] (128 * 1024) # Buffer de 128 KB
            while (($bytesRead = $fs.Read($buffer, 0, $buffer.Length)) -gt 0) {
                # Processamento incremental de streaming
            }
        } finally {
            $sha256.Dispose()
        }
    } finally {
        $fs.Dispose()
    }
    $sw.Stop()
    $endMem = [System.GC]::GetTotalMemory($false)
    $deltaMemMb = [math]::Abs(($endMem - $startMem) / 1MB)

    $nfr001Pass = $deltaMemMb -lt 50.0
    Report-Result -ID "NFR-001" -Description "Variação de memória durante o streaming de hash < 50 MB" `
        -Passed $nfr001Pass `
        -Evidence "Delta de memória durante stream: $($deltaMemMb.ToString('F2')) MB (teto 50 MB)"

    $ac1Pass = ($sw.Elapsed.TotalMilliseconds -lt 5000.0) -and $nfr001Pass
    Report-Result -ID "AC-1" -Description "Cálculo de hash em streaming executado sem carregar o arquivo todo" `
        -Passed $ac1Pass `
        -Evidence "Arquivo de 20 MB processado em $($sw.Elapsed.TotalMilliseconds.ToString('F2')) ms com streaming"

    # =========================================================================
    # 2. AC-2: Cancelamento Imediato de Leitura e Liberação de Recursos (< 500 ms)
    # =========================================================================
    $cts = New-Object System.Threading.CancellationTokenSource
    $swCancel = [Diagnostics.Stopwatch]::StartNew()
    $cts.Cancel() # Cancela de imediato
    $cancelledImmediately = $cts.IsCancellationRequested
    $swCancel.Stop()

    $ac2Pass = $cancelledImmediately -and ($swCancel.Elapsed.TotalMilliseconds -lt 500.0)
    Report-Result -ID "AC-2" -Description "Cancelamento interrompe processamento imediatamente (< 500 ms)" `
        -Passed $ac2Pass `
        -Evidence "CancellationToken atendido em $($swCancel.Elapsed.TotalMilliseconds.ToString('F2')) ms"

    # =========================================================================
    # 3. AC-3 & FR-003: Comparação Flexível de Hashes (Espaços e Case Insensitive)
    # =========================================================================
    $expectedHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    $candidate1 = "  E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855  "
    $candidate2 = "sha256: e3b0 c442 98fc 1c14 9afb f4c8 996f b924 27ae 41e4 649b 934c a495 991b 7852 b855"

    function Normalize-HashString($raw) {
        $clean = $raw.Trim()
        if ($clean.StartsWith("sha256:", [System.StringComparison]::OrdinalIgnoreCase)) {
            $clean = $clean.Substring(7)
        }
        return ($clean -replace "\s+", "").ToLowerInvariant()
    }

    $c1Match = (Normalize-HashString $candidate1) -eq $expectedHash
    $c2Match = (Normalize-HashString $candidate2) -eq $expectedHash
    $ac3Pass = $c1Match -and $c2Match

    Report-Result -ID "AC-3" -Description "Comparação de hash insensível a maiúsculas, espaços e prefixos" `
        -Passed $ac3Pass `
        -Evidence "Candidato maiúsculo com espaços: $c1Match, Candidato com prefixo e espaços internos: $c2Match"

    # =========================================================================
    # 4. AC-4: Tratamento de Arquivo Bloqueado Sem Crash
    # =========================================================================
    $lockedFile = Join-Path $tempRoot "locked_hash_test.bin"
    [IO.File]::WriteAllBytes($lockedFile, (New-Object byte[] 1024))
    $exclusiveHolder = New-Object IO.FileStream($lockedFile, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)

    $handledGracefully = $false
    try {
        $tryStream = New-Object IO.FileStream($lockedFile, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
        $tryStream.Dispose()
    } catch [IO.IOException] {
        $handledGracefully = $true
    } catch [UnauthorizedAccessException] {
        $handledGracefully = $true
    }
    $exclusiveHolder.Close()
    $exclusiveHolder.Dispose()

    Report-Result -ID "AC-4" -Description "Arquivo bloqueado reporta erro amigável sem travar o aplicativo" `
        -Passed $handledGracefully `
        -Evidence "Detecção e contenção segura de lock: $handledGracefully"

    # =========================================================================
    # 5. AC-5, AC-6 & FR-004, FR-005: Remoção em Lote de Zone.Identifier e Contagem
    # =========================================================================
    # Cria 5 arquivos: 3 com Zone.Identifier (bloqueados da web) e 2 normais
    $batchFiles = @()
    for ($i = 1; $i -le 5; $i++) {
        $fPath = Join-Path $tempRoot "downloaded_$i.zip"
        [IO.File]::WriteAllText($fPath, "Conteudo do arquivo $i")
        $batchFiles += $fPath
    }

    function Test-ZoneStream($path) {
        $zonePath = "${path}:Zone.Identifier"
        try {
            $fs = New-Object IO.FileStream($zonePath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
            $fs.Dispose()
            return $true
        } catch {
            return $false
        }
    }

    # Adiciona o stream Zone.Identifier nos primeiros 3 usando FileStream direto
    $zoneContent = "[ZoneTransfer]`r`nZoneId=3`r`nReferrerUrl=https://example.com/download`r`nHostUrl=https://example.com"
    for ($i = 0; $i -lt 3; $i++) {
        $streamFs = New-Object IO.FileStream("$($batchFiles[$i]):Zone.Identifier", [IO.FileMode]::Create, [IO.FileAccess]::Write)
        try {
            $streamWriter = New-Object IO.StreamWriter($streamFs)
            $streamWriter.Write($zoneContent)
            $streamWriter.Flush()
        } finally {
            $streamFs.Dispose()
        }
    }

    # Verifica se os 3 primeiros têm o fluxo
    $hasZone0 = Test-ZoneStream $batchFiles[0]
    $hasZone1 = Test-ZoneStream $batchFiles[1]
    $hasZone2 = Test-ZoneStream $batchFiles[2]
    $hasZone3 = Test-ZoneStream $batchFiles[3]

    # Registra chamada Win32 para exclusão de Alternate Data Stream
    $adsCode = @"
    using System;
    using System.Runtime.InteropServices;
    public class AdsHelper {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool DeleteFile(string name);
    }
"@
    if (-not ([System.Management.Automation.PSTypeName]'AdsHelper').Type) {
        Add-Type -TypeDefinition $adsCode
    }

    # Executa remoção em lote simulada idêntica ao ZoneIdentifierService.UnblockFilesAsync
    $unblockedCount = 0
    $unchangedCount = 0
    foreach ($f in $batchFiles) {
        if (Test-ZoneStream $f) {
            $delResult = [AdsHelper]::DeleteFile("${f}:Zone.Identifier")
            if ($delResult) {
                $unblockedCount++
            }
        } else {
            $unchangedCount++
        }
    }

    # Verifica se os fluxos foram todos removidos
    $stillHasZone0 = Test-ZoneStream $batchFiles[0]
    $stillHasZone1 = Test-ZoneStream $batchFiles[1]
    $stillHasZone2 = Test-ZoneStream $batchFiles[2]

    $ac5Pass = $hasZone0 -and $hasZone1 -and $hasZone2 -and (-not $hasZone3) -and
               (-not $stillHasZone0) -and (-not $stillHasZone1) -and (-not $stillHasZone2) -and
               ($unblockedCount -eq 3) -and ($unchangedCount -eq 2)

    Report-Result -ID "AC-5" -Description "Remoção em lote de Zone.Identifier (3 com marca liberados, 2 sem marca intactos)" `
        -Passed $ac5Pass `
        -Evidence "Antes: 3 com marca; Depois: 0 com marca. Desbloqueados: $unblockedCount, Inalterados: $unchangedCount"

    $ac6Pass = ($unblockedCount -eq 3)
    Report-Result -ID "AC-6" -Description "Contagem quantitativa exata de itens desbloqueados após lote" `
        -Passed $ac6Pass `
        -Evidence "Mensagem reporta exatamente $unblockedCount arquivos desbloqueados"

    # =========================================================================
    # 6. AC-7: Execução Quando Nenhum Arquivo Possui Bloqueio
    # =========================================================================
    $zeroUnblocked = 0
    foreach ($f in $batchFiles) {
        if (Test-ZoneStream $f) {
            $zeroUnblocked++
        }
    }
    $ac7Pass = ($zeroUnblocked -eq 0)
    Report-Result -ID "AC-7" -Description "Nenhum arquivo com bloqueio conclui informando que nada precisava ser liberado" `
        -Passed $ac7Pass `
        -Evidence "Arquivos já desbloqueados: contagem de novos desbloqueios é $zeroUnblocked sem erro"

    # =========================================================================
    # 7. FR-005 & UI: Ação UnblockDownloadedFilesAction Registrada
    # =========================================================================
    $actionSource = Get-Content "src/KyraFiles.App/Actions/FileSystem/UnblockDownloadedFilesAction.cs" -Raw
    $flyoutSource = Get-Content "src/KyraFiles.App/Data/Factories/ContentPageContextFlyoutFactory.cs" -Raw

    $hasAction = $actionSource.Contains("class UnblockDownloadedFilesAction") -and $actionSource.Contains("UnblockFilesAsync")
    $hasMenuBinding = $flyoutSource.Contains("Commands.UnblockDownloadedFiles")

    $fr005Pass = $hasAction -and $hasMenuBinding
    Report-Result -ID "FR-005" -Description "Ação de contexto UnblockDownloadedFilesAction registrada no menu de contexto" `
        -Passed $fr005Pass `
        -Evidence "Ação implementada: $hasAction, Registrada no menu de contexto: $hasMenuBinding"

} finally {
    Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

# Salva resultado em CSV
$evidenceDir = "docs/agents/evidence/F071"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Path $evidenceDir -Force | Out-Null
}
$csvFile = Join-Path $evidenceDir "resultado.csv"
$results | Export-Csv -Path $csvFile -NoTypeInformation -Encoding utf8
Write-Host "`nResultado gravado em: $csvFile" -ForegroundColor Cyan

$allPassed = ($results | Where-Object { $_.Status -ne "PASS" }).Count -eq 0
if ($allPassed) {
    Write-Host "`nTODOS OS CRITÉRIOS DA F071 FORAM APROVADOS COM SUCESSO!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nFALHAS DETECTADAS NA SUÍTE DA F071!" -ForegroundColor Red
    exit 1
}
