param([string]$Phase = 'disk')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSCommandPath
$guid = Get-Content "$root\.guid"
$exe = Get-ChildItem "$env:TEMP\Compiler" -Recurse -Filter CopySpike.exe | Where-Object FullName -match $guid | Select-Object -First 1 -ExpandProperty FullName
$src = "$root\src"; $dst = "$root\dst"
$d = "D:\Temp\claude\$guid\desenv\utils\KyraFiles\Files\spike2"
New-Item -ItemType Directory -Force $src,$dst,$d | Out-Null
$out = "$root\results2-$Phase.csv"
function Log($l) { Write-Host $l; Add-Content $out $l }
function Run($a) { ,@(& $exe @a 2>&1 | ForEach-Object { "$_" }) }
function Hash($p) { (Run @('hash', $p))[-1] }
function Clean($dir) { Get-ChildItem $dir -File -Force -ErrorAction SilentlyContinue | ForEach-Object { $_.Attributes = 'Normal'; Remove-Item $_.FullName -Force } }
function ShellCopy($file, $destDir, $expectLen) {
  $sh = New-Object -ComObject Shell.Application
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $sh.Namespace($destDir).CopyHere($file, 4 + 16)
  $name = Split-Path $file -Leaf; $target = Join-Path $destDir $name
  while ($sw.Elapsed.TotalSeconds -lt 180) {
    Start-Sleep -Milliseconds 300
    if ((Test-Path -LiteralPath $target) -and ((Get-Item -LiteralPath $target -Force).Length -eq $expectLen)) { Start-Sleep -Milliseconds 700; break }
  }
  $sw.Stop(); return $sw.Elapsed.TotalMilliseconds
}

if ($Phase -eq 'disk') {
  if (-not (Test-Path "$src\a.bin")) { Run @('gen', "$src\a.bin", '1024') | Out-Null }
  $h = Hash "$src\a.bin"; $len = (Get-Item "$src\a.bin").Length
  Log "teste,detalhe"
  foreach ($r in 1..2) {
    foreach ($m in 'filecopy','cf2','shell','chunk4') {
      Clean $d
      switch ($m) {
        'filecopy' { $l = (Run @('filecopy', "$src\a.bin", "$d\a.bin"))[-1] }
        'cf2'      { $l = (Run @('cf2', "$src\a.bin", "$d\a.bin"))[-1] }
        'chunk4'   { $l = (Run @('chunk', "$src\a.bin", "$d\a.bin", '--chunk', '4'))[-1] }
        'shell'    { $ms = ShellCopy "$src\a.bin" $d $len; $l = "shell,-,$len,$([math]::Round($ms)),$([math]::Round(1024/($ms/1000)))" }
      }
      Log "C-para-D-run$r-$m,$l,hash_igual=$((Hash "$d\a.bin") -eq $h)"
    }
  }
  foreach ($r in 1..3) {
    Clean $d
    $l = (Run @('cf2b', "$src\a.bin", "$d\a.bin", '--pause-after-mib', '300', '--pause2-after-mib', '700', '--pause-for-ms', '2000'))[-1]
    Log "C-para-D-pausa-dupla-run$r,$l,hash_igual=$((Hash "$d\a.bin") -eq $h)"
  }
  # sentido contrário: D para C
  Clean $d; Copy-Item "$src\a.bin" "$d\a.bin"
  foreach ($r in 1..2) {
    Clean $dst
    $l = (Run @('cf2b', "$d\a.bin", "$dst\a.bin", '--pause-after-mib', '300', '--pause-for-ms', '2000'))[-1]
    Log "D-para-C-pausa-run$r,$l,hash_igual=$((Hash "$dst\a.bin") -eq $h)"
  }
  Clean $d; Clean $dst
}

if ($Phase -eq 'cancel') {
  if (-not (Test-Path "$src\a.bin")) { Run @('gen', "$src\a.bin", '1024') | Out-Null }
  $h = Hash "$src\a.bin"
  Log "teste,detalhe"
  foreach ($t in @(@{n='C-para-C';dir=$dst}, @{n='C-para-D';dir=$d})) {
    Clean $t.dir
    $l = (Run @('cf2b', "$src\a.bin", "$($t.dir)\a.bin", '--pause-after-mib', '300', '--cancel-after-pause'))[-1]
    Log "cancelar-na-pausa-$($t.n),$l,hash_igual=$((Hash "$($t.dir)\a.bin") -eq $h)"
    Clean $t.dir
  }
}

