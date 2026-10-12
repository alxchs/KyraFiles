# F018: prova no app aberto (UI Automation) que o menu Ajuda tem "Sobre" e "Verificar atualizações" e que as duas funcionam.
# Temporarios e capturas em %TEMP%\claude\desenv\utils\FilesApp\.
param([string]$Aumid = 'FilesDev_ykqwq8d6ps0ag!App')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
$tmp = Join-Path $env:TEMP 'claude\desenv\utils\FilesApp'
New-Item -ItemType Directory -Force $tmp | Out-Null
$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$Scope = [System.Windows.Automation.TreeScope]

function Find-First($root, $name, $type) {
    $conds = @((New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $name)))
    if ($type) { $conds += New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $type) }
    $cond = if ($conds.Count -gt 1) { New-Object System.Windows.Automation.AndCondition($conds) } else { $conds[0] }
    $root.FindFirst($Scope::Descendants, $cond)
}
function Wait-For($scriptBlock, $seconds = 10) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $seconds) { $r = & $scriptBlock; if ($r) { return $r }; Start-Sleep -Milliseconds 300 }
    $null
}
function Invoke-Element($el) {
    $p = $null
    if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$p)) { $p.Invoke(); return }
    if ($el.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$p)) { $p.Expand(); return }
    throw "Elemento sem Invoke/Expand: $($el.Current.Name)"
}
Add-Type @"
using System; using System.Runtime.InteropServices;
public class F018W { [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r); [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); public struct RECT { public int L, T, R, B; } }
"@
# Captura so a janela do Files, trazida para a frente (outras janelas cobriam a tela inteira).
function Save-Shot($name) {
    $p = Get-Process Files | Where-Object MainWindowHandle -ne 0 | Select -First 1
    [void][F018W]::SetWindowPos($p.MainWindowHandle, [IntPtr](-1), 0, 0, 0, 0, 0x43)
    [void][F018W]::SetForegroundWindow($p.MainWindowHandle); Start-Sleep -Milliseconds 600
    $r = New-Object F018W+RECT; [void][F018W]::GetWindowRect($p.MainWindowHandle, [ref]$r)
    $bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
    [void][F018W]::SetWindowPos($p.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x43)
    $f = Join-Path $tmp "$name.png"; $bmp.Save($f); $f
}

Stop-Process -Name Files -Force -ErrorAction SilentlyContinue; Start-Sleep 2
Start-Process "shell:AppsFolder\$Aumid"
$win = Wait-For { $AE::RootElement.FindFirst($Scope::Children, (New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, (Get-Process Files -ErrorAction SilentlyContinue | Select -First 1).Id))) } 30
if (-not $win) { throw 'Janela do Files nao encontrada' }
Start-Sleep 5

$filesPid = (Get-Process Files | Select -First 1).Id
function Find-All($type) {
    $c = New-Object System.Windows.Automation.AndCondition((New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $filesPid)), (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $type)))
    $AE::RootElement.FindAll($Scope::Descendants, $c)
}

$result = [ordered]@{}
$help = Wait-For { Find-First $win 'Help' $CT::MenuItem } 10
$result['menu Ajuda encontrado'] = [bool]$help
Invoke-Element $help; Start-Sleep 1
$root = $AE::RootElement
$popupItems = Find-All $CT::MenuItem | ForEach-Object { $_.Current.Name } | Where-Object { $_ }
$result['itens (Ajuda)'] = ($popupItems | Select-Object -Unique) -join ' | '
$result['AC-1 tem Sobre'] = [bool]($popupItems -contains 'About' -or $popupItems -contains 'Sobre')
$result['AC-1 tem Verificar atualizacoes'] = [bool]($popupItems -contains 'Check for updates' -or $popupItems -contains 'Verificar atualizações')
$result['captura menu'] = Save-Shot 'f018_menu_ajuda'

# Verificar atualizacoes
$upd = $root.FindFirst($Scope::Descendants, (New-Object System.Windows.Automation.OrCondition(
    (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, 'Check for updates')),
    (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, 'Verificar atualizações')))))
if ($upd) { Invoke-Element $upd }
$dialogText = Wait-For {
    $t = Find-All $CT::Text | ForEach-Object { $_.Current.Name } | Where-Object { $_ -match 'published|publicada|latest|recente|available|dispon|Could not|Não foi' }
    if ($t) { $t -join ' / ' }
} 20
$result['AC-5/3/6 texto do dialogo'] = $dialogText
$result['captura dialogo'] = Save-Shot 'f018_dialogo_atualizacao'
$btn = $root.FindFirst($Scope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, 'OK')))
if ($btn) { Invoke-Element $btn }
Start-Sleep 1

# Sobre
$help = Find-First $win 'Help' $CT::MenuItem; Invoke-Element $help; Start-Sleep 1
$aboutMenu = Find-All $CT::MenuItem | Where-Object { $_.Current.Name -in 'About', 'Sobre' } | Select -Last 1
if ($aboutMenu) { Invoke-Element $aboutMenu }
Start-Sleep 4
$texts = Find-All $CT::Text | ForEach-Object { $_.Current.Name }
$result['AC-2 pagina Sobre mostra versao'] = [bool]($texts | Where-Object { $_ -match '\d+\.\d+\.\d+\.\d+' })
$result['AC-2 versoes vistas'] = ($texts | Where-Object { $_ -match '\d+\.\d+\.\d+\.\d+' } | Select-Object -Unique) -join ' | '
$result['captura sobre'] = Save-Shot 'f018_sobre'

$result.GetEnumerator() | ForEach-Object { '{0,-36} {1}' -f $_.Key, $_.Value }
Stop-Process -Name Files -Force -ErrorAction SilentlyContinue
