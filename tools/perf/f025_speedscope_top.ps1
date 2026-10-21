# F025/F026 — resume um trace Speedscope "evented" (dotnet-trace convert --format Speedscope) por thread:
# tempo ocupado por thread e as N funcoes mais quentes (exclusivo e inclusivo, em ms e % do trace).
# Uso: pwsh -File f025_speedscope_top.ps1 -Json <arquivo.speedscope.json> [-ThreadId 60104] [-Top 25]
param([Parameter(Mandatory)][string]$Json, [string]$ThreadId, [int]$Top = 25)
[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::InvariantCulture
$j = Get-Content $Json -Raw | ConvertFrom-Json
$frames = $j.shared.frames
function Measure-Profile($p) {
    $excl = @{}; $incl = @{}; $stack = New-Object System.Collections.Generic.List[int]; $prev = $p.startValue; $busy = 0.0
    foreach ($e in $p.events) {
        $dt = $e.at - $prev; $prev = $e.at
        if ($dt -gt 0 -and $stack.Count -gt 0) {
            $busy += $dt
            $leaf = $frames[$stack[$stack.Count - 1]].name; $excl[$leaf] = $excl[$leaf] + $dt
            foreach ($f in ($stack | Sort-Object -Unique)) { $n = $frames[$f].name; $incl[$n] = $incl[$n] + $dt }
        }
        if ($e.type -eq 'O') { $stack.Add([int]$e.frame) } elseif ($stack.Count -gt 0) { $stack.RemoveAt($stack.Count - 1) }
    }
    [pscustomobject]@{ Name = $p.name; Busy = $busy; Span = ($p.endValue - $p.startValue); Excl = $excl; Incl = $incl }
}
$all = foreach ($p in $j.profiles) { if ($p.events.Count -gt 0) { Measure-Profile $p } }
"threads com amostras: $($all.Count)"
$all | Sort-Object Busy -Descending | Select-Object -First 8 | ForEach-Object { '{0,-18} ocupado {1,8:N0} ms de {2:N0} ms' -f $_.Name, $_.Busy, $_.Span }
$t = if ($ThreadId) { $all | Where-Object { $_.Name -match $ThreadId } | Select-Object -First 1 } else { $all | Sort-Object Busy -Descending | Select-Object -First 1 }
"`nthread analisada: $($t.Name) (ocupado $([int]$t.Busy) ms de $([int]$t.Span) ms)"
"--- exclusivo (top $Top) ---"
$t.Excl.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First $Top | ForEach-Object { '{0,7:N0} ms {1,5:N1}%  {2}' -f $_.Value, (100 * $_.Value / $t.Span), $_.Key.Substring(0, [Math]::Min(150, $_.Key.Length)) }
"--- inclusivo (top $Top) ---"
$t.Incl.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First $Top | ForEach-Object { '{0,7:N0} ms {1,5:N1}%  {2}' -f $_.Value, (100 * $_.Value / $t.Span), $_.Key.Substring(0, [Math]::Min(150, $_.Key.Length)) }
