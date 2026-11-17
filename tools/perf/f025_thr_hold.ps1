# F025 — CPU por thread do KyraFiles.exe durante a tecla segurada (separa UI, compositor e pool).
# Roda f025_hold_key.ps1 (1 rodada) e amostra os threads so na janela de 5 s da tecla.
# Uso: pwsh -File f025_thr_hold.ps1 -Label D1-atual   (app aberto; mesmo formato para o upstream)
param([Parameter(Mandatory)][string]$Label)
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture
Add-Type -TypeDefinition 'using System;using System.Runtime.InteropServices;public class TW2{[DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);}' -ErrorAction SilentlyContinue
$p = Get-Process Files | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
$o = 0; $ui = [TW2]::GetWindowThreadProcessId($p.MainWindowHandle, [ref]$o)
$job = Start-Job { param($s) pwsh -NoProfile -File "$using:PSScriptRoot\f025_hold_key.ps1" -Label $s -Key Down -Runs 1 } -ArgumentList $Label
Start-Sleep 9   # navegacao pela omnibar (~7 s) + volta ao inicio (1 s)
$a = @{}; $p.Refresh(); $p.Threads | ForEach-Object { $a[$_.Id] = $_.TotalProcessorTime.TotalMilliseconds }; $t0 = Get-Date
Start-Sleep 5
$p.Refresh()
$d = $p.Threads | ForEach-Object { [pscustomobject]@{ Tid = $_.Id; Ui = ($_.Id -eq $ui); CpuMs = [int]($_.TotalProcessorTime.TotalMilliseconds - $a[$_.Id]) } } | Sort-Object CpuMs -Descending
'{0}: janela {1:N1}s; CPU total {2} ms; thread de UI {3} ms' -f $Label, ((Get-Date) - $t0).TotalSeconds, ($d | Measure-Object CpuMs -Sum).Sum, ($d | Where-Object Ui).CpuMs
$d | Select-Object -First 6 | Format-Table -AutoSize | Out-String
Receive-Job $job -Wait | Select-Object -Last 1
