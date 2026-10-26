# F029 — A/B das setas: mede o Explorer do Windows (meta) e cada variante do Files (bandeiras em C:\FilesUXLab\f029_flags.txt),
# 5 rodadas de Down segurado cada, com o mesmo f025_hold_key.ps1. So comeca depois de -IdleS segundos sem uso do computador,
# para nao disputar teclado e mouse com o Alexandre; cada rodada para se a janela medida sair do primeiro plano.
# Uso: pwsh -File f029_ab.ps1 [-Variants 'base','nothrottle'] [-Runs 5] [-SkipExplorer] [-Key Down]
param([string[]]$Variants = @('base', 'nothrottle', 'throttle1', 'ownarrows', 'ownarrows+nothrottle'),
      [int]$Runs = 5, [int]$IdleS = 60, [switch]$SkipExplorer, [string]$Key = 'Down',
      [string]$Folder = 'C:\FilesUXLab\perf\1600')
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type 'using System;using System.Runtime.InteropServices;public class AbIdle{[StructLayout(LayoutKind.Sequential)]public struct LII{public uint cb;public uint t;}[DllImport("user32.dll")]public static extern bool GetLastInputInfo(ref LII p);public static uint Ms(){var l=new LII();l.cb=(uint)Marshal.SizeOf(l);GetLastInputInfo(ref l);return (uint)Environment.TickCount-l.t;}[DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();public static IntPtr Fg(){return GetForegroundWindow();}}'
function Log($m) { '{0:HH:mm:ss} {1}' -f (Get-Date), $m }

$deadline = (Get-Date).AddHours(3)
while ([AbIdle]::Ms() -lt $IdleS * 1000) {
    if ((Get-Date) -gt $deadline) { Log 'computador nao ficou ocioso em 3 h; nada medido'; exit 2 }
    Start-Sleep -Seconds 15
}
Log "ocioso ha $([int]([AbIdle]::Ms()/1000)) s; comecando"

if (-not $SkipExplorer) {
    Start-Process explorer.exe $Folder; Start-Sleep -Seconds 4
    pwsh -NoProfile -File "$here\prep_window.ps1" -WindowClass CabinetWClass -NameLike "*$(Split-Path $Folder -Leaf)*"
    if ($LASTEXITCODE -ne 0) { Log 'Explorer nao veio para a frente; medicao do Explorer pulada'; exit 3 }
    pwsh -NoProfile -File "$here\f025_hold_key.ps1" -Label EXPLORER -Key $Key -NoNav -Runs $Runs -WindowClass CabinetWClass | Select-Object -Last $Runs
    $cond = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::ClassNameProperty, 'CabinetWClass')
    $w = [Windows.Automation.AutomationElement]::RootElement.FindAll([Windows.Automation.TreeScope]::Children, $cond) | Where-Object { $_.Current.Name -like "*$(Split-Path $Folder -Leaf)*" } | Select-Object -First 1
    if ($w) { ($w.GetCurrentPattern([Windows.Automation.WindowPattern]::Pattern)).Close() }
}

foreach ($v in $Variants) {
    Set-Content 'C:\FilesUXLab\f029_flags.txt' ($v -replace '\+', ' ')
    Get-Process Files -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep -Seconds 2
    Start-Process explorer.exe 'shell:AppsFolder\FilesDev_ykqwq8d6ps0ag!App'; Start-Sleep -Seconds 10
    $leaf = Split-Path $Folder -Leaf
    $p = Get-Process Files | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
    pwsh -NoProfile -File "$here\prep_window.ps1" -ProcessName Files
    if ($LASTEXITCODE -ne 0) { Log "variante ${v}: janela do Files nao veio para a frente; pulada, nenhuma tecla enviada"; continue }
    if ($p.MainWindowTitle -notlike "$leaf*") {
        # abre a pasta pela barra de endereco, so com o Files em primeiro plano
        Add-Type -AssemblyName System.Windows.Forms
        if ([AbIdle]::Fg() -ne $p.MainWindowHandle) { Log "variante ${v}: Files fora do primeiro plano; pulada, nenhuma tecla enviada"; continue }
        [System.Windows.Forms.SendKeys]::SendWait('^l'); Start-Sleep -Milliseconds 1200
        if ([AbIdle]::Fg() -ne $p.MainWindowHandle) { Log "variante ${v}: Files saiu do primeiro plano; pulada"; continue }
        [System.Windows.Forms.SendKeys]::SendWait($Folder + '{ENTER}'); Start-Sleep -Seconds 6
        $p.Refresh()
        if ($p.MainWindowTitle -notlike "$leaf*") { Log "variante ${v}: Files nao abriu na pasta ($($p.MainWindowTitle)); pulada"; continue }
        pwsh -NoProfile -File "$here\prep_window.ps1" -ProcessName Files
    }
    pwsh -NoProfile -File "$here\f025_hold_key.ps1" -Label "F029-$v" -Key $Key -NoNav -Runs $Runs | Select-Object -Last $Runs
}
Set-Content 'C:\FilesUXLab\f029_flags.txt' ''
Log 'fim'
