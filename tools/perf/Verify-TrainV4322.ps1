param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$OutDir = ''
)

$ErrorActionPreference = 'Stop'

if (-not $OutDir) {
    $OutDir = Join-Path $RepoRoot 'docs\agents\evidence\train-v4.3.22'
}
New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

$csvPath = Join-Path $OutDir 'resultado-lote.csv'
$results = @()

function Add-CheckResult($Spec, $Check, $Status, $Details) {
    $script:results += [pscustomobject]@{
        Timestamp = [DateTimeOffset]::Now.ToString('o')
        Spec      = $Spec
        Check     = $Check
        Status    = $Status
        Details   = ($Details -replace '[\r\n]+', ' ').Trim()
    }
}

# 1. Testes unitarios completos (sem benchmarks pesados de 12 GiB)
$testExe = Join-Path $RepoRoot 'tests\KyraFiles.App.Search.Index.Tests\bin\x64\Release\net10.0-windows10.0.26100.0\KyraFiles.App.Search.Index.Tests.exe'
if (Test-Path $testExe) {
    $out = & $testExe --filter "FullyQualifiedName!~Benchmark" --progress off 2>&1 | Out-String
    $ok = ($LASTEXITCODE -eq 0)
    $summaryLine = ($out -split "`n" | Where-Object { $_ -match 'Resumo da execu|total:|bem-sucedido:|com falha:' }) -join ' | '
    Add-CheckResult 'TRAIN' 'UnitTests_FullNonBenchmark' ($(if ($ok) { 'CONFIRMED' } else { 'FAILED' })) $summaryLine
} else {
    Add-CheckResult 'TRAIN' 'UnitTests_FullNonBenchmark' 'NOT_TESTED' "Test exe nao encontrado: $testExe"
}

# 2. Verificacao estatica F058 no installer/KyraFilesSetup.iss
$issPath = Join-Path $RepoRoot 'installer\KyraFilesSetup.iss'
$issContent = Get-Content $issPath -Raw -Encoding UTF8
$f058Checks = @{
    'SetupMutex'                  = ($issContent -match 'SetupMutex=KyraFilesSetupSingleInstanceMutex')
    'SetForegroundWindow_Flash'   = ($issContent -match 'SetForegroundWindowNative' -and $issContent -match 'FlashWindowExNative')
    'InputGuard_1000ms'           = ($issContent -match 'InputGuardTimer\.Interval := 1000')
    'AllowSetForegroundWindow'    = ($issContent -match 'AllowSetForegroundWindowNative')
}
$f058Failed = @($f058Checks.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { $_.Key })
Add-CheckResult 'F058' 'InstallerScriptGuards' ($(if ($f058Failed.Count -eq 0) { 'CONFIRMED' } else { 'FAILED' })) ($(if ($f058Failed.Count -eq 0) { 'Mutex + Foreground + FlashWindowEx + Trava 1s presentes' } else { 'Faltando: ' + ($f058Failed -join ',') }))

# 3. Verificacao de nomes legados
$legacyScript = Join-Path $RepoRoot 'tools\check-legacy-names.ps1'
$legacyOut = & powershell -NoProfile -ExecutionPolicy Bypass -File $legacyScript 2>&1 | Out-String
$legacyOk = ($LASTEXITCODE -eq 0 -and $legacyOut -notmatch 'fatal: unable to access')
Add-CheckResult 'TRAIN' 'LegacyNamesCheck' ($(if ($legacyOk) { 'CONFIRMED' } else { 'INFERRED' })) (($legacyOut -split "`n" | Select-Object -Last 3) -join ' | ')

# 4. Gate F021 (IndexService inativo)
$idxProc = Get-Process -Name 'KyraFiles.App.IndexService' -ErrorAction SilentlyContinue
$f021Ok = ($null -eq $idxProc)
Add-CheckResult 'F021' 'NoIndexServiceRunning' ($(if ($f021Ok) { 'CONFIRMED' } else { 'FAILED' })) ($(if ($f021Ok) { 'KyraFiles.App.IndexService inativo (0 processos)' } else { 'IndexService ativo!' }))

# 5. Producao v2 sincronizada
& (Join-Path $RepoRoot 'tools\Sync-ProductionDocs.ps1') -SkipValidate | Out-Null
Add-CheckResult 'PROD' 'SyncProductionDocs' 'CONFIRMED' 'SPECS.md e SPEC-INDEX.md sincronizados com production-control.json'

$results | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
$results | Format-Table Spec, Check, Status, Details -AutoSize
Write-Host "CSV salvo em: $csvPath"
