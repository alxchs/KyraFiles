<#
.SYNOPSIS
    Verificador ampliado de nomes e marcas legadas para o KyraFiles.
    Em conformidade com INSTRUCTIONS_OPERARY.md (Regras R1 a R5).

.DESCRIPTION
    Varre todos os arquivos versionados (git ls-files) por:
    1. Nomes de pastas e arquivos contendo variantes de "Files"
    2. Conteudo dos arquivos contendo a marca legada e suas variantes
    3. Valida contra a lista de excecoes permitidas em tools/legacy-names-allowlist.json
    Retorna exit 0 se houver 0 violacoes; exit 1 com lista detalhada caso contrario.
#>
[CmdletBinding()]
param(
    [string]$Root,
    [switch]$Detailed,
    [string]$OutputFile
)

$ErrorActionPreference = "Stop"
if (-not $Root) {
    if ($PSScriptRoot) {
        $Root = Split-Path $PSScriptRoot -Parent
    } else {
        $Root = (Get-Location).Path
    }
}
Set-Location $Root

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  KyraFiles - Verificador Ampliado de Nomes e Marcas Legadas    " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Carregar lista de excecoes autorizadas (R5)
$allowlistPath = Join-Path $Root "tools\legacy-names-allowlist.json"
$allowlist = @()
if (Test-Path $allowlistPath) {
    try {
        $json = Get-Content $allowlistPath -Raw | ConvertFrom-Json
        $allowlist = $json.exceptions
        Write-Host "Carregadas $($allowlist.Count) excecoes autorizadas da allowlist." -ForegroundColor Gray
    } catch {
        Write-Error "Erro ao ler tools/legacy-names-allowlist.json: $_"
        exit 1
    }
} else {
    Write-Warning "Allowlist nao encontrada em $allowlistPath. Nenhuma excecao sera aplicada."
}