if ($Phase -eq 'attrs') {
  Log "teste,detalhe"
  Run @('mksparse', "$src\sp.bin", '64') | Out-Null
  Log "origem-esparso: $((Run @('size', "$src\sp.bin"))[-1])"
  $z = New-Object byte[] (16MB); [IO.File]::WriteAllBytes("$src\cp.bin", $z); compact /c "$src\cp.bin" | Out-Null
  Log "origem-compactado: $((Run @('size', "$src\cp.bin"))[-1])"
  foreach ($f in 'sp.bin','cp.bin') {
    $len = (Get-Item "$src\$f").Length
    foreach ($m in 'filecopy','cf2','shell','chunk4') {
      Clean $dst
      switch ($m) {
        'filecopy' { Run @('filecopy', "$src\$f", "$dst\$f") | Out-Null }
        'cf2'      { Run @('cf2', "$src\$f", "$dst\$f") | Out-Null }
        'chunk4'   { Run @('chunk', "$src\$f", "$dst\$f", '--chunk', '4') | Out-Null }
        'shell'    { ShellCopy "$src\$f" $dst $len | Out-Null }
      }
      Log "$f-$m`: $((Run @('size', "$dst\$f"))[-1])"
    }
  }
  Clean $dst
}

if ($Phase -eq 'long') {
  Log "teste,detalhe"
  Run @('gen', "$src\l.bin", '8') | Out-Null
  $h = Hash "$src\l.bin"
  $sub = (1..40 | ForEach-Object { 'pasta_longa_{0:D2}' -f $_ }) -join '\'
  $longDir = "\\?\$d\$sub"
  New-Item -ItemType Directory -Force -Path $longDir | Out-Null
  Log "comprimento_do_caminho_do_destino: $("$d\$sub\l.bin".Length)"
  foreach ($m in 'filecopy','cf2','chunk4','shell') {
    Get-ChildItem -LiteralPath $longDir -File -Force -ErrorAction SilentlyContinue | Remove-Item -Force
    try {
      switch ($m) {
        'filecopy' { $r = (Run @('filecopy', "$src\l.bin", "$longDir\l.bin")) }
        'cf2'      { $r = (Run @('cf2', "$src\l.bin", "$longDir\l.bin")) }
        'chunk4'   { $r = (Run @('chunk', "$src\l.bin", "$longDir\l.bin", '--chunk', '4')) }
        'shell'    { $sh = New-Object -ComObject Shell.Application; $sh.Namespace("$d\$sub").CopyHere("$src\l.bin", 4 + 16); Start-Sleep 4; $r = @('shell') }
      }
      $ok = Test-Path -LiteralPath "$longDir\l.bin"
      $hm = if ($ok) { (Hash "$longDir\l.bin") -eq $h } else { 'n/a' }
      Log "caminho-longo-$m`: existe=$ok,hash_igual=$hm,saida=$((($r | Select-Object -Last 1) -replace ',',';').Substring(0,[Math]::Min(120,(($r | Select-Object -Last 1)).Length)))"
    } catch { Log "caminho-longo-$m`: EXCECAO $($_.Exception.Message)" }
  }
  Remove-Item -LiteralPath "\\?\$d\pasta_longa_01" -Recurse -Force -ErrorAction SilentlyContinue
}

if ($Phase -eq 'smb') {
  Log "teste,detalhe"
  $unc = '\\localhost\desenv\utils\KyraFiles\artifacts\Setup\KyraFilesSetup-4.3.6.exe'
  $local = 'C:\desenv\utils\KyraFiles\artifacts\Setup\KyraFilesSetup-4.3.6.exe'
  Log "origem-existe-por-smb=$(Test-Path $unc)"
  $h = Hash $local
  foreach ($r in 1..2) {
    Clean $dst
    $l = (Run @('cf2', $unc, "$dst\s.exe"))[-1]
    Log "smb-leitura-cf2-run$r,$l,hash_igual=$((Hash "$dst\s.exe") -eq $h)"
    Clean $dst
    $l = (Run @('filecopy', $unc, "$dst\s.exe"))[-1]
    Log "smb-leitura-filecopy-run$r,$l,hash_igual=$((Hash "$dst\s.exe") -eq $h)"
  }
  foreach ($r in 1..2) {
    Clean $dst
    $l = (Run @('cf2b', $unc, "$dst\s.exe", '--pause-after-mib', '10', '--pause-for-ms', '1500'))[-1]
    Log "smb-pausa-run$r,$l,hash_igual=$((Hash "$dst\s.exe") -eq $h)"
  }
  Clean $dst
}

if ($Phase -eq 'var') {
  if (-not (Test-Path "$src\a.bin")) { Run @('gen', "$src\a.bin", '1024') | Out-Null }
  $h = Hash "$src\a.bin"
  Log "teste,detalhe"
  foreach ($r in 1..6) {
    Clean $dst
    $l = (Run @('cf2b', "$src\a.bin", "$dst\a.bin", '--pause-after-mib', '300', '--pause-for-ms', '1000'))[-1]
    Log "retomada-C-para-C-run$r,$l,hash_igual=$((Hash "$dst\a.bin") -eq $h)"
  }
  Clean $dst
}
