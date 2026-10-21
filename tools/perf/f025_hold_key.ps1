# F025 — segura uma tecla por N s numa pasta e mede o app de fora.
# Metricas: CPU do processo (DIFERENCA de TotalProcessorTime), working set e memoria privada,
# e latencia do loop de mensagens da UI (SendMessageTimeout WM_NULL a cada ~100 ms, durante a tecla).
# Uso: pwsh -File f025_hold_key.ps1 -Label atual -Key Down -Folder C:\FilesUXLab\perf\1600 [-HoldS 5] [-Runs 3]
# O app ja deve estar aberto (shell:AppsFolder\FilesDev_ykqwq8d6ps0ag!App) e em primeiro plano.
param(
    [Parameter(Mandatory)][string]$Label,
    [ValidateSet('Down', 'End', 'Up', 'PageDown')][string]$Key = 'Down',
    [string]$Folder = 'C:\FilesUXLab\perf\1600',
    [int]$HoldS = 5,
    [int]$Runs = 3,
    [string]$ProcessName = 'Files',
    [switch]$NoNav,
    [string]$WindowClass,   # ex.: CabinetWClass para medir o Explorer do Windows na janela ja aberta na pasta
    [string]$Out = "$PSScriptRoot\..\..\docs\agents\evidence\F025\hold_key2.csv"
)
$ErrorActionPreference = 'Stop'
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture
Add-Type -AssemblyName System.Windows.Forms, UIAutomationClient, UIAutomationTypes
Add-Type @"
using System; using System.Runtime.InteropServices;
public class F025Win {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte sc, uint fl, UIntPtr ex);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll", SetLastError=true)] public static extern IntPtr SendMessageTimeout(IntPtr h, uint m, IntPtr w, IntPtr l, uint f, uint t, out IntPtr r);
}
"@
$vk = @{ Down = 0x28; Up = 0x26; End = 0x23; PageDown = 0x22 }[$Key]
$p = Get-Process $ProcessName -ErrorAction SilentlyContinue | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
if (-not $p -and -not $WindowClass) { throw 'Files.exe com janela nao encontrado. Abra o app antes.' }
$h = $p.MainWindowHandle
if ($WindowClass) {
    $cond = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::ClassNameProperty, $WindowClass)
    $win = [Windows.Automation.AutomationElement]::RootElement.FindFirst([Windows.Automation.TreeScope]::Children, $cond)
    if (-not $win) { throw "janela $WindowClass nao encontrada" }
    $h = [IntPtr]$win.Current.NativeWindowHandle; $p = Get-Process -Id $win.Current.ProcessId
}
[F025Win]::SetForegroundWindow($h) | Out-Null; Start-Sleep -Milliseconds 500
# trava: as teclas so saem se a janela medida estiver em primeiro plano (nunca em outra janela aberta)
if ([F025Win]::GetForegroundWindow() -ne $h) { try { [Windows.Automation.AutomationElement]::FromHandle($h).SetFocus() } catch {}; Start-Sleep -Milliseconds 500 }
if ([F025Win]::GetForegroundWindow() -ne $h) { throw "a janela medida nao esta em primeiro plano; nenhuma tecla enviada" }
# navega pela omnibar (igual nos dois builds): Ctrl+L, caminho, Enter
if (-not $NoNav) {
[System.Windows.Forms.SendKeys]::SendWait('^l'); Start-Sleep -Milliseconds 400
[System.Windows.Forms.SendKeys]::SendWait($Folder.Replace('{','{{}').Replace('}','{}}') + '{ENTER}'); Start-Sleep -Seconds 6 }

function Ping-Ui { $sw = [Diagnostics.Stopwatch]::StartNew(); $r = [IntPtr]::Zero
  $ok = [F025Win]::SendMessageTimeout($h, 0, [IntPtr]::Zero, [IntPtr]::Zero, 2, 5000, [ref]$r); $sw.Stop()
  if ($ok -eq [IntPtr]::Zero) { return 5000 } else { return $sw.Elapsed.TotalMilliseconds } }

