# Monta o layout do pacote (AppX), registra e abre o KyraFiles Dev.
#
# O KyraFiles é um app empacotado (MSIX): o KyraFiles.exe de bin\ aberto direto não tem
# identidade de pacote e morre com REGDB_E_CLASSNOTREG. O `dotnet build` só gera a
# receita do layout (KyraFiles.build.appxrecipe); quem copia os arquivos para AppX\
# é o deploy do Visual Studio. Este script faz essa etapa.
#
# Uso: .\Open-KyraFilesDev.ps1 [-Configuration Release|Debug] [-NoLaunch]

param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'

$tfm = 'net10.0-windows10.0.26100.0'
$appBin = Join-Path $PSScriptRoot "src\KyraFiles.App\bin\x64\$Configuration\$tfm\win-x64"
$serverBin = Join-Path $PSScriptRoot "src\KyraFiles.App.Server\bin\x64\$Configuration\$tfm\win-x64"
$indexServiceBin = Join-Path $PSScriptRoot "src\KyraFiles.App.IndexService\bin\x64\$Configuration\$tfm"
$recipePath = Join-Path $appBin 'KyraFiles.build.appxrecipe'

if (-not (Test-Path $recipePath)) {
    throw "Receita não encontrada: $recipePath. Compile antes: mkfile $($Configuration.Substring(0,1).ToLower()) src\KyraFiles.App\KyraFiles.App.csproj"
}

[xml]$recipe = Get-Content $recipePath -Raw
$ns = @{ m = 'http://schemas.microsoft.com/developer/msbuild/2003' }
$layoutDir = (Select-Xml -Xml $recipe -Namespace $ns -XPath '//m:LayoutDir').Node.InnerText
$items = Select-Xml -Xml $recipe -Namespace $ns -XPath '//m:AppxPackagedFile | //m:AppXManifest' | ForEach-Object Node

[xml]$manifest = Get-Content (Join-Path $appBin 'AppxManifest.xml') -Raw
$identity = $manifest.Package.Identity
$appId = $manifest.Package.Applications.Application.Id

# Arquivos em uso impedem a cópia
Get-Process KyraFiles, KyraFiles.App.Server, KyraFiles.App.IndexService, Files, Files.App.Server, Files.App.IndexService -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and $_.Path.StartsWith($layoutDir, [StringComparison]::OrdinalIgnoreCase) } |
    Stop-Process -Force
Start-Sleep -Milliseconds 500
Get-ChildItem -LiteralPath $layoutDir -Filter "*.locked.*" -File -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

function Copy-IfChanged([string]$source, [string]$target) {
    $dst = Get-Item -LiteralPath $target -ErrorAction SilentlyContinue
    $src = Get-Item -LiteralPath $source
    if ($dst -and $dst.Length -eq $src.Length -and $dst.LastWriteTimeUtc -eq $src.LastWriteTimeUtc) { return 0 }
    New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
    try {
        Copy-Item -LiteralPath $source -Destination $target -Force
    } catch {
        # Se o arquivo estiver mapeado em memoria (ex: resources.pri pelo Shell/Search), move e copia
        $tmp = "$target.locked." + [System.Guid]::NewGuid().ToString("N")
        Move-Item -LiteralPath $target -Destination $tmp -Force -ErrorAction SilentlyContinue
        Copy-Item -LiteralPath $source -Destination $target -Force
        Remove-Item -LiteralPath $tmp -Force -ErrorAction SilentlyContinue
    }
    return 1
}

$copied = 0
$missing = @()
foreach ($item in $items) {
    if (-not (Test-Path -LiteralPath $item.Include)) { $missing += $item.Include; continue }
    $copied += Copy-IfChanged $item.Include (Join-Path $layoutDir $item.PackagePath)
}
if ($missing) { throw "Arquivos da receita ausentes ($($missing.Count)), recompile. Primeiro: $($missing[0])" }

# Servidor COM fora de processo declarado no manifesto (KyraFiles.App.Server\KyraFiles.App.Server.exe);
# não é ProjectReference, então não entra na receita.
if (-not (Test-Path (Join-Path $serverBin 'KyraFiles.App.Server.exe'))) { throw "KyraFiles.App.Server não compilado: $serverBin" }
Get-ChildItem $serverBin -File -Recurse | ForEach-Object {
    $copied += Copy-IfChanged $_.FullName (Join-Path $layoutDir ('KyraFiles.App.Server' + $_.FullName.Substring($serverBin.Length)))
}

# Serviço de índice de busca fora de processo (F015 T10/T11: KyraFiles.App.IndexService\KyraFiles.App.IndexService.exe)
if (Test-Path (Join-Path $indexServiceBin 'KyraFiles.App.IndexService.exe')) {
    Get-ChildItem $indexServiceBin -File -Recurse | ForEach-Object {
        $copied += Copy-IfChanged $_.FullName (Join-Path $layoutDir ('KyraFiles.App.IndexService' + $_.FullName.Substring($indexServiceBin.Length)))
    }
}
Write-Host "Layout: $layoutDir ($($items.Count) itens da receita, $copied copiados)"

# Registro de desenvolvimento (pasta solta). Precisa do Modo de Desenvolvedor do Windows.
$layoutManifest = Join-Path $layoutDir 'AppxManifest.xml'
$installed = Get-AppxPackage -Name $identity.Name
if ($installed -and $installed.InstallLocation -ne $layoutDir) {
    Remove-AppxPackage $installed.PackageFullName -PreserveApplicationData
}
Add-AppxPackage -Register $layoutManifest -ForceApplicationShutdown -ForceUpdateFromAnyVersion
$package = Get-AppxPackage -Name $identity.Name
Write-Host "Registrado: $($package.PackageFullName) -> $($package.InstallLocation)"

if (-not $NoLaunch) {
    Start-Process explorer.exe -ArgumentList "shell:AppsFolder\$($package.PackageFamilyName)!$appId"
}
