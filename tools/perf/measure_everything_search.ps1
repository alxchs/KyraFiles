param([string]$Es = "$env:USERPROFILE\bin\es.exe", [string]$Folder = "$env:USERPROFILE\Downloads", [int]$Runs = 7)
# Times es.exe with the same arguments EverythingSearchProvider builds (F094 NFR-001). Hidden, no console.
function Measure-Es([string]$label, [string]$argsLine) {
	$times = @(); $count = 0; $first = @()
	for ($i = 0; $i -lt $Runs; $i++) {
		$psi = [Diagnostics.ProcessStartInfo]::new($Es, $argsLine)
		$psi.UseShellExecute = $false; $psi.CreateNoWindow = $true; $psi.RedirectStandardOutput = $true
		$psi.StandardOutputEncoding = [Text.Encoding]::UTF8
		$sw = [Diagnostics.Stopwatch]::StartNew()
		$p = [Diagnostics.Process]::Start($psi)
		$line = $p.StandardOutput.ReadLine(); $first += $sw.Elapsed.TotalMilliseconds
		$n = if ($null -ne $line) { 1 } else { 0 }
		while ($null -ne $p.StandardOutput.ReadLine()) { $n++ }
		$p.WaitForExit(); $sw.Stop(); $times += $sw.Elapsed.TotalMilliseconds; $count = $n
	}
	$s = $times | Sort-Object; $f = $first | Sort-Object
	"{0,-28} itens={1,4}  primeiro(mediana)={2,6:N1} ms  total(mediana)={3,6:N1} ms  total(min/max)={4:N1}/{5:N1}" -f $label, $count, $f[[int]($Runs/2)], $s[[int]($Runs/2)], $s[0], $s[-1]
}
$base = '-tsv -no-header -attrib -size -dm -n 500'
Measure-Es 'antes (pasta Downloads)' "$base -path `"$Folder`" *.exe"
Measure-Es 'depois (indice todo)' "$base *.exe"
Measure-Es 'depois (pasta digitada)' "$base -path `"$Folder`" *.exe"
