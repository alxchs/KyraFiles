. "$PSScriptRoot\SmokeTest-Helper.ps1"

$p = Get-Process Files | Select-Object -First 1
$hwnd = [VC]::FindMainWindow($p.Id)

$script = @"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

`$p = Get-Process Files | Select-Object -First 1
Write-Output ("Files PID: {0}" -f `$p.Id)

foreach (`$s in [System.Windows.Forms.Screen]::AllScreens) {
    Write-Output ("Screen: {0}, Bounds: {1}, Primary: {2}" -f `$s.DeviceName, `$s.Bounds, `$s.Primary)
}
"@

$res = Invoke-OnDefault -PowerShellScript $script
Write-Output $res
