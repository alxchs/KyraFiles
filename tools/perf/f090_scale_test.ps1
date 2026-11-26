# tools/perf/f090_scale_test.ps1
# Prova automatizada não-intrusiva da F090 (Escala da Interface):
# - Sem entrada global crua e sem roubar foco (usa user_settings.json + PrintWindow + UIA somente-leitura)
# - Captura a janela principal em 80%, 100%, 125% e 150%
# - Compara CPU em repouso e Working Set (NFR-1) e gera docs/agents/evidence/f090/resultado.csv
# - Restaura user_settings.json original ao final

[CmdletBinding()]
param(
    [int[]]$Scales = @(80, 100, 125, 150),
    [int]$WaitSeconds = 6,
    [string]$EvidenceDir = ""
)

$ErrorActionPreference = 'Stop'

# Nao fecha instancia do usuario em uso
$runningBefore = @(Get-Process -Name "KyraFiles" -ErrorAction SilentlyContinue)
if ($runningBefore.Count -gt 0) {
    Write-Error "Abortado: ja existe instancia do KyraFiles aberta (PID: $($runningBefore.Id -join ', ')). Feche-a antes de rodar este teste."
    exit 1
}

$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
if (-not $EvidenceDir) {
    $EvidenceDir = Join-Path $repoRoot "docs\agents\evidence\f090"
}
New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null

$guid = [guid]::NewGuid().ToString()
$tempDir = Join-Path $env:TEMP "agy\$guid\desenv\utils\KyraFiles\KyraFiles"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

# Descobre o pacote registrado e o user_settings.json
$pkg = Get-AppxPackage -Name "KyraFiles" -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $pkg) {
    Write-Error "Pacote KyraFiles nao esta registrado. Execute .\tools\Quick-Ship.ps1 -Fast antes."
    exit 1
}

[xml]$manifest = Get-Content (Join-Path $pkg.InstallLocation "AppxManifest.xml") -Raw
$appId = $manifest.Package.Applications.Application.Id
$aumid = "shell:AppsFolder\$($pkg.PackageFamilyName)!$appId"

$settingsPath = Join-Path $env:LOCALAPPDATA "Packages\$($pkg.PackageFamilyName)\LocalState\settings\user_settings.json"
if (-not (Test-Path -LiteralPath $settingsPath)) {
    Write-Error "Arquivo de configuracoes nao encontrado em: $settingsPath"
    exit 1
}

$backupPath = Join-Path $tempDir "user_settings.backup.json"
Copy-Item -LiteralPath $settingsPath -Destination $backupPath -Force

# Helper Win32 PrintWindow (somente leitura / sem troca de foco)
Add-Type -AssemblyName System.Drawing, UIAutomationClient, UIAutomationTypes
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

public static class F090CaptureHelper {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    public static bool SaveWindowCapture(IntPtr hwnd, string outputPath, out int width, out int height) {
        width = 0;
        height = 0;
        if (hwnd == IntPtr.Zero) return false;
        RECT r;
        if (!GetWindowRect(hwnd, out r)) return false;
        width = r.Right - r.Left;
        height = r.Bottom - r.Top;
        if (width <= 0 || height <= 0) return false;

        using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp)) {
            IntPtr hdc = g.GetHdc();
            try {
                // PW_RENDERFULLCONTENT = 2
                PrintWindow(hwnd, hdc, 2);
            } finally {
                g.ReleaseHdc(hdc);
            }
            bmp.Save(outputPath, ImageFormat.Png);
        }
        return true;
    }
}
"@ -ReferencedAssemblies System.Drawing

function Set-UiScaleSetting([int]$percent) {
    $raw = Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8
    $json = $raw | ConvertFrom-Json
    if ($null -eq $json.PSObject.Properties["AppUiScalePercent"]) {
        $json | Add-Member -NotePropertyName "AppUiScalePercent" -NotePropertyValue $percent
    } else {
        $json.AppUiScalePercent = $percent
    }
    $outJson = $json | ConvertTo-Json -Depth 20
    [System.IO.File]::WriteAllText($settingsPath, $outJson, [System.Text.UTF8Encoding]::new($false))
}

