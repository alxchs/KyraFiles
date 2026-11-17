# F015 T17: perfil de CPU do KyraFiles.App.IndexService durante a reconciliacao do snapshot (NFR-005).
# Inicia o servico, espera WarmupS, coleta DurationS de amostragem com dotnet-trace e imprime as funcoes mais caras.
param(
    [string]$ServiceExe = 'C:\desenv\utils\KyraFiles\src\KyraFiles.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX\KyraFiles.App.IndexService\KyraFiles.App.IndexService.exe',
    [int]$WarmupS = 30,
    [int]$DurationS = 30
)
$ErrorActionPreference = 'Stop'
$out = Join-Path $env:TEMP 'claude\desenv\utils\KyraFiles\f015'; New-Item -ItemType Directory -Force $out | Out-Null
$trace = Join-Path $out ('reconcile-{0:yyyyMMdd_HHmmss}.nettrace' -f (Get-Date))

$p = Start-Process $ServiceExe -PassThru -WindowStyle Hidden
try {
    Start-Sleep $WarmupS
    $p.Refresh(); $c0 = $p.TotalProcessorTime; $t0 = Get-Date
    "Servico PID $($p.Id), threads $($p.Threads.Count), CPU ate agora $([int]$c0.TotalMilliseconds) ms"
    & dotnet-trace collect -p $p.Id --profile dotnet-sampled-thread-time --duration ('00:00:00:{0:D2}' -f $DurationS) -o $trace | Out-Null
    $p.Refresh()
    'CPU durante a coleta: {0:N0} ms em {1:N1} s' -f ($p.TotalProcessorTime - $c0).TotalMilliseconds, ((Get-Date) - $t0).TotalSeconds
    $p.Threads | Sort-Object TotalProcessorTime -Descending | Select-Object -First 6 Id, @{n = 'CpuMs'; e = { [int]$_.TotalProcessorTime.TotalMilliseconds } }, PriorityLevel, ThreadState | Format-Table -AutoSize | Out-String
    "Trace: $trace"
    & dotnet-trace report $trace topN -n 25 --inclusive
    & dotnet-trace report $trace topN -n 25
}
finally {
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
}
