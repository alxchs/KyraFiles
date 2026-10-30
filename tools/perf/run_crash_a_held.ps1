param([string]$Mode = 'preview')   # preview | control
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\Send-GuardedInput.ps1"
$script:GuardedTimeoutSec = 600
$guid = [guid]::NewGuid().ToString()
$tempDir = Join-Path $env:TEMP "agy\$guid\desenv\utils\FilesApp\Files\crashA"
New-Item -ItemType Directory $tempDir -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmm'
$csv = "$PSScriptRoot\..\..\docs\agents\evidence\crash-reproducao\resultado-held-$Mode-$stamp.csv"
$res = @()
function Count1000 { param($since) @(Get-WinEvent -FilterHashtable @{LogName='Application';Id=1000,1026;StartTime=$since} -ErrorAction SilentlyContinue | ? { $_.Message -like '*Files.exe*' }).Count }
try {
  1..12 | % { Copy-Item -LiteralPath 'D:\DriveD\Downloads\Abertura de telas.mp4' (Join-Path $tempDir ("{0:00}a.mp4" -f $_)); Copy-Item -LiteralPath 'D:\DriveD\Downloads\devolver.mp4' (Join-Path $tempDir ("{0:00}b.mp4" -f $_)); Set-Content (Join-Path $tempDir ("{0:00}c.txt" -f $_)) 'x' }
  $t0 = Get-Date
  $before = @(Get-Process Files -ErrorAction SilentlyContinue).Id
  Start-Process files-dev.exe -ArgumentList "`"$tempDir`""
  $p = $null; 1..40 | % { if(-not $p){ Start-Sleep -Milliseconds 500; $p = Get-Process Files -ErrorAction SilentlyContinue | ? { $before -notcontains $_.Id -and $_.MainWindowHandle -ne 0 } | select -First 1 } }
  if (-not $p) { throw 'ABORT: Files de teste nao abriu' }
  $script:GuardedPid=$p.Id; $script:GuardedProcess=$p; $script:GuardedProcessName='Files'; $script:GuardedProcessPath=(Get-CimInstance Win32_Process -Filter "ProcessId=$($p.Id)").ExecutablePath; $script:GuardedStartTime.Restart()
  Start-Sleep 6
  $UP=[byte]0x26; $DN=[byte]0x28; $ALT=[byte]0x12; $P=[byte]0x50
  # preview ligado/desligado pelo user_settings.json (app fechado), nunca por Alt+P (alterna)
  Add-Type -AssemblyName System.Windows.Forms,System.Drawing
  function Snap($n){ Assert-ForegroundOwned; $b=[Windows.Forms.SystemInformation]::VirtualScreen; $bmp=New-Object Drawing.Bitmap $b.Width,$b.Height; $g=[Drawing.Graphics]::FromImage($bmp); $g.CopyFromScreen($b.Location,[Drawing.Point]::Empty,$b.Size); $f=Join-Path $env:TEMP ('crashA_'+$n+'.png'); $bmp.Save($f); $g.Dispose(); $bmp.Dispose(); $f }
  Send-GuardedKey -VirtualKey $DN -WaitAfterMs 500
  Send-GuardedKeyHold -VirtualKey $DN -HoldMs 400; Start-Sleep 2; $snap = Snap $Mode; Write-Host SNAP=$snap
  $cfg = @(); foreach ($h in 1000,2000,3000) { $cfg += ,@('hold',$h) }; foreach ($g in 30,100,300) { $cfg += ,@('tap',$g) }
  foreach ($c in $cfg) { for ($r=1; $r -le 10; $r++) {
    $st='COMPLETED'
    try {
      if ($p.HasExited) { $st='PROCESS_EXITED' }
      elseif ($c[0] -eq 'hold') { Send-GuardedKeyHold -VirtualKey $UP -HoldMs $c[1]; Start-Sleep -Milliseconds 120; Send-GuardedKeyHold -VirtualKey $DN -HoldMs $c[1]; Start-Sleep -Milliseconds 120 }
      else { 1..6 | % { Send-GuardedKey -VirtualKey $UP -WaitAfterMs $c[1] }; 1..6 | % { Send-GuardedKey -VirtualKey $DN -WaitAfterMs $c[1] } }
    } catch { $st='ABORTED_GUARD: ' + $_.Exception.Message.Substring(0,[Math]::Min(80,$_.Exception.Message.Length)) }
    $res += [pscustomobject]@{Mode=$Mode;Type=$c[0];Param=$c[1];Round=$r;Status=$st;AliveAfter=(-not $p.HasExited);Time=(Get-Date).ToString('o')}
    if ($st -ne 'COMPLETED') { break }
  }; if ($res[-1].Status -ne 'COMPLETED') { break } }
  $ev = Count1000 $t0
  $res | % { $_ | Add-Member -NotePropertyName WinEvents1000_1026_total -NotePropertyValue $ev -PassThru } | Export-Csv $csv -NoTypeInformation -Encoding utf8
  "CSV: $csv ; eventos 1000/1026 do Files.exe desde $t0 : $ev ; rodadas=$($res.Count) completas=$(@($res|? Status -eq COMPLETED).Count)"
} finally {
  Get-Process Files -ErrorAction SilentlyContinue | ? { $before -notcontains $_.Id } | Stop-Process -Force -ErrorAction SilentlyContinue
  Remove-Item (Split-Path $tempDir -Parent | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent | Split-Path -Parent) -Recurse -Force -ErrorAction SilentlyContinue
}

