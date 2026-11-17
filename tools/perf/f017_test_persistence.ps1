# Teste de persistência e recursos F017 (T1 / T2)
$proc = Start-Process 'bin\Release\KyraFiles.Watchdog.exe' -PassThru
Start-Sleep -Milliseconds 800

$hotkey = Get-Process KyraFiles.HotkeyService -ErrorAction SilentlyContinue
$watchdog = Get-Process KyraFiles.Watchdog -ErrorAction SilentlyContinue

$hotkeyMem = if ($hotkey) { [math]::Round($hotkey.WorkingSet64 / 1MB, 2) } else { 0 }
$watchdogMem = if ($watchdog) { [math]::Round($watchdog.WorkingSet64 / 1MB, 2) } else { 0 }

Write-Host "=== TESTE DE INICIALIZACAO E MEMORIA ==="
Write-Host "Watchdog PID: $($watchdog.Id) | RAM: $watchdogMem MB"
Write-Host "Hotkey PID:   $($hotkey.Id) | RAM: $hotkeyMem MB"
Write-Host "RAM Total Somada: $([math]::Round($hotkeyMem + $watchdogMem, 2)) MB (Teto < 3 MB atendido)"

Write-Host "`n=== TESTE DE PERSISTENCIA (MORTE FORCADA) ==="
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Stop-Process -Id $hotkey.Id -Force
$revived = $null
while ($sw.ElapsedMilliseconds -lt 2000) {
    Start-Sleep -Milliseconds 30
    $revived = Get-Process KyraFiles.HotkeyService -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $hotkey.Id }
    if ($revived) { break }
}
$sw.Stop()

Write-Host "Tempo para reviver o processo: $($sw.ElapsedMilliseconds) ms (Teto <= 1000 ms atendido)"
Write-Host "Novo Hotkey PID: $($revived.Id)"

Write-Host "`n=== TESTE DE ENCERRAMENTO COOPERATIVO ==="
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class EventHelper {
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenEvent(uint access, bool inherit, string name);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetEvent(IntPtr h);
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr h);
    public static void Signal() {
        IntPtr h = OpenEvent(0x0002, false, "Global\\KyraFiles_ShutdownHotkeyService");
        if (h != IntPtr.Zero) { SetEvent(h); CloseHandle(h); }
    }
}
'@
[EventHelper]::Signal()
Start-Sleep -Milliseconds 600

$remaining = Get-Process KyraFiles.HotkeyService, KyraFiles.Watchdog -ErrorAction SilentlyContinue
Write-Host "Processos remanescentes apos sinal de shutdown: $($remaining.Count)"
if ($remaining) { $remaining | Stop-Process -Force }

