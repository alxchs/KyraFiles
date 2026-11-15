# tools/Quick-Ship.ps1 - Compilação e Deploy Ágil (Modo Rápido e Modo Completo)
# Atende F093 (Item 2 da priorização do Alexandre)

[CmdletBinding()]
param(
    [switch]$Fast,
    [switch]$Full,
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$SkipInstaller,
    [switch]$SkipVerify
)

$ErrorActionPreference = 'Stop'
$sw = [System.Diagnostics.Stopwatch]::StartNew()

# Default para Fast se nenhum for passado
if (-not $Fast -and -not $Full) {
    $Fast = $true
}

$modeName = if ($Full) { "COMPLETO (Full/Release Oficial)" } else { "RÁPIDO (Fast/Dev Incremental)" }

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  KyraFiles Quick-Ship - Modo: $modeName" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

$repoRoot = (Split-Path $PSScriptRoot -Parent)
Set-Location $repoRoot

# 1. Encerra processos ativos com seguranca (estritamente restrito a KyraFiles/FilesCustom locais)
Write-Host "[1/5] Encerrando processos KyraFiles ativos..." -ForegroundColor Yellow
$processNames = @(
    "KyraFiles", "KyraFiles.App.Server", "KyraFiles.App.IndexService",
    "KyraFiles.Watchdog", "KyraFiles.HotkeyService",
    "Files", "Files.App.Server", "Files.Watchdog", "Files.HotkeyService"
)
$targetDirs = @(
    (Join-Path $env:LOCALAPPDATA "Programs\KyraFiles").ToLowerInvariant(),
    (Join-Path $env:LOCALAPPDATA "Programs\FilesCustom").ToLowerInvariant(),
    $repoRoot.ToLowerInvariant()
)
Get-Process -Name $processNames -ErrorAction SilentlyContinue | Where-Object {
    try {
        $pPath = $_.Path
        if ($pPath) {
            $pLower = $pPath.ToLowerInvariant()
            foreach ($td in $targetDirs) {
                if ($pLower.StartsWith($td)) { return $true }
            }
        }
    } catch {}
    return $false
} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 400

# 2. Testes (apenas no modo completo)
if ($Full) {
    Write-Host "[2/5] Executando bateria de testes unitarios (modo isolado)..." -ForegroundColor Yellow
    $testTarget = "tests/KyraFiles.App.Search.Index.Tests/KyraFiles.App.Search.Index.Tests.csproj"
    $testResult = & dotnet test $testTarget -p:Platform=$Platform -c $Configuration --no-restore 2>&1
    if ($LASTEXITCODE -ne 0) {
        $testResult | ForEach-Object { Write-Host $_ }
        throw "Testes falharam com codigo de saida $LASTEXITCODE"
    }
    Write-Host "Testes unitarios concluidos com sucesso." -ForegroundColor Green
} else {
    Write-Host "[2/5] Modo rapido: testes unitarios pulados para maxima agilidade." -ForegroundColor Gray
}

# 3. Compilacao .NET / WinUI 3
Write-Host "[3/5] Compilando KyraFiles ($Configuration|$Platform)..." -ForegroundColor Yellow
$buildTarget = if ($Full) { "KyraFiles.slnx" } else { "src/KyraFiles.App/KyraFiles.App.csproj" }
$buildResult = & dotnet build $buildTarget -c $Configuration -p:Platform=$Platform --no-restore -v:minimal -clp:ErrorsOnly 2>&1
if ($LASTEXITCODE -ne 0) {
    $buildResult | ForEach-Object { Write-Host $_ }
    throw "Compilacao falhou com codigo $LASTEXITCODE"
}
Write-Host "Compilacao concluida com sucesso." -ForegroundColor Green

# 4. Inno Setup (Geracao do Instalador)
$setupDir = Join-Path $repoRoot "artifacts\Setup"
if (-not $SkipInstaller) {
    Write-Host "[4/5] Gerando instalador oficial via Inno Setup..." -ForegroundColor Yellow
    $cmd = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    $isccCandidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )
    if ($cmd -and $cmd.Source) { $isccCandidates += $cmd.Source }
    $iscc = $isccCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

    if ($iscc -and (Test-Path $iscc)) {
        if (-not (Test-Path $setupDir)) {
            New-Item -ItemType Directory -Path $setupDir -Force | Out-Null
        }

        $issFile = Join-Path $repoRoot "installer\KyraFilesSetup.iss"
        $issArgs = if ($Fast) { @("/DFAST_COMPRESSION", $issFile) } else { @($issFile) }

        $isccResult = & $iscc @issArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            $isccResult | ForEach-Object { Write-Host $_ }
            throw "Geracao do instalador falhou com codigo $LASTEXITCODE"
        }

        # Purga segura: somente apos geracao comprovada do novo instalador, remove os mais antigos
        $latestSetup = Get-ChildItem -Path $setupDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latestSetup) {
            Get-ChildItem -Path $setupDir -Filter "*.exe" | Where-Object { $_.FullName -ne $latestSetup.FullName } | Remove-Item -Force -ErrorAction SilentlyContinue
            Write-Host "Instalador gerado com sucesso: $($latestSetup.Name) ($([math]::Round($latestSetup.Length / 1MB, 2)) MB)" -ForegroundColor Green
        }
    } else {
        Write-Warning "ISCC.exe nao encontrado. Geracao do instalador pulada."
    }
} else {
    Write-Host "[4/5] Geracao do instalador pulada pelo usuario." -ForegroundColor Gray
}

# 5. Deploy Local em %LOCALAPPDATA%\Programs\KyraFiles
Write-Host "[5/5] Realizando deploy em %LOCALAPPDATA%\Programs\KyraFiles..." -ForegroundColor Yellow
$deployScript = Join-Path $repoRoot "tools\Deploy-Release.ps1"
if (Test-Path $deployScript) {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $deployScript
    if ($LASTEXITCODE -ne 0) {
        throw "Deploy local falhou com codigo de saida $LASTEXITCODE"
    }
} else {
    Write-Warning "Deploy-Release.ps1 nao encontrado."
}

# 6. Validacao F021 (Contencao de Recursos)
if (-not $SkipVerify) {
    Write-Host "Verificando Gate F021 (IndexService inativo e CPU em repouso)..." -ForegroundColor Yellow
    $verifyScript = Join-Path $repoRoot "tools\perf\verify_no_index_service.ps1"
    if (Test-Path $verifyScript) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $verifyScript
        if ($LASTEXITCODE -ne 0) {
            throw "Gate F021 falhou com codigo de saida $LASTEXITCODE (IndexService ativo ou CPU > 5%)"
        }
    }
}

$sw.Stop()
$elapsed = $sw.Elapsed
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  Quick-Ship concluido em $($elapsed.Minutes)m $($elapsed.Seconds)s!" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Cyan
