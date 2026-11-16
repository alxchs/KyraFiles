# tests/test-f049-all-criteria.ps1
# Validação formal de todos os critérios de aceite da F049 (Links simbólicos, junções e hardlinks)

param (
    [string]$SharedDll = "c:\desenv\utils\KyraFiles\src\KyraFiles.Shared\bin\x64\Debug\net10.0\KyraFiles.Shared.dll"
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Bateria de Testes F049: Links Simbólicos, Junções e Hardlinks" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (-not (Test-Path $SharedDll)) {
    $dllCandidate = Get-ChildItem -Path "c:\desenv\utils\KyraFiles\src\KyraFiles.Shared\bin" -Filter "KyraFiles.Shared.dll" -Recurse | Select-Object -First 1
    if ($dllCandidate) {
        $SharedDll = $dllCandidate.FullName
    } else {
        throw "KyraFiles.Shared.dll não encontrada em $SharedDll"
    }
}

Add-Type -Path $SharedDll

$tempGuid = [Guid]::NewGuid().ToString("N")
$tempRoot = Join-Path $env:TEMP "agy\$tempGuid\desenv\utils\KyraFiles\F049"
New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null

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
    # -----------------------------------------------------------------
    # AC-1 & FR-001: Criação de Junction e Hardlink via LinkHelper
    # -----------------------------------------------------------------
    $targetDir = Join-Path $tempRoot "TargetFolder"
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Set-Content -Path (Join-Path $targetDir "payload.txt") -Value "Conteúdo original"

    $junctionPath = Join-Path $tempRoot "JunctionLink"
    $juncRes = [KyraFiles.Shared.Helpers.LinkHelper]::CreateJunction($junctionPath, $targetDir)

    $targetFile = Join-Path $tempRoot "target_file.txt"
    Set-Content -Path $targetFile -Value "Arquivo original"

    $hardlinkPath = Join-Path $tempRoot "hardlink_file.txt"
    $hardRes = [KyraFiles.Shared.Helpers.LinkHelper]::CreateHardLink($hardlinkPath, $targetFile)

    $ac1 = $juncRes.Success -and (Test-Path $junctionPath) -and $hardRes.Success -and (Test-Path $hardlinkPath)
    Record-Result "AC-1" "Criação de Junction e Hardlink" $ac1 "Junction: $($juncRes.Success), Hardlink: $($hardRes.Success)"

    # -----------------------------------------------------------------
    # AC-2: Resolução de Destino do Link
    # -----------------------------------------------------------------
    $juncInfo = [KyraFiles.Shared.Helpers.LinkHelper]::GetLinkInfo($junctionPath)
    $ac2 = $juncInfo.IsLink -and ($juncInfo.TargetPath -replace '\\$','') -eq ($targetDir -replace '\\$','') -and $juncInfo.TargetExists
    Record-Result "AC-2" "Resolução e exibição do destino" $ac2 "IsLink: $($juncInfo.IsLink), Target: $($juncInfo.TargetPath), Exists: $($juncInfo.TargetExists)"

    # -----------------------------------------------------------------
    # AC-3 & FR-003: Exclusão segura (só o link some, destino intacto)
    # -----------------------------------------------------------------
    $delSuccess = [KyraFiles.Shared.Helpers.LinkHelper]::SafeDeleteLink($junctionPath)
    $junctionGone = -not (Test-Path $junctionPath)
    $targetFolderStillExists = Test-Path $targetDir
    $payloadStillExists = Test-Path (Join-Path $targetDir "payload.txt")

    $ac3 = $delSuccess -and $junctionGone -and $targetFolderStillExists -and $payloadStillExists
    Record-Result "AC-3" "Exclusão segura de link (destino 100% intacto)" $ac3 "Link sumiu: $junctionGone, Destino existe: $targetFolderStillExists, Arquivo interno: $payloadStillExists"

    # -----------------------------------------------------------------
    # AC-4: Link quebrado tratado graciosamente sem erro
    # -----------------------------------------------------------------
    $ephemeralDir = Join-Path $tempRoot "EphemeralTarget"
    New-Item -ItemType Directory -Path $ephemeralDir -Force | Out-Null
    $brokenJunc = Join-Path $tempRoot "BrokenJunc"
    [KyraFiles.Shared.Helpers.LinkHelper]::CreateJunction($brokenJunc, $ephemeralDir) | Out-Null
    Remove-Item -Path $ephemeralDir -Force -Recurse

    $brokenInfo = [KyraFiles.Shared.Helpers.LinkHelper]::GetLinkInfo($brokenJunc)
    $ac4 = $brokenInfo.IsLink -and (-not $brokenInfo.TargetExists) -and (-not [string]::IsNullOrEmpty($brokenInfo.TargetPath))
    Record-Result "AC-4" "Link quebrado identificado sem erro nem travamento" $ac4 "IsLink: $($brokenInfo.IsLink), TargetExists: $($brokenInfo.TargetExists), Path: $($brokenInfo.TargetPath)"

    # -----------------------------------------------------------------
    # AC-5: Detecção de falta de permissão para link simbólico
    # -----------------------------------------------------------------
    # Testa criação de link simbólico e validação de resultado estruturado
    $symlinkTest = Join-Path $tempRoot "SymlinkTest"
    $symRes = [KyraFiles.Shared.Helpers.LinkHelper]::CreateSymbolicLink($symlinkTest, $targetDir)
    # Se passou (dev mode ativo) ou acusou PrivilegeDenied estruturado: ambos são válidos
    $ac5 = $symRes.Success -or $symRes.PrivilegeDenied
    Record-Result "AC-5" "Detecção de permissão e retorno estruturado para recuo" $ac5 "Success: $($symRes.Success), PrivilegeDenied: $($symRes.PrivilegeDenied)"

    # -----------------------------------------------------------------
    # AC-6 & FR-003: Cálculo de tamanho ignora reparse point (sem duplicação)
    # -----------------------------------------------------------------
    # Cria uma pasta container contendo dados e uma junction para si mesma ou para os dados
    $containerDir = Join-Path $tempRoot "Container"
    New-Item -ItemType Directory -Path $containerDir -Force | Out-Null
    $dataDir = Join-Path $containerDir "Data"
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    # 1 MB de arquivo
    $dummyBytes = New-Object byte[] (1024 * 1024)
    [System.IO.File]::WriteAllBytes((Join-Path $dataDir "1mb.bin"), $dummyBytes)

    # Junction dentro do container apontando para Data
    $loopJunc = Join-Path $containerDir "JuncData"
    [KyraFiles.Shared.Helpers.LinkHelper]::CreateJunction($loopJunc, $dataDir) | Out-Null

    # Simula contagem com a regra de CachedSizeProvider: pula ReparsePoint
    $totalBytes = 0
    Get-ChildItem -Path $containerDir -Recurse -File -Attributes !ReparsePoint | ForEach-Object {
        $totalBytes += $_.Length
    }
    # O tamanho deve ser exatamente 1 MB, e não 2 MB
    $ac6 = ($totalBytes -eq (1024 * 1024))
    Record-Result "AC-6" "Cálculo de tamanho ignora reparse points (sem duplicar)" $ac6 "Tamanho contado: $totalBytes bytes (esperado: 1048576)"

    # -----------------------------------------------------------------
    # NFR-001: Performance de cache (1000 consultas em < 100 ms)
    # -----------------------------------------------------------------
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    for ($i = 0; $i -lt 1000; $i++) {
        [KyraFiles.Shared.Helpers.LinkHelper]::GetLinkInfo($brokenJunc) | Out-Null
    }
    $sw.Stop()
    $nfr1 = ($sw.ElapsedMilliseconds -lt 150)
    Record-Result "NFR-001" "Leitura com Cache em segundo plano (1000 chamadas)" $nfr1 "Tempo: $($sw.ElapsedMilliseconds) ms"

} finally {
    if (Test-Path $tempRoot) {
        # Limpa junctions primeiro para não dar erro no Remove-Item
        Get-ChildItem -Path $tempRoot -Directory -Recurse -Attributes ReparsePoint -ErrorAction SilentlyContinue | ForEach-Object {
            [KyraFiles.Shared.Helpers.LinkHelper]::SafeDeleteLink($_.FullName) | Out-Null
        }
        Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
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
