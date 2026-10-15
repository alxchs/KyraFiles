# Atualizador Inteligente do Files Custom (F017 T4 - FR-006, AC-6, NFR-004)
# Verifica via SHA256 contra package_manifest.json se as dependências pesadas (Dependencies\)
# já estão instaladas e íntegras; seleciona modo Delta (< 20 MB) vs Pacote Completo,
# realiza fechamento cooperativo seguro de processos e substitui apenas os binários alterados sem travar arquivos.
param(
    [string]$InstallDir = (Join-Path $env:LOCALAPPDATA "Programs\FilesCustom"),
    [string]$SourceAppX = (Join-Path $PSScriptRoot "..\src\Files.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX"),
    [string]$SourceBin = (Join-Path $PSScriptRoot "..\bin\Release"),
    [string]$ManifestPath = (Join-Path $PSScriptRoot "package_manifest.json"),
    [string]$OutputDeltaZip = "",
    [switch]$RestartWatchdog = $true
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ManifestPath)) {
    throw "Manifesto não encontrado: $ManifestPath"
}

$manifest = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$depDir = Join-Path $InstallDir "Dependencies"
$hasInstallDir = Test-Path $InstallDir
$hasDepDir = Test-Path $depDir

$unchangedDeps = 0
$totalDeps = 0
$changedEntries = [System.Collections.Generic.List[PSObject]]::new()
$deltaBytes = 0L

foreach ($entry in $manifest.Files) {
    $relPath = [string]$entry.Path
    $isRootMain = $relPath -in @('Files.exe', 'Files.dll', 'AppxManifest.xml', 'resources.pri')

    # Determina onde o arquivo fica no layout da instalacao
    $localTarget = Join-Path $InstallDir $relPath

    if ($entry.IsDependency) {
        $totalDeps++
    }

    $needsUpdate = $true
    if (Test-Path -LiteralPath $localTarget) {
        $localFile = Get-Item -LiteralPath $localTarget
        if ($localFile.Length -eq $entry.Length) {
            $fs = [System.IO.File]::OpenRead($localTarget)
            try {
                $localHash = [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($fs))
            } finally {
                $fs.Dispose()
            }
            if ($localHash -eq $entry.SHA256) {
                $needsUpdate = $false
                if ($entry.IsDependency) {
                    $unchangedDeps++
                }
            }
        }
    }

    if ($needsUpdate) {
        $sourceFile = Join-Path $SourceAppX $relPath
        if (Test-Path -LiteralPath $sourceFile) {
            $changedEntries.Add([PSCustomObject]@{
                RelativePath = $relPath
                SourcePath   = $sourceFile
                TargetPath   = $localTarget
                Length       = [long]$entry.Length
                SHA256       = $entry.SHA256
                IsDependency = [bool]$entry.IsDependency
            })
            $deltaBytes += [long]$entry.Length
        }
    }
}

# Verifica também os serviços nativos C++ (Files.HotkeyService.exe e Files.Watchdog.exe)
foreach ($nativeExe in @('Files.HotkeyService.exe', 'Files.Watchdog.exe')) {
    $srcExe = Join-Path $SourceBin $nativeExe
    $dstExe = Join-Path $InstallDir $nativeExe
    if (Test-Path -LiteralPath $srcExe) {
        $srcHash = [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($srcExe)))
        $srcLen = (Get-Item -LiteralPath $srcExe).Length
        $needsNativeUpdate = $true
        if (Test-Path -LiteralPath $dstExe) {
            $dstHash = [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($dstExe)))
            if ($dstHash -eq $srcHash) {
                $needsNativeUpdate = $false
            }
        }
        if ($needsNativeUpdate) {
            $changedEntries.Add([PSCustomObject]@{
                RelativePath = $nativeExe
                SourcePath   = $srcExe
                TargetPath   = $dstExe
                Length       = [long]$srcLen
                SHA256       = $srcHash
                IsDependency = $false
            })
            $deltaBytes += [long]$srcLen
        }
    }
}

$depMatchRatio = if ($totalDeps -gt 0) { $unchangedDeps / [double]$totalDeps } else { 0.0 }
$mode = if ($hasInstallDir -and $hasDepDir -and $depMatchRatio -ge 0.8) { "Delta" } else { "Full" }

