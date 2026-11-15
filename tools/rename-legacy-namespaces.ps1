# F091: troca o prefixo de namespace/identificador Files.* por KyraFiles.* no código (src, tests, harnesses de tools/perf).
# Idempotente: rodar de novo não muda nada. Uso numa branch antiga antes do rebase sobre a main renomeada:
#   pwsh tools\rename-legacy-namespaces.ps1   (depois commit, depois git rebase origin/main)
# Preserva BOM e fim de linha. Arquivos que não são UTF-8 válido são pulados e listados.
[CmdletBinding()]
param([string]$Root = (Split-Path $PSScriptRoot -Parent))

$ErrorActionPreference = 'Stop'
Set-Location $Root

$extensions = '.cs', '.xaml', '.json', '.manifest', '.cpp', '.h', '.ps1', '.csproj', '.props', '.targets'
$roots = 'src', 'tests', 'tools/perf/HexEditorHarness', 'tools/perf/SplitCombineHarness'

# Identificador sem aspas antes: namespace, using, global using, using: do XAML, nome qualificado, comentário.
$unquoted = [regex]'(?<![A-Za-z0-9_"])Files\.(?=(?:App|Shared|Core|InteractionTests|HotkeyService|Watchdog)\b)'
# Entre aspas: x:Class, strings dos geradores de código, nomes de perfil e de manifesto.
$quoted = [regex]'"Files\.(?=(?:App|Shared|Core|InteractionTests)\b)'
# Arquivos em que "Files.*" entre aspas é nome de instalação antiga (limpeza/compatibilidade) e deve ficar.
$keepQuoted = @(
	'src/KyraFiles.App/Program.cs',
	'src/KyraFiles.App/Utils/Shell/ExplorerWindowRedirector.cs',
	'src/KyraFiles.App/ViewModels/Settings/AboutViewModel.cs',
	'src/KyraFiles.App/ViewModels/Settings/AdvancedViewModel.cs',
	'src/KyraFiles.App/Helpers/BuildInfo.cs',
	'src/KyraFiles.App/Data/Parameters/TabBarItemParameter.cs'
)

$strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
$changed = 0; $skipped = @()

foreach ($file in (git ls-files -- $roots)) {
	if ($extensions -notcontains [IO.Path]::GetExtension($file).ToLowerInvariant()) { continue }
	$bytes = [IO.File]::ReadAllBytes((Join-Path $Root $file))
	$hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
	$offset = if ($hasBom) { 3 } else { 0 }
	try { $text = $strictUtf8.GetString($bytes, $offset, $bytes.Length - $offset) }
	catch { $skipped += $file; continue }

	$new = $unquoted.Replace($text, 'KyraFiles.')
	if ($keepQuoted -notcontains $file) { $new = $quoted.Replace($new, '"KyraFiles.') }
	if ($new -ceq $text) { continue }

	$out = [System.Text.UTF8Encoding]::new($hasBom).GetBytes($new)
	if ($hasBom) { $out = [byte[]](0xEF, 0xBB, 0xBF) + $out }
	[IO.File]::WriteAllBytes((Join-Path $Root $file), $out)
	$changed++
}

Write-Host "Arquivos alterados: $changed"
if ($skipped) { Write-Warning ("Não UTF-8 (conferir à mão): " + ($skipped -join ', ')) }
