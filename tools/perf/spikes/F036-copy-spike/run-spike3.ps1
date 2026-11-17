param([string]$Phase = 'copy', [string]$OutDir = 'C:\desenv\utils\KyraFiles-specs-backlog\docs\agents\evidence\F036')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSCommandPath
$guid = Get-Content "$root\.guid"
$exe = Get-ChildItem "$env:TEMP\Compiler" -Recurse -Filter CopySpike.exe | Where-Object FullName -match $guid | Select-Object -First 1 -ExpandProperty FullName
$S = "\\hd-roteador\sda1\temp\claude\$guid"          # TODOS os dados de teste ficam aqui dentro e só aqui
$A = "$S\a"; $B = "$S\b"
New-Item -ItemType Directory -Force -Path $A,$B | Out-Null
$out = Join-Path $OutDir "results3-$Phase.csv"
function Log($l) { Write-Host $l; Add-Content $out $l }
function Run($a) { ,@(& $exe @a 2>&1 | ForEach-Object { "$_" }) }
function Hash($p) { (Run @('hash', $p))[-1] }
function CleanDir($dir) { Get-ChildItem -LiteralPath $dir -File -Force -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Attributes = 'Normal' } catch {}; Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue } }
function Mbps($bytes, $ms) { [math]::Round(($bytes/1MB) / ($ms/1000.0), 1) }
function ShellCopy($file, $destDir, $expectLen) {
  $sh = New-Object -ComObject Shell.Application
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $sh.Namespace($destDir).CopyHere($file, 4 + 16)
  $target = Join-Path $destDir (Split-Path $file -Leaf)
  while ($sw.Elapsed.TotalSeconds -lt 240) {
    Start-Sleep -Milliseconds 300
    if ((Test-Path -LiteralPath $target) -and ((Get-Item -LiteralPath $target -Force).Length -eq $expectLen)) { Start-Sleep -Milliseconds 700; break }
  }
  $sw.Stop(); return $sw.Elapsed.TotalMilliseconds
}

if ($Phase -eq 'copy') {
  Log "teste,detalhe"
  $mib = 256
  $sw = [Diagnostics.Stopwatch]::StartNew(); Run @('gen', "$A\n.bin", "$mib") | Out-Null; $sw.Stop()
  Log "gravar-no-NAS-blocos-gen,$mib MiB,$([math]::Round($sw.Elapsed.TotalMilliseconds)) ms,$(Mbps ($mib*1MB) $sw.Elapsed.TotalMilliseconds) MiB/s"
  $len = (Get-Item -LiteralPath "$A\n.bin").Length
  $sw.Restart(); $h = Hash "$A\n.bin"; $sw.Stop()
  Log "ler-do-NAS-hash,$mib MiB,$([math]::Round($sw.Elapsed.TotalMilliseconds)) ms,$(Mbps $len $sw.Elapsed.TotalMilliseconds) MiB/s"
  foreach ($r in 1..2) {
    foreach ($m in 'filecopy','cf2','shell','chunk4') {
      CleanDir $B
      switch ($m) {
        'filecopy' { $l = (Run @('filecopy', "$A\n.bin", "$B\n.bin"))[-1] }
        'cf2'      { $l = (Run @('cf2', "$A\n.bin", "$B\n.bin"))[-1] }
        'chunk4'   { $l = (Run @('chunk', "$A\n.bin", "$B\n.bin", '--chunk', '4'))[-1] }
        'shell'    { $ms = ShellCopy "$A\n.bin" $B $len; $l = "shell,-,$len,$([math]::Round($ms)),$([math]::Round(($len/1MB)/($ms/1000)))" }
      }
      Log "NAS-para-NAS-run$r-$m,$l,hash_igual=$((Hash "$B\n.bin") -eq $h)"
    }
  }
  CleanDir $B
}

