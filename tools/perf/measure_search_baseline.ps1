# Medição de linha de base da busca para F065 (T0 / NFR-001)
# Executa 10 rodadas de busca por *.exe na pasta Downloads do usuário
# Mede: tempo até o 1º resultado (FirstResultMs) e tempo total (TotalTimeMs)
# Instância própria via streaming direto de es.exe com o mesmo pipeline de EverythingSearchProvider, sem roubar foco.

param(
    [int]$Rounds = 10,
    [string]$Query = "*.exe",
    [string]$OutputCsv = "docs/agents/evidence/F065/t0_baseline.csv"
)

$ErrorActionPreference = "Stop"

# 1. Determina pasta Downloads do usuário (lendo do registro ou pasta padrão)
$downloads = "D:\DriveD\Downloads"
if (-not (Test-Path $downloads)) {
    try {
        $regVal = (Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" -ErrorAction SilentlyContinue)."{374DE290-123F-4565-9164-39C4925E467B}"
        if ($regVal) {
            $downloads = [System.Environment]::ExpandEnvironmentVariables($regVal)
        }
    } catch { }
}
if (-not (Test-Path $downloads)) {
    $downloads = Join-Path $env:USERPROFILE "Downloads"
}

Write-Host "Pasta de Downloads: $downloads" -ForegroundColor Cyan

# 2. Localiza es.exe
$possibleEsPaths = @(
    "C:\Users\alxch\bin\es.exe",
    (Join-Path $env:LOCALAPPDATA "Programs\FilesCustom\es.exe"),
    "C:\Program Files\Everything\es.exe",
    "C:\Program Files (x86)\Everything\es.exe"
)
$esPath = $null
foreach ($p in $possibleEsPaths) {
    if (Test-Path $p) {
        $esPath = $p
        break
    }
}
if (-not $esPath) {
    throw "es.exe não encontrado em nenhum dos locais esperados."
}
Write-Host "Executável Everything CLI: $esPath" -ForegroundColor Cyan

# 3. Anota o motor configurado no user_settings.json do Alexandre
$settingsFile = "$env:LOCALAPPDATA\Packages\FilesDev_ykqwq8d6ps0ag\LocalState\settings\user_settings.json"
$enginePref = "Desconhecido"
if (Test-Path $settingsFile) {
    $json = Get-Content $settingsFile -Raw | ConvertFrom-Json
    $enginePref = switch ($json.SearchEnginePreference) {
        0 { "Native (0)" }
        1 { "AgentRansack (1)" }
        2 { "Everything (2)" }
        default { "$($json.SearchEnginePreference)" }
    }
    Write-Host "Motor de Busca Configurado do Alexandre: $enginePref" -ForegroundColor Green
    Write-Host "ShowPathColumn: $($json.ShowPathColumn), PathColumnWidth: $($json.PathColumnWidth)" -ForegroundColor Green
}

# 4. Rodada de aquecimento (warmup)
Write-Host "`nExecutando rodada de aquecimento (warmup)..." -ForegroundColor Yellow
$psiWarmup = [System.Diagnostics.ProcessStartInfo]::new()
$psiWarmup.FileName = $esPath
$psiWarmup.Arguments = "-tsv -no-header -attrib -size -dm -n 500 -path `"$downloads`" $Query"
$psiWarmup.UseShellExecute = $false
$psiWarmup.CreateNoWindow = $true
$psiWarmup.RedirectStandardOutput = $true
$procWarmup = [System.Diagnostics.Process]::Start($psiWarmup)
$procWarmup.StandardOutput.ReadToEnd() | Out-Null
$procWarmup.WaitForExit()

# 5. Executa as 10 rodadas de medição
$results = [System.Collections.Generic.List[PSCustomObject]]::new()
Write-Host "Iniciando $Rounds rodadas de medição..." -ForegroundColor Cyan

for ($r = 1; $r -le $Rounds; $r++) {
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $esPath
    $psi.Arguments = "-tsv -no-header -attrib -size -dm -n 500 -path `"$downloads`" $Query"
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $proc = [System.Diagnostics.Process]::Start($psi)

    $firstResultMs = $null
    $itemCount = 0

    $reader = $proc.StandardOutput
    while (($line = $reader.ReadLine()) -ne $null) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }

        if ($firstResultMs -eq $null) {
            $firstResultMs = $sw.ElapsedMilliseconds
        }

        # Simula parsing idêntico ao EverythingSearchProvider.cs
        $parts = $line.Split("`t")
        $path = if ($parts.Length -gt 0) { $parts[0].Trim().Trim('"') } else { "" }
        $attrStr = if ($parts.Length -gt 1) { $parts[1].Trim() } else { "" }
        $sizeStr = if ($parts.Length -gt 2) { $parts[2].Trim() } else { "" }
        $dateStr = if ($parts.Length -gt 3) { $parts[3].Trim() } else { "" }

        $isFolder = $path.EndsWith('\') -or $path.EndsWith('/')
        $cleanPath = if ($isFolder) { $path.TrimEnd('\', '/') } else { $path }
        $fileName = [System.IO.Path]::GetFileName($cleanPath)

        $itemCount++
    }

    $proc.WaitForExit()
    $sw.Stop()
    $totalMs = $sw.ElapsedMilliseconds
    $proc.Dispose()

    $firstMs = if ($firstResultMs -ne $null) { $firstResultMs } else { $totalMs }

    $resObj = [PSCustomObject]@{
        Round = $r
        Query = $Query
        Folder = $downloads
        Engine = $enginePref
        FirstResultMs = $firstMs
        TotalTimeMs = $totalMs
        ItemsCount = $itemCount
    }
    $results.Add($resObj)

    Write-Host "  Rodada ${r}: 1º resultado = ${firstMs} ms | Total = ${totalMs} ms | Itens = $itemCount"
    Start-Sleep -Milliseconds 100
}

# 6. Estatísticas
$firstList = $results | ForEach-Object { $_.FirstResultMs } | Sort-Object
$totalList = $results | ForEach-Object { $_.TotalTimeMs } | Sort-Object

$medIndex = [math]::Floor($results.Count / 2)
$medFirst = $firstList[$medIndex]
$medTotal = $totalList[$medIndex]

$avgFirst = [math]::Round(($firstList | Measure-Object -Average).Average, 1)
$avgTotal = [math]::Round(($totalList | Measure-Object -Average).Average, 1)

$minFirst = ($firstList | Measure-Object -Minimum).Minimum
$maxFirst = ($firstList | Measure-Object -Maximum).Maximum

$minTotal = ($totalList | Measure-Object -Minimum).Minimum
$maxTotal = ($totalList | Measure-Object -Maximum).Maximum

Write-Host "`n--- RESUMO DA LINHA DE BASE (T0) ---" -ForegroundColor Green
Write-Host "Motor Configurado: $enginePref"
Write-Host "Itens Retornados: $itemCount"
Write-Host "Tempo até o 1º Resultado: Méd=$avgFirst ms | Mediana=$medFirst ms | Min=$minFirst ms | Max=$maxFirst ms"
Write-Host "Tempo Total até o Fim:    Méd=$avgTotal ms | Mediana=$medTotal ms | Min=$minTotal ms | Max=$maxTotal ms"

# 7. Salva CSV se diretório existir
$csvDir = Split-Path $OutputCsv -Parent
if (-not (Test-Path $csvDir)) {
    New-Item -ItemType Directory -Path $csvDir -Force | Out-Null
}
$results | Export-Csv -Path $OutputCsv -NoTypeInformation -Encoding utf8
Write-Host "Resultados gravados em: $OutputCsv" -ForegroundColor Cyan
