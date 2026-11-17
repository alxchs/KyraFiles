# =====================================================================
# KyraFiles - Script Versionado de Purga Mecanica de Nomes Legados
# Conforme INSTRUCTIONS_OPERARY.md (Modo Acelerado e Limite Juridico)
# =====================================================================

[CmdletBinding()]
param(
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

# Arquivos rigorosamente protegidos por limites juridicos e de ferramenta
$protectedFiles = @(
    "LICENSE-MIT",
    "LICENSE-MPL",
    ".github/NOTICE.md",
    "tools/check-legacy-names.ps1",
    "tools/purge-names.ps1",
    "tools/legacy-names-allowlist.json",
    "docs/specs/F091-identidade-kyrafiles-no-codigo/inventario.csv",
    "docs/specs/F091-identidade-kyrafiles-no-codigo/inventario.md",
    "docs/agents/evidence/F047/nfr001_scan_pruning_50k_files.csv"
)

# Extensoes textuais alvo
$targetExtensions = @(
    ".md", ".txt", ".json", ".xml", ".xaml", ".cs", ".cpp", ".h", ".idl",
    ".rc", ".rgs", ".def", ".iss", ".ps1", ".yml", ".yaml", ".resw",
    ".csproj", ".vcxproj", ".filters", ".sln", ".slnx", ".props", ".targets",
    ".html", ".desativado", ".csv", ".log"
)

$allFiles = git ls-files

Write-Host "Iniciando purga mecanica de marcas legadas..." -ForegroundColor Cyan

$modifiedFilesCount = 0
$totalReplacements = 0

foreach ($relPath in $allFiles) {
    if ($protectedFiles -contains $relPath) {
        continue
    }

    $ext = [System.IO.Path]::GetExtension($relPath)
    if ($targetExtensions -notcontains $ext) {
        continue
    }

    $fullPath = Join-Path $repoRoot $relPath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        continue
    }

    $content = $null
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        $fs = $null
        $reader = $null
        try {
            $fs = [System.IO.FileStream]::new($fullPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
            $reader = [System.IO.StreamReader]::new($fs, [System.Text.Encoding]::UTF8)
            $content = $reader.ReadToEnd()
            break
        } catch {
            if ($attempt -lt 3) {
                Start-Sleep -Milliseconds 100
            } else {
                Write-Warning "Falha ao ler $relPath : $($_.Exception.Message)"
            }
        } finally {
            if ($null -ne $reader) { $reader.Dispose() }
            if ($null -ne $fs) { $fs.Dispose() }
        }
    }

    if ($null -eq $content) {
        continue
    }

    $original = $content

    # 1. Substituições de URLs e Repositórios
    $content = $content -replace 'https://github\.com/(files-community|alxchs)/(Files|FilesCustom)', 'https://github.com/alxchs/KyraFiles'
    $content = $content -replace 'github\.com/(files-community|alxchs)/(Files|FilesCustom)', 'github.com/alxchs/KyraFiles'
    $content = $content -replace 'https://files\.community', 'https://kyrafiles.org'
    $content = $content -replace 'https://discord\.gg/files', 'https://discord.gg/kyrafiles'
    $content = $content -replace 'https://crowdin\.com/project/files-app', 'https://crowdin.com/project/kyrafiles'
    $content = $content -replace 'filesapp@outlook\.com', 'contato@kyrafiles.com'
    $content = $content -replace 'alxchs/(FilesCustom|Files)\b', 'alxchs/KyraFiles'

    # 2. Substituições de Nomes de Produtos / Marcas Legadas
    $content = $content -replace '\bFiles Custom\b', 'KyraFiles'
    $content = $content -replace '\bFiles Dev\b', 'KyraFiles'
    $content = $content -replace '\bFiles App\b', 'KyraFiles'
    $content = $content -replace '\bFiles Community\b', 'KyraFiles Community'
    $content = $content -replace '\bFilesCustom\b', 'KyraFiles'
    $content = $content -replace '\bFilesDev\b', 'KyraFiles'
    $content = $content -replace '\bFilesApp\b', 'KyraFiles'
    $content = $content -replace '\bFilesOpenDialog\b', 'KyraFilesOpenDialog'
    $content = $content -replace '\bFilesSaveDialog\b', 'KyraFilesSaveDialog'

    # 3. Protocolos, Aliases e IDs de Pacotes
    $content = $content -replace '\bfiles-dev\.exe\b', 'kyrafiles.exe'
    $content = $content -replace '\bfiles-dev:\b', 'kyrafiles:'
    $content = $content -replace '\bfiles-stable\b', 'kyrafiles-stable'
    $content = $content -replace '\bfiles-preview\b', 'kyrafiles-preview'
    $content = $content -replace '\bfiles-dev\b', 'kyrafiles-dev'
    $content = $content -replace '\bcom\.files\b', 'com.kyrafiles'
    $content = $content -replace '\borg\.files\b', 'org.kyrafiles'
    $content = $content -replace '\bFiles\.Package\b', 'KyraFiles.Package'

    # 4. Projetos, Namespaces, Serviços e Caminhos de código
    $content = $content -replace '(?<![A-Za-z0-9_])Files\.(App|Shared|Core|Server|Index|InteractionTests|BackgroundTasks|HotkeyService|Watchdog|Launcher|Storage)\b', 'KyraFiles.$1'
    $content = $content -replace '(?<![A-Za-z0-9_])(src|tests)[\\/]Files\.', '$1/KyraFiles.'
    $content = $content -replace '(?<![A-Za-z0-9_])Files\.App[\\/]', 'KyraFiles.App/'
    $content = $content -replace '(?<![A-Za-z0-9_])FilesApp[\\/]', 'KyraFiles/'
    $content = $content -replace '(?<![A-Za-z0-9_])C:\\desenv\\utils\\FilesApp\b', 'C:\desenv\utils\KyraFiles'
    $content = $content -replace '(?<![A-Za-z0-9_])claude\\desenv\\utils\\FilesApp\b', 'claude\desenv\utils\KyraFiles'
    $content = $content -replace '(?<![A-Za-z0-9_])Update-FilesCustom\.ps1\b', 'Update-KyraFiles.ps1'
    $content = $content -replace '(?<![A-Za-z0-9_])FilesCustomSetup\b', 'KyraFilesSetup'
    $content = $content -replace '(?<![A-Za-z0-9_])Files\.App\.csproj\b', 'KyraFiles.App.csproj'
    $content = $content -replace '(?<![A-Za-z0-9_])Files\.exe\b', 'KyraFiles.exe'
    $content = $content -replace '(?<![A-Za-z0-9_])Files\.dll\b', 'KyraFiles.dll'
    $content = $content -replace '(?<![A-Za-z0-9_])(FilesApp|FilesCustom|FilesDev|FilesOpenDialog|FilesSaveDialog)\b', 'KyraFiles'

    # 5. Textos específicos em documentação (.md) e recursos (.resw)
    if ($ext -in @('.md', '.resw', '.txt', '.html', '.yml')) {
        $content = $content -replace '(?<![A-Za-z0-9])Files CI\b', 'KyraFiles CI'
        $content = $content -replace '(?<![A-Za-z0-9])Files CD\b', 'KyraFiles CD'
        $content = $content -replace '(?<![A-Za-z0-9])Files PR\b', 'KyraFiles PR'
        $content = $content -replace '(?<![A-Za-z0-9])About Files\b', 'About KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])Sobre o Files\b', 'Sobre o KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])Files version\b', 'KyraFiles version'
        $content = $content -replace '(?<![A-Za-z0-9])Versão do Files\b', 'Versão do KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])the Files app\b', 'the KyraFiles app'
        $content = $content -replace '(?<![A-Za-z0-9])The Files App\b', 'The KyraFiles App'
        $content = $content -replace '(?<![A-Za-z0-9])the Files project\b', 'the KyraFiles project'
        $content = $content -replace '(?<![A-Za-z0-9])o projeto Files\b', 'o projeto KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])o Files\b', 'o KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])no Files\b', 'no KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])do Files\b', 'do KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])pelo Files\b', 'pelo KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])para o Files\b', 'para o KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])ao Files\b', 'ao KyraFiles'
        $content = $content -replace '(?<![A-Za-z0-9])com o Files\b', 'com o KyraFiles'
    }

    if ($content -ne $original) {
        $modifiedFilesCount++
        if (-not $WhatIf) {
            for ($attempt = 1; $attempt -le 3; $attempt++) {
                $fs = $null
                $writer = $null
                try {
                    $fs = [System.IO.FileStream]::new($fullPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
                    $writer = [System.IO.StreamWriter]::new($fs, [System.Text.Encoding]::UTF8)
                    $writer.Write($content)
                    break
                } catch {
                    if ($attempt -lt 3) {
                        Start-Sleep -Milliseconds 150
                    } else {
                        Write-Warning "Falha ao gravar $relPath : $($_.Exception.Message)"
                    }
                } finally {
                    if ($null -ne $writer) { $writer.Dispose() }
                    if ($null -ne $fs) { $fs.Dispose() }
                }
            }
        }
    }
}

Write-Host "Purga mecânica concluída!" -ForegroundColor Green
Write-Host "Arquivos modificados: $modifiedFilesCount"
