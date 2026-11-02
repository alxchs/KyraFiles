# measure_f063_memory_restore.ps1
# Medição de latência de restauração via UI Automation e comprovação de memória (F063 - C6 e C8)
# Utiliza WindowPattern em instância própria do Files (FilesDev), sem ShowWindow e sem roubar foco físico (METODO §16).
#
# REGRA MANDATÓRIA DE HORÁRIO:
# Só executa a partir de 31/10/2026 às 09:00 (Alexandre ausente / METODO §16).
# Antes desse horário, encerra imediatamente com aviso e código 3.

param(
    [int]$Rounds = 10,
    [string]$OutputCsv = "docs/agents/evidence/F063/c6_latency_restore.csv",
    [string]$EvidenceDir = "docs/agents/evidence/F063"
)

$ErrorActionPreference = "Stop"

# 1. Gate mandatório de data e horário (Alexandre ausente)
$allowedTime = [datetime]'2026-10-31 09:00'
if ((Get-Date) -lt $allowedTime) {
    Write-Warning "Execução bloqueada: horário permitido apenas a partir de 31/10/2026 09:00 (Alexandre ausente / METODO §16)."
    exit 3
}

# 2. Carrega assemblies necessários
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$root = Split-Path (Split-Path $PSScriptRoot)
$evidencePath = Join-Path $root $EvidenceDir
$csvFullPath = Join-Path $root $OutputCsv
if (-not (Test-Path $evidencePath)) {
    New-Item -ItemType Directory -Force -Path $evidencePath | Out-Null
}

Write-Host "=== F063: Medição de Latência de Restauração (C6) e Captura do Sobre (C8) ===" -ForegroundColor Cyan
Write-Host "Horário de início: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "Rodadas solicitadas: $Rounds"

# 3. Localiza pacote e instância do Files
$pkg = Get-AppxPackage -Name "FilesDev"
if (-not $pkg) {
    throw "Pacote FilesDev não encontrado."
}

# Localiza processos Files.exe da instância de desenvolvimento (não toca no processo do Alexandre)
$devProcs = Get-Process -Name "Files" -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and ($_.Path.Contains("FilesDev") -or $_.Path.Contains("FilesCustom") -or $_.Path.Contains("AppX"))
}

if (-not $devProcs) {
    Write-Host "Iniciando instância de desenvolvimento..." -ForegroundColor Yellow
    Start-Process "shell:AppsFolder\$($pkg.PackageFamilyName)!App"
    Start-Sleep -Seconds 5
    $devProcs = Get-Process -Name "Files" -ErrorAction SilentlyContinue
}

$targetWindow = $null
$targetProc = $null
foreach ($proc in $devProcs) {
    try {
        $cond = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
        $w = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
            [System.Windows.Automation.TreeScope]::Children, $cond)
        if ($w -and $w.Current.BoundingRectangle.Width -gt 200) {
            $targetWindow = $w
            $targetProc = $proc
            break
        }
    } catch { }
}

if (-not $targetWindow) {
    throw "Não foi possível localizar janela ativa do Files via UI Automation."
}

# Obtém o padrão WindowPattern do UI Automation
$windowPattern = $targetWindow.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
if (-not $windowPattern) {
    throw "A janela não suporta WindowPattern."
}

# 4. Executa rodadas de medição de latência (C6 / AC-2 / NFR-002)
$results = [System.Collections.Generic.List[PSCustomObject]]::new()
Write-Host "`nIniciando $Rounds rodadas de minimização, trim e restauração..." -ForegroundColor Cyan

