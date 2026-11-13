# Prepara e valida o ambiente completo de compilação do KyraFiles na Heloize-PC
# Uso: pwsh tools\setup_compiler_environment.ps1 [-InstallMissing]

param(
    [switch]$InstallMissing
)

$ErrorActionPreference = "Continue"

Write-Host "=== Diagnostico de Ambiente de Compilacao KyraFiles ===" -ForegroundColor Cyan

# 1. .NET SDK
Write-Host "`n[1/4] Verificando .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($dotnetVersion) {
    Write-Host "  .NET SDK detectado: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "  ERRO: .NET SDK nao encontrado no PATH." -ForegroundColor Red
}

# 2. Windows SDK (10.0.26100)
Write-Host "`n[2/4] Verificando Windows SDK 10.0.26100..." -ForegroundColor Yellow
$sdkKey = "HKLM:\SOFTWARE\Microsoft\Windows Kits\Installed Roots"
$sdkKeyWow64 = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows Kits\Installed Roots"
$sdkPath = (Get-ItemProperty $sdkKey -ErrorAction SilentlyContinue).KitsRoot10
if (-not $sdkPath) {
    $sdkPath = (Get-ItemProperty $sdkKeyWow64 -ErrorAction SilentlyContinue).KitsRoot10
}

$hasSdk26100 = $false
if ($sdkPath) {
    $sdkUnionHeader = Join-Path $sdkPath "Include\10.0.26100.0\um\windows.h"
    $sdkMetadata = Join-Path $sdkPath "UnionMetadata\10.0.26100.0\Windows.winmd"
    if ((Test-Path $sdkUnionHeader) -or (Test-Path $sdkMetadata)) {
        $hasSdk26100 = $true
        Write-Host "  Windows SDK 10.0.26100 detectado em: $sdkPath" -ForegroundColor Green
    }
}

if (-not $hasSdk26100) {
    Write-Host "  AVISO: Windows SDK 10.0.26100 nao detectado no registro/disco." -ForegroundColor Yellow
    if ($InstallMissing) {
        Write-Host "  Instalando Windows SDK 10.0.26100 via winget..." -ForegroundColor Cyan
        winget install --id Microsoft.WindowsSDK.10.0.26100 --accept-source-agreements --accept-package-agreements --silent
    }
}

# 3. Visual Studio Build Tools e C++ Native Tools
Write-Host "`n[3/4] Verificando Visual Studio Build Tools e C++ (VC.Tools / ATL)..." -ForegroundColor Yellow
$vswherePaths = @(
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
)
$vswhere = $vswherePaths | Where-Object { Test-Path $_ } | Select-Object -First 1

$hasVsTools = $false
if ($vswhere) {
    $vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 Microsoft.VisualStudio.Component.VC.ATL -property installationPath
    if ($vsPath) {
        $hasVsTools = $true
        Write-Host "  Visual Studio com VC.Tools e ATL detectado em: $vsPath" -ForegroundColor Green
    }
}

if (-not $hasVsTools) {
    Write-Host "  AVISO: Visual Studio Build Tools com VC.Tools e ATL nao detectado." -ForegroundColor Yellow
    if ($InstallMissing) {
        Write-Host "  Instalando Visual Studio 2022 Build Tools..." -ForegroundColor Cyan
        winget install --id Microsoft.VisualStudio.2022.BuildTools --accept-source-agreements --accept-package-agreements --silent `
            --override "--passive --wait --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 --add Microsoft.VisualStudio.Component.VC.ATL --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools"
    }
}

# 4. Inno Setup 6 (Compilador de instalador)
Write-Host "`n[4/4] Verificando Inno Setup 6..." -ForegroundColor Yellow
$isccPaths = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)
$iscc = $isccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($iscc) {
    Write-Host "  Inno Setup detectado em: $iscc" -ForegroundColor Green
} else {
    Write-Host "  AVISO: Inno Setup 6 nao detectado." -ForegroundColor Yellow
    if ($InstallMissing) {
        Write-Host "  Instalando Inno Setup 6 via winget..." -ForegroundColor Cyan
        winget install --id JRSoftware.InnoSetup --accept-source-agreements --accept-package-agreements --silent
    }
}

Write-Host "`n=== Fim do Diagnostico ===" -ForegroundColor Cyan
