# tools/Publish-Release.ps1
# Comando oficial "Publicar" do KyraFiles (usado por /deploy e /ship).
# 1. Compila Release x64, gera instalador Inno Setup, faz deploy local e valida Gate F021 (Quick-Ship.ps1).
# 2. Cria/envia tag v<versao> para origin e public.
# 3. Publica GitHub Release com o .exe em alxchs/KyraFiles (canal oficial de updates) e alxchs/KyraFiles-private.
# 4. Verifica no ar via API do GitHub (https://api.github.com/repos/alxchs/KyraFiles/releases/latest).

[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$Full,
    [string[]]$Repos = @("alxchs/KyraFiles", "alxchs/KyraFiles-private")
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Split-Path $PSScriptRoot -Parent)
Set-Location $repoRoot

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  KyraFiles - Publicacao Oficial de Release (Local + GitHub)     " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Ler versao oficial do Package.appxmanifest
$manifestPath = Join-Path $repoRoot "src\KyraFiles.App\Package.appxmanifest"
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Package.appxmanifest nao encontrado em: $manifestPath"
}
[xml]$manifestXml = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8
$fullVersion = $manifestXml.Package.Identity.Version
if (-not $fullVersion) {
    throw "Nao foi possivel extrair Identity.Version de Package.appxmanifest."
}
$parts = $fullVersion.Split('.')
$shortVersion = "$($parts[0]).$($parts[1]).$($parts[2])"
$tagName = "v$fullVersion"
$setupPath = Join-Path $repoRoot "artifacts\Setup\KyraFilesSetup-$shortVersion.exe"

Write-Host "Versao detectada: $fullVersion (tag: $tagName | instalador: KyraFilesSetup-$shortVersion.exe)" -ForegroundColor Cyan

# 2. Verificar autenticacao do gh CLI antes de iniciar build demorado
& gh auth status 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "GitHub CLI (gh) nao autenticado. Execute 'gh auth login' antes de publicar."
}

# 3. Build, instalador, deploy local e Gate F021
if (-not $SkipBuild) {
    $quickShip = Join-Path $repoRoot "tools\Quick-Ship.ps1"
    $qsArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $quickShip)
    if ($Full) { $qsArgs += "-Full" } else { $qsArgs += "-Fast" }
    & powershell.exe @qsArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Quick-Ship.ps1 falhou com codigo $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $setupPath)) {
    throw "Instalador nao encontrado em: $setupPath"
}

# 4. Higiene obrigatoria: purgar pastas Debug efemeras
Get-ChildItem -Path $repoRoot -Recurse -Directory -Filter "Debug" -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

$setupItem = Get-Item -LiteralPath $setupPath
$setupSize = $setupItem.Length
$setupSha256 = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash
Write-Host "Instalador pronto: $($setupItem.Name) ($setupSize bytes | SHA256: $setupSha256)" -ForegroundColor Green

# 5. Localizar nota de liberacao correspondente
$notesFile = Get-ChildItem -Path (Join-Path $repoRoot "docs\releases") -Filter "*$shortVersion*.md" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

# 6. Criar e enviar tag v<fullVersion> para origin e public
$existingTag = & git tag -l $tagName
if (-not $existingTag) {
    Write-Host "Criando tag anotada $tagName..." -ForegroundColor Yellow
    & git tag -a $tagName -m "Release $tagName"
    if ($LASTEXITCODE -ne 0) { throw "Falha ao criar tag $tagName." }
}

foreach ($remote in @("origin", "public")) {
    $hasRemote = & git remote | Where-Object { $_ -eq $remote }
    if ($hasRemote) {
        Write-Host "Enviando tag $tagName para remote '$remote'..." -ForegroundColor Yellow
        & git push $remote $tagName
        if ($LASTEXITCODE -ne 0) { throw "Falha ao enviar tag $tagName para '$remote'." }
    }
}

# 7. Publicar ou atualizar GitHub Release nos repositorios alvo
$releaseTitle = "KyraFiles $shortVersion"
foreach ($repo in $Repos) {
    Write-Host "Publicando release $tagName em $repo..." -ForegroundColor Yellow
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & gh release view $tagName --repo $repo 2>$null | Out-Null
    $releaseExists = ($LASTEXITCODE -eq 0)
    $ErrorActionPreference = $prevEap
    if ($releaseExists) {
        & gh release upload $tagName $setupPath --repo $repo --clobber
        if ($LASTEXITCODE -ne 0) { throw "Falha ao atualizar asset em $repo." }
    } else {
        $ghArgs = @("release", "create", $tagName, $setupPath, "--repo", $repo, "--title", $releaseTitle, "--latest")
        if ($notesFile) {
            $ghArgs += @("--notes-file", $notesFile.FullName)
        } else {
            $ghArgs += @("--notes", "Release $tagName ($($setupItem.Name), SHA256: $setupSha256)")
        }
        & gh @ghArgs
        if ($LASTEXITCODE -ne 0) { throw "Falha ao criar release $tagName em $repo." }
    }
}

# 8. Verificacao em producao na API real consultada pelo app
Write-Host "Verificando API publica de atualizacoes..." -ForegroundColor Yellow
$headers = @{ "User-Agent" = "KyraFiles-UpdateCheck"; "Accept" = "application/vnd.github+json" }
$latest = Invoke-RestMethod -Uri "https://api.github.com/repos/alxchs/KyraFiles/releases/latest" -Headers $headers
$publishedAsset = $latest.assets | Where-Object { $_.name -eq $setupItem.Name } | Select-Object -First 1

if ($latest.tag_name -ne $tagName -or -not $publishedAsset -or $publishedAsset.size -ne $setupSize) {
    throw "Verificacao em producao falhou! Esperado tag=$tagName, asset=$($setupItem.Name) ($setupSize bytes); obtido tag=$($latest.tag_name), asset=$($publishedAsset.name) ($($publishedAsset.size) bytes)."
}

Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  PUBLICACAO CONCLUIDA E VERIFICADA NO AR: $tagName" -ForegroundColor Green
Write-Host "  URL: $($publishedAsset.browser_download_url)" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
exit 0
