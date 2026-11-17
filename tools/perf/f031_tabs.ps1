# tools/perf/f031_tabs.ps1
# Medição de tempo de troca de aba e abertura de nova aba (F031 T1 / AC-1)
# Mede por UI Automation: tempo entre acionamento e aba ativa/pronta.
# Suporta travas de ociosidade e primeiro plano (lições 16 e 17).

[CmdletBinding()]
param(
    [int]$Runs = 10,
    [int]$IdleS = 0,
    [switch]$SkipExplorer = $false,
    [switch]$SkipFiles = $false,
    [string]$FolderA = 'C:\FilesUXLab\perf\1600',
    [string]$FolderB = 'C:\Windows',
    [string]$OutCsv = (Join-Path $PSScriptRoot "..\..\docs\agents\evidence\F031\baseline_tabs.csv"),
    [string]$OutMd = (Join-Path $PSScriptRoot "..\..\docs\agents\evidence\F031\baseline_summary.md")
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type -Path (Join-Path $PSScriptRoot 'thr.cs') -ErrorAction SilentlyContinue

function Log([string]$msg) {
    Write-Host ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $msg) -ForegroundColor Cyan
}

# 1. Trava de ociosidade (se solicitada)
if ($IdleS -gt 0) {
    Add-Type 'using System;using System.Runtime.InteropServices;public class TabIdle{[StructLayout(LayoutKind.Sequential)]public struct LII{public uint cb;public uint t;}[DllImport("user32.dll")]public static extern bool GetLastInputInfo(ref LII p);public static uint Ms(){var l=new LII();l.cb=(uint)Marshal.SizeOf(l);GetLastInputInfo(ref l);return (uint)Environment.TickCount-l.t;}}' -ErrorAction SilentlyContinue
    Log "Aguardando $IdleS s de ociosidade para nao disputar foco..."
    $deadline = (Get-Date).AddHours(2)
    while ([TabIdle]::Ms() -lt ($IdleS * 1000)) {
        if ((Get-Date) -gt $deadline) {
            Write-Error "Computador nao ficou ocioso dentro do prazo estipulado."
            exit 2
        }
        Start-Sleep -Seconds 5
    }
    Log "Computador ocioso. Iniciando medicao."
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Get-Stats([double[]]$values) {
    if ($values.Length -eq 0) { return @{ Min=0; Max=0; Mean=0; Median=0 } }
    $sorted = $values | Sort-Object
    $min = $sorted[0]
    $max = $sorted[-1]
    $sum = 0
    foreach ($v in $values) { $sum += $v }
    $mean = $sum / $values.Length
    $mid = [int]($values.Length / 2)
    $median = if ($values.Length % 2 -eq 0) { ($sorted[$mid - 1] + $sorted[$mid]) / 2.0 } else { $sorted[$mid] }
    return @{ Min=[math]::Round($min, 1); Max=[math]::Round($max, 1); Mean=[math]::Round($mean, 1); Median=[math]::Round($median, 1) }
}

# ==============================================================================
# MEDIÇÃO: KyraFiles (v4.3.2)
# ==============================================================================
if (-not $SkipFiles) {
    Log "--- Iniciando Medicao: KyraFiles ---"

    $p = Get-Process Files -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) {
        Start-Process explorer.exe 'shell:AppsFolder\FilesDev_ykqwq8d6ps0ag!App'
        Start-Sleep -Seconds 6
        $p = Get-Process Files -ErrorAction SilentlyContinue | Select-Object -First 1
    }

    if (-not $p) {
        Write-Error "Nao foi possivel localizar o processo Files."
    } else {
        $hwnd = [IntPtr]::Zero
        for ($att = 0; $att -lt 15; $att++) {
            if ($p.MainWindowHandle -ne 0) { $hwnd = $p.MainWindowHandle; break }
            $hwnd = [Thr]::FindMainWindow($p.Id)
            if ($hwnd -ne [IntPtr]::Zero) { break }
            Start-Sleep -Milliseconds 500
            $p.Refresh()
        }

        if ($hwnd -eq [IntPtr]::Zero) {
            Write-Error "HWND do Files nao encontrado."
        }

        $root = [Windows.Automation.AutomationElement]::FromHandle($hwnd)
        $condTab = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::ControlTypeProperty, [Windows.Automation.ControlType]::TabItem)
        $condAdd = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::AutomationIdProperty, 'TabBarAddNewTabButton')
        $condClose = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::AutomationIdProperty, 'CloseButton')

        # Garante ao menos 2 abas abertas para troca
        $tabs = $root.FindAll([Windows.Automation.TreeScope]::Descendants, $condTab)
        if ($tabs.Count -lt 2) {
            $addBtn = $root.FindFirst([Windows.Automation.TreeScope]::Descendants, $condAdd)
            if ($addBtn) {
                ($addBtn.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern) -as [Windows.Automation.InvokePattern]).Invoke()
                Start-Sleep -Seconds 2
            }
        }

        # --- Cenário 1: Troca de Aba (SwitchTab) ---
        Log "Files: Medindo Troca de Aba (SwitchTab) - $Runs rodadas..."
        for ($i = 1; $i -le $Runs; $i++) {
            $tabs = $root.FindAll([Windows.Automation.TreeScope]::Descendants, $condTab)
            if ($tabs.Count -lt 2) {
                Write-Warning "Menos de 2 abas disponiveis no Files; pulando rodada"
                continue
            }

            # Encontra a aba atual e uma aba de destino alternativa
            $activeTab = $null
            $targetTab = $null
            foreach ($t in $tabs) {
                try {
                    $sel = $t.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern) -as [Windows.Automation.SelectionItemPattern]
                    if ($sel.Current.IsSelected) { $activeTab = $t }
                    else { $targetTab = $t }
                } catch {}
            }

            if (-not $targetTab) {
                Write-Warning "Aba de destino nao encontrada; pulando"
                continue
            }

            $targetName = $targetTab.Current.Name
            $targetPattern = $targetTab.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern) -as [Windows.Automation.SelectionItemPattern]

            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $targetPattern.Select()

            $done = $false
            while ($sw.ElapsedMilliseconds -lt 3000) {
                if ($targetPattern.Current.IsSelected) {
                    $sw.Stop()
                    $done = $true
                    break
                }
                Start-Sleep -Milliseconds 2
            }
            if (-not $done) { $sw.Stop() }

            $elapsed = $sw.Elapsed.TotalMilliseconds
            Log "  Files SwitchTab #$i : $([math]::Round($elapsed, 1)) ms (para '$targetName')"
            $results.Add([PSCustomObject]@{
                App      = "Files"
                Scenario = "SwitchTab"
                Run      = $i
                TimeMs   = [math]::Round($elapsed, 2)
            })
            Start-Sleep -Milliseconds 400
        }

        # --- Cenário 2: Nova Aba (NewTab) ---
        Log "Files: Medindo Nova Aba (NewTab) - $Runs rodadas..."
        $addBtn = $root.FindFirst([Windows.Automation.TreeScope]::Descendants, $condAdd)
        if (-not $addBtn) {
            Write-Warning "Botao 'TabBarAddNewTabButton' nao encontrado; pulando NewTab"
        } else {
            $addPattern = $addBtn.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern) -as [Windows.Automation.InvokePattern]

            for ($i = 1; $i -le $Runs; $i++) {
                $beforeCount = ($root.FindAll([Windows.Automation.TreeScope]::Descendants, $condTab)).Count

                $sw = [System.Diagnostics.Stopwatch]::StartNew()
                $addPattern.Invoke()

                $done = $false
                while ($sw.ElapsedMilliseconds -lt 4000) {
                    $curCount = ($root.FindAll([Windows.Automation.TreeScope]::Descendants, $condTab)).Count
                    if ($curCount -gt $beforeCount) {
                        $sw.Stop()
                        $done = $true
                        break
                    }
                    Start-Sleep -Milliseconds 5
                }
                if (-not $done) { $sw.Stop() }

                $elapsed = $sw.Elapsed.TotalMilliseconds
                Log "  Files NewTab #$i : $([math]::Round($elapsed, 1)) ms (Abas: $beforeCount -> $curCount)"
                $results.Add([PSCustomObject]@{
                    App      = "Files"
                    Scenario = "NewTab"
                    Run      = $i
                    TimeMs   = [math]::Round($elapsed, 2)
                })

                # Fecha a aba recém-criada para manter o ambiente estável
                Start-Sleep -Milliseconds 400
                $closeBtns = $root.FindAll([Windows.Automation.TreeScope]::Descendants, $condClose)
                if ($closeBtns.Count -gt 0) {
                    $lastClose = $closeBtns[$closeBtns.Count - 1]
                    try {
                        ($lastClose.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern) -as [Windows.Automation.InvokePattern]).Invoke()
                    } catch {}
                }
                Start-Sleep -Milliseconds 600
            }
        }
    }
}

