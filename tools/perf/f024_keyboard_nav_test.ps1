# Bateria de Testes Automatizada de Navegação por Teclado (F024)
param(
    [string]$TargetFolder = 'C:\FilesUXLab\perf\1600',
    [int]$WaitLoadS = 4
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms

# 1. Garante que a pasta existe com os 1.600 arquivos
if (-not (Test-Path $TargetFolder) -or ((Get-ChildItem -File $TargetFolder).Count -lt 1600)) {
    Write-Host "Pasta $TargetFolder inexistente ou incompleta. Gerando..."
    & pwsh (Join-Path $PSScriptRoot "setup_1600_lab.ps1")
}

# 2. Navega até a pasta no Files
Write-Host "Abrindo $TargetFolder no Files..."
$filesProc = Get-Process -Name Files -ErrorAction SilentlyContinue | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
if (-not $filesProc) {
    Start-Process "shell:AppsFolder\FilesDev_ykqwq8d6ps0ag!App"
    Start-Sleep -Seconds 3
}
Start-Process "files-dev:$TargetFolder"

$swWait = [System.Diagnostics.Stopwatch]::StartNew()
while ($swWait.Elapsed.TotalSeconds -lt 15) {
    $filesProc = Get-Process -Name Files -ErrorAction SilentlyContinue | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
    if ($filesProc) { break }
    Start-Sleep -Milliseconds 500
}
if (-not $filesProc) {
    throw "Processo Files não encontrado com janela ativa após 15s."
}
Start-Sleep -Seconds $WaitLoadS

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinUtilF024 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    public const int VK_CONTROL = 0x11;
    public const int VK_HOME = 0x24;
    public const int VK_END = 0x23;
    public const int VK_PRIOR = 0x21; // PgUp
    public const int VK_NEXT = 0x22;  // PgDn
    public const uint KEYEVENTF_KEYUP = 0x0002;

    public static void SendCtrlKey(byte key) {
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
    public static void SendSingleKey(byte key) {
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
"@

[WinUtilF024]::SetForegroundWindow($filesProc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 500

Write-Host "Iniciando medições de teclas de navegação rápida..."
$results = [ordered]@{}

# Teste 1: Ctrl + End (Salto para o final da lista de 1.600 arquivos)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
[WinUtilF024]::SendCtrlKey([WinUtilF024]::VK_END)
Start-Sleep -Milliseconds 150 # Janela de resposta esperada
$sw.Stop()
$results["Ctrl+End_TempoRespostaMs"] = $sw.ElapsedMilliseconds

# Teste 2: Ctrl + Home (Salto de volta para o início da lista)
$sw.Restart()
[WinUtilF024]::SendCtrlKey([WinUtilF024]::VK_HOME)
Start-Sleep -Milliseconds 150
$sw.Stop()
$results["Ctrl+Home_TempoRespostaMs"] = $sw.ElapsedMilliseconds

# Teste 3: Page Down
$sw.Restart()
[WinUtilF024]::SendSingleKey([WinUtilF024]::VK_NEXT)
Start-Sleep -Milliseconds 100
$sw.Stop()
$results["PgDn_TempoRespostaMs"] = $sw.ElapsedMilliseconds

# Teste 4: Page Up
$sw.Restart()
[WinUtilF024]::SendSingleKey([WinUtilF024]::VK_PRIOR)
Start-Sleep -Milliseconds 100
$sw.Stop()
$results["PgUp_TempoRespostaMs"] = $sw.ElapsedMilliseconds

Write-Host "--- RESULTADOS DA BATERIA DE TECLADO ---"
$results | Format-Table -AutoSize
return $results