# Funcao para verificar se uma ocorrencia esta autorizada na allowlist
function Is-Allowed {
    param(
        [string]$FilePath,
        [string]$Snippet
    )
    $normPath = $FilePath.Replace('\', '/')
    foreach ($entry in $allowlist) {
        $entryFile = $entry.file.Replace('\', '/')
        if ($normPath -eq $entryFile) {
            if ([string]::IsNullOrWhiteSpace($entry.exact_snippet) -or ($Snippet.IndexOf($entry.exact_snippet, [System.StringComparison]::OrdinalIgnoreCase) -ge 0)) {
                return $true
            }
        }
    }
    return $false
}

$violations = [System.Collections.Generic.List[PSCustomObject]]::new()

# 2. Verificacao R3: Nomes de pastas e arquivos no git ls-files
Write-Host "`n[1/2] Verificando nomes de arquivos e diretorios no git ls-files..." -ForegroundColor Yellow
$allFiles = git ls-files
$pathPattern = '(?<![A-Za-z0-9])Files(?!(?i)(ystem|ave|ubtype|ize))'

foreach ($file in $allFiles) {
    # Ignora os proprios scripts de verificacao e testes de nomes e inventario base
    if ($file -match 'tools/(check-legacy-names|rename-legacy-namespaces|purge-names)\.ps1' -or $file -match 'tools/legacy-names-allowlist\.json' -or $file -match 'docs/specs/F091-identidade-kyrafiles-no-codigo/inventario\.(csv|md)') {
        continue
    }

    # Verifica partes do caminho
    $segments = $file.Split('/')
    foreach ($seg in $segments) {
        if ($seg -match $pathPattern) {
            if (-not (Is-Allowed -FilePath $file -Snippet $seg)) {
                $violations.Add([PSCustomObject]@{
                    Type = "FileName"
                    File = $file
                    Line = 0
                    Snippet = $seg
                    Category = "Estrutura de Pastas e Arquivos"
                })
                break
            }
        }
    }
}
Write-Host "Verificacao de caminhos concluida. ($($violations.Count) violacoes encontradas)" -ForegroundColor Gray

# 3. Verificacao R2: Conteudo de todos os arquivos versionados
Write-Host "`n[2/2] Verificando conteudo dos arquivos versionados..." -ForegroundColor Yellow

# Padrões específicos de marca e prefixos
$contentPatterns = @(
    '(?<![A-Za-z0-9_])Files\.(App|Shared|Core|Server|Index|InteractionTests|BackgroundTasks|HotkeyService|Watchdog|Launcher|Storage)',
    '(?<![A-Za-z0-9_])Files(App|Custom|Dev|OpenDialog|SaveDialog|Launcher|UXLab|Community|Preview|Stable|UWP)',
    '(?<![A-Za-z0-9_])Files\s+(Community|Custom|UWP|Team)',
    'files-community|files\.community|files-app|files-org',
    '9NGHP3DX8HDX|725513575971684472|49306atecsolution',
    '(?<![A-Za-z0-9_])KyraFiles\s+Community'
)

$combinedPattern = ($contentPatterns -join '|')

$grepResults = git grep -n -i -I -P $combinedPattern -- .
if ($grepResults) {
    foreach ($line in $grepResults) {
        # Formato do git grep: arquivo:linha:conteudo
        $firstColon = $line.IndexOf(':')
        if ($firstColon -le 0) { continue }
        $secondColon = $line.IndexOf(':', $firstColon + 1)
        if ($secondColon -le 0) { continue }

        $file = $line.Substring(0, $firstColon)
        $lineNumStr = $line.Substring($firstColon + 1, $secondColon - $firstColon - 1)
        $snippet = $line.Substring($secondColon + 1)

        # Ignora arquivos da propria ferramenta de verificacao e purga e inventario base
        if ($file -match 'tools/(check-legacy-names|rename-legacy-namespaces|purge-names)\.ps1' -or $file -match 'tools/legacy-names-allowlist\.json' -or $file -match 'docs/specs/F091-identidade-kyrafiles-no-codigo/inventario\.(csv|md)') {
            continue
        }

        if (-not (Is-Allowed -FilePath $file -Snippet $snippet)) {
            $cat = "Codigo e Textos"
            if ($file -match '^docs/') { $cat = "Documentacao" }
            elseif ($file -match '^tools/') { $cat = "Ferramentas e Scripts" }
            elseif ($file -match '\.(xaml|resw)$') { $cat = "UI e Recursos" }
            elseif ($file -match '(installer|\.iss)') { $cat = "Instalador" }

            $violations.Add([PSCustomObject]@{
                Type = "Content"
                File = $file
                Line = [int]$lineNumStr
                Snippet = $snippet.Trim()
                Category = $cat
            })
        }
    }
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  RESULTADO DA VERIFICACAO: $(if ($violations.Count -eq 0) { 'APROVADO' } else { 'FALHA' })" -ForegroundColor $(if ($violations.Count -eq 0) { 'Green' } else { 'Red' })
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Total de violacoes ativas encontradas: $($violations.Count)"

if ($violations.Count -gt 0) {
    $byCategory = $violations | Group-Object Category
    Write-Host "`nResumo por Categoria:" -ForegroundColor Yellow
    foreach ($grp in $byCategory) {
        Write-Host "  • $($grp.Name): $($grp.Count) ocorrencia(s)" -ForegroundColor White
    }

    if ($Detailed) {
        Write-Host "`nLista Detalhada das Primeiras 100 Violacoes:" -ForegroundColor Yellow
        $violations | Select-Object -First 100 | Format-Table Type, File, Line, Snippet -AutoSize | Out-String | Write-Host
    }

    if ($OutputFile) {
        $violations | Export-Csv -Path $OutputFile -NoTypeInformation -Encoding UTF8
        Write-Host "`nInventario completo exportado para: $OutputFile" -ForegroundColor Green
    }

    exit 1
}

Write-Host "OK: nenhum residuo legado encontrado fora da allowlist." -ForegroundColor Green
exit 0
