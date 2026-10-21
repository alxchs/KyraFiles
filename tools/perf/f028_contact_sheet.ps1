# F028 — segura uma tecla e captura quadros da janela (so CopyFromScreen, sem UI Automation, para nao perturbar),
# depois monta uma folha de contato com os quadros reduzidos, em ordem, para conferir a olho se a linha selecionada
# e a barra de rolagem acompanham a tecla.
# Uso: pwsh -File f028_contact_sheet.ps1 -Label f028 -Key PageDown [-HoldS 4] [-Frames 8] [-ProcessName Files]
param(
    [Parameter(Mandatory)][string]$Label,
    [ValidateSet('Down', 'Up', 'PageDown', 'PageUp')][string]$Key = 'PageDown',
    [int]$HoldS = 4, [int]$Frames = 8, [string]$ProcessName = 'Files',
    [string]$OutDir = "$PSScriptRoot\..\..\docs\agents\evidence\F028"
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public class F028Win {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte sc, uint fl, UIntPtr ex);
}
"@
$vk = @{ Down = 0x28; Up = 0x26; PageDown = 0x22; PageUp = 0x21 }[$Key]
$p = Get-Process $ProcessName | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
[F028Win]::SetForegroundWindow($p.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 600
[F028Win]::keybd_event(0x24, 0, 0, [UIntPtr]::Zero); [F028Win]::keybd_event(0x24, 0, 2, [UIntPtr]::Zero); Start-Sleep -Seconds 2
$r = New-Object F028Win+RECT; [F028Win]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$w = $r.R - $r.L; $h = $r.B - $r.T
$every = [int]($HoldS * 1000 / $Frames); $shots = @(); $t = [Diagnostics.Stopwatch]::StartNew(); $next = 0
while ($t.ElapsedMilliseconds -lt $HoldS * 1000) {
    [F028Win]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 33
    if ($t.ElapsedMilliseconds -ge $next -and $shots.Count -lt $Frames) {
        $b = New-Object Drawing.Bitmap $w, $h; $g = [Drawing.Graphics]::FromImage($b); $g.CopyFromScreen($r.L, $r.T, 0, 0, $b.Size); $g.Dispose()
        $shots += [pscustomobject]@{ Ms = $t.ElapsedMilliseconds; Bmp = $b }; $next = $t.ElapsedMilliseconds + $every
    }
}
[F028Win]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 800
$b = New-Object Drawing.Bitmap $w, $h; $g = [Drawing.Graphics]::FromImage($b); $g.CopyFromScreen($r.L, $r.T, 0, 0, $b.Size); $g.Dispose()
$shots += [pscustomobject]@{ Ms = -800; Bmp = $b }   # 800 ms depois de soltar
# folha: 3 colunas, cada quadro com 40% do tamanho, area da lista (corta 14% a esquerda: barra lateral)
$x0 = [int]($w * 0.14); $cw = $w - $x0; $sc = 0.40; $tw = [int]($cw * $sc); $th = [int]($h * $sc); $cols = 3
$rows = [math]::Ceiling($shots.Count / $cols)
$sheet = New-Object Drawing.Bitmap ($tw * $cols), (($th + 22) * $rows); $gs = [Drawing.Graphics]::FromImage($sheet)
$gs.Clear([Drawing.Color]::Black); $font = New-Object Drawing.Font 'Segoe UI', 11
for ($i = 0; $i -lt $shots.Count; $i++) {
    $cx = ($i % $cols) * $tw; $cy = [math]::Floor($i / $cols) * ($th + 22)
    $gs.DrawImage($shots[$i].Bmp, (New-Object Drawing.Rectangle $cx, ($cy + 22), $tw, $th), (New-Object Drawing.Rectangle $x0, 0, $cw, $h), [Drawing.GraphicsUnit]::Pixel)
    $txt = if ($shots[$i].Ms -lt 0) { '800 ms apos soltar' } else { '{0} ms segurando' -f $shots[$i].Ms }
    $gs.DrawString($txt, $font, [Drawing.Brushes]::Yellow, $cx + 4, $cy + 2)
    $shots[$i].Bmp.Dispose()
}
$gs.Dispose()
New-Item -ItemType Directory -Force $OutDir | Out-Null
$out = Join-Path $OutDir ("contato_{0}_{1}.png" -f $Label, $Key); $sheet.Save($out); $sheet.Dispose(); $out
