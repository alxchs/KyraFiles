# Copyright (c) Files Community
# SPDX-License-Identifier: MPL-2.0

param(
    [string]$SourcePng = "D:\DriveD\Downloads\KyraFiles.png"
)

Add-Type -AssemblyName System.Drawing

function Build-Ico {
    param(
        [string]$SourcePath,
        [string]$TargetPath,
        [int[]]$Dimensions = @(16, 24, 32, 48, 64, 128, 256)
    )

    $src = [System.Drawing.Image]::FromFile($SourcePath)
    $frames = @()

    foreach ($dim in $Dimensions) {
        $bmp = New-Object System.Drawing.Bitmap($dim, $dim, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)
        $g.DrawImage($src, 0, 0, $dim, $dim)
        $g.Dispose()

        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        $frames += ,@($dim, $ms.ToArray())
        $ms.Dispose()
    }
    $src.Dispose()

    $fs = [System.IO.File]::Create($TargetPath)
    $bw = New-Object System.IO.BinaryWriter($fs)

    # ICONDIR
    $bw.Write([uint16]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]$frames.Count)

    $offset = 6 + ($frames.Count * 16)
    foreach ($f in $frames) {
        $d = $f[0]
        $bytes = $f[1]
        $byteWidth = if ($d -ge 256) { [byte]0 } else { [byte]$d }
        $byteHeight = if ($d -ge 256) { [byte]0 } else { [byte]$d }

        $bw.Write([byte]$byteWidth)
        $bw.Write([byte]$byteHeight)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]32)
        $bw.Write([uint32]$bytes.Length)
        $bw.Write([uint32]$offset)
        $offset += $bytes.Length
    }

    foreach ($f in $frames) {
        $bw.Write($f[1])
    }

    $bw.Flush()
    $bw.Dispose()
    $fs.Dispose()

    $fi = [System.IO.FileInfo]::new($TargetPath)
    Write-Host "Criado $TargetPath com $($fi.Length) bytes ($($frames.Count) frames)."
}

