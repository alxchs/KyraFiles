# F012 AC-6: launcher com o Files "desinstalado" abre o Explorer, sem caixa de mensagem.
# Simula a desinstalação apontando LOCALAPPDATA para uma pasta vazia (o alias files-dev.exe
# não é encontrado); o registro real não é tocado, porque o .reg de reversão também é
# procurado nessa pasta vazia.
#
# Uso: pwsh tests\test-launcher-fallback.ps1

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$launcher = Join-Path $root 'src\KyraFiles.App\Assets\FilesOpenDialog\KyraFiles.App.Launcher.exe'
$failed = 0
function Check([string]$name, [bool]$ok, [string]$detail) {
    if ($ok) { Write-Host "PASS $name $detail" -ForegroundColor Green }
    else { Write-Host "FAIL $name $detail" -ForegroundColor Red; $script:failed++ }
}

Check 'launcher existe' (Test-Path $launcher) $launcher

$fakeLocal = Join-Path ([IO.Path]::GetTempPath()) "f012-fake-localappdata-$PID"
$target = Join-Path ([IO.Path]::GetTempPath()) "F012 pasta com espaço $PID"
New-Item -ItemType Directory -Force $fakeLocal, $target | Out-Null

$shell = New-Object -ComObject Shell.Application
function Get-ExplorerWindowsFor([string]$path) {
    @($shell.Windows() | Where-Object { $_.Document -and $_.Document.Folder.Self.Path -eq $path })
}
$before = (Get-ExplorerWindowsFor $target).Count

$psi = [Diagnostics.ProcessStartInfo]::new($launcher, "`"$target`"")
$psi.UseShellExecute = $false
$psi.Environment['LOCALAPPDATA'] = $fakeLocal
$sw = [Diagnostics.Stopwatch]::StartNew()
$proc = [Diagnostics.Process]::Start($psi)
$exited = $proc.WaitForExit(10000)
$sw.Stop()

Check 'launcher termina sozinho (sem caixa de mensagem esperando OK)' $exited "($($sw.ElapsedMilliseconds) ms)"
if (-not $exited) { $proc.Kill() }

$opened = $false
for ($i = 0; $i -lt 20 -and -not $opened; $i++) {
    Start-Sleep -Milliseconds 250
    $opened = (Get-ExplorerWindowsFor $target).Count -gt $before
}
Check 'Explorer abriu a pasta pedida' $opened $target

$cmd = (Get-ItemProperty 'Registry::HKEY_CLASSES_ROOT\Folder\shell\open\command' -ErrorAction SilentlyContinue).'(default)'
Write-Host "INFO Folder\shell\open\command = $cmd"

Get-ExplorerWindowsFor $target | ForEach-Object { $_.Quit() }
Remove-Item $fakeLocal, $target -Recurse -Force -ErrorAction SilentlyContinue

if ($failed) { Write-Host "$failed falha(s)" -ForegroundColor Red; exit 1 }
Write-Host 'Todos os testes do fallback passaram' -ForegroundColor Green