function Stop-KyraFilesTestProcesses {
    Get-Process -Name "KyraFiles", "KyraFiles.App.Server", "KyraFiles.App.IndexService" -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

try {
    foreach ($scale in $Scales) {
        Stop-KyraFilesTestProcesses
        Start-Sleep -Milliseconds 600
        Set-UiScaleSetting -percent $scale

        $swLaunch = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Process explorer.exe -ArgumentList $aumid | Out-Null

        $proc = $null
        $hwnd = [IntPtr]::Zero
        $deadline = (Get-Date).AddSeconds(20)
        while ((Get-Date) -lt $deadline) {
            $proc = Get-Process -Name "KyraFiles" -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
            if ($proc) {
                $hwnd = $proc.MainWindowHandle
                break
            }
            Start-Sleep -Milliseconds 200
        }
        $swLaunch.Stop()

        if (-not $proc -or $hwnd -eq [IntPtr]::Zero) {
            $results.Add([PSCustomObject]@{
                ScalePercent = $scale
                WindowWidth = 0
                WindowHeight = 0
                RatioW = 0
                RatioH = 0
                LaunchMs = [int]$swLaunch.ElapsedMilliseconds
                IdleCpuPercent = -1
                WorkingSetMB = -1
                IndexServiceActive = $false
                CaptureFile = ""
                Status = "FAIL_NO_WINDOW"
            })
            continue
        }

        Start-Sleep -Seconds $WaitSeconds

        $cpuStart = $proc.TotalProcessorTime.TotalSeconds
        $swIdle = [System.Diagnostics.Stopwatch]::StartNew()
        Start-Sleep -Seconds 3
        $swIdle.Stop()
        $proc.Refresh()
        $cpuEnd = $proc.TotalProcessorTime.TotalSeconds
        $idleCpu = [math]::Round((([math]::Max(0, $cpuEnd - $cpuStart)) / ($swIdle.Elapsed.TotalSeconds * [Environment]::ProcessorCount)) * 100, 2)
        $wsMb = [math]::Round($proc.WorkingSet64 / 1MB, 2)
        $indexActive = @(Get-Process -Name "KyraFiles.App.IndexService" -ErrorAction SilentlyContinue).Count -gt 0

        $pngPath = Join-Path $EvidenceDir "scale_$scale.png"
        [int]$w = 0
        [int]$h = 0
        $captured = [F090CaptureHelper]::SaveWindowCapture($hwnd, $pngPath, [ref]$w, [ref]$h)

        $ratioW = 0.0
        $ratioH = 0.0
        try {
            $uiaRoot = [System.Windows.Automation.AutomationElement]::RootElement
            $uiaCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
            $uiaWin = $uiaRoot.FindFirst([System.Windows.Automation.TreeScope]::Children, $uiaCond)
            if ($uiaWin) {
                $wr = $uiaWin.Current.BoundingRectangle
                if ($wr.Width -gt 0 -and $wr.Height -gt 0) {
                    $allEls = $uiaWin.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
                    $maxR = $wr.Left
                    $maxB = $wr.Top
                    foreach ($el in $allEls) {
                        $r = $el.Current.BoundingRectangle
                        if (-not [double]::IsInfinity($r.Right) -and $r.Right -le ($wr.Right + 2) -and $r.Right -gt $maxR) { $maxR = $r.Right }
                        if (-not [double]::IsInfinity($r.Bottom) -and $r.Bottom -le ($wr.Bottom + 2) -and $r.Bottom -gt $maxB) { $maxB = $r.Bottom }
                    }
                    $ratioW = [math]::Round(($maxR - $wr.Left) / $wr.Width, 3)
                    $ratioH = [math]::Round(($maxB - $wr.Top) / $wr.Height, 3)
                }
            }
        } catch {
            $ratioW = -1.0
            $ratioH = -1.0
        }

        $status = if ($captured -and -not $indexActive -and $idleCpu -lt 5.0 -and $ratioW -ge 0.95 -and $ratioH -ge 0.95) { "OBSERVED_PASS" } else { "CHECK_REQUIRED" }
        $results.Add([PSCustomObject]@{
            ScalePercent = $scale
            WindowWidth = $w
            WindowHeight = $h
            RatioW = $ratioW
            RatioH = $ratioH
            LaunchMs = [int]$swLaunch.ElapsedMilliseconds
            IdleCpuPercent = $idleCpu
            WorkingSetMB = $wsMb
            IndexServiceActive = $indexActive
            CaptureFile = "scale_$scale.png"
            Status = $status
        })
    }
}
finally {
    Stop-KyraFilesTestProcesses
    if (Test-Path -LiteralPath $backupPath) {
        Copy-Item -LiteralPath $backupPath -Destination $settingsPath -Force
    }
    Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
}

$csvPath = Join-Path $EvidenceDir "resultado.csv"
$results | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
$results | Format-Table -AutoSize
Write-Host "Evidencias gravadas em: $EvidenceDir" -ForegroundColor Green
exit 0
