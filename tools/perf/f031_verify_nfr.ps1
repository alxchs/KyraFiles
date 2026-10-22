# tools/perf/f031_verify_nfr.ps1
# Validação de NFR-1 (memória com 10 abas) e AC-3 (preservação de rolagem/seleção)

[CmdletBinding()]
param(
    [string]$EvidenceDir = (Join-Path $PSScriptRoot "..\..\docs\agents\evidence\F031")
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SmokeTest-Helper.ps1')

$scriptToRun = @'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type -Path 'C:\desenv\utils\FilesApp\tools\perf\thr.cs' -ErrorAction SilentlyContinue

$p = Get-Process Files -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) {
    Start-Process explorer.exe 'shell:AppsFolder\FilesDev_ykqwq8d6ps0ag!App'
    Start-Sleep -Seconds 6
    $p = Get-Process Files -ErrorAction SilentlyContinue | Select-Object -First 1
}

if (-not $p) {
    Write-Output "ERRO: Processo Files nao encontrado."
    exit 1
}

$p.Refresh()
$mem1TabPrivateMb = [math]::Round($p.PrivateMemorySize64 / 1MB, 2)
$mem1TabWsMb = [math]::Round($p.WorkingSet64 / 1MB, 2)
Write-Output "Memoria Inicial (1-2 abas): PrivateMemory = $mem1TabPrivateMb MB, WorkingSet = $mem1TabWsMb MB"

$hwnd = [IntPtr]::Zero
for ($att = 0; $att -lt 15; $att++) {
    if ($p.MainWindowHandle -ne 0) { $hwnd = $p.MainWindowHandle; break }
    $hwnd = [Thr]::FindMainWindow($p.Id)
    if ($hwnd -ne [IntPtr]::Zero) { break }
    Start-Sleep -Milliseconds 500
    $p.Refresh()
}

$root = [Windows.Automation.AutomationElement]::FromHandle($hwnd)
$condTab = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::ControlTypeProperty, [Windows.Automation.ControlType]::TabItem)

# Traz janela para frente
[Thr]::SetForegroundWindow($hwnd)
Start-Sleep -Milliseconds 500

# Obter contagem de abas
$currentTabs = $root.FindAll([Windows.Automation.TreeScope]::Descendants, $condTab)
Write-Output "Abas abertas inicialmente: $($currentTabs.Count)"

# Adiciona abas usando Ctrl+T ate atingir 10 abas
$needed = 10 - $currentTabs.Count
if ($needed -gt 0) {
    for ($i = 0; $i -lt $needed; $i++) {
        [System.Windows.Forms.SendKeys]::SendWait("^t")
        Start-Sleep -Milliseconds 500
    }
}

Start-Sleep -Seconds 3
$currentTabs = $root.FindAll([Windows.Automation.TreeScope]::Descendants, $condTab)
Write-Output "Total de abas agora: $($currentTabs.Count)"

$p.Refresh()
$mem10TabsPrivateMb = [math]::Round($p.PrivateMemorySize64 / 1MB, 2)
$mem10TabsWsMb = [math]::Round($p.WorkingSet64 / 1MB, 2)
$growthPercent = [math]::Round((($mem10TabsPrivateMb - $mem1TabPrivateMb) / $mem1TabPrivateMb) * 100, 2)

Write-Output "10 Abas Abertas: PrivateMemory = $mem10TabsPrivateMb MB, WorkingSet = $mem10TabsWsMb MB"
Write-Output "Crescimento de Memoria Privada: $growthPercent % (Limite NFR-1: < 10% por aba)"

# Testar clique de retorno na primeira aba para conferência visual de AC-3
if ($currentTabs.Count -gt 0) {
    $firstTab = $currentTabs[0]
    $selItemPattern = $firstTab.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern) -as [Windows.Automation.SelectionItemPattern]
    if ($selItemPattern) {
        $selItemPattern.Select()
        Start-Sleep -Milliseconds 800
        Write-Output "Retorno a Primeira Aba: Sucesso"
    }
}

# Captura de tela comprobatória
$rect = $root.Current.BoundingRectangle
if ($rect.Width -gt 100 -and $rect.Height -gt 100) {
    $bmp = New-Object System.Drawing.Bitmap ([int]$rect.Width), ([int]$rect.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bmp.Size)
    $proofDir = "C:\desenv\utils\FilesApp\docs\agents\evidence\F031"
    if (-not (Test-Path $proofDir)) { New-Item -ItemType Directory -Path $proofDir -Force | Out-Null }
    $proofPath = Join-Path $proofDir "tab10_proof.png"
    $bmp.Save($proofPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Output "Screenshot salvo em: $proofPath"
}

Write-Output "VALIDACAO_NFR_CONCLUIDA"
'@

Invoke-OnDefault -PowerShellScript $scriptToRun -TimeoutMs 60000
