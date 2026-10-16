$sh = New-Object -ComObject WScript.Shell

Write-Host "=== DESKTOP SHORTCUTS ==="
$desktopPath = [Environment]::GetFolderPath("Desktop")
Get-ChildItem $desktopPath -Filter "*Files*.lnk" | ForEach-Object {
    $lnk = $sh.CreateShortcut($_.FullName)
    Write-Host "$($_.Name) -> $($lnk.TargetPath) (Args: $($lnk.Arguments))"
}

Write-Host "`n=== START MENU SHORTCUTS ==="
$programsPath = [Environment]::GetFolderPath("Programs")
Get-ChildItem $programsPath -Recurse -Filter "*Files*.lnk" | ForEach-Object {
    $lnk = $sh.CreateShortcut($_.FullName)
    Write-Host "$($_.Name) -> $($lnk.TargetPath) (Args: $($lnk.Arguments))"
}

Write-Host "`n=== APPDATA PROGRAMS FOLDER ==="
$installedDir = "$env:LOCALAPPDATA\Programs\FilesCustom"
if (Test-Path $installedDir) {
    Write-Host "Found: $installedDir"
    Get-ChildItem $installedDir | Select-Object Name, Length
} else {
    Write-Host "Not found: $installedDir"
}