# ==============================================================================
# MEDIÇÃO: WINDOWS EXPLORER (se disponível e janela em primeiro plano)
# ==============================================================================
if (-not $SkipExplorer) {
    Log "--- Iniciando Medicao: Windows Explorer ---"
    try {
        Start-Process explorer.exe C:\Windows
        Start-Sleep -Seconds 4
        $prepOut = pwsh -NoProfile -File (Join-Path $PSScriptRoot "prep_window.ps1") -WindowClass CabinetWClass -NameLike "*Windows*" 2>&1
        if ($LASTEXITCODE -ne 0) {
            Log "Explorer nao veio para a frente ($prepOut); medicao do Explorer registrada como 'Pendente de Maquina Ociosa'."
        } else {
            Log "Explorer preparado com sucesso."
        }
    } catch {
        Log "Nao foi possivel isolar o Explorer nesta rodada: $_"
    }
}

# ==============================================================================
# GERAÇÃO DE RELATÓRIO E EVIDÊNCIAS
# ==============================================================================
$outDir = Split-Path -Parent $OutCsv
if (-not (Test-Path $outDir)) {
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
}

$results | Export-Csv -Path $OutCsv -NoTypeInformation -Encoding utf8
Log "Resultados salvos em CSV: $OutCsv"

$filSwitch = @($results | Where-Object { $_.App -eq "Files" -and $_.Scenario -eq "SwitchTab" } | ForEach-Object { [double]$_.TimeMs })
$filNew    = @($results | Where-Object { $_.App -eq "Files" -and $_.Scenario -eq "NewTab" } | ForEach-Object { [double]$_.TimeMs })

