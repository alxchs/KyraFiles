# Gera manifesto SHA256 do pacote para permitir atualizações diferenciais (delta) e detecção de integridade
param(
    [string]$PackageDir = "src\KyraFiles.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64",
    [string]$OutputFile = "installer\package_manifest.json"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $PackageDir)) {
    throw "Diretório de pacote não encontrado: $PackageDir"
}

Write-Host "Calculando hashes SHA256 para arquivos em: $PackageDir"
$files = Get-ChildItem -Path $PackageDir -File -Recurse | Where-Object {
    $_.Name -notmatch '\.locked\.' -and $_.Extension -ne '.pdb'
}

$manifestItems = [System.Collections.Generic.List[PSObject]]::new()
$totalBytes = 0

foreach ($f in $files) {
    $relPath = $f.FullName.Substring((Resolve-Path $PackageDir).Path.Length).TrimStart('\')
    
    # Classifica se é binário principal ou dependência pesada
    $isDependency = $f.Extension -match '\.(dll|winmd|pri)$' -and $f.Name -ne 'KyraFiles.dll' -and $f.Name -ne 'KyraFiles.exe' -and $f.Name -ne 'Files.dll' -and $f.Name -ne 'Files.exe'
    $totalBytes += $f.Length
    
    $manifestItems.Add([PSCustomObject]@{
        Path = $relPath
        Length = $f.Length
        SHA256 = $hash
        IsDependency = $isDependency
    })
}

$issFile = Join-Path $PSScriptRoot "KyraFilesSetup.iss"
$version = "4.3.11"
if (Test-Path $issFile) {
    $issContent = Get-Content $issFile -Raw
    if ($issContent -match '#define MyAppVersion "([^"]+)"') {
        $version = $matches[1]
    }
}

$manifest = [PSCustomObject]@{
    Version = $version
    GeneratedAtUtc = [System.DateTime]::UtcNow.ToString("o")
    TotalFiles = $manifestItems.Count
    TotalBytes = $totalBytes
    Files = $manifestItems
}

$json = $manifest | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot "package_manifest.json"), $json, [System.Text.Encoding]::UTF8)

Write-Host "Manifesto gerado com sucesso: $(Join-Path $PSScriptRoot 'package_manifest.json') ($($manifestItems.Count) arquivos, $([math]::Round($totalBytes / 1MB, 2)) MB)"