New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
if (-not (Test-Path $Out)) { 'label,key,run,hold_s,cpu_ms,ws_mb,private_mb,ui_lat_median_ms,ui_lat_max_ms,ui_pings,ui_recover_ms,presses,backlog_steps_after_release,drain_ms,final_item,date' | Set-Content $Out }
for ($run = 1; $run -le $Runs; $run++) {
    [F025Win]::SetForegroundWindow($h) | Out-Null; Start-Sleep -Milliseconds 300
    if ([F025Win]::GetForegroundWindow() -ne $h) { throw "rodada ${run}: janela medida fora do primeiro plano; parado" }
    # volta ao inicio da lista
    [F025Win]::keybd_event(0x24, 0, 0, [UIntPtr]::Zero); [F025Win]::keybd_event(0x24, 0, 2, [UIntPtr]::Zero); Start-Sleep -Seconds 1
    $startFocus = try { [Windows.Automation.AutomationElement]::FocusedElement.Current.Name } catch { '?' }
    $p.Refresh(); $cpu0 = $p.TotalProcessorTime
    $lat = New-Object System.Collections.Generic.List[double]
    $t = [Diagnostics.Stopwatch]::StartNew(); $next = 0; $sent = 0
    while ($t.Elapsed.TotalSeconds -lt $HoldS) {
        if ([F025Win]::GetForegroundWindow() -ne $h) { [F025Win]::keybd_event($vk, 0, 2, [UIntPtr]::Zero); throw "rodada ${run}: janela medida saiu do primeiro plano durante a tecla; parado" }
        [F025Win]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); $sent++   # repeticao do gerador (ritmo real contado em presses)
        Start-Sleep -Milliseconds 33
        if ($t.ElapsedMilliseconds -ge $next) { $lat.Add((Ping-Ui)); $next = $t.ElapsedMilliseconds + 100 }
    }
    [F025Win]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
    $rec = [Diagnostics.Stopwatch]::StartNew()
    # fila de teclas: depois de soltar, quantos itens a selecao ainda anda e por quanto tempo (o "congelamento" sentido)
    function Get-Focus { try { [Windows.Automation.AutomationElement]::FocusedElement.Current.Name } catch { '?' } }
    $prev = Get-Focus; $steps = 0; $lastChange = 0; $drain = [Diagnostics.Stopwatch]::StartNew()
    while ($drain.Elapsed.TotalSeconds -lt 40 -and (($drain.ElapsedMilliseconds - $lastChange) -lt 2000)) {
        Start-Sleep -Milliseconds 100; $cur = Get-Focus
        if ($cur -ne $prev) { $steps++; $prev = $cur; $lastChange = $drain.ElapsedMilliseconds }
    }
    $final = $prev
    while ((Ping-Ui) -gt 100 -and $rec.Elapsed.TotalSeconds -lt 60) { Start-Sleep -Milliseconds 100 }
    $p.Refresh(); $cpu = ($p.TotalProcessorTime - $cpu0).TotalMilliseconds
    $presses = $sent; $s = $lat | Sort-Object; $med = $s[[int]($s.Count / 2)]; $max = $s[-1]
    $row = '{0},{1},{2},{3},{4:F0},{5:F0},{6:F0},{7:F0},{8:F0},{9},{10:F0},{11},{12},{13},{14},{15},{16}' -f $Label, $Key, $run, $HoldS, $cpu, ($p.WorkingSet64/1MB), ($p.PrivateMemorySize64/1MB), $med, $max, $lat.Count, $rec.ElapsedMilliseconds, $presses, $steps, $lastChange, ($final -replace ",",";"), (Get-Date -Format s), ($startFocus -replace ",",";")
    Add-Content $Out $row; Write-Host $row
    Start-Sleep -Seconds 3
}
