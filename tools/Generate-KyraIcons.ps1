# tools/Generate-KyraIcons.ps1
# Generates all AppTiles PNG assets and Logo.ico from KyraPet.png
param(
    [string]$SourcePath = "src/KyraFiles.App/Assets/KyraPet.png"
)

Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $SourcePath)) {
    throw "Source image not found: $SourcePath"
}

$sourceImg = [System.Drawing.Image]::FromFile((Resolve-Path $SourcePath).Path)

function Resize-Image($src, $w, $h) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.DrawImage($src, 0, 0, $w, $h)
    $g.Dispose()
    return $bmp
}

$dirs = @(
    "src/KyraFiles.App/Assets/AppTiles/Dev",
    "src/KyraFiles.App/Assets/AppTiles/Release"
)

foreach ($dir in $dirs) {
    if (-not (Test-Path $dir)) { continue }
    Write-Host "Updating icons in $dir..."

    # Square44x44
    $scales44 = @{ 100=44; 125=55; 150=66; 200=88; 400=176 }
    foreach ($kv in $scales44.GetEnumerator()) {
        $bmp = Resize-Image $sourceImg $kv.Value $kv.Value
        $bmp.Save((Join-Path $dir "Square44x44Logo.scale-$($kv.Key).png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }

    $targets = @(16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256)
    foreach ($sz in $targets) {
        $bmp = Resize-Image $sourceImg $sz $sz
        $bmp.Save((Join-Path $dir "Square44x44Logo.targetsize-$sz.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Save((Join-Path $dir "Square44x44Logo.targetsize-$($sz)_altform-unplated.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Save((Join-Path $dir "Square44x44Logo.targetsize-$($sz)_altform-lightunplated_theme-light.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }

    # Square150x150
    $scales150 = @{ 100=150; 125=188; 150=225; 200=300; 400=600 }
    foreach ($kv in $scales150.GetEnumerator()) {
        $bmp = Resize-Image $sourceImg $kv.Value $kv.Value
        $bmp.Save((Join-Path $dir "Square150x150Logo.scale-$($kv.Key).png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }

    # StoreLogo
    $scalesStore = @{ 100=50; 125=63; 150=75; 200=100; 400=200 }
    foreach ($kv in $scalesStore.GetEnumerator()) {
        $bmp = Resize-Image $sourceImg $kv.Value $kv.Value
        $bmp.Save((Join-Path $dir "StoreLogo.scale-$($kv.Key).png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }

    # Small71x71
    $scales71 = @{ 100=71; 125=89; 150=107; 200=142; 400=284 }
    foreach ($kv in $scales71.GetEnumerator()) {
        $bmp = Resize-Image $sourceImg $kv.Value $kv.Value
        $bmp.Save((Join-Path $dir "Small71x71Logo.scale-$($kv.Key).png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }

    # Large310x310
    $scales310 = @{ 100=310; 125=388; 150=465; 200=620; 400=1240 }
    foreach ($kv in $scales310.GetEnumerator()) {
        $bmp = Resize-Image $sourceImg $kv.Value $kv.Value
        $bmp.Save((Join-Path $dir "Large310x310Logo.scale-$($kv.Key).png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    }

    # Generate Logo.ico using 256x256, 128x128, 64x64, 48x48, 32x32, 16x16
    $icoSizes = @(256, 128, 64, 48, 32, 16)
    $icoPath = Join-Path $dir "Logo.ico"
    
    # Save 256x256 bitmap directly to icon via handle
    $iconBmp = Resize-Image $sourceImg 256 256
    $hIcon = $iconBmp.GetHicon()
    $icon = [System.Drawing.Icon]::FromHandle($hIcon)
    $fs = [System.IO.File]::OpenWrite($icoPath)
    $icon.Save($fs)
    $fs.Close()
    $icon.Dispose()
    $iconBmp.Dispose()
    # Clean up icon handle
}

$sourceImg.Dispose()
Write-Host "All Kyra icons generated successfully!" -ForegroundColor Green
