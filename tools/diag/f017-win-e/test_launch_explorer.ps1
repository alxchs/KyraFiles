$pkg = Get-AppxPackage -Name FilesDev
$aumid = "$($pkg.PackageFamilyName)!App"
Write-Host "Launching via explorer shell:AppsFolder\$aumid"
Start-Process explorer.exe -ArgumentList "shell:AppsFolder\$aumid"
Start-Sleep -Seconds 4
$proc = Get-Process Files -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Files is running with PID $($proc.Id), MainWindowTitle: '$($proc.MainWindowTitle)', MainWindowHandle: $($proc.MainWindowHandle)"
} else {
    Write-Host "Files is not running."
}

