# F025/F026 — captura quadros da janela enquanto uma tecla fica segurada, e mede quanto da lista ficou "vazia".
# Vazio = fracao de linhas horizontais da area da lista sem nenhum pixel de texto (muito diferente do fundo).
# Uso: pwsh -File f025_capture_hold.ps1 -Label atual [-Key Down] [-HoldS 5] [-ProcessName Files] [-Folder ...] [-NoNav]
param(
    [Parameter(Mandatory)][string]$Label,
    [ValidateSet('Down', 'End', 'Up', 'PageDown')][string]$Key = 'Down',
    [int]$HoldS = 5, [int]$FrameMs = 250,
    [string]$ProcessName = 'Files', [string]$Folder = 'C:\FilesUXLab\perf\1600', [switch]$NoNav,
    [string]$OutDir = "$PSScriptRoot\..\..\docs\agents\evidence\F026\frames"
)
$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public class F026Win {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte sc, uint fl, UIntPtr ex);
}
"@
$vk = @{ Down = 0x28; Up = 0x26; End = 0x23; PageDown = 0x22 }[$Key]
$p = Get-Process $ProcessName | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
$h = $p.MainWindowHandle
[F026Win]::SetForegroundWindow($h) | Out-Null; Start-Sleep -Milliseconds 500
if (-not $NoNav) {
    [System.Windows.Forms.SendKeys]::SendWait('^l'); Start-Sleep -Milliseconds 400
    [System.Windows.Forms.SendKeys]::SendWait($Folder.Replace('{','{{}').Replace('}','{}}') + '{ENTER}'); Start-Sleep -Seconds 6
}
[F026Win]::keybd_event(0x24, 0, 0, [UIntPtr]::Zero); [F026Win]::keybd_event(0x24, 0, 2, [UIntPtr]::Zero); Start-Sleep -Seconds 1
New-Item -ItemType Directory -Force $OutDir | Out-Null
$r = New-Object F026Win+RECT; [F026Win]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.R - $r.L; $ht = $r.B - $r.T
function Get-EmptyFraction($bmp) {
    # area da lista: 30%..95% da altura, 15%..90% da largura; linha "com conteudo" = tem pixel com contraste forte
    $y0 = [int]($bmp.Height * 0.30); $y1 = [int]($bmp.Height * 0.95); $x0 = [int]($bmp.Width * 0.15); $x1 = [int]($bmp.Width * 0.90)
    $empty = 0; $rows = 0
    for ($y = $y0; $y -lt $y1; $y += 3) {
        $rows++; $min = 255; $max = 0
        for ($x = $x0; $x -lt $x1; $x += 4) { $c = $bmp.GetPixel($x, $y); $l = ($c.R + $c.G + $c.B) / 3; if ($l -lt $min) { $min = $l }; if ($l -gt $max) { $max = $l } }
        if (($max - $min) -lt 60) { $empty++ }
    }
    return $empty / $rows
}
$t = [Diagnostics.Stopwatch]::StartNew(); $next = 0; $i = 0; $raw = @()
while ($t.Elapsed.TotalSeconds -lt $HoldS) {
    [F026Win]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 33
    if ($t.ElapsedMilliseconds -ge $next) {
        # so captura aqui (barato); analise e gravacao em disco ficam para depois, senao perturbam a tecla
        $bmp = New-Object Drawing.Bitmap $w, $ht
        $g = [Drawing.Graphics]::FromImage($bmp); $g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size); $g.Dispose()
        $raw += [pscustomobject]@{ I = $i; TMs = $t.ElapsedMilliseconds; Bmp = $bmp }
        $i++; $next = $t.ElapsedMilliseconds + $FrameMs
    }
}
[F026Win]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
$fr = foreach ($x in $raw) {
    $e = Get-EmptyFraction $x.Bmp
    $x.Bmp.Save((Join-Path $OutDir ("{0}_{1:D2}.png" -f $Label, $x.I))); $x.Bmp.Dispose()
    [pscustomobject]@{ Frame = $x.I; TMs = $x.TMs; EmptyPct = [math]::Round($e * 100) }
}
$fr | Format-Table -AutoSize | Out-String
'{0}: quadros={1}; vazio mediano={2}%; vazio max={3}%' -f $Label, @($fr).Count, ((@($fr).EmptyPct | Sort-Object)[[int](@($fr).Count / 2)]), (@($fr).EmptyPct | Measure-Object -Maximum).Maximum