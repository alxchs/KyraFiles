# F028 — junta os quadros de f028_focus_frames.ps1 numa folha (area da lista, sem a barra lateral), com o nome de cada quadro.
# Uso: pwsh -File f028_frames_sheet.ps1 -Dir <pasta dos quadros> -Out <png> [-Scale 0.5] [-Cols 3] [-CropLeft 0.22]
param([Parameter(Mandatory)][string]$Dir, [Parameter(Mandatory)][string]$Out, [double]$Scale = 0.5, [int]$Cols = 3, [double]$CropLeft = 0.22, [string]$Filter = '*.png')
Add-Type -AssemblyName System.Drawing
$files = Get-ChildItem $Dir -Filter $Filter | Sort-Object Name
$first = [Drawing.Image]::FromFile($files[0].FullName); $w = $first.Width; $h = $first.Height; $first.Dispose()
$x0 = [int]($w * $CropLeft); $cw = $w - $x0; $tw = [int]($cw * $Scale); $th = [int]($h * $Scale)
$rows = [math]::Ceiling($files.Count / $Cols)
$sheet = New-Object Drawing.Bitmap ($tw * $Cols), (($th + 22) * $rows); $g = [Drawing.Graphics]::FromImage($sheet)
$g.Clear([Drawing.Color]::Black); $font = New-Object Drawing.Font 'Segoe UI', 11
for ($i = 0; $i -lt $files.Count; $i++) {
    $img = [Drawing.Image]::FromFile($files[$i].FullName)
    $cx = ($i % $Cols) * $tw; $cy = [math]::Floor($i / $Cols) * ($th + 22)
    $g.DrawImage($img, (New-Object Drawing.Rectangle $cx, ($cy + 22), $tw, $th), (New-Object Drawing.Rectangle $x0, 0, $cw, $h), [Drawing.GraphicsUnit]::Pixel)
    $g.DrawString($files[$i].BaseName, $font, [Drawing.Brushes]::Yellow, $cx + 4, $cy + 2); $img.Dispose()
}
$g.Dispose(); New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null; $sheet.Save($Out); $sheet.Dispose(); $Out
