param([string]$OutDir = 'C:\desenv\utils\FilesApp-specs-backlog\docs\agents\evidence\F036')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSCommandPath
$guid = Get-Content "$root\.guid"
$exe = Get-ChildItem "$env:TEMP\Compiler" -Recurse -Filter CopySpike.exe | Where-Object FullName -match $guid | Select-Object -First 1 -ExpandProperty FullName
$S = "\\hd-roteador\sda1\temp\claude\$guid"; $B = "$S\b"
New-Item -ItemType Directory -Force -Path $B | Out-Null
$src = 'D:\DriveD\Downloads\PhotoVideoEdge20Pro.zip'      # origem existente, SOMENTE LEITURA
$out = Join-Path $OutDir 'results3-up.csv'
function Log($l) { Write-Host $l; Add-Content $out $l }
function Run($a) { ,@(& $exe @a 2>&1 | ForEach-Object { "$_" }) }
function Hash($p) { (Run @('hash', $p))[-1] }
function CleanB { Get-ChildItem -LiteralPath $B -File -Force -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Attributes = 'Normal' } catch {}; Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue } }
$len = (Get-Item -LiteralPath $src).Length
Log "teste,detalhe"
Log "origem(somente leitura),$src,$([math]::Round($len/1MB)) MiB"
$sw = [Diagnostics.Stopwatch]::StartNew(); $h = Hash $src; $sw.Stop()
Log "ler-origem-local-hash,$([math]::Round($sw.Elapsed.TotalMilliseconds)) ms"

foreach ($r in 1..2) { CleanB; $l = (Run @('cf2', $src, "$B\up.zip"))[-1]; Log "envio-cf2-direto-run$r,$l,hash_igual=$((Hash "$B\up.zip") -eq $h)" }
foreach ($r in 1..2) { CleanB; $l = (Run @('filecopy', $src, "$B\up.zip"))[-1]; Log "envio-filecopy-run$r,$l,hash_igual=$((Hash "$B\up.zip") -eq $h)" }
foreach ($r in 1..2) { CleanB; $l = (Run @('cf2b', $src, "$B\up.zip", '--pause-after-mib', '200', '--pause2-after-mib', '500', '--pause-for-ms', '2000'))[-1]; Log "envio-pausa-dupla-run$r,$l,hash_igual=$((Hash "$B\up.zip") -eq $h)" }
CleanB
$l = (Run @('cf2b', $src, "$B\up.zip", '--pause-after-mib', '200', '--cancel-after-pause'))[-1]; Log "envio-cancelar-na-pausa,$l,hash_igual=$((Hash "$B\up.zip") -eq $h)"
CleanB
# o plano da fase C: gravar com nome temporário e renomear no fim (também substituindo um arquivo existente)
$l = (Run @('cf2', $src, "$B\up.zip.filespart"))[-1]
[IO.File]::Move("$B\up.zip.filespart", "$B\up.zip")
Log "envio-temp-e-renomear,existe_final=$(Test-Path -LiteralPath "$B\up.zip"),existe_temp=$(Test-Path -LiteralPath "$B\up.zip.filespart"),hash_igual=$((Hash "$B\up.zip") -eq $h)"
$l = (Run @('cf2', $src, "$B\up.zip.filespart"))[-1]
try { [IO.File]::Move("$B\up.zip.filespart", "$B\up.zip", $true); Log "envio-renomear-substituindo,ok=True,hash_igual=$((Hash "$B\up.zip") -eq $h)" } catch { Log "envio-renomear-substituindo,ERRO=$($_.Exception.Message)" }
CleanB
Log "fim"
