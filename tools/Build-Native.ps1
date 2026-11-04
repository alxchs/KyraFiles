# Compila os projetos C++ do upstream que o `dotnet build` (mkfile) não compila:
# Files.App.Launcher (gerenciador padrão / Win+E) e as DLLs de diálogo de abrir/salvar.
# O pós-build de cada projeto copia a saída para src\Files.App\Assets\FilesOpenDialog,
# que entra no pacote. Rode antes do mkfile quando esses projetos mudarem ou num clone novo.
#
# Uso: pwsh tools\Build-Native.ps1 [-Configuration Release|Debug]

param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$vs = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 Microsoft.VisualStudio.Component.VC.ATL -property installationPath
if (-not $vs) {
    throw 'Visual Studio com C++ (VC.Tools.x86.x64) e ATL (VC.ATL) não encontrado pelo vswhere.'
}
$msbuild = Join-Path $vs 'MSBuild\Current\Bin\amd64\MSBuild.exe'

# O upstream usa o toolset v145 (VS 18); aqui vale o mais novo instalado.
$toolset = (Get-ChildItem (Join-Path $vs 'MSBuild\Microsoft\VC\*\Platforms\x64\PlatformToolsets\v*') -Directory |
    Sort-Object Name -Descending | Select-Object -First 1).Name
$sdk = (Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\Lib" -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName 'um\x64\kernel32.lib') } |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1).Name
Write-Host "VS: $vs | toolset $toolset | SDK $sdk"

$common = @(
    "-p:Configuration=$Configuration",
    "-p:PlatformToolset=$toolset",
    "-p:WindowsTargetPlatformVersion=$sdk",
    "-p:SolutionDir=$root\",
    '-v:minimal', '-nologo'
)

& $msbuild "$root\src\KyraFiles.App.Launcher\KyraFiles.App.Launcher.vcxproj" -t:restore -p:RestorePackagesConfig=true @common
if ($LASTEXITCODE) { throw 'Falha no restore do launcher.' }

$builds = @(
    @('src\KyraFiles.App.Launcher\KyraFiles.App.Launcher.vcxproj', 'x64'),
    @('src\KyraFiles.HotkeyService\KyraFiles.HotkeyService.vcxproj', 'x64'),
    @('src\KyraFiles.Watchdog\KyraFiles.Watchdog.vcxproj', 'x64'),
    @('src\KyraFiles.App.OpenDialog\KyraFiles.App.OpenDialog.vcxproj', 'x64'),
    @('src\KyraFiles.App.SaveDialog\KyraFiles.App.SaveDialog.vcxproj', 'x64')
)

foreach ($b in $builds) {
    Write-Host "== $($b[0]) [$($b[1])]"
    & $msbuild (Join-Path $root $b[0]) -t:Build "-p:Platform=$($b[1])" @common
    if ($LASTEXITCODE) { throw "Falha ao compilar $($b[0]) [$($b[1])]." }
}

Get-ChildItem (Join-Path $root 'src\KyraFiles.App\Assets\FilesOpenDialog') |
    Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
