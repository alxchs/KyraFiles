param([string]$Phase = 'throughput')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSCommandPath
$exe = Get-ChildItem "$env:TEMP\Compiler" -Recurse -Filter CopySpike.exe | Where-Object FullName -match ([regex]::Escape((Get-Content "$root\.guid"))) | Select-Object -First 1 -ExpandProperty FullName
$src = "$root\src"; $dst = "$root\dst"; New-Item -ItemType Directory -Force $dst | Out-Null
$out = "$root\results-$Phase.csv"
function Log($l) { Write-Host $l; Add-Content $out $l }
function Run($a) { ,@(& $exe @a 2>&1 | ForEach-Object { "$_" }) }   # devolve SEMPRE array: [-1] é a última linha, não o último caractere
function RmDst { Get-ChildItem $dst -File -Force -ErrorAction SilentlyContinue | ForEach-Object { $_.Attributes = 'Normal'; Remove-Item $_.FullName -Force } }

if ($Phase -eq 'throughput') {
  if (-not (Test-Path "$src\a.bin")) { Run @('gen', "$src\a.bin", '1024') | Out-Null }
  Log "metodo,bloco,bytes,ms,MiBps,extra"
  foreach ($r in 1..3) {
    foreach ($m in 'filecopy','robocopy','chunk1','chunk4','chunk16','cf2') {
      RmDst
      switch -Regex ($m) {
        '^filecopy$' { $l = Run @('filecopy', "$src\a.bin", "$dst\a.bin") }
        '^cf2$'      { $l = Run @('cf2', "$src\a.bin", "$dst\a.bin") }
        '^chunk(\d+)$' { $l = Run @('chunk', "$src\a.bin", "$dst\a.bin", '--chunk', $Matches[1]) }
        '^robocopy$' { $sw = [Diagnostics.Stopwatch]::StartNew(); robocopy $src $dst a.bin /NFL /NDL /NJH /NJS /NP | Out-Null; $sw.Stop(); $l = "robocopy,-,1073741824,{0:F0},{1:F0}," -f $sw.Elapsed.TotalMilliseconds, (1024/($sw.Elapsed.TotalMilliseconds/1000)) }
      }
      Log "run$r,$l"
    }
  }
  RmDst
}

if ($Phase -eq 'big') {
  if (-not (Test-Path "$src\b.bin")) { Run @('gen', "$src\b.bin", '2048') | Out-Null }
  Log "metodo,bloco,bytes,ms,MiBps,extra"
  foreach ($r in 1..3) {
    foreach ($m in 'filecopy','robocopy','chunk4','cf2') {
      RmDst
      switch -Regex ($m) {
        '^filecopy$' { $l = Run @('filecopy', "$src\b.bin", "$dst\b.bin") }
        '^cf2$'      { $l = Run @('cf2', "$src\b.bin", "$dst\b.bin") }
        '^chunk(\d+)$' { $l = Run @('chunk', "$src\b.bin", "$dst\b.bin", '--chunk', $Matches[1]) }
        '^robocopy$' { $sw = [Diagnostics.Stopwatch]::StartNew(); robocopy $src $dst b.bin /NFL /NDL /NJH /NJS /NP | Out-Null; $sw.Stop(); $l = "robocopy,-,2147483648,{0:F0},{1:F0}," -f $sw.Elapsed.TotalMilliseconds, (2048/($sw.Elapsed.TotalMilliseconds/1000)) }
      }
      Log "run$r,$l"
    }
  }
  RmDst
}

if ($Phase -eq 'pause') {
  if (-not (Test-Path "$src\b.bin")) { Run @('gen', "$src\b.bin", '2048') | Out-Null }
  $h = (Run @('hash', "$src\b.bin"))[-1]
  Log "teste,detalhe"
  foreach ($r in 1..3) {
    RmDst
    $l = Run @('chunk', "$src\b.bin", "$dst\b.bin", '--chunk', '4', '--pause-at-ms', '700', '--pause-for-ms', '3000')
    $hd = (Run @('hash', "$dst\b.bin"))[-1]
    Log "chunk-pausa-run$r,$l,hash_igual=$($h -eq $hd)"
  }
  foreach ($r in 1..3) {
    RmDst
    $l = Run @('cf2', "$src\b.bin", "$dst\b.bin", '--pause-after-mib', '500', '--pause-for-ms', '3000')
    $hd = (Run @('hash', "$dst\b.bin"))[-1]
    Log "cf2-pausa-run$r,$l,hash_igual=$($h -eq $hd)"
  }
  # morte do processo no meio e retomada por outro processo
  foreach ($r in 1..2) {
    RmDst
    $p = Start-Process $exe -ArgumentList @('chunk', "$src\b.bin", "$dst\b.bin", '--chunk', '4') -PassThru -WindowStyle Hidden
    Start-Sleep -Milliseconds 700; Stop-Process -Id $p.Id -Force; Start-Sleep -Milliseconds 300
    $parcial = (Get-Item "$dst\b.bin").Length / 1MB
    $l = Run @('resume', "$src\b.bin", "$dst\b.bin", '--chunk', '4')
    $hd = (Run @('hash', "$dst\b.bin"))[-1]
    Log "morte-e-retomada-run$r,parcial_mib=$([math]::Round($parcial)),$l,hash_igual=$($h -eq $hd)"
  }
  RmDst
}

if ($Phase -eq 'meta') {
  Run @('gen', "$src\m.bin", '8') | Out-Null
  Run @('stamp', "$src\m.bin") | Out-Null
  Log "origem: $((Run @('meta', "$src\m.bin"))[-1])"
  foreach ($m in 'filecopy','robocopy','robocopy-sec','chunk','cf2') {
    RmDst
    switch ($m) {
      'filecopy' { Run @('filecopy', "$src\m.bin", "$dst\m.bin") | Out-Null }
      'robocopy' { robocopy $src $dst m.bin /NFL /NDL /NJH /NJS /NP | Out-Null }
      'robocopy-sec' { robocopy $src $dst m.bin /COPY:DATSOU /NFL /NDL /NJH /NJS /NP | Out-Null }
      'chunk' { Run @('chunk', "$src\m.bin", "$dst\m.bin", '--chunk', '4') | Out-Null }
      'cf2' { Run @('cf2', "$src\m.bin", "$dst\m.bin") | Out-Null }
    }
    Log "$m`: $((Run @('meta', "$dst\m.bin"))[-1])"
  }
  RmDst
}
