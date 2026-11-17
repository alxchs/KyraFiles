# F015 T17: latencia da busca por nome pelo Named Pipe do KyraFiles.App.IndexService (AC-1, AC-2), RAM (NFR-003) e CPU parado (NFR-005).
# Protocolo: JSON por linha (camelCase). Inicia o servico se nao estiver rodando e encerra ao final (se foi este script que iniciou).
param(
    [string]$ServiceExe = 'C:\desenv\utils\KyraFiles\src\KyraFiles.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX\KyraFiles.App.IndexService\KyraFiles.App.IndexService.exe',
    [string[]]$Terms = @('readme', 'config', 'program', 'test', 'index', 'xaml', 'resw', 'perf', 'spec', 'handoff'),
    [string]$Scope = 'C:\desenv',
    [int]$Repeats = 5,
    [int]$ReadyTimeoutS = 600,
    [int]$IdleS = 10,
    [int]$ReconcileTimeoutS = 900
)
$ErrorActionPreference = 'Stop'
$sid = (Get-Process -Id $PID).SessionId
$pipe = "KyraFiles.IndexService.Basic.S$sid"
$out = Join-Path $env:TEMP 'claude\desenv\utils\KyraFiles\f015'; New-Item -ItemType Directory -Force $out | Out-Null

function Send([hashtable]$req, [scriptblock]$onLine) {
    $c = New-Object System.IO.Pipes.NamedPipeClientStream('.', $pipe, 'InOut')
    $c.Connect(3000)
    $r = New-Object System.IO.StreamReader($c, [Text.UTF8Encoding]::new($false))
    $w = New-Object System.IO.StreamWriter($c, [Text.UTF8Encoding]::new($false)); $w.AutoFlush = $true
    $req.version = 1
    $w.WriteLine(($req | ConvertTo-Json -Compress))
    while ($null -ne ($line = $r.ReadLine())) {
        if (-not $line.Trim()) { continue }
        $o = $line | ConvertFrom-Json
        if (& $onLine $o) { break }
    }
    $c.Dispose()
}

$started = $null
$alive = $false
try { Send @{ command = 'ping' } { param($o) $script:alive = $true; $true } } catch {}
if (-not $alive) {
    $started = Start-Process $ServiceExe -PassThru -WindowStyle Hidden
    $t0 = Get-Date
    while (-not $alive -and ((Get-Date) - $t0).TotalSeconds -lt 30) { Start-Sleep 1; try { Send @{ command = 'ping' } { param($o) $script:alive = $true; $true } } catch {} }
    if (-not $alive) { throw 'servico nao respondeu ao ping em 30 s' }
}
$svc = Get-Process KyraFiles.App.IndexService | Select-Object -First 1
"Servico PID $($svc.Id) (iniciado por este script: $([bool]$started))"

# espera Ready
$t0 = Get-Date; $ready = $false
while (-not $ready -and ((Get-Date) - $t0).TotalSeconds -lt $ReadyTimeoutS) {
    Send @{ command = 'status' } { param($o) $script:st = $o; $true }
    $ready = $st.volumes -and -not ($st.volumes | Where-Object { "$($_.state)" -notmatch 'Ready|2' -and "$($_.state)" -notmatch 'Lock|Excl|Unav' })
    if (-not $ready) { Start-Sleep -Milliseconds 500 }
}
"Indexacao pronta: $ready em {0:N1} s" -f ((Get-Date) - $t0).TotalSeconds
$st.volumes | Format-Table -AutoSize | Out-String

