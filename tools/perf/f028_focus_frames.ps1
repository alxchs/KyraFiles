# F028 — segura uma tecla e grava quadros em resolucao real da janela (CopyFromScreen), um PNG por quadro,
# para conferir onde o contorno de foco e desenhado (linha com conteudo ou linha vazia). Inclui quadros depois de soltar.
# Uso: pwsh -File f028_focus_frames.ps1 -Label base -Key PageDown [-HoldS 3] [-EveryMs 150] [-Folder C:\FilesUXLab\perf\1600]
param(
    [Parameter(Mandatory)][string]$Label,
    [ValidateSet('Down', 'Up', 'PageDown', 'PageUp')][string]$Key = 'PageDown',
    [double]$HoldS = 3, [int]$EveryMs = 150, [string]$ProcessName = 'Files',
    [string]$Folder = 'C:\FilesUXLab\perf\1600', [switch]$NoNav, [switch]$FromEnd,
    [string]$OutDir = "$env:TEMP\claude\desenv\utils\FilesApp\f028_frames"
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing, System.Windows.Forms
Add-Type @"
using System; using System.Runtime.InteropServices;
public class F028Frm {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte sc, uint fl, UIntPtr ex);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, int x, int y, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int hh, bool r);
}
"@
[F028Frm]::SetProcessDPIAware() | Out-Null   # coordenadas em pixels reais, senao a captura sai cortada com escala de tela
function Tap([byte]$k) { [F028Frm]::keybd_event($k, 0, 0, [UIntPtr]::Zero); [F028Frm]::keybd_event($k, 0, 2, [UIntPtr]::Zero) }
$vk = @{ Down = 0x28; Up = 0x26; PageDown = 0x22; PageUp = 0x21 }[$Key]
$p = Get-Process $ProcessName | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
# janela inteira dentro da area de trabalho, para a linha selecionada na base aparecer na captura
$wa = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
[F028Frm]::ShowWindow($p.MainWindowHandle, 9) | Out-Null
[F028Frm]::MoveWindow($p.MainWindowHandle, $wa.X + 40, $wa.Y + 20, [math]::Min(1600, $wa.Width - 80), $wa.Height - 40, $true) | Out-Null
[F028Frm]::SetForegroundWindow($p.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 600
if (-not $NoNav) {
    [System.Windows.Forms.SendKeys]::SendWait('^l'); Start-Sleep -Milliseconds 400
    [System.Windows.Forms.SendKeys]::SendWait($Folder + '{ENTER}'); Start-Sleep -Seconds 5
    Tap 0x28; Start-Sleep -Milliseconds 500   # primeiro Down foca a lista
}
# clique numa linha da lista (60% da largura, 45% da altura) para o foco estar nela e nao na barra de endereco
$r0 = New-Object F028Frm+RECT; [F028Frm]::GetWindowRect($p.MainWindowHandle, [ref]$r0) | Out-Null
[F028Frm]::SetCursorPos($r0.L + [int](($r0.R - $r0.L) * 0.6), $r0.T + [int](($r0.B - $r0.T) * 0.45)) | Out-Null
[F028Frm]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); [F028Frm]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 500
[F028Frm]::SetCursorPos($r0.R + 50, $r0.T + 50) | Out-Null   # tira o ponteiro de cima da lista (hover)
Tap ($(if ($FromEnd) { 0x23 } else { 0x24 })); Start-Sleep -Seconds 2
$r = New-Object F028Frm+RECT; [F028Frm]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$w = $r.R - $r.L; $h = $r.B - $r.T
$dir = Join-Path $OutDir $Label; Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue; New-Item -ItemType Directory -Force $dir | Out-Null
function Shot([string]$name) {
    $b = New-Object Drawing.Bitmap $w, $h; $g = [Drawing.Graphics]::FromImage($b); $g.CopyFromScreen($r.L, $r.T, 0, 0, $b.Size); $g.Dispose()
    $b.Save((Join-Path $dir "$name.png")); $b.Dispose()
}
$t = [Diagnostics.Stopwatch]::StartNew(); $next = 0; $i = 0
while ($t.ElapsedMilliseconds -lt $HoldS * 1000) {
    [F028Frm]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 33
    if ($t.ElapsedMilliseconds -ge $next) { Shot ('{0:D2}_{1}ms' -f $i++, $t.ElapsedMilliseconds); $next = $t.ElapsedMilliseconds + $EveryMs }
}
[F028Frm]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
foreach ($ms in 100, 400, 1000) { Start-Sleep -Milliseconds ($ms - 50); Shot ('{0:D2}_solto_{1}ms' -f $i++, $ms) }
# um toque unico, depois de parado
Tap $vk; Start-Sleep -Milliseconds 120; Shot ('{0:D2}_toque_120ms' -f $i++); Start-Sleep -Milliseconds 600; Shot ('{0:D2}_toque_720ms' -f $i++)
$dir
