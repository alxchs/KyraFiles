# F091 (AC-7): falha (exit 1) se o prefixo antigo Files.* voltar como identificador de código.
# Exceções: linhas que existem para limpar ou reconhecer instalações antigas (marcadas abaixo).
[CmdletBinding()]
param([string]$Root = (Split-Path $PSScriptRoot -Parent))

Set-Location $Root
$pattern = '(?<![A-Za-z0-9_])Files\.(App|Shared|Core|InteractionTests|HotkeyService|Watchdog)\b'
$roots = 'src', 'tests', 'tools/perf/HexEditorHarness', 'tools/perf/SplitCombineHarness'

# arquivo => trecho que justifica a linha (nome de processo/arquivo de versão antiga, leitura de dado antigo)
$allowed = @{
	'src/KyraFiles.App/Program.cs'                               = 'GetProcessesByName("Files.App.Server")'
	'src/KyraFiles.App/Utils/Shell/ExplorerWindowRedirector.cs'  = '"Files.App.Launcher.exe"'
	'src/KyraFiles.App/ViewModels/Settings/AboutViewModel.cs'    = '"Files.App.Launcher.exe"'
	'src/KyraFiles.App/ViewModels/Settings/AdvancedViewModel.cs' = '"Files.App.'
	'src/KyraFiles.App/Data/Parameters/TabBarItemParameter.cs'   = 'LegacyNamespacePrefix'
	'src/KyraFiles.App/Helpers/BuildInfo.cs'                     = '"Files.App.dll"'
}

$hits = git grep -n -P $pattern -- $roots ':!*.md' | Where-Object {
	$file = $_.Split(':')[0]
	-not ($allowed.ContainsKey($file) -and $_.Contains($allowed[$file]))
}

if ($hits) {
	Write-Host "Nome antigo encontrado ($(@($hits).Count) linha(s)):" -ForegroundColor Yellow
	$hits | ForEach-Object { Write-Host "  $_" }
	exit 1
}
Write-Host 'OK: nenhum identificador Files.* fora das exceções.'
exit 0