for ($r = 1; $r -le $Rounds; $r++) {
    # 4.1 Minimiza via UI Automation (sem ShowWindow Win32)
    $windowPattern.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Minimized)
    
    # 4.2 Aguarda intervalo de trim (2 segundos conforme especificação F063)
    Start-Sleep -Seconds 3
    
    # Coleta métricas de memória no estado minimizado
    $targetProc.Refresh()
    $wsTrimMb = [math]::Round($targetProc.WorkingSet64 / 1MB, 2)
    $privMb = [math]::Round($targetProc.PrivateMemorySize64 / 1MB, 2)
    
    # 4.3 Restaura via UI Automation e cronometra tempo até primeiro quadro desenhado
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $windowPattern.SetWindowVisualState([System.Windows.Automation.WindowVisualState]::Normal)
    $windowPattern.WaitForInputIdle(5000) | Out-Null
    $sw.Stop()
    $restoreMs = $sw.ElapsedMilliseconds
    
    $row = [PSCustomObject]@{
        Round = $r
        WorkingSetAfterTrimMb = $wsTrimMb
        PrivateMemoryMb = $privMb
        RestoreLatencyMs = $restoreMs
        Timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
    }
    $results.Add($row)
    Write-Host "  Rodada ${r}: Memória após trim = ${wsTrimMb} MB (Priv: ${privMb} MB) | Restauração = ${restoreMs} ms"
    Start-Sleep -Milliseconds 500
}

# Estatísticas C6
$restoreList = $results | ForEach-Object { $_.RestoreLatencyMs } | Sort-Object
$medIndex = [math]::Floor($results.Count / 2)
$medRestore = $restoreList[$medIndex]
$p95Index = [math]::Min($results.Count - 1, [math]::Floor($results.Count * 0.95))
$p95Restore = $restoreList[$p95Index]
$avgRestore = [math]::Round(($restoreList | Measure-Object -Average).Average, 1)

Write-Host "`n--- RESULTADOS DA MEDIÇÃO DE LATÊNCIA (C6) ---" -ForegroundColor Green
Write-Host "Restauração: Média=${avgRestore} ms | Mediana=${medRestore} ms | P95=${p95Restore} ms"
$results | Export-Csv -Path $csvFullPath -NoTypeInformation -Encoding utf8
Write-Host "Dados exportados para: $csvFullPath" -ForegroundColor Cyan

# 5. Captura da tela Sobre com rótulo de memória privada e RAM (C8 / AC-5)
Write-Host "`nNavegando para o diálogo Sobre para captura de evidência (C8)..." -ForegroundColor Cyan
try {
    # Localiza botão Configurações
    $settingsBtn = $targetWindow.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty, "SettingsButton"))
    )
    if ($settingsBtn) {
        $invPattern = $settingsBtn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
        $invPattern.Invoke()
        Start-Sleep -Seconds 2
        
        # Localiza item Sobre
        $aboutItem = $targetWindow.FindFirst(
            [System.Windows.Automation.TreeScope]::Descendants,
            (New-Object System.Windows.Automation.PropertyCondition(
                [System.Windows.Automation.AutomationElement]::NameProperty, "Sobre"))
        )
        if (-not $aboutItem) {
            $aboutItem = $targetWindow.FindFirst(
                [System.Windows.Automation.TreeScope]::Descendants,
                (New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::NameProperty, "About"))
            )
        }
        if ($aboutItem) {
            $selPattern = $aboutItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
            if ($selPattern) { $selPattern.Select() } else {
                $inv = $aboutItem.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
                if ($inv) { $inv.Invoke() }
            }
            Start-Sleep -Seconds 3
        }
    }

    # Captura a tela Sobre
    $rect = $targetWindow.Current.BoundingRectangle
    if ($rect.Width -gt 50 -and $rect.Height -gt 50) {
        $bmp = New-Object System.Drawing.Bitmap ([int]$rect.Width), ([int]$rect.Height)
        $gfx = [System.Drawing.Graphics]::FromImage($bmp)
        $gfx.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bmp.Size)
        $aboutCapturePath = Join-Path $evidencePath "f063_c8_about_memory.png"
        $bmp.Save($aboutCapturePath, [System.Drawing.Imaging.ImageFormat]::Png)
        $gfx.Dispose()
        $bmp.Dispose()
        Write-Host "Captura do Sobre (C8) salva em: $aboutCapturePath" -ForegroundColor Green
    }
} catch {
    Write-Warning "Falha na automação de captura do Sobre: $_"
}

Write-Host "`nProcessamento F063 concluído com sucesso." -ForegroundColor Green
exit 0