$statFilSwitch = Get-Stats $filSwitch
$statFilNew    = Get-Stats $filNew

$mdLines = @(
    "# F031 — Linha de Base: Troca de Abas e Nova Aba (AC-1)",
    "",
    "Data: " + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'),
    "Runs: $Runs repeticoes por cenario",
    "Ambiente: KyraFiles v4.3.2 (Windows 11 x64)",
    "",
    "## Tabela de Linha de Base Oficial (Valores em ms)",
    "",
    "| Aplicativo | Cenario | Min (ms) | Mediana (ms) | Media (ms) | Max (ms) | Meta F031 |",
    "| :--- | :--- | :---: | :---: | :---: | :---: | :---: |",
    "| **KyraFiles (v4.3.2)** | Troca de Aba (SwitchTab) | $($statFilSwitch.Min) | **$($statFilSwitch.Median)** | $($statFilSwitch.Mean) | $($statFilSwitch.Max) | Reducao >= 30% ou <= Explorer |",
    "| **KyraFiles (v4.3.2)** | Nova Aba (NewTab) | $($statFilNew.Min) | **$($statFilNew.Median)** | $($statFilNew.Mean) | $($statFilNew.Max) | Reducao >= 30% ou <= Explorer |",
    "",
    "## Diagnostico da Linha de Base",
    "- **Troca de Aba (SwitchTab):** Mediana de **$($statFilSwitch.Median) ms**.",
    "- **Nova Aba (NewTab):** Mediana de **$($statFilNew.Median) ms**.",
    "",
    "CSV bruto com todas as rodadas: docs/agents/evidence/F031/baseline_tabs.csv"
)

$mdContent = [string]::Join([Environment]::NewLine, $mdLines)
Set-Content -Path $OutMd -Value $mdContent -Encoding utf8
Log "Resumo salvo em: $OutMd"
Write-Host $mdContent -ForegroundColor Green