# espera a reconciliacao do snapshot terminar (statusDetail deixa de dizer "reconciliando"), medindo CPU e RAM nesse periodo
$svc = Get-Process -Id $svc.Id; $cR = $svc.TotalProcessorTime; $tR = Get-Date; $peakWs = 0; $peakPriv = 0
do {
    Send @{ command = 'status' } { param($o) $script:st = $o; $true }
    $svc.Refresh(); $peakWs = [math]::Max($peakWs, $svc.WorkingSet64); $peakPriv = [math]::Max($peakPriv, $svc.PrivateMemorySize64)
    # reconcilePending existe desde a T17c; antes disso, o texto do statusDetail
    $pending = @($st.volumes | Where-Object { $_.reconcilePending -or "$($_.statusDetail)" -match 'reconcili' })
    if ($pending) { Start-Sleep 1 }
} while ($pending -and ((Get-Date) - $tR).TotalSeconds -lt $ReconcileTimeoutS)
$svc.Refresh()
'Reconciliacao: {0} em {1:N1} s, CPU {2:N0} ms, pico WorkingSet {3:N0} MB / Private {4:N0} MB' -f ($(if ($pending) { 'NAO terminou' } else { 'terminou' })), ((Get-Date) - $tR).TotalSeconds, ($svc.TotalProcessorTime - $cR).TotalMilliseconds, ($peakWs / 1MB), ($peakPriv / 1MB)
$st.volumes | Select-Object displayRoot, itemCount, overflowCount, reconcilePending, statusDetail | Format-Table -AutoSize | Out-String
$items = ($st.volumes | Measure-Object itemCount -Sum).Sum
'RAM em repouso antes das consultas: WorkingSet {0:N0} MB, Private {1:N0} MB, itens {2:N0} ({3:N0} MB privados por milhao)' -f ($svc.WorkingSet64 / 1MB), ($svc.PrivateMemorySize64 / 1MB), $items, ($svc.PrivateMemorySize64 / 1MB / ($items / 1e6))

$rows = @()
foreach ($term in $Terms) {
    for ($i = 0; $i -lt $Repeats; $i++) {
        $sw = [Diagnostics.Stopwatch]::StartNew(); $first = $null; $n = 0
        Send @{ command = 'query'; requestId = [guid]::NewGuid().ToString('N'); query = $term; scopePath = $Scope; maxResults = 100000 } {
            param($o)
            if ($o.type -eq 'batch' -and $null -eq $script:first) { $script:first = $sw.Elapsed.TotalMilliseconds }
            if ($o.type -eq 'completed') { $script:n = $o.totalMatches; return $true }
            if ($o.type -eq 'error') { throw $o.errorMessage }
            $false
        }
        $rows += [pscustomobject]@{ Term = $term; Run = $i; FirstMs = $first; TotalMs = $sw.Elapsed.TotalMilliseconds; Matches = $n }
        $first = $null
    }
}
$rows | Export-Csv (Join-Path $out 'f015_latency.csv') -NoTypeInformation
function Pct($v, $p) { $s = $v | Sort-Object; $s[[math]::Min($s.Count - 1, [int][math]::Ceiling($p * $s.Count) - 1)] }
$tot = $rows.TotalMs; $fst = $rows.FirstMs | Where-Object { $_ -ne $null }
'Consultas: {0}; escopo {1}' -f $rows.Count, $Scope
'Total   : mediana {0:N1} ms, p95 {1:N1} ms, max {2:N1} ms' -f (Pct $tot .5), (Pct $tot .95), ($tot | Measure-Object -Maximum).Maximum
if ($fst) { '1o lote : mediana {0:N1} ms, p95 {1:N1} ms' -f (Pct $fst .5), (Pct $fst .95) }
$rows | Group-Object Term | ForEach-Object { '  {0,-10} resultados={1,8} mediana total={2:N1} ms' -f $_.Name, $_.Group[0].Matches, (Pct $_.Group.TotalMs .5) }

$svc.Refresh()
'RAM do servico: WorkingSet {0:N0} MB, Private {1:N0} MB' -f ($svc.WorkingSet64 / 1MB), ($svc.PrivateMemorySize64 / 1MB)
$c0 = $svc.TotalProcessorTime; Start-Sleep $IdleS; $svc.Refresh()
'CPU parado: {0:N0} ms em {1} s' -f ($svc.TotalProcessorTime - $c0).TotalMilliseconds, $IdleS

if ($started) { try { Send @{ command = 'shutdown' } { param($o) $true } } catch {}; Start-Sleep 2; Stop-Process -Id $started.Id -Force -ErrorAction SilentlyContinue }
