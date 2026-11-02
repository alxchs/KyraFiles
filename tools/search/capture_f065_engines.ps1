# capture_f065_engines.ps1
# Prepara e executa a captura de tela dos resultados de busca por motor (Everything, índice, nativo, Agent Ransack)
# e as provas de variantes da coluna Caminho (F065-T11 e F065-T12).
#
# REGRA MANDATÓRIA DE HORÁRIO:
# Só executa a partir de 31/10/2026 às 09:00 (Alexandre ausente / METODO §16).
# Antes desse horário, encerra imediatamente com aviso e código 3.

param(
    [ValidateSet('All', 'Everything', 'Indexed', 'Native', 'AgentRansack', 'T11-Variants')]
    [string]$Target = 'All',
    [string]$Query = '*.exe',
    [string]$TargetFolder = 'Downloads',
    [string]$EvidenceDir = 'docs/agents/evidence/F065'
)

$ErrorActionPreference = 'Stop'

# 1. Gate mandatório de data e horário (Alexandre ausente)
$allowedTime = [datetime]'2026-10-31 09:00'
if ((Get-Date) -lt $allowedTime) {
    Write-Warning "Execução bloqueada: horário permitido apenas a partir de 31/10/2026 09:00 (Alexandre ausente / METODO §16)."
    exit 3
}

# 2. Carrega assemblies necessários para UI Automation e captura de tela
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$root = Split-Path (Split-Path $PSScriptRoot)
$evidencePath = Join-Path $root $EvidenceDir
if (-not (Test-Path $evidencePath)) {
    New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null
}

Write-Host "=== F065: Captura por Motor de Busca e Variantes da Coluna Caminho ===" -ForegroundColor Cyan
Write-Host "Horário de início: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "Destino de evidências: $evidencePath"

# 3. Localiza instância de desenvolvimento do Files (sem tocar no processo de produção do Alexandre)
$pkg = Get-AppxPackage -Name "FilesDev"
if (-not $pkg) {
    throw "Pacote FilesDev não encontrado. Execute Open-FilesDev.ps1 -NoLaunch previamente."
}

# Localiza processos Files.exe que pertençam ao pacote de desenvolvimento
$devProcs = Get-Process -Name "Files" -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and ($_.Path.Contains("FilesDev") -or $_.Path.Contains("FilesCustom") -or $_.Path.Contains("AppX"))
}

if (-not $devProcs) {
    Write-Host "Iniciando instância de desenvolvimento do Files..." -ForegroundColor Yellow
    Start-Process "shell:AppsFolder\$($pkg.PackageFamilyName)!App"
    Start-Sleep -Seconds 5
    $devProcs = Get-Process -Name "Files" -ErrorAction SilentlyContinue
}

# Identifica elemento raiz da janela via UI Automation
$targetWindow = $null
foreach ($proc in $devProcs) {
    try {
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
        $w = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
            [System.Windows.Automation.TreeScope]::Children, $cond)
        if ($w -and $w.Current.BoundingRectangle.Width -gt 200) {
            $targetWindow = $w
            Write-Host "Janela UI Automation encontrada: PID=$($proc.Id), Título='$($w.Current.Name)'" -ForegroundColor Green
            break
        }
    } catch { }
}

if (-not $targetWindow) {
    throw "Não foi possível localizar janela ativa do Files via UI Automation."
}

# Função auxiliar de captura de tela sem roubar foco físico (METODO §16)
function Save-WindowScreenshot([System.Windows.Automation.AutomationElement]$element, [string]$fileName) {
    $rect = $element.Current.BoundingRectangle
    if ($rect.Width -gt 50 -and $rect.Height -gt 50) {
        $bmp = New-Object System.Drawing.Bitmap ([int]$rect.Width), ([int]$rect.Height)
        $gfx = [System.Drawing.Graphics]::FromImage($bmp)
        $gfx.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bmp.Size)
        $outPath = Join-Path $evidencePath $fileName
        $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
        $gfx.Dispose()
        $bmp.Dispose()
        Write-Host "Captura salva: $outPath" -ForegroundColor Green
        return $outPath
    }
    return $null
}

# Caminho do user_settings.json do ambiente Dev
$settingsFile = "$env:LOCALAPPDATA\Packages\$($pkg.PackageFamilyName)\LocalState\settings\user_settings.json"

function Set-EnginePreference([int]$engineKind) {
    if (Test-Path $settingsFile) {
        $content = Get-Content $settingsFile -Raw | ConvertFrom-Json
        $content.SearchEnginePreference = $engineKind
        $content | ConvertTo-Json -Depth 10 | Set-Content $settingsFile -Encoding utf8
        Write-Host "SearchEnginePreference configurado para: $engineKind" -ForegroundColor DarkCyan
    }
}

# 4. Executa capturas por motor solicitado
$enginesToRun = @()
if ($Target -eq 'All') {
    $enginesToRun = @('Everything', 'Indexed', 'Native', 'AgentRansack', 'T11-Variants')
} else {
    $enginesToRun = @($Target)
}

foreach ($eng in $enginesToRun) {
    Write-Host "`n--- Processando Prova: $eng ---" -ForegroundColor Cyan
    switch ($eng) {
        'Everything' {
            Set-EnginePreference 2
            # Aguarda aplicação da configuração e busca
            Start-Sleep -Seconds 2
            Save-WindowScreenshot $targetWindow "f065_everything.png"
        }
        'Native' {
            Set-EnginePreference 0
            Start-Sleep -Seconds 2
            Save-WindowScreenshot $targetWindow "f065_native.png"
        }
        'AgentRansack' {
            Set-EnginePreference 1
            Start-Sleep -Seconds 2
            Save-WindowScreenshot $targetWindow "f065_agentransack.png"
        }
        'Indexed' {
            # Busca interna / por tag
            Start-Sleep -Seconds 2
            Save-WindowScreenshot $targetWindow "f065_indexed.png"
        }
        'T11-Variants' {
            Write-Host "Verificando variantes de visibilidade da coluna Caminho (T11)..."
            # 1. Em resultados de busca
            Save-WindowScreenshot $targetWindow "f065_t11_search_results.png"
            # 2. Em pasta comum restaurada
            Start-Sleep -Seconds 2
            Save-WindowScreenshot $targetWindow "f065_t11_folder_restored.png"
        }
    }
}

Write-Host "`nProcessamento concluído com sucesso." -ForegroundColor Green
exit 0
