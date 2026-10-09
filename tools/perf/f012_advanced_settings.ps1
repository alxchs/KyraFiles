# F012 / diálogo de arquivos: abre Configurações > Avançado no Files Dev, lê ou alterna as
# duas opções por UI Automation, confere o registro e captura a janela.
#
# Uso: pwsh tools\perf\f012_advanced_settings.ps1 [-DefaultManager On|Off] [-FileDialog On|Off] [-Capture <nome>]

param(
    [ValidateSet('On', 'Off', '')] [string]$DefaultManager = '',
    [ValidateSet('On', 'Off', '')] [string]$FileDialog = '',
    [string]$Capture = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class F012Win {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint f, UIntPtr e);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
'@

$A = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]
function NameCond([string]$n) { [System.Windows.Automation.PropertyCondition]::new($A::NameProperty, $n) }
function IdCond([string]$n) { [System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty, $n) }
function WaitFind($root, $cond, [int]$ms = 8000) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $ms) {
        $e = $root.FindFirst($Scope::Descendants, $cond)
        if ($e) { return $e }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

$procs = Get-Process Files -ErrorAction Stop | Where-Object MainWindowHandle -ne 0
$proc = $procs | Where-Object MainWindowTitle -like 'Configura*' | Select-Object -First 1
if (-not $proc) { $proc = $procs | Select-Object -First 1 }
$win = $A::FromHandle($proc.MainWindowHandle)
[F012Win]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null

# Configurações abre como diálogo dentro da janela
$advanced = $win.FindFirst($Scope::Descendants, (NameCond 'Avançado'))
if (-not $advanced) {
    $btn = WaitFind $win (IdCond 'SettingsButton')
    if (-not $btn) { throw 'Botão de Configurações não encontrado.' }
    $btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    $advanced = WaitFind $win (NameCond 'Avançado')
}
if (-not $advanced) { throw 'Item Avançado não encontrado.' }
# Pode haver mais de uma aba de Configurações: usar o item visível
$advanced = $win.FindAll($Scope::Descendants, (NameCond 'Avançado')) | Where-Object { -not $_.Current.IsOffscreen } | Select-Object -First 1
if (-not $advanced) { throw 'Item Avançado visível não encontrado.' }

# Select() marca o item mas não navega: clique real no centro dele, só com o Files na frente
[F012Win]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null
[F012Win]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [F012Win]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
[F012Win]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
try { $advanced.SetFocus() } catch { }
Start-Sleep -Milliseconds 500
if ([F012Win]::GetForegroundWindow() -ne $proc.MainWindowHandle) { throw 'O Files não ficou em primeiro plano; clique cancelado.' }
$rc = $advanced.Current.BoundingRectangle
[F012Win]::SetCursorPos([int]($rc.X + $rc.Width / 2), [int]($rc.Y + $rc.Height / 2)) | Out-Null
[F012Win]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
[F012Win]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Seconds 2

function Get-Toggle([string]$label) {
    $text = WaitFind $win (NameCond $label)
    if (-not $text) { throw "Opção '$label' não encontrada." }
    if ($text.GetSupportedPatterns() -contains [System.Windows.Automation.TogglePattern]::Pattern) { return $text }
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
    $card = $walker.GetParent($text)
    for ($i = 0; $i -lt 4 -and $card; $i++) {
        $tg =$card.FindAll($Scope::Descendants, [System.Windows.Automation.Condition]::TrueCondition) |
            Where-Object { $_.GetSupportedPatterns() -contains [System.Windows.Automation.TogglePattern]::Pattern } | Select-Object -First 1
        if ($tg) { return $tg }
        $card = $walker.GetParent($card)
    }
    throw "Chave da opção '$label' não encontrada."
}
function State($t) { $t.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Current.ToggleState.ToString() }
function SetToggle($t, [string]$want) {
    $wantState = if ($want -eq 'On') { 'On' } else { 'Off' }
    if ((State $t) -ne $wantState) { $t.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle() }
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt 20000) { Start-Sleep -Milliseconds 500; if ((State $t) -eq $wantState) { break } }
    Start-Sleep -Seconds 3 # a detecção roda depois do regsvr32/reg import
}

$labelDefault = 'Definir o Files como o gerenciador de arquivos padrão'
$labelDialog = 'Usar o Files como diálogo para Abrir Arquivos'

if ($DefaultManager) { SetToggle (Get-Toggle $labelDefault) $DefaultManager }
if ($FileDialog) { SetToggle (Get-Toggle $labelDialog) $FileDialog }

$tDefault = Get-Toggle $labelDefault
$tDialog = Get-Toggle $labelDialog
$dialogEnabled = $tDialog.Current.IsEnabled
$open = (Get-ItemProperty 'Registry::HKEY_CLASSES_ROOT\Folder\shell\open\command' -ErrorAction SilentlyContinue).'(default)'
$clsidOpen = (Get-ItemProperty 'Registry::HKEY_CURRENT_USER\Software\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}' -ErrorAction SilentlyContinue).'(default)'
$clsidSave = (Get-ItemProperty 'Registry::HKEY_CURRENT_USER\Software\Classes\CLSID\{C0B4E2F3-BA21-4773-8DBA-335EC946EB8B}' -ErrorAction SilentlyContinue).'(default)'

"UI gerenciador padrão = $(State $tDefault)"
"UI diálogo de arquivos = $(State $tDialog) (habilitado: $dialogEnabled)"
"REG Folder\shell\open\command = $open"
"REG CLSID open dialog = $clsidOpen"
"REG CLSID save dialog = $clsidSave"

if ($Capture) {
    $dir = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'docs\agents\evidence\f012'
    New-Item -ItemType Directory -Force $dir | Out-Null
    $r = New-Object F012Win+RECT
    [F012Win]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
    $bmp = [System.Drawing.Bitmap]::new($r.R - $r.L, $r.B - $r.T)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [F012Win]::PrintWindow($proc.MainWindowHandle, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc)
    $path = Join-Path $dir "$Capture.png"
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    "CAPTURA $path"
}