function Build-Png {
    param(
        [string]$SourcePath,
        [string]$TargetPath,
        [int]$Width,
        [int]$Height
    )

    $src = [System.Drawing.Image]::FromFile($SourcePath)
    $bmp = New-Object System.Drawing.Bitmap($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($src, 0, 0, $Width, $Height)
    $g.Dispose()
    $src.Dispose()

    $bmp.Save($TargetPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# 1. Copiar imagem fonte
$sourceFull = (Resolve-Path $SourcePng).Path
$devDir = "src\KyraFiles.App\Assets\AppTiles\Dev"
$relDir = "src\KyraFiles.App\Assets\AppTiles\Release"
$prevDir = "src\KyraFiles.App\Assets\AppTiles\Preview"

Copy-Item $sourceFull (Join-Path $devDir "KyraLogo.png") -Force
Copy-Item $sourceFull (Join-Path $relDir "KyraLogo.png") -Force
Copy-Item $sourceFull (Join-Path $prevDir "KyraLogo.png") -Force

# 2. Gerar Logo.ico
Build-Ico -SourcePath $sourceFull -TargetPath (Join-Path $devDir "Logo.ico")
Build-Ico -SourcePath $sourceFull -TargetPath (Join-Path $relDir "Logo.ico")
Build-Ico -SourcePath $sourceFull -TargetPath (Join-Path $prevDir "Logo.ico")

# 3. Gerar os tiles em Dev e Release para substituir as pastas rosa
$tileSizes = @(
    @{ Name = "Square44x44Logo.targetsize-16.png"; W = 16; H = 16 },
    @{ Name = "Square44x44Logo.targetsize-20.png"; W = 20; H = 20 },
    @{ Name = "Square44x44Logo.targetsize-24.png"; W = 24; H = 24 },
    @{ Name = "Square44x44Logo.targetsize-30.png"; W = 30; H = 30 },
    @{ Name = "Square44x44Logo.targetsize-32.png"; W = 32; H = 32 },
    @{ Name = "Square44x44Logo.targetsize-36.png"; W = 36; H = 36 },
    @{ Name = "Square44x44Logo.targetsize-40.png"; W = 40; H = 40 },
    @{ Name = "Square44x44Logo.targetsize-48.png"; W = 48; H = 48 },
    @{ Name = "Square44x44Logo.targetsize-60.png"; W = 60; H = 60 },
    @{ Name = "Square44x44Logo.targetsize-64.png"; W = 64; H = 64 },
    @{ Name = "Square44x44Logo.targetsize-72.png"; W = 72; H = 72 },
    @{ Name = "Square44x44Logo.targetsize-80.png"; W = 80; H = 80 },
    @{ Name = "Square44x44Logo.targetsize-96.png"; W = 96; H = 96 },
    @{ Name = "Square44x44Logo.targetsize-256.png"; W = 256; H = 256 },
    @{ Name = "Square44x44Logo.scale-100.png"; W = 44; H = 44 },
    @{ Name = "Square44x44Logo.scale-125.png"; W = 55; H = 55 },
    @{ Name = "Square44x44Logo.scale-150.png"; W = 66; H = 66 },
    @{ Name = "Square44x44Logo.scale-200.png"; W = 88; H = 88 },
    @{ Name = "Square44x44Logo.scale-400.png"; W = 176; H = 176 },
    @{ Name = "Square150x150Logo.scale-100.png"; W = 150; H = 150 },
    @{ Name = "Square150x150Logo.scale-125.png"; W = 188; H = 188 },
    @{ Name = "Square150x150Logo.scale-150.png"; W = 225; H = 225 },
    @{ Name = "Square150x150Logo.scale-200.png"; W = 300; H = 300 },
    @{ Name = "Square150x150Logo.scale-400.png"; W = 600; H = 600 },
    @{ Name = "Small71x71Logo.scale-100.png"; W = 71; H = 71 },
    @{ Name = "Small71x71Logo.scale-125.png"; W = 89; H = 89 },
    @{ Name = "Small71x71Logo.scale-150.png"; W = 107; H = 107 },
    @{ Name = "Small71x71Logo.scale-200.png"; W = 142; H = 142 },
    @{ Name = "Small71x71Logo.scale-400.png"; W = 284; H = 284 },
    @{ Name = "StoreLogo.scale-100.png"; W = 50; H = 50 },
    @{ Name = "StoreLogo.scale-125.png"; W = 63; H = 63 },
    @{ Name = "StoreLogo.scale-150.png"; W = 75; H = 75 },
    @{ Name = "StoreLogo.scale-200.png"; W = 100; H = 100 },
    @{ Name = "StoreLogo.scale-400.png"; W = 200; H = 200 },
    @{ Name = "StoreLogo.png"; W = 50; H = 50 },
    @{ Name = "Square44x44Logo.png"; W = 44; H = 44 },
    @{ Name = "Square150x150Logo.png"; W = 150; H = 150 }
)

foreach ($tile in $tileSizes) {
    $devTarget = Join-Path $devDir $tile.Name
    $relTarget = Join-Path $relDir $tile.Name
    if (Test-Path $devTarget) {
        Build-Png -SourcePath $sourceFull -TargetPath $devTarget -Width $tile.W -Height $tile.H
    }
    if (Test-Path $relTarget) {
        Build-Png -SourcePath $sourceFull -TargetPath $relTarget -Width $tile.W -Height $tile.H
    }
}

# Também substituir as variantes altform-unplated do Square44x44
Get-ChildItem -Path $devDir, $relDir -Filter "Square44x44Logo.targetsize-*_altform-unplated.png" | ForEach-Object {
    $match = [regex]::Match($_.Name, "targetsize-(\d+)")
    if ($match.Success) {
        $sz = [int]$match.Groups[1].Value
        Build-Png -SourcePath $sourceFull -TargetPath $_.FullName -Width $sz -Height $sz
    }
}

Get-ChildItem -Path $devDir, $relDir -Filter "Square44x44Logo.targetsize-*_altform-lightunplated*.png" | ForEach-Object {
    $match = [regex]::Match($_.Name, "targetsize-(\d+)")
    if ($match.Success) {
        $sz = [int]$match.Groups[1].Value
        Build-Png -SourcePath $sourceFull -TargetPath $_.FullName -Width $sz -Height $sz
    }
}

Write-Host "Processamento de icones e tiles do KyraFiles concluido com sucesso!"
