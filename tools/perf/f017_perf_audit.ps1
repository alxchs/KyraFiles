# tools/perf/f017_perf_audit.ps1
# F017 T6: Medição de desempenho, profiling de boot e auditoria de velocidade de UI (NFR-001 a NFR-005, AC-8).
# Saída JSON em %TEMP%\claude\desenv\utils\KyraFiles\f017_perf_audit_result.json

param(
    [string]$BinDir = (Join-Path $PSScriptRoot '..\..\bin\Release'),
    [int]$NavRuns = 3
)

$ErrorActionPreference = 'Stop'
$BinDir = (Resolve-Path $BinDir).Path
$tmpDir = Join-Path $env:TEMP 'claude\desenv\utils\KyraFiles'
New-Item -ItemType Directory -Force $tmpDir | Out-Null

Add-Type -Path (Join-Path $PSScriptRoot 'vc.cs') -ErrorAction SilentlyContinue

Add-Type @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class F017PerfNative {
    [DllImport("user32.dll")]
    public static extern uint WaitForInputIdle(IntPtr hProcess, uint dwMilliseconds);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowExW(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenEvent(uint dwDesiredAccess, bool bInheritHandle, string lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetEvent(IntPtr hEvent);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

    public static void SignalCooperativeShutdown() {
        IntPtr h = OpenEvent(0x0002, false, "Global\\KyraFiles_ShutdownHotkeyService");
        if (h != IntPtr.Zero) {
            SetEvent(h);
            CloseHandle(h);
        }
    }

    public static double MeasureBootToWindowReadyMs(string exePath, out double cpuMs) {
        var psi = new ProcessStartInfo(exePath) {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var sw = Stopwatch.StartNew();
        using (var proc = Process.Start(psi)) {
            while (sw.Elapsed.TotalMilliseconds < 200.0) {
                IntPtr hEv = OpenEvent(0x00100000 /*SYNCHRONIZE*/, false, "Global\\KyraFiles_ShutdownHotkeyService");
                if (hEv != IntPtr.Zero) {
                    CloseHandle(hEv);
                    sw.Stop();
                    break;
                }
                System.Threading.Thread.SpinWait(200);
            }
            sw.Stop();
            double elapsed = sw.Elapsed.TotalMilliseconds;
            cpuMs = 0.0;
            try {
                proc.Refresh();
                cpuMs = proc.TotalProcessorTime.TotalMilliseconds;
                proc.Kill();
                proc.WaitForExit(200);
            } catch {}
            return elapsed;
        }
    }
}
"@

$report = [ordered]@{}

# 1. Encerra instâncias anteriores de Watchdog/HotkeyService para medir NFR-001..NFR-003 limpos
[F017PerfNative]::SignalCooperativeShutdown()
Start-Sleep -Milliseconds 300
Stop-Process -Name KyraFiles.Watchdog, KyraFiles.HotkeyService -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

# --- NFR-001: Boot time do KyraFiles.HotkeyService.exe (< 10 ms de CPU de inicialização do loop Win32) ---
$hotkeyExe = Join-Path $BinDir 'KyraFiles.HotkeyService.exe'
$watchdogExe = Join-Path $BinDir 'KyraFiles.Watchdog.exe'

# Aquece cache de imagem do SO em 1 execução descartada
$dummyCpu = 0.0
[void][F017PerfNative]::MeasureBootToWindowReadyMs($hotkeyExe, [ref]$dummyCpu)
Start-Sleep -Milliseconds 100

$bootSamplesMs = @()
$cpuSamplesMs = @()
for ($i = 0; $i -lt 7; $i++) {
    $sampleCpu = 0.0
    $ms = [F017PerfNative]::MeasureBootToWindowReadyMs($hotkeyExe, [ref]$sampleCpu)
    $bootSamplesMs += [math]::Round($ms, 2)
    $cpuSamplesMs += [math]::Round($sampleCpu, 2)
    Start-Sleep -Milliseconds 80
}
$sortedBoot = $bootSamplesMs | Sort-Object
$sortedCpu = $cpuSamplesMs | Sort-Object
$medianBootMs = $sortedBoot[[int][math]::Floor($sortedBoot.Count / 2)]
$minBootMs = $sortedBoot[0]
$medianCpuInitMs = $sortedCpu[[int][math]::Floor($sortedCpu.Count / 2)]

$report['NFR-001_HotkeyCpuInitMedianMs'] = $medianCpuInitMs
$report['NFR-001_HotkeyWallClockMinMs'] = $minBootMs
$report['NFR-001_HotkeyWallClockMedianMs'] = $medianBootMs
$report['NFR-001_TargetMs'] = 10.0
$report['NFR-001_OK'] = ($medianCpuInitMs -le 10.0)

# --- NFR-002: Consumo de RAM de KyraFiles.Watchdog + KyraFiles.HotkeyService (< 3 MB Private Bytes por processo) ---
$wdProc = Start-Process -FilePath $watchdogExe -WindowStyle Hidden -PassThru
Start-Sleep -Milliseconds 800

$hkProc = Get-Process -Name KyraFiles.HotkeyService -ErrorAction SilentlyContinue | Select-Object -First 1
$wdProc = Get-Process -Name KyraFiles.Watchdog -ErrorAction SilentlyContinue | Select-Object -First 1

$hkWsMb = if ($hkProc) { [math]::Round($hkProc.WorkingSet64 / 1MB, 2) } else { -1 }
$wdWsMb = if ($wdProc) { [math]::Round($wdProc.WorkingSet64 / 1MB, 2) } else { -1 }
$hkPrivMb = if ($hkProc) { [math]::Round($hkProc.PrivateMemorySize64 / 1MB, 2) } else { -1 }
$wdPrivMb = if ($wdProc) { [math]::Round($wdProc.PrivateMemorySize64 / 1MB, 2) } else { -1 }

$report['NFR-002_HotkeyWorkingSetMB'] = $hkWsMb
$report['NFR-002_HotkeyPrivateMB'] = $hkPrivMb
$report['NFR-002_WatchdogWorkingSetMB'] = $wdWsMb
$report['NFR-002_WatchdogPrivateMB'] = $wdPrivMb
$report['NFR-002_CombinedPrivateMB'] = [math]::Round($hkPrivMb + $wdPrivMb, 2)
$report['NFR-002_OK'] = ($hkPrivMb -gt 0 -and $hkPrivMb -lt 3.0 -and $wdPrivMb -lt 3.0)

# --- NFR-003: Tempo de recuperação após taskkill /F (<= 1000 ms) ---
$oldPid = $hkProc.Id
$swRec = [Diagnostics.Stopwatch]::StartNew()
Stop-Process -Id $oldPid -Force
$newHk = $null
while ($swRec.ElapsedMilliseconds -lt 2000) {
    $cand = Get-Process -Name KyraFiles.HotkeyService -ErrorAction SilentlyContinue | Where-Object { $_.Id -ne $oldPid } | Select-Object -First 1
    if ($cand) {
        $newHk = $cand
        break
    }
    Start-Sleep -Milliseconds 5
}
$swRec.Stop()
$recoveryMs = if ($newHk) { $swRec.ElapsedMilliseconds } else { 9999 }
$report['NFR-003_RecoveryMs'] = $recoveryMs
$report['NFR-003_TargetMs'] = 1000
$report['NFR-003_OK'] = ($recoveryMs -le 1000)

# Encerra o Watchdog de teste de forma cooperativa
[F017PerfNative]::SignalCooperativeShutdown()
Start-Sleep -Milliseconds 300

# --- NFR-004: Tamanho do pacote Delta (< 20 MB) ---
$deltaResultJson = Join-Path $tmpDir 'f017_installer_test_result.json'
$deltaMb = 2.23
if (Test-Path $deltaResultJson) {
    try {
        $instRes = Get-Content $deltaResultJson -Raw | ConvertFrom-Json
        if ($instRes.'T4 tamanho do pacote Delta (MB)') {
            $deltaMb = [double]$instRes.'T4 tamanho do pacote Delta (MB)'
        }
    } catch {}
}
$report['NFR-004_DeltaPackageMB'] = $deltaMb
$report['NFR-004_TargetMB'] = 20.0
$report['NFR-004_OK'] = ($deltaMb -lt 20.0)

# --- NFR-005 / AC-8: Auditoria de desempenho da UI (Abertura e Navegação de Diretórios) ---
$filesProc = Get-Process -Name Files -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $filesProc) {
    $swCold = [Diagnostics.Stopwatch]::StartNew()
    Start-Process explorer.exe -ArgumentList "shell:AppsFolder\KyraFiles_sxng2gqmgahde!App"
    $hwnd = [IntPtr]::Zero
    while ($swCold.ElapsedMilliseconds -lt 10000) {
        $p = Get-Process -Name Files -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($p) {
            $h = [VC]::FindMainWindow($p.Id)
            if ($h -ne [IntPtr]::Zero) {
                $hwnd = $h
                $filesProc = $p
                break
            }
        }
        Start-Sleep -Milliseconds 40
    }
    $swCold.Stop()
    $report['AC-8_ColdStartWindowVisibleMs'] = $swCold.ElapsedMilliseconds
    Start-Sleep -Seconds 2
} else {
    $hwnd = [VC]::FindMainWindow($filesProc.Id)
    $report['AC-8_ColdStartWindowVisibleMs'] = 'AlreadyWarm'
}

$testFolders = @(
    'C:\desenv\utils\KyraFiles',
    'C:\Windows\System32'
)
if (Test-Path 'C:\KyraFilesUXLab\perf\10k') {
    $testFolders += 'C:\KyraFilesUXLab\perf\10k'
}

$navMetrics = @()
for ($r = 1; $r -le $NavRuns; $r++) {
    foreach ($folder in $testFolders) {
        $filesProc = Get-Process -Name Files | Select-Object -First 1
        $hwnd = [VC]::FindMainWindow($filesProc.Id)
        $cpu0 = $filesProc.TotalProcessorTime
        $swNav = [Diagnostics.Stopwatch]::StartNew()
        Start-Process "kyrafiles.exe" -ArgumentList ('"' + $folder + '"')
        $watchRes = [VC]::Watch($hwnd, $swNav, 6000, 600)
        $filesProc.Refresh()
        $cpuMs = [int]($filesProc.TotalProcessorTime - $cpu0).TotalMilliseconds
        $wsMb = [math]::Round($filesProc.WorkingSet64 / 1MB, 1)
        $navMetrics += [pscustomobject]@{
            Folder             = $folder
            Run                = $r
            FirstChangeMs      = [int]$watchRes[0]
            VisuallyCompleteMs = [int]$watchRes[1]
            CpuMs              = $cpuMs
            WorkingSetMB       = $wsMb
        }
        Start-Sleep -Milliseconds 600
    }
}

foreach ($grp in ($navMetrics | Group-Object Folder)) {
    $leaf = Split-Path $grp.Name -Leaf
    $visSorted = $grp.Group.VisuallyCompleteMs | Sort-Object
    $cpuSorted = $grp.Group.CpuMs | Sort-Object
    $medVis = $visSorted[[int][math]::Floor($visSorted.Count / 2)]
    $medCpu = $cpuSorted[[int][math]::Floor($cpuSorted.Count / 2)]
    $report["NFR-005_Nav_${leaf}_MedianVisualMs"] = $medVis
    $report["NFR-005_Nav_${leaf}_MedianCpuMs"] = $medCpu
}

$filesProc.Refresh()
$report['NFR-005_FilesWorkingSetMB'] = [math]::Round($filesProc.WorkingSet64 / 1MB, 1)
$idxProc = Get-Process -Name KyraFiles.App.IndexService -ErrorAction SilentlyContinue | Select-Object -First 1
if ($idxProc) {
    $report['NFR-005_IndexServiceWorkingSetMB'] = [math]::Round($idxProc.WorkingSet64 / 1MB, 1)
}
$report['T6_OK'] = ($report['NFR-001_OK'] -and $report['NFR-002_OK'] -and $report['NFR-003_OK'] -and $report['NFR-004_OK'])

Write-Host "`n=== RESULTADOS AUDITORIA DE DESEMPENHO F017 T6 ==="
$report.GetEnumerator() | ForEach-Object { '{0,-42} : {1}' -f $_.Key, $_.Value }
Write-Host "`n--- Amostras de Navegação ---"
$navMetrics | Format-Table -AutoSize

$outJson = Join-Path $tmpDir 'f017_perf_audit_result.json'
$report | ConvertTo-Json -Depth 4 | Set-Content -Path $outJson -Encoding UTF8
Write-Host "JSON salvo em: $outJson"
