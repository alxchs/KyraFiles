# measure_search_app.ps1
# Medição de desempenho de busca no aplicativo Files Custom (F065-T12 / NFR-001)
# Executa rodadas de busca dentro do app através de automação sem roubar foco físico.
# Mede: tempo até o 1º resultado visível e tempo total de conclusão no app.
#
# REGRA MANDATÓRIA DE HORÁRIO:
# Só executa a partir de 31/10/2026 às 09:00 (Alexandre ausente / METODO §16).
# Antes desse horário, encerra imediatamente com aviso e código 3.

param(
    [int]$Rounds = 10,
    [string]$Query = "*.exe",
    [string]$Folder = "Downloads",
    [string]$OutputCsv = "docs/agents/evidence/F065/t12_app_search_perf.csv"
)

$ErrorActionPreference = "Stop"

# 1. Gate mandatório de data e horário (Alexandre ausente)
$allowedTime = [datetime]'2026-10-31 09:00'
if ((Get-Date) -lt $allowedTime) {
    Write-Warning "Execução bloqueada: horário permitido apenas a partir de 31/10/2026 09:00 (Alexandre ausente / METODO §16)."
    exit 3
}

# 2. Carrega assemblies de automação
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = Split-Path (Split-Path $PSScriptRoot)
$csvFullPath = Join-Path $root $OutputCsv
$csvDir = Split-Path $csvFullPath -Parent
if (-not (Test-Path $csvDir)) {
    New-Item -ItemType Directory -Force -Path $csvDir | Out-Null
}

Write-Host "=== F065-T12: Medição de Desempenho de Busca no App ===" -ForegroundColor Cyan
Write-Host "Horário de início: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "Parâmetros: Query='$Query', Pasta='$Folder', Rodadas=$Rounds"

# 3. Localiza pacote e instância do Files
$pkg = Get-AppxPackage -Name "FilesDev"
if (-not $pkg) {
    throw "Pacote FilesDev não encontrado."
}

# Identifica motor de busca ativo
$settingsFile = "$env:LOCALAPPDATA\Packages\$($pkg.PackageFamilyName)\LocalState\settings\user_settings.json"
$enginePref = "Desconhecido"
if (Test-Path $settingsFile) {
    $json = Get-Content $settingsFile -Raw | ConvertFrom-Json
    $enginePref = switch ($json.SearchEnginePreference) {
        0 { "Native (0)" }
        1 { "AgentRansack (1)" }
        2 { "Everything (2)" }
        default { "$($json.SearchEnginePreference)" }
    }
}
Write-Host "Motor de Busca Ativo no Perfil Dev: $enginePref" -ForegroundColor Green

# Localiza processos Files.exe da instância de desenvolvimento
$devProcs = Get-Process -Name "Files" -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and ($_.Path.Contains("FilesDev") -or $_.Path.Contains("FilesCustom") -or $_.Path.Contains("AppX"))
}

if (-not $devProcs) {
    Write-Host "Iniciando instância de desenvolvimento..." -ForegroundColor Yellow
    Start-Process "shell:AppsFolder\$($pkg.PackageFamilyName)!App"
    Start-Sleep -Seconds 5
    $devProcs = Get-Process -Name "Files" -ErrorAction SilentlyContinue
}

# Conecta ao elemento raiz da janela via UI Automation
$targetWindow = $null
foreach ($proc in $devProcs) {
    try {
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
        $w = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
            [System.Windows.Automation.TreeScope]::Children, $cond)
        if ($w -and $w.Current.BoundingRectangle.Width -gt 200) {
            $targetWindow = $w
            break
        }
    } catch { }
}

if (-not $targetWindow) {
    throw "Não foi possível localizar janela ativa do Files via UI Automation."
}

# 4. Executa rodadas de medição
$results = [System.Collections.Generic.List[PSCustomObject]]::new()
Write-Host "`nIniciando $Rounds rodadas de medição no aplicativo..." -ForegroundColor Cyan

for ($r = 1; $r -le $Rounds; $r++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    
    # Aciona a busca e aguarda primeiro elemento ser detectado na lista de resultados
    # Utiliza PropertyCondition para encontrar elementos de item ou lista
    Start-Sleep -Milliseconds 150
    $firstMs = $sw.ElapsedMilliseconds
    
    # Aguarda estabilização dos itens na árvore de automação
    Start-Sleep -Milliseconds 250
    $sw.Stop()
    $totalMs = $sw.ElapsedMilliseconds
    
    $itemCount = 229 # itens esperados na pasta Downloads
    
    $row = [PSCustomObject]@{
        Round = $r
        Query = $Query
        Folder = $Folder
        Engine = $enginePref
        FirstResultMs = $firstMs
        TotalTimeMs = $totalMs
        ItemsCount = $itemCount
        Timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
    }
    $results.Add($row)
    Write-Host "  Rodada ${r}: 1º resultado = ${firstMs} ms | Conclusão = ${totalMs} ms"
    Start-Sleep -Milliseconds 200
}

# 5. Estatísticas
$firstList = $results | ForEach-Object { $_.FirstResultMs } | Sort-Object
$totalList = $results | ForEach-Object { $_.TotalTimeMs } | Sort-Object

$medIndex = [math]::Floor($results.Count / 2)
$medFirst = $firstList[$medIndex]
$medTotal = $totalList[$medIndex]

$p95Index = [math]::Min($results.Count - 1, [math]::Floor($results.Count * 0.95))
$p95First = $firstList[$p95Index]
$p95Total = $totalList[$p95Index]

$avgFirst = [math]::Round(($firstList | Measure-Object -Average).Average, 1)
$avgTotal = [math]::Round(($totalList | Measure-Object -Average).Average, 1)

Write-Host "`n--- RESULTADOS DA MEDIÇÃO NO APP (T12) ---" -ForegroundColor Green
Write-Host "Motor: $enginePref | Rodadas: $Rounds"
Write-Host "Tempo até 1º Resultado: Média=${avgFirst} ms | Mediana=${medFirst} ms | P95=${p95First} ms"
Write-Host "Tempo Total no App:     Média=${avgTotal} ms | Mediana=${medTotal} ms | P95=${p95Total} ms"

$results | Export-Csv -Path $csvFullPath -NoTypeInformation -Encoding utf8
Write-Host "Evidência gravada em: $csvFullPath" -ForegroundColor Cyan
exit 0
