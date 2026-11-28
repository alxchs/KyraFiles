# tools/perf/f054_preview_baseline.ps1
# F054 T0/T5: time to first preview and memory of the preview pane, measured from outside via UI Automation.
# Never activates the window: it starts minimized and is shown with SW_SHOWNOACTIVATE; no keys or mouse.

[CmdletBinding()]
param(
	[string]$AppPath = (Join-Path $env:LOCALAPPDATA 'Programs\KyraFiles\KyraFiles.exe'),
	[int]$Rounds = 3,
	[int]$TimeoutMs = 15000,
	[string[]]$Kinds = @('cs', 'json', 'csv', 'log'),
	[int[]]$SizesMB = @(1, 200),
	[string]$OutCsv
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -Namespace F054 -Name Win -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ShowWindow(System.IntPtr hWnd, int nCmdShow);
'@

$guid = [guid]::NewGuid().ToString('N')
$work = Join-Path $env:TEMP "claude\$guid\desenv\utils\KyraFiles\KyraFiles\F054"
New-Item -ItemType Directory -Force $work | Out-Null

function New-SampleFile([string]$kind, [int]$sizeMB) {
	$path = Join-Path $work "$kind-$($sizeMB)mb.$kind"
	$target = [long]$sizeMB * 1MB
	$writer = [IO.StreamWriter]::new($path, $false, [Text.UTF8Encoding]::new($false), 1MB)
	try {
		$writer.WriteLine("F054-MARK-$kind-$sizeMB")
		$i = 0
		while ($writer.BaseStream.Length -lt $target) {
			$line = switch ($kind) {
				'cs' { "public sealed class Sample$i { public int Value { get; set; } = $i; // linha $i" }
				'json' { "{""id"": $i, ""name"": ""item $i"", ""ok"": true, ""score"": $($i % 97).5}," }
				'csv' { "$i,item $i,$($i % 97).5,2026-11-27,ok" }
				default { "2026-11-27 10:00:$('{0:D2}' -f ($i % 60)) INFO linha $i do log de teste" }
			}
			$writer.WriteLine($line)
			$i++
			if ($i % 4096 -eq 0) { $writer.Flush() }
		}
	}
	finally { $writer.Dispose() }
	return $path
}

$ua = [System.Windows.Automation.AutomationElement]
$scope = [System.Windows.Automation.TreeScope]

function Find-ByName($root, [string]$prefix) {
	$cond = [System.Windows.Automation.PropertyCondition]::new($ua::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)
	foreach ($el in $root.FindAll($scope::Descendants, $cond)) {
		if ($el.Current.Name -like "$prefix*") { return $el }
	}
	return $null
}

function Get-PreviewText($root) {
	$cond = [System.Windows.Automation.PropertyCondition]::new($ua::ClassNameProperty, 'RichTextBlock')
	foreach ($el in $root.FindAll($scope::Descendants, $cond)) {
		$pattern = $null
		if ($el.TryGetCurrentPattern([System.Windows.Automation.TextPattern]::Pattern, [ref]$pattern)) {
			$text = $pattern.DocumentRange.GetText(64)
			if ($text) { return $text }
		}
	}
	return ''
}

$files = foreach ($k in $Kinds) { foreach ($s in $SizesMB) { [pscustomobject]@{ Kind = $k; SizeMB = $s; Path = (New-SampleFile $k $s) } } }
Write-Host "Arquivos de teste em $work"

$proc = $null
$results = [Collections.Generic.List[object]]::new()
try {
	if (Get-Process -Name KyraFiles -ErrorAction SilentlyContinue) { throw 'KyraFiles ja esta aberto; feche para medir uma instancia propria.' }
	$proc = Start-Process -FilePath $AppPath -ArgumentList "`"$work`"" -WindowStyle Minimized -PassThru
	$sw = [Diagnostics.Stopwatch]::StartNew()
	while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and $sw.ElapsedMilliseconds -lt 30000) { Start-Sleep -Milliseconds 200; $proc.Refresh() }
	if ($proc.MainWindowHandle -eq [IntPtr]::Zero) { throw 'Janela principal nao apareceu em 30 s.' }
	[F054.Win]::ShowWindow($proc.MainWindowHandle, 4) | Out-Null # SW_SHOWNOACTIVATE
	$root = $ua::FromHandle($proc.MainWindowHandle)

	$sw.Restart()
	while (-not (Find-ByName $root "$($Kinds[0])-") -and $sw.ElapsedMilliseconds -lt 30000) { Start-Sleep -Milliseconds 300 }

	$btn = $root.FindFirst($scope::Descendants, [System.Windows.Automation.PropertyCondition]::new($ua::AutomationIdProperty, 'PreviewPane'))
	if (-not $btn) { throw 'Botao PreviewPane nao encontrado.' }
	$toggle = $btn.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
	if ($toggle.Current.ToggleState -ne 'On') { $toggle.Toggle() }
	Start-Sleep -Milliseconds 800

	# Cost of one preview poll, so the reader knows the resolution of the timings.
	$probe = [Diagnostics.Stopwatch]::StartNew(); [void](Get-PreviewText $root); $probe.Stop()
	$pollMs = [math]::Round($probe.Elapsed.TotalMilliseconds, 1)

	for ($round = 1; $round -le $Rounds; $round++) {
		foreach ($f in $files) {
			$item = Find-ByName $root "$($f.Kind)-$($f.SizeMB)mb"
			if (-not $item) { throw "Item $($f.Kind)-$($f.SizeMB)mb nao encontrado." }
			$marker = "F054-MARK-$($f.Kind)-$($f.SizeMB)"
			[GC]::Collect()
			$proc.Refresh(); $before = $proc.PrivateMemorySize64; $peak = $before

			$sel = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
			$sw.Restart()
			$sel.Select()
			$shown = $false
			while ($sw.ElapsedMilliseconds -lt $TimeoutMs) {
				$proc.Refresh(); if ($proc.PrivateMemorySize64 -gt $peak) { $peak = $proc.PrivateMemorySize64 }
				if ((Get-PreviewText $root).Contains($marker)) { $shown = $true; break }
			}
			$ms = $sw.ElapsedMilliseconds
			Start-Sleep -Milliseconds 1500
			$proc.Refresh(); if ($proc.PrivateMemorySize64 -gt $peak) { $peak = $proc.PrivateMemorySize64 }
			$after = $proc.PrivateMemorySize64

			$results.Add([pscustomobject]@{
				Round = $round; Kind = $f.Kind; SizeMB = $f.SizeMB
				Shown = $shown; FirstPreviewMs = $(if ($shown) { $ms } else { $null })
				PrivateBeforeMB = [math]::Round($before / 1MB, 1); PrivatePeakMB = [math]::Round($peak / 1MB, 1); PrivateAfterMB = [math]::Round($after / 1MB, 1)
				PollMs = $pollMs
			})
		}
	}
}
finally {
	if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id -Force; $proc.WaitForExit(5000) | Out-Null }
	Remove-Item -Recurse -Force (Join-Path $env:TEMP "claude\$guid") -ErrorAction SilentlyContinue
}

$results | Format-Table -AutoSize | Out-String -Width 200 | Write-Host
if ($OutCsv) {
	$results | Export-Csv -Path $OutCsv -NoTypeInformation -Encoding utf8NoBOM
	Write-Host "CSV: $OutCsv"
}
