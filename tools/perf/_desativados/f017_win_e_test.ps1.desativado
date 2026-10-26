# F017 T1/T2: prova que Win+E abre o Files (nao o Windows Explorer), que o servico renasce apos taskkill
# e que o desligamento cooperativo devolve o Win+E ao Explorer. Simula o teclado com keybd_event
# (nao substitui o teste com teclado fisico). Temporarios em %TEMP%\claude\desenv\utils\FilesApp\.
param(
    [string]$Bin = (Join-Path $PSScriptRoot '..\..\bin\Release'),
    [string]$PackageAumid = 'FilesDev_ykqwq8d6ps0ag!App'
)

$ErrorActionPreference = 'Stop'
$Bin = (Resolve-Path $Bin).Path
$tmp = Join-Path $env:TEMP 'claude\desenv\utils\FilesApp'
New-Item -ItemType Directory -Force $tmp | Out-Null

Add-Type @"
using System; using System.Runtime.InteropServices;
public class F017K {
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte sc, uint fl, UIntPtr ex);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
}
"@

function Send-WinE {
    [F017K]::keybd_event(0x5B, 0, 0, [UIntPtr]::Zero)
    [F017K]::keybd_event(0x45, 0, 0, [UIntPtr]::Zero)
    [F017K]::keybd_event(0x45, 0, 2, [UIntPtr]::Zero)
    [F017K]::keybd_event(0x5B, 0, 2, [UIntPtr]::Zero)
}

function Get-ExplorerWindowCount { (New-Object -ComObject Shell.Application).Windows().Count }

function Get-ForegroundProcessName {
    $pid2 = 0
    [void][F017K]::GetWindowThreadProcessId([F017K]::GetForegroundWindow(), [ref]$pid2)
    (Get-Process -Id $pid2 -ErrorAction SilentlyContinue).ProcessName
}

function Stop-All {
    Stop-Process -Name Files.Watchdog, Files.HotkeyService, Files -Force -ErrorAction SilentlyContinue
    Start-Sleep 1
}

function Start-Hidden([string]$name) {
    Start-Process (Join-Path $Bin $name) -WindowStyle Hidden -PassThru | Out-Null
}

$result = [ordered]@{}

# --- 1. Win+E com o Files fechado (AC-1) -----------------------------------------------------------------------
Stop-All
$before = Get-ExplorerWindowCount
Start-Hidden 'Files.HotkeyService.exe'; Start-Sleep 2
$result['servico vivo'] = [bool](Get-Process Files.HotkeyService -ErrorAction SilentlyContinue)
Send-WinE; Start-Sleep -Milliseconds 400
$fg = Get-ForegroundProcessName
$result['primeiro plano a 400ms (nao pode ser Iniciar)'] = $fg
$result['Iniciar abriu'] = ($fg -in 'StartMenuExperienceHost', 'SearchHost')
Start-Sleep 8
$result['AC-1 Files rodando'] = [bool](Get-Process Files -ErrorAction SilentlyContinue)
$after = Get-ExplorerWindowCount
$result['AC-1 janelas do Explorer antes/depois'] = "$before / $after"
$result['AC-1 OK'] = ($result['AC-1 Files rodando'] -and $before -eq $after -and -not $result['Iniciar abriu'])

# --- 2. Win+E com o Files ja aberto: vai para o primeiro plano (AC-2) -------------------------------------------
$before2 = Get-ExplorerWindowCount
Send-WinE; Start-Sleep 3
$result['AC-2 primeiro plano'] = Get-ForegroundProcessName
$result['AC-2 janelas do Explorer antes/depois'] = "$before2 / $(Get-ExplorerWindowCount)"
$result['AC-2 OK'] = ($result['AC-2 primeiro plano'] -eq 'Files' -and $before2 -eq (Get-ExplorerWindowCount))

# --- 3. taskkill /F: o Watchdog traz o servico de volta em ~200 ms (AC-3) --------------------------------------
Stop-Process -Name Files -Force -ErrorAction SilentlyContinue
Stop-Process -Name Files.HotkeyService -Force -ErrorAction SilentlyContinue
Start-Hidden 'Files.Watchdog.exe'; Start-Sleep 2
$svc = Get-Process Files.HotkeyService -ErrorAction SilentlyContinue
$result['AC-3 servico subiu pelo Watchdog'] = [bool]$svc
$oldPid = $svc.Id
$sw = [Diagnostics.Stopwatch]::StartNew()
Stop-Process -Id $oldPid -Force
$newPid = $null
while ($sw.ElapsedMilliseconds -lt 5000) {
    $p = Get-Process Files.HotkeyService -ErrorAction SilentlyContinue
    if ($p -and $p.Id -ne $oldPid) { $newPid = $p.Id; break }
    Start-Sleep -Milliseconds 10
}
$result['AC-3 reaparecimento (ms, inclui o polling de 10 ms)'] = if ($newPid) { $sw.ElapsedMilliseconds } else { 'NAO VOLTOU' }
$result['AC-3 OK'] = ($newPid -and $sw.ElapsedMilliseconds -le 1000)

# Depois do renascimento o atalho ainda funciona?
Start-Sleep 1
$before3 = Get-ExplorerWindowCount
Send-WinE; Start-Sleep 8
$result['AC-3 Win+E apos renascer: Files rodando'] = [bool](Get-Process Files -ErrorAction SilentlyContinue)
$result['AC-3 janelas do Explorer antes/depois'] = "$before3 / $(Get-ExplorerWindowCount)"

# --- 4. Desligamento cooperativo: Win+E volta ao Explorer (AC-7) -----------------------------------------------
Stop-Process -Name Files -Force -ErrorAction SilentlyContinue
$ev = [System.Threading.EventWaitHandle]::OpenExisting('Global\FilesCustom_ShutdownHotkeyService')
[void]$ev.Set()
Start-Sleep 2
$result['AC-7 Watchdog/servico encerrados'] = -not (Get-Process Files.Watchdog, Files.HotkeyService -ErrorAction SilentlyContinue)
$before4 = Get-ExplorerWindowCount
Send-WinE; Start-Sleep 3
$result['AC-7 janelas do Explorer antes/depois (deve subir 1)'] = "$before4 / $(Get-ExplorerWindowCount)"
$result['AC-7 Files rodando (deve ser False)'] = [bool](Get-Process Files -ErrorAction SilentlyContinue)
$result['AC-7 OK'] = ((Get-ExplorerWindowCount) -eq $before4 + 1 -and -not $result['AC-7 Files rodando (deve ser False)'])

# limpeza: fecha a janela do Explorer aberta pelo teste e o que sobrou
$w = @((New-Object -ComObject Shell.Application).Windows() | Where-Object LocationName -eq 'This PC')
if ($w.Count) { $w[-1].Quit() }
Stop-Process -Name Files.Watchdog, Files.HotkeyService, Files -Force -ErrorAction SilentlyContinue

$result.GetEnumerator() | ForEach-Object { '{0,-62} {1}' -f $_.Key, $_.Value }
$result | ConvertTo-Json | Set-Content (Join-Path $tmp 'f017_win_e_result.json')
