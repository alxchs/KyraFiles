# Deploy-Release.ps1 - Atualiza %LOCALAPPDATA%\Programs\FilesCustom a partir da compilacao Release
param(
    [string]$Source = ""
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path

if ([string]::IsNullOrEmpty($Source)) {
    $branch = & git -C $repoRoot rev-parse --abbrev-ref HEAD 2>$null
    $tagRepo = if ($branch -and $branch -ne 'HEAD') { $branch -replace '[/\\:*?"<>|]', '-' } else { 'colab' }
    
    $rootDrive = [System.IO.Path]::GetPathRoot($repoRoot).Substring(0, 1).ToLower()
    $repoRel = $repoRoot.Substring([System.IO.Path]::GetPathRoot($repoRoot).Length).TrimStart('\', '/')

    $cands = @(
        (Join-Path $env:TEMP "Compiler\$rootDrive\$repoRel\$tagRepo\bin\KyraFiles.App\release_win-x64"),
        (Join-Path $env:TEMP "Compiler\$rootDrive\$repoRel\colab\bin\KyraFiles.App\release_win-x64")
    )

    foreach ($c in $cands) {
        if (Test-Path $c) {
            $Source = $c
            break
        }
    }
    if ([string]::IsNullOrEmpty($Source)) {
        $Source = $cands[0]
    }
}

$dst = Join-Path $env:LOCALAPPDATA "Programs\FilesCustom"

if (-not (Test-Path $Source)) {
    throw "Source not found: $Source"
}

Write-Host "Fonte do deploy: $Source" -ForegroundColor Cyan
Write-Host "Destino: $dst" -ForegroundColor Cyan

Write-Host "Encerrando processos de suporte..."
Get-Process KyraFiles.Watchdog, KyraFiles.HotkeyService, KyraFiles, Files.Watchdog, Files.HotkeyService, Files, Files.App.Server, KyraFiles.App.IndexService -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

Write-Host "Atualizando arquivos..."
# Se resources.pri estiver mapeado, renomeia antes de substituir
$priDst = Join-Path $dst "resources.pri"
$priOld = Join-Path $dst "resources.pri.old"
if (Test-Path $priDst) {
    if (Test-Path $priOld) {
        Remove-Item $priOld -Force -ErrorAction SilentlyContinue
    }
    try {
        Move-Item $priDst $priOld -Force -ErrorAction SilentlyContinue
    } catch {}
}

# Robocopy todos os arquivos exceto PDBs
& robocopy $Source $dst /E /NFL /NDL /NJH /NJS /NC /NS /NP /XF *.pdb | Out-Null

# O servidor COM e o indexador sao compilados a parte (nao sao ProjectReference) e ficam ao lado do app, na pasta bin da compilacao
$binRoot = Split-Path (Split-Path $Source -Parent) -Parent
$serverSrc = Join-Path $binRoot "KyraFiles.App.Server\release_win-x64"
$indexSrc = Join-Path $binRoot "KyraFiles.App.IndexService\release"
if (-not (Test-Path (Join-Path $serverSrc "KyraFiles.App.Server.exe"))) { throw "Servidor COM nao encontrado em $serverSrc (compile a solucao em Release)" }
if (-not (Test-Path (Join-Path $indexSrc "KyraFiles.App.IndexService.exe"))) { throw "Indexador nao encontrado em $indexSrc (compile a solucao em Release)" }
& robocopy $serverSrc (Join-Path $dst "KyraFiles.App.Server") /E /NFL /NDL /NJH /NJS /NC /NS /NP /XF *.pdb | Out-Null
& robocopy $indexSrc (Join-Path $dst "KyraFiles.App.IndexService") /E /NFL /NDL /NJH /NJS /NC /NS /NP /XF *.pdb | Out-Null

# Restos do nome antigo de deploys anteriores (os assemblies Files.App.BackgroundTasks e a pasta do servidor ainda usam esse nome no produto)
Get-ChildItem $dst -File -Force | Where-Object { $_.Name -like 'Files.*' -or $_.Name -like '*.old' } | Remove-Item -Force -ErrorAction SilentlyContinue
foreach ($legacy in 'Files.App.Controls', 'Files.App.IndexService', 'Files.App.Server') { Remove-Item (Join-Path $dst $legacy) -Recurse -Force -ErrorAction SilentlyContinue }

# Copia servicos nativos KyraFiles
$binRelease = Join-Path $repoRoot "bin\Release"
if (Test-Path (Join-Path $binRelease "KyraFiles.HotkeyService.exe")) {
    Copy-Item (Join-Path $binRelease "KyraFiles.HotkeyService.exe") $dst -Force
}
if (Test-Path (Join-Path $binRelease "KyraFiles.Watchdog.exe")) {
    Copy-Item (Join-Path $binRelease "KyraFiles.Watchdog.exe") $dst -Force
}

Write-Host "Iniciando KyraFiles.Watchdog para Win+E..." -ForegroundColor Cyan
$watchdogPath = Join-Path $dst "KyraFiles.Watchdog.exe"
if (Test-Path $watchdogPath) {
    Start-Process -FilePath $watchdogPath -WindowStyle Hidden
}

Write-Host "Deploy de KyraFiles concluido com sucesso!" -ForegroundColor Green