if ($Phase -eq 'pause') {
  Log "teste,detalhe"
  $n = "$A\n.bin"; if (-not (Test-Path -LiteralPath $n)) { Run @('gen', $n, '256') | Out-Null }
  $h = Hash $n
  foreach ($r in 1..3) {
    CleanDir $B
    $l = (Run @('cf2b', $n, "$B\n.bin", '--pause-after-mib', '64', '--pause2-after-mib', '160', '--pause-for-ms', '2000'))[-1]
    Log "NAS-para-NAS-pausa-dupla-run$r,$l,hash_igual=$((Hash "$B\n.bin") -eq $h)"
  }
  CleanDir $B
  # cancelar na pausa: o parcial pode ser apagado?
  $l = (Run @('cf2b', $n, "$B\n.bin", '--pause-after-mib', '64', '--cancel-after-pause'))[-1]
  Log "NAS-cancelar-na-pausa,$l,hash_igual=$((Hash "$B\n.bin") -eq $h)"
  CleanDir $B
  # nome temporário + renomear (o plano da fase C), inclusive substituindo um arquivo existente
  Run @('cf2', $n, "$B\n.bin.filespart") | Out-Null
  [IO.File]::Move("$B\n.bin.filespart", "$B\n.bin")
  Log "NAS-temp-e-renomear,existe_final=$(Test-Path -LiteralPath "$B\n.bin"),existe_temp=$(Test-Path -LiteralPath "$B\n.bin.filespart"),hash_igual=$((Hash "$B\n.bin") -eq $h)"
  Run @('cf2', $n, "$B\n.bin.filespart") | Out-Null
  try { [IO.File]::Move("$B\n.bin.filespart", "$B\n.bin", $true); Log "NAS-renomear-substituindo,ok=True,hash_igual=$((Hash "$B\n.bin") -eq $h)" } catch { Log "NAS-renomear-substituindo,ERRO=$($_.Exception.Message)" }
  CleanDir $B
}

if ($Phase -eq 'ads') {
  Log "teste,detalhe"
  # origem LOCAL somente leitura: um arquivo pequeno que já tenha Zone.Identifier (marca de baixado da internet)
  $cand = Get-ChildItem 'D:\DriveD\Downloads','C:\Users\alxch\Downloads' -File -ErrorAction SilentlyContinue | Where-Object { $_.Length -lt 20MB -and $_.Length -gt 1KB } | Where-Object { Get-Item -LiteralPath $_.FullName -Stream Zone.Identifier -ErrorAction SilentlyContinue } | Select-Object -First 1
  if (-not $cand) { Log "sem-origem-com-Zone.Identifier-disponivel"; return }
  Log "origem(somente leitura): $($cand.Name) $($cand.Length) bytes"
  Log "origem-meta: $((Run @('meta', $cand.FullName))[-1])"
  $h = Hash $cand.FullName
  foreach ($m in 'filecopy','cf2','shell','chunk4') {
    CleanDir $B
    $res = ''
    try {
      switch ($m) {
        'filecopy' { $res = (Run @('filecopy', $cand.FullName, "$B\$($cand.Name)"))[-1] }
        'cf2'      { $res = (Run @('cf2', $cand.FullName, "$B\$($cand.Name)"))[-1] }
        'chunk4'   { $res = (Run @('chunk', $cand.FullName, "$B\$($cand.Name)", '--chunk', '4'))[-1] }
        'shell'    { ShellCopy $cand.FullName $B $cand.Length | Out-Null; $res = 'shell' }
      }
    } catch { $res = "EXCECAO $($_.Exception.Message)" }
    $ex = Test-Path -LiteralPath "$B\$($cand.Name)"
    $hm = if ($ex) { (Hash "$B\$($cand.Name)") -eq $h } else { 'n/a' }
    Log "para-NAS-$m,existe=$ex,hash_igual=$hm,saida=$((($res -replace ',',';')).Substring(0,[Math]::Min(150,$res.Length)))"
    if ($ex) { Log "  destino-meta: $((Run @('meta', "$B\$($cand.Name)"))[-1])" }
  }
  CleanDir $B
}

