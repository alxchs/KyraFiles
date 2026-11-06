$ErrorActionPreference = 'Stop'

$appxDir = "src\KyraFiles.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX"
$distDir = "dist"
$stageDir = Join-Path $distDir "FilesApp-v4.2.37.0-x64-standalone"

if (-not (Test-Path $appxDir)) {
    throw "AppX directory not found: $appxDir"
}

Write-Host "Preparando pasta de staging em $stageDir..." -ForegroundColor Cyan
if (Test-Path $stageDir) {
    Remove-Item $stageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

# Launcher (gerenciador padrão) e DLLs de diálogo vêm de tools\Build-Native.ps1, rodado antes do mkfile
$nativeFiles = 'KyraFiles.App.Launcher.exe', 'Files.App.OpenDialog64.dll', 'Files.App.OpenDialog32.dll', 'Files.App.SaveDialog64.dll', 'Files.App.SaveDialog32.dll'
$missing = $nativeFiles | Where-Object { -not (Test-Path (Join-Path $appxDir "Assets\FilesOpenDialog\$_")) }
if ($missing) {
    throw "Faltam no layout AppX: $($missing -join ', '). Rode tools\Build-Native.ps1 e depois o mkfile release."
}

# Copiar arquivos do AppX usando robocopy para máxima velocidade
Write-Host "Copiando arquivos do layout AppX..." -ForegroundColor Cyan
& robocopy $appxDir $stageDir /E /NFL /NDL /NJH /NJS /NC /NS /NP | Out-Null

# Criar scripts de instalação e inicialização
$cmdContent = @"
@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar-E-Executar.ps1"
"@
[System.IO.File]::WriteAllText((Join-Path $stageDir "Instalar-E-Executar.cmd"), $cmdContent, [System.Text.Encoding]::ASCII)

$ps1Content = @'
# Instala e executa o Files Custom v4.2.37.0 autocontido
$manifest = Join-Path $PSScriptRoot "AppxManifest.xml"
if (-not (Test-Path $manifest)) {
    Write-Error "AppxManifest.xml não encontrado em $PSScriptRoot"
    exit 1
}

Write-Host "Registrando pacote no Windows..." -ForegroundColor Cyan
Add-AppxPackage -Register $manifest -ForceApplicationShutdown -ForceUpdateFromAnyVersion
$pkg = Get-AppxPackage -Name "FilesDev"
if ($pkg) {
    Write-Host "Registrado com sucesso: $($pkg.PackageFullName)" -ForegroundColor Green
    Write-Host "Iniciando Files Custom..." -ForegroundColor Cyan
    Start-Process "shell:AppsFolder\$($pkg.PackageFamilyName)!App"
} else {
    Write-Error "Falha ao registrar pacote."
}
'@
[System.IO.File]::WriteAllText((Join-Path $stageDir "Instalar-E-Executar.ps1"), $ps1Content, [System.Text.Encoding]::UTF8)

$readmeContent = @"
Files Custom — Release Autocontida v4.2.37.0 (x64)
Commit: a558f56c1 na branch custom/main / main
Compilacao: 08/10/2026 (0 Warning(s), 0 Error(s))

REQUISITOS:
- Windows 10 (Build 19041+) ou Windows 11 (x64)
- Modo de Desenvolvedor ativado no Windows (Configuracoes > Sistema > Para Desenvolvedores)

COMO USAR:
1. Extraia o conteudo deste arquivo ZIP em uma pasta (ex: C:\FilesCustom).
2. Execute o arquivo "Instalar-E-Executar.cmd" (ou "Instalar-E-Executar.ps1" no PowerShell).
3. O Files Custom sera registrado no sistema e abrira automaticamente.

DESTAQUES DESTA RELEASE:
- F010: Todas as Colunas do Windows Explorer (369 colunas elegiveis catalogadas, dialogo "Mais...", extracao assincrona pelo Shell e persistencia por pasta).
- F001: Rename UX com extensao protegida contra apagamento acidental e navegacao Tab/Shift+Tab.
- F002: Redimensionamento de colunas 100% independente em pixels absolutos.
- F003: Nova aba (Ctrl+T) vs duplicar aba (Ctrl+Shift+T).
- F004: Barra de menus tradicional completa (Alt+F, Alt+E, etc.).
- F005: Atalhos diretos Alt+P (Preview) e Alt+Shift+P (Details) com foco restaurado a lista.
- F006 / F011: Busca integrada plugavel (Nativa, Agent Ransack e Everything).
- F007-B: Otimizacao de repouso do compositor WinUI 3.
- F008: Modo compacto OneCommander.
- F009: Configuracao de fontes separadas para lista, navegacao e menus.
"@
[System.IO.File]::WriteAllText((Join-Path $stageDir "LEIA-ME.txt"), $readmeContent, [System.Text.Encoding]::UTF8)

# Gerar arquivo ZIP autocontido
$zipFile = Join-Path $distDir "FilesApp-v4.2.37.0-x64-standalone.zip"
Write-Host "Compactando em $zipFile..." -ForegroundColor Cyan
if (Test-Path $zipFile) {
    Remove-Item $zipFile -Force
}

Push-Location $distDir
try {
    & tar.exe -a -c -f "FilesApp-v4.2.37.0-x64-standalone.zip" "FilesApp-v4.2.37.0-x64-standalone"
} finally {
    Pop-Location
}

Write-Host "Release autocontida gerada com sucesso em $distDir!" -ForegroundColor Green

# Compilar Instalador executavel standalone (C# Windows Forms com payload embutido)
$installerSrc = "tools\installer\Program.cs"
$installerExe = Join-Path $distDir "Instalador-FilesCustom-v4.2.37.0.exe"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if ((Test-Path $installerSrc) -and (Test-Path $csc)) {
    Write-Host "Compilando Instalador Executavel standalone..." -ForegroundColor Cyan
    & $csc /target:winexe /platform:x64 /optimize+ /out:$installerExe `
        /win32icon:src\KyraFiles.App\Assets\AppTiles\Dev\Logo.ico `
        /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
        /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll `
        "/resource:$zipFile,FilesAppPayload.zip" `
        $installerSrc | Out-Null
    Copy-Item $installerExe (Join-Path $distDir "Instalador-FilesCustom.exe") -Force
}

# Remover pasta temporaria de staging
Remove-Item $stageDir -Recurse -Force -ErrorAction SilentlyContinue

# Calcular checksums e tamanhos
$items = Get-ChildItem $distDir -File

Write-Host "`nResumo dos artefatos:" -ForegroundColor Yellow
foreach ($it in $items) {
    $hash = (Get-FileHash $it.FullName -Algorithm SHA256).Hash
    $sizeMB = [Math]::Round($it.Length / 1MB, 2)
    Write-Host ("- {0}: {1} MB (SHA256: {2})" -f $it.Name, $sizeMB, $hash)
}