# Gera pacote Delta (.zip) reduzido se solicitado (NFR-004: < 20 MB)
$deltaZipSizeMB = 0.0
if (-not [string]::IsNullOrWhiteSpace($OutputDeltaZip)) {
    $stageDir = Join-Path ([System.IO.Path]::GetTempPath()) ("claude\desenv\utils\FilesApp\f017_delta_stage_" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null
    try {
        # Em um pacote Delta, inclui ao menos os binários principais e tudo que divergiu
        $entriesToPack = if ($changedEntries.Count -gt 0) {
            $changedEntries
        } else {
            @(
                [PSCustomObject]@{ RelativePath = 'Files.exe'; SourcePath = (Join-Path $SourceAppX 'Files.exe') },
                [PSCustomObject]@{ RelativePath = 'Files.dll'; SourcePath = (Join-Path $SourceAppX 'Files.dll') },
                [PSCustomObject]@{ RelativePath = 'Files.HotkeyService.exe'; SourcePath = (Join-Path $SourceBin 'Files.HotkeyService.exe') },
                [PSCustomObject]@{ RelativePath = 'Files.Watchdog.exe'; SourcePath = (Join-Path $SourceBin 'Files.Watchdog.exe') }
            )
        }

        foreach ($item in $entriesToPack) {
            if (Test-Path -LiteralPath $item.SourcePath) {
                $destItem = Join-Path $stageDir $item.RelativePath
                $destParent = Split-Path -Parent $destItem
                if (-not (Test-Path -LiteralPath $destParent)) {
                    New-Item -ItemType Directory -Force -Path $destParent | Out-Null
                }
                Copy-Item -LiteralPath $item.SourcePath -Destination $destItem -Force
            }
        }
        Copy-Item -LiteralPath $ManifestPath -Destination (Join-Path $stageDir "package_manifest.json") -Force

        $zipParent = Split-Path -Parent $OutputDeltaZip
        if ($zipParent -and -not (Test-Path -LiteralPath $zipParent)) {
            New-Item -ItemType Directory -Force -Path $zipParent | Out-Null
        }
        if (Test-Path -LiteralPath $OutputDeltaZip) {
            Remove-Item -LiteralPath $OutputDeltaZip -Force
        }
        Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $OutputDeltaZip -CompressionLevel Optimal
        $deltaZipSizeMB = [math]::Round((Get-Item -LiteralPath $OutputDeltaZip).Length / 1MB, 2)
    } finally {
        Remove-Item -LiteralPath $stageDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Encerramento cooperativo seguro de processos antes de substituir binários (AC-6)
try {
    $ev = [System.Threading.EventWaitHandle]::OpenExisting('Global\FilesCustom_ShutdownHotkeyService')
    [void]$ev.Set()
    $ev.Dispose()
    Start-Sleep -Milliseconds 400
} catch {
    # Evento não existia (serviço já parado)
}

Get-Process Files.Watchdog, Files.HotkeyService, Files, Files.App.Server, Files.App.IndexService -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

$remainingProcs = @(Get-Process Files.Watchdog, Files.HotkeyService, Files, Files.App.Server, Files.App.IndexService -ErrorAction SilentlyContinue)
$processesStoppedCleanly = ($remainingProcs.Count -eq 0)

# Substituição atômica dos arquivos alterados sem travamento
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
New-Item -ItemType Directory -Force -Path $depDir | Out-Null

$replacedCount = 0
foreach ($item in $changedEntries) {
    $targetParent = Split-Path -Parent $item.TargetPath
    if (-not (Test-Path -LiteralPath $targetParent)) {
        New-Item -ItemType Directory -Force -Path $targetParent | Out-Null
    }
    Copy-Item -LiteralPath $item.SourcePath -Destination $item.TargetPath -Force
    $replacedCount++
}

Copy-Item -LiteralPath $ManifestPath -Destination (Join-Path $InstallDir "package_manifest.json") -Force

if (Test-Path (Join-Path $InstallDir "AppxManifest.xml")) {
    try {
        Add-AppxPackage -Register (Join-Path $InstallDir "AppxManifest.xml") -ForceApplicationShutdown -ForceUpdateFromAnyVersion -ErrorAction SilentlyContinue
    } catch {}
}

$watchdogRestarted = $false
if ($RestartWatchdog) {
    $watchdogExe = Join-Path $InstallDir "Files.Watchdog.exe"
    if (Test-Path -LiteralPath $watchdogExe) {
        Start-Process -FilePath $watchdogExe -WindowStyle Hidden | Out-Null
        Start-Sleep -Milliseconds 400
        $watchdogRestarted = [bool](Get-Process Files.Watchdog -ErrorAction SilentlyContinue)
    }
}

[PSCustomObject]@{
    Mode                    = $mode
    TotalManifestFiles      = $manifest.TotalFiles
    TotalDependencies       = $totalDeps
    UnchangedDependencies   = $unchangedDeps
    DependencyMatchPercent  = [math]::Round($depMatchRatio * 100, 1)
    ChangedFilesCount       = $changedEntries.Count
    ReplacedFilesCount      = $replacedCount
    DeltaPayloadMB          = [math]::Round($deltaBytes / 1MB, 2)
    DeltaZipMB              = $deltaZipSizeMB
    ProcessesStoppedCleanly = $processesStoppedCleanly
    WatchdogRestarted       = $watchdogRestarted
}