if ($Phase -eq 'conc') {
  Log "teste,detalhe"
  $bc = "$S\lote_copiar"; $bd1 = "$S\lote_apagar1"; $bd2 = "$S\lote_apagar2"; $dstA = "$S\destino1"; $dstB = "$S\destino2"
  New-Item -ItemType Directory -Force -Path $bc,$bd1,$bd2,$dstA,$dstB | Out-Null
  # lote a copiar: 24 arquivos de 8 MiB gerados NO NAS; lotes a apagar: 300 arquivos pequenos cada
  foreach ($i in 1..24) { Run @('gen', "$bc\f$i.bin", '8') | Out-Null }
  $small = New-Object byte[] (64KB)
  foreach ($i in 1..300) { [IO.File]::WriteAllBytes("$bd1\s$i.dat", $small); [IO.File]::WriteAllBytes("$bd2\s$i.dat", $small) }
  $bytes = 24 * 8MB
  function CopyBatch($dst) { $sw = [Diagnostics.Stopwatch]::StartNew(); robocopy $bc $dst /NFL /NDL /NJH /NJS /NP | Out-Null; $sw.Stop(); $sw.Elapsed.TotalMilliseconds }
  function DelBatch($dir) { $sw = [Diagnostics.Stopwatch]::StartNew(); Get-ChildItem -LiteralPath $dir -File | Remove-Item -Force; $sw.Stop(); $sw.Elapsed.TotalMilliseconds }
  $tc = CopyBatch $dstA;  Log "copiar-lote-sozinho,24x8MiB,$([math]::Round($tc)) ms,$(Mbps $bytes $tc) MiB/s"
  $td = DelBatch $bd1;    Log "apagar-300-pequenos-sozinho,$([math]::Round($td)) ms"
  CleanDir $dstA
  # concorrente: copia o lote enquanto apaga o outro
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $job = Start-Job -ScriptBlock { param($s,$d) $x = [Diagnostics.Stopwatch]::StartNew(); robocopy $s $d /NFL /NDL /NJH /NJS /NP | Out-Null; $x.Elapsed.TotalMilliseconds } -ArgumentList $bc,$dstB
  $tdc = DelBatch $bd2
  $tcc = Receive-Job $job -Wait -AutoRemoveJob
  $sw.Stop()
  Log "copiar-e-apagar-AO-MESMO-TEMPO,copia=$([math]::Round($tcc)) ms,$(Mbps $bytes $tcc) MiB/s,apagar=$([math]::Round($tdc)) ms,parede=$([math]::Round($sw.Elapsed.TotalMilliseconds)) ms,soma_sozinhos=$([math]::Round($tc+$td)) ms"
  Log "conferencia,arquivos_copiados=$((Get-ChildItem -LiteralPath $dstB -File | Measure-Object).Count)/24,restantes_a_apagar=$((Get-ChildItem -LiteralPath $bd2 -File -ErrorAction SilentlyContinue | Measure-Object).Count)"
  # duas cópias grandes no mesmo servidor: em sequência ou juntas?
  $n = "$A\n.bin"; if (-not (Test-Path -LiteralPath $n)) { Run @('gen', $n, '256') | Out-Null }
  $len = (Get-Item -LiteralPath $n).Length
  CleanDir $B
  $sw.Restart(); Run @('filecopy', $n, "$B\x1.bin") | Out-Null; Run @('filecopy', $n, "$B\x2.bin") | Out-Null; $seq = $sw.Elapsed.TotalMilliseconds
  Log "2-copias-256MiB-em-SEQUENCIA,$([math]::Round($seq)) ms,$(Mbps (2*$len) $seq) MiB/s no total"
  CleanDir $B
  $sw.Restart()
  $p1 = Start-Process $exe -ArgumentList @('filecopy', $n, "$B\y1.bin") -PassThru -WindowStyle Hidden
  $p2 = Start-Process $exe -ArgumentList @('filecopy', $n, "$B\y2.bin") -PassThru -WindowStyle Hidden
  $p1.WaitForExit(); $p2.WaitForExit(); $par = $sw.Elapsed.TotalMilliseconds
  Log "2-copias-256MiB-JUNTAS,$([math]::Round($par)) ms,$(Mbps (2*$len) $par) MiB/s no total"
  foreach ($d in $bc,$bd1,$bd2,$dstA,$dstB) { Remove-Item -LiteralPath $d -Recurse -Force -ErrorAction SilentlyContinue }
  CleanDir $B
}
