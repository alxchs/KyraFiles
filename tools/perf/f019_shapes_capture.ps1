# F019: captura a janela do Files com cada forma (ControlShapeStyle) para conferir os efeitos visuais.
# Antes: .\Open-FilesDev.ps1 -Configuration Debug -NoLaunch (o build sozinho nao atualiza o layout registrado).
# Saida: %TEMP%\claude\desenv\utils\FilesApp\shape_<n>.png
param([string]$Shapes = '0,1,2,3,4,5,6,7,8,9', [string]$Aumid = 'FilesDev_ykqwq8d6ps0ag!App')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System; using System.Runtime.InteropServices;
public class F019W { [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r); [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f); [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); public struct RECT { public int L, T, R, B; } }
"@
$tmp = Join-Path $env:TEMP 'claude\desenv\utils\FilesApp'
New-Item -ItemType Directory -Force $tmp | Out-Null
$pkg = Split-Path $Aumid.Split('!')[0] -Leaf
$settings = Join-Path $env:LOCALAPPDATA "Packages\$($Aumid.Split('!')[0])\LocalState\settings\user_settings.json"
$orig = Get-Content $settings -Raw

try {
    foreach ($k in ($Shapes -split ',' | ForEach-Object { [int]$_ })) {
        Stop-Process -Name Files -Force -ErrorAction SilentlyContinue; Start-Sleep 2
        [IO.File]::WriteAllText($settings, ($orig -replace '"ControlShapeStyle":\s*\d+', "`"ControlShapeStyle`": $k"))
        Start-Process "shell:AppsFolder\$Aumid"; Start-Sleep 9
        $p = Get-Process Files | Where-Object MainWindowHandle -ne 0 | Select -First 1
        [void][F019W]::SetWindowPos($p.MainWindowHandle, [IntPtr](-1), 0, 0, 0, 0, 0x43)
        [void][F019W]::SetForegroundWindow($p.MainWindowHandle); Start-Sleep 2
        $r = New-Object F019W+RECT; [void][F019W]::GetWindowRect($p.MainWindowHandle, [ref]$r)
        $bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
        [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
        [void][F019W]::SetWindowPos($p.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x43)
        $f = Join-Path $tmp "shape_$k.png"; $bmp.Save($f)
        '{0}: vivo={1} {2}x{3} -> {4}' -f $k, (-not $p.HasExited), $bmp.Width, $bmp.Height, $f
    }
}
finally {
    [IO.File]::WriteAllText($settings, $orig)
    Stop-Process -Name Files -Force -ErrorAction SilentlyContinue
}
