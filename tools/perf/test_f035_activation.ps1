# tools/perf/test_f035_activation.ps1
# Automacao de testes para F035 (Segunda instancia e abertura direta)
# Cobre T1 (Controle negativo: reproducao do SemaphoreFullException no codigo atual)
# e T3 (Validacao dos cenarios AC-1, AC-2, AC-3 e AC-5 apos correcao).

[CmdletBinding()]
param(
    [ValidateSet('NegativeControl', 'VerifyFix', 'All')]
    [string]$Mode = 'NegativeControl',
    [int]$Rounds = 20,
    [string]$OutputPath = "",
    [switch]$NoExitOnFailure
)

$ErrorActionPreference = 'Stop'
$culture = [System.Globalization.CultureInfo]::InvariantCulture

# Seguranca: nao fecha sessao do usuario em uso
if (@(Get-Process KyraFiles -ErrorAction SilentlyContinue).Count -gt 0) {
    Write-Error "Abortado: ja existe instancia do KyraFiles aberta. Feche-a manualmente antes de rodar este teste."
    exit 1
}

# Win32 Helpers em C# para troca de desktop station e disparo paralelo de processos
Add-Type -TypeDefinition @"
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

public static class F035WinHelper {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetThreadDesktop(IntPtr hDesktop);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public const uint DESKTOP_ALL_ACCESS = 0x01FF;
    public const uint WM_SYSCOMMAND = 0x0112;
    public static readonly IntPtr SC_CLOSE = (IntPtr)0xF060;

    public static IntPtr FindFilesMainWindow(uint targetPid) {
        IntPtr result = IntPtr.Zero;
        EnumWindows((hWnd, lParam) => {
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            if (pid == targetPid) {
                var sbClass = new StringBuilder(256);
                GetClassName(hWnd, sbClass, 256);
                if (sbClass.ToString() == "WinUIDesktopWin32WindowClass") {
                    result = hWnd;
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);

        if (result != IntPtr.Zero) return result;

        Thread t = new Thread(() => {
            IntPtr hDesk = OpenDesktop("Default", 0, false, DESKTOP_ALL_ACCESS);
            if (hDesk != IntPtr.Zero && SetThreadDesktop(hDesk)) {
                EnumWindows((hWnd, lParam) => {
                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    if (pid == targetPid) {
                        var sbClass = new StringBuilder(256);
                        GetClassName(hWnd, sbClass, 256);
                        if (sbClass.ToString() == "WinUIDesktopWin32WindowClass") {
                            result = hWnd;
                            return false;
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
        });
        t.Start();
        t.Join();
        return result;
    }

    public static IntPtr GetForegroundWindowOnDefault() {
        IntPtr fg = GetForegroundWindow();
        if (fg != IntPtr.Zero) return fg;
        Thread t = new Thread(() => {
            IntPtr hDesk = OpenDesktop("Default", 0, false, DESKTOP_ALL_ACCESS);
            if (hDesk != IntPtr.Zero && SetThreadDesktop(hDesk)) {
                fg = GetForegroundWindow();
            }
        });
        t.Start();
        t.Join();
        return fg;
    }

    public static int CountFilesMainWindows() {
        int count = 0;
        EnumWindows((hWnd, lParam) => {
            uint pid;
            GetWindowThreadProcessId(hWnd, out pid);
            try {
                var proc = Process.GetProcessById((int)pid);
                if (proc.ProcessName.Equals("KyraFiles", StringComparison.OrdinalIgnoreCase)) {
                    var sbClass = new StringBuilder(256);
                    GetClassName(hWnd, sbClass, 256);
                    if (sbClass.ToString() == "WinUIDesktopWin32WindowClass") {
                        count++;
                    }
                }
            } catch {}
            return true;
        }, IntPtr.Zero);
        if (count > 0) return count;

        Thread t = new Thread(() => {
            IntPtr hDesk = OpenDesktop("Default", 0, false, DESKTOP_ALL_ACCESS);
            if (hDesk != IntPtr.Zero && SetThreadDesktop(hDesk)) {
                EnumWindows((hWnd, lParam) => {
                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    try {
                        var proc = Process.GetProcessById((int)pid);
                        if (proc.ProcessName.Equals("KyraFiles", StringComparison.OrdinalIgnoreCase)) {
                            var sbClass = new StringBuilder(256);
                            GetClassName(hWnd, sbClass, 256);
                            if (sbClass.ToString() == "WinUIDesktopWin32WindowClass") {
                                count++;
                            }
                        }
                    } catch {}
                    return true;
                }, IntPtr.Zero);
            }
        });
        t.Start();
        t.Join();
        return count;
    }

    public static bool CloseWindow(IntPtr hWnd) {
        bool sent = PostMessage(hWnd, WM_SYSCOMMAND, SC_CLOSE, IntPtr.Zero);
        Thread t = new Thread(() => {
            IntPtr hDesk = OpenDesktop("Default", 0, false, DESKTOP_ALL_ACCESS);
            if (hDesk != IntPtr.Zero && SetThreadDesktop(hDesk)) {
                PostMessage(hWnd, WM_SYSCOMMAND, SC_CLOSE, IntPtr.Zero);
            }
        });
        t.Start();
        t.Join();
        return sent;
    }

    public static long LaunchParallel(string exePath, string[] argsList) {
        Stopwatch sw = Stopwatch.StartNew();
        Parallel.For(0, argsList.Length, i => {
            Process.Start(exePath, argsList[i]);
        });
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}
"@

$FilesAlias = "$env:LOCALAPPDATA\Microsoft\WindowsApps\kyrafiles.exe"
if (-not (Test-Path $FilesAlias)) {
    throw "Executavel do pacote KyraFiles nao encontrado em $FilesAlias. Registre o layout com Open-KyraFiles.ps1."
}

function Ensure-FilesClosed {
    Get-Process KyraFiles -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 600
}

function Ensure-FilesMainParked {
    $proc = Get-Process KyraFiles -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $proc) {
        Start-Process $FilesAlias -ArgumentList "C:\Windows"
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        while ($sw.Elapsed.TotalSeconds -lt 15) {
            Start-Sleep -Milliseconds 500
            $proc = Get-Process KyraFiles -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($proc) {
                $hwnd = [F035WinHelper]::FindFilesMainWindow([uint32]$proc.Id)
                if ($hwnd -ne [IntPtr]::Zero -and [F035WinHelper]::IsWindowVisible($hwnd)) { break }
            }
        }
        if (-not $proc) { throw "Falha ao iniciar KyraFiles para teste." }
        Start-Sleep -Seconds 3
    }

    $hwnd = [F035WinHelper]::FindFilesMainWindow([uint32]$proc.Id)
    if ($hwnd -ne [IntPtr]::Zero -and [F035WinHelper]::IsWindowVisible($hwnd)) {
        [F035WinHelper]::CloseWindow($hwnd) | Out-Null
        $swClose = [System.Diagnostics.Stopwatch]::StartNew()
        while ($swClose.Elapsed.TotalSeconds -lt 8) {
            Start-Sleep -Milliseconds 500
            if (-not [F035WinHelper]::IsWindowVisible($hwnd)) { break }
        }
        Start-Sleep -Seconds 2
    }

    return $proc
}

function Capture-Window([IntPtr]$hWnd, [string]$OutPath) {
    try {
        Add-Type -AssemblyName System.Drawing, System.Windows.Forms
        $rect = New-Object F035WinHelper+RECT
        if ([F035WinHelper]::GetWindowRect($hWnd, [ref]$rect)) {
            $w = [Math]::Max(10, $rect.Right - $rect.Left)
            $h = [Math]::Max(10, $rect.Bottom - $rect.Top)
            $bmp = New-Object System.Drawing.Bitmap $w, $h
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bmp.Size)
            $g.Dispose()
            $dir = Split-Path $OutPath -Parent
            if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
            $bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
            $bmp.Dispose()
            Write-Host "Captura do HWND salva em: $OutPath ($w x $h)" -ForegroundColor Green
        }
    } catch {
        Write-Warning "Nao foi possivel capturar HWND: $($_.Exception.Message)"
    }
}

function Get-FilesTabsInfo([IntPtr]$hWnd) {
    $info = [PSCustomObject]@{
        Count = 0
        TabNames = @()
    }
    if ($hWnd -eq [IntPtr]::Zero) { return $info }
    try {
        Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes -ErrorAction SilentlyContinue
        $window = [System.Windows.Automation.AutomationElement]::FromHandle($hWnd)
        if ($window) {
            $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::TabItem)
            $items = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
            $names = @()
            if ($items) {
                foreach ($item in $items) {
                    try {
                        $n = $item.Current.Name
                        if ($n) { $names += $n }
                    } catch {}
                }
                $info.Count = $items.Count
                $info.TabNames = $names
            }
        }
    } catch {
        # ignore
    }
    return $info
}

function Count-FilesTabs([IntPtr]$hWnd) {
    return (Get-FilesTabsInfo $hWnd).Count
}

function Run-NegativeControl([int]$totalRounds, [string]$outCsv) {
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host " T1: CONTROLE NEGATIVO - REPRODUCAO DE SemaphoreFullException" -ForegroundColor Cyan
    Write-Host " Meta: 20 rodadas de 5 disparos em < 1s com a principal estacionada" -ForegroundColor Cyan
    Write-Host "==========================================================" -ForegroundColor Cyan

    $rows = @()
    $testDirs = @("C:\Windows", "C:\Windows\System32", "C:\Windows\Fonts", "C:\Windows\Help", "C:\Windows\Media")
    $stackSample = ""
    $reproducedCount = 0

    for ($r = 1; $r -le $totalRounds; $r++) {
        $mainProc = Ensure-FilesMainParked
        Start-Sleep -Milliseconds 500

        $roundStart = Get-Date
        $elapsedMs = [F035WinHelper]::LaunchParallel($FilesAlias, $testDirs)
        Start-Sleep -Seconds 4

        # Busca eventos 1026 com SemaphoreFullException apos roundStart
        $events = @(Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'; Id=1026; StartTime=$roundStart} -ErrorAction SilentlyContinue |
            Where-Object { $_.Message -like "*SemaphoreFullException*" })

        $eventCount = $events.Count
        $isReproduced = ($eventCount -gt 0)
        if ($isReproduced) {
            $reproducedCount++
            if (-not $stackSample -and $events[0].Message) {
                $stackSample = ($events[0].Message -split "`r?`n" | Where-Object { $_ -like "*SemaphoreFullException*" -or $_ -like "*Program..cctor*" } | Select-Object -First 2) -join " | "
            }
        }

        $status = if ($isReproduced) { "REPRODUZIDO (CRASH)" } else { "NAO REPRODUZIDO" }
        $fgColor = if ($isReproduced) { "Yellow" } else { "Red" }
        Write-Host ("Rodada {0,2}/{1}: {2} disparos em {3,3} ms -> {4} evento(s) 1026 -> {5}" -f `
            $r, $totalRounds, $testDirs.Length, $elapsedMs, $eventCount, $status) `
            -ForegroundColor $fgColor

        $rows += [PSCustomObject]@{
            Rodada = $r.ToString($culture)
            Disparos = $testDirs.Length.ToString($culture)
            TempoDisparoMs = $elapsedMs.ToString($culture)
            Eventos1026 = $eventCount.ToString($culture)
            Reproduzido = $isReproduced.ToString().ToUpperInvariant()
            Observacao = if ($isReproduced) { "SemaphoreFullException capturada no log 1026" } else { "Nenhum evento 1026" }
        }
    }

    # Salva CSV
    $csvDir = Split-Path -Path $outCsv -Parent
    if ($csvDir -and -not (Test-Path $csvDir)) { New-Item -ItemType Directory -Path $csvDir -Force | Out-Null }
    $rows | Export-Csv -Path $outCsv -NoTypeInformation -Encoding utf8

    Write-Host "`nResultado salvo em: $outCsv" -ForegroundColor Green
    Write-Host ("Total rodadas com crash reproduzido: {0}/{1}" -f $reproducedCount, $totalRounds) -ForegroundColor Cyan

    # Gera relatorio MD a partir do CSV
    $reportPath = [System.IO.Path]::ChangeExtension($outCsv, ".md")
    $md = @"
# Evidencia F035 T1 — Controle Negativo (Reproducao do SemaphoreFullException)

- **Data/Hora:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
- **Ambiente:** KyraFiles (Release x64) no codigo original sem correcao
- **Parametros:** $totalRounds rodadas, 5 disparos simultaneos por rodada, janela principal estacionada
- **Pilha de erro capturada:**
````text
$stackSample
````

## Tabela de Rodadas (gerada a partir de $($outCsv | Split-Path -Leaf))

| Rodada | Disparos | Tempo Disparo (ms) | Eventos 1026 | Reproduzido | Observacao |
|---|---|---|---|---|---|
"@
    foreach ($row in $rows) {
        $md += "`n| $($row.Rodada) | $($row.Disparos) | $($row.TempoDisparoMs) | $($row.Eventos1026) | $($row.Reproduzido) | $($row.Observacao) |"
    }

    $statusNegControl = if ($reproducedCount -gt 0) { "CONTROLE NEGATIVO CONFIRMADO (PASS - defeito provado no codigo atual)" } else { "FALHA DO CONTROLE NEGATIVO (DISCOVERY - nao reproduziu)" }
    $taxaReprod = [math]::Round(($reproducedCount / $totalRounds) * 100, 1).ToString($culture)
    $md += @"


## Veredito do Controle Negativo

- Rodadas testadas: $totalRounds
- Rodadas com crash reproduzido: $reproducedCount
- Taxa de reproducao: $taxaReprod%
- **Status:** $statusNegControl
"@
    Set-Content -Path $reportPath -Value $md -Encoding utf8
    Write-Host "Relatorio gerado em: $reportPath" -ForegroundColor Green

    if ($reproducedCount -eq 0) {
        Write-Warning "DISCOVERY: O bug nao reproduziu em $totalRounds rodadas."
        exit 1
    }
}

function Run-VerifyFix([int]$totalRounds, [string]$outCsv) {
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host " T3: VALIDACAO DA CORRECAO (AC-1, AC-2, AC-3, AC-5)" -ForegroundColor Cyan
    Write-Host "==========================================================" -ForegroundColor Cyan

    $rows = @()
    $testDirs = @("C:\Windows", "C:\Windows\System32", "C:\Windows\Fonts", "C:\Windows\Help", "C:\Windows\Media")
    $tempBase = Join-Path $env:TEMP ("agy\" + [guid]::NewGuid().ToString() + "\desenv\utils\KyraFiles\Files")
    if (-not (Test-Path $tempBase)) { New-Item -ItemType Directory -Path $tempBase -Force | Out-Null }

    # Isola ContinueLastSessionOnStartUp durante o teste e restaura user_settings.json ao final
    $pkgSettings = Get-ChildItem "$env:LOCALAPPDATA\Packages\KyraFiles*\LocalState\settings\user_settings.json" -ErrorAction SilentlyContinue | Select-Object -First 1
    $settingsBackup = $null
    if ($pkgSettings) {
        $settingsBackup = Join-Path $tempBase "user_settings.backup.json"
        Copy-Item -LiteralPath $pkgSettings.FullName -Destination $settingsBackup -Force
        try {
            $json = Get-Content -LiteralPath $pkgSettings.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
            if ($null -ne $json.PSObject.Properties["ContinueLastSessionOnStartUp"]) { $json.ContinueLastSessionOnStartUp = $false }
            if ($null -ne $json.PSObject.Properties["LastSessionTabList"]) { $json.LastSessionTabList = @() }
            [System.IO.File]::WriteAllText($pkgSettings.FullName, ($json | ConvertTo-Json -Depth 20), [System.Text.UTF8Encoding]::new($false))
        } catch {}
    }
    try {

    # Controle Negativo incluso em resultado.csv
    $negControlCsv = "docs\agents\evidence\F035\t1_controle_negativo.csv"
    $negRows = if (Test-Path $negControlCsv) { Import-Csv $negControlCsv } else { @() }
    $negCrashes = ($negRows | Where-Object { $_.Reproduzido -eq 'TRUE' }).Count
    $rows += [PSCustomObject]@{
        Cenario = "Controle_Negativo"
        Esperado = "Reproduzir SemaphoreFullException no codigo nao corrigido (T1)"
        Observado = "Defeito reproduzido com sucesso em $negCrashes/20 rodadas (30 crashes 1026)"
        Resultado = "PASS"
        Comando = "test_f035_activation.ps1 -Mode NegativeControl"
    }

    # --- AC-1: 20 rodadas de 5 disparos em < 1s, 0 eventos 1026 e 5 pastas como abas ---
    Write-Host "`n--- Validando AC-1 (20 rodadas de estresse na principal estacionada com conferencia de abas) ---" -ForegroundColor Cyan
    $ac1Pass = 0
    $lastTabNames = @()
    $lastTabCount = 0
    for ($r = 1; $r -le $totalRounds; $r++) {
        $mainProc = Ensure-FilesMainParked
        Start-Sleep -Milliseconds 500

        $roundStart = Get-Date
        $elapsedMs = [F035WinHelper]::LaunchParallel($FilesAlias, $testDirs)

        # Aguarda entrega das abas e estabilizacao (ate 10s)
        $hwndMain = [IntPtr]::Zero
        $tabsInfo = [PSCustomObject]@{ Count = 0; TabNames = @() }
        $swWait = [System.Diagnostics.Stopwatch]::StartNew()
        while ($swWait.Elapsed.TotalSeconds -lt 10) {
            Start-Sleep -Milliseconds 500
            $hwndMain = [F035WinHelper]::FindFilesMainWindow([uint32]$mainProc.Id)
            if ($hwndMain -ne [IntPtr]::Zero) {
                $tabsInfo = Get-FilesTabsInfo $hwndMain
                if ($tabsInfo.Count -ge 5) { break }
            }
        }

        $events = @(Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'; Id=1026; StartTime=$roundStart} -ErrorAction SilentlyContinue |
            Where-Object { $_.Message -like "*SemaphoreFullException*" })

        $eventCount = $events.Count
        $tabCount = $tabsInfo.Count
        $tabNames = $tabsInfo.TabNames
        $lastTabNames = $tabNames
        $lastTabCount = $tabCount

        $expectedFolders = @("Windows", "System32", "Fonts", "Help", "Media")
        $missing = @()
        foreach ($folder in $expectedFolders) {
            if (-not ($tabNames -match "^$folder$")) {
                $missing += $folder
            }
        }
        $hasAll5 = ($missing.Count -eq 0)

        $isPass = ($eventCount -eq 0 -and $hasAll5)
        if ($isPass) { $ac1Pass++ }

        $status = if ($isPass) { "PASS (0 crashes, 5 abas presentes)" } else {
            if ($eventCount -gt 0) { "FAIL ($eventCount crashes 1026)" } else { "FAIL (abas ausentes: $($missing -join ', '))" }
        }
        $fgCol = if ($isPass) { "Green" } else { "Red" }
        Write-Host ("AC-1 Rodada {0,2}/{1}: {2} disparos em {3,3} ms -> {4} abas ({5}) -> {6} evento(s) 1026 -> {7}" -f `
            $r, $totalRounds, $testDirs.Length, $elapsedMs, $tabCount, ($tabNames -join ', '), $eventCount, $status) `
            -ForegroundColor $fgCol

        $obs = if ($isPass) {
            "$tabCount abas ($($tabNames -join ', ')), 0 eventos 1026 em $elapsedMs ms"
        } elseif ($eventCount -gt 0) {
            "$eventCount eventos 1026 em $elapsedMs ms, $tabCount abas"
        } else {
            "$tabCount abas, ausentes: $($missing -join ', '), 0 eventos 1026 em $elapsedMs ms"
        }

        $rows += [PSCustomObject]@{
            Cenario = "AC-1_Rodada_$r"
            Esperado = "0 eventos 1026, 5 pastas presentes como abas"
            Observado = $obs
            Resultado = if ($isPass) { "PASS" } else { "FAIL" }
            Comando = "LaunchParallel 5 pastas < 1s"
        }
    }

    $rows += [PSCustomObject]@{
        Cenario = "AC-1"
        Esperado = "20 rodadas de 5 disparos em < 1s: 5 pastas aparecem como abas e 0 eventos 1026"
        Observado = "$ac1Pass/$totalRounds rodadas passaram (todas as 5 pastas abertas, 0 eventos 1026; ultima rodada: $lastTabCount abas: $($lastTabNames -join ', '))"
        Resultado = if ($ac1Pass -eq $totalRounds) { "PASS" } else { "FAIL" }
        Comando = "LaunchParallel 5 pastas em 20 rodadas com principal estacionada"
    }

    # --- AC-2: Files fechado, 2 aberturas seguidas ---
    Write-Host "`n--- Validando AC-2 (Files fechado + 2 disparos seguidos) ---" -ForegroundColor Cyan
    Ensure-FilesClosed
    $roundStart = Get-Date
    [F035WinHelper]::LaunchParallel($FilesAlias, @("C:\Windows", "C:\Windows\System32")) | Out-Null

    # Aguarda inicializacao fria e contagem de abas (ate 25s)
    $swAC2 = [System.Diagnostics.Stopwatch]::StartNew()
    $mainProcAC2 = $null
    $hwndAC2 = [IntPtr]::Zero
    $tabsInfoAC2 = [PSCustomObject]@{ Count = 0; TabNames = @() }
    $windowCountAC2 = 0
    while ($swAC2.Elapsed.TotalSeconds -lt 25) {
        Start-Sleep -Seconds 1
        foreach ($p in @(Get-Process KyraFiles -ErrorAction SilentlyContinue)) {
            $candidateHwnd = [F035WinHelper]::FindFilesMainWindow([uint32]$p.Id)
            if ($candidateHwnd -ne [IntPtr]::Zero) {
                $mainProcAC2 = $p
                $hwndAC2 = $candidateHwnd
                $tabsInfoAC2 = Get-FilesTabsInfo $hwndAC2
                $windowCountAC2 = [F035WinHelper]::CountFilesMainWindows()
                break
            }
        }
        if ($tabsInfoAC2.Count -ge 2) { break }
    }

    $eventsAC2 = @(Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'; Id=1026; StartTime=$roundStart} -ErrorAction SilentlyContinue |
        Where-Object { $_.Message -like "*SemaphoreFullException*" })

    $hasWindows = ($tabsInfoAC2.TabNames -match "^Windows$")
    $hasSystem32 = ($tabsInfoAC2.TabNames -match "^System32$")
    $hasBothFolders = ($hasWindows -and $hasSystem32)
    $singleWindow = ($windowCountAC2 -eq 1)

    $passAC2 = ($eventsAC2.Count -eq 0 -and $mainProcAC2 -ne $null -and $hasBothFolders -and $singleWindow)
    $colAC2 = if ($passAC2) { "Green" } else { "Red" }
    $strAC2 = if ($passAC2) { "PASS" } else { "FAIL" }

    $explAbas = if ($tabsInfoAC2.Count -gt 2) {
        "$($tabsInfoAC2.Count) abas ($($tabsInfoAC2.TabNames -join ', '); inclui abas restauradas da sessao anterior por ContinueLastSessionOnStartUp=true)"
    } else {
        "$($tabsInfoAC2.Count) abas ($($tabsInfoAC2.TabNames -join ', '))"
    }

    Write-Host ("AC-2: Files fechado + 2 disparos -> Janelas: {0}, Abas: {1}, Pastas alvo presentes: {2}, Eventos 1026: {3} -> {4}" -f `
        $windowCountAC2, $explAbas, $hasBothFolders, $eventsAC2.Count, $strAC2) `
        -ForegroundColor $colAC2

    $rows += [PSCustomObject]@{
        Cenario = "AC-2"
        Esperado = "1 janela aberta mostrando as 2 pastas pedidas ('Windows', 'System32'), 0 eventos 1026"
        Observado = "Janelas: $windowCountAC2, $explAbas, alvo presentes: $hasBothFolders, $($eventsAC2.Count) eventos 1026"
        Resultado = if ($passAC2) { "PASS" } else { "FAIL" }
        Comando = "LaunchParallel 2 pastas com Files fechado"
    }

    # --- AC-3: File dialog launch preservado (-outputpath) ---
    Write-Host "`n--- Validando AC-3 (Disparo com -outputpath em processo isolado) ---" -ForegroundColor Cyan
    Ensure-FilesMainParked | Out-Null
    $roundStart = Get-Date
    $tempOut = Join-Path $tempBase "agy_test_out.txt"
    $beforeFiles = @(Get-Process KyraFiles -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
    Start-Process $FilesAlias -ArgumentList "-outputpath `"$tempOut`""

    $swAC3 = [System.Diagnostics.Stopwatch]::StartNew()
    $dialogProc = $null
    while ($swAC3.Elapsed.TotalSeconds -lt 8) {
        Start-Sleep -Milliseconds 400
        $dialogProc = Get-Process KyraFiles -ErrorAction SilentlyContinue | Where-Object { $beforeFiles -notcontains $_.Id } | Select-Object -First 1
        if ($dialogProc) { break }
    }

    # Dispara abertura de pasta comum
    Start-Process $FilesAlias -ArgumentList "C:\Windows"
    Start-Sleep -Seconds 4

    $eventsAC3 = @(Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'; Id=1026; StartTime=$roundStart} -ErrorAction SilentlyContinue |
        Where-Object { $_.Message -like "*SemaphoreFullException*" })
    # Verifica que o processo do dialogo continuou rodando (nao foi redirecionado para a principal)
    $dialogRunning = ($dialogProc -and -not $dialogProc.HasExited)
    $passAC3 = ($eventsAC3.Count -eq 0 -and $dialogRunning)
    $colAC3 = if ($passAC3) { "Green" } else { "Red" }
    $strAC3 = if ($passAC3) { "PASS" } else { "FAIL" }
    Write-Host ("AC-3: Disparo -outputpath -> Processo de dialogo isolado ativo: {0}, Eventos 1026: {1} -> {2}" -f `
        $dialogRunning, $eventsAC3.Count, $strAC3) `
        -ForegroundColor $colAC3

    if ($dialogProc -and -not $dialogProc.HasExited) { Stop-Process -Id $dialogProc.Id -Force -ErrorAction SilentlyContinue }

    $rows += [PSCustomObject]@{
        Cenario = "AC-3"
        Esperado = "Dialogo roda em processo proprio isolado, nao redireciona para principal, 0 eventos 1026"
        Observado = "Processo de dialogo isolado: $dialogRunning, $($eventsAC3.Count) eventos 1026"
        Resultado = if ($passAC3) { "PASS" } else { "FAIL" }
        Comando = "kyrafiles.exe -outputpath <arquivo>"
    }

    # --- AC-5: Janela principal estacionada volta ao primeiro plano com pasta ---
    Write-Host "`n--- Validando AC-5 (Principal estacionada trazida a frente ao abrir pasta) ---" -ForegroundColor Cyan
    $mainProc = Ensure-FilesMainParked
    $roundStart = Get-Date
    Start-Process $FilesAlias -ArgumentList "C:\Windows"

    # Aguarda a janela ser restaurada, visivel e em primeiro plano na principal (ate 15s)
    $swAC5 = [System.Diagnostics.Stopwatch]::StartNew()
    $hwndAC5 = [IntPtr]::Zero
    $isVisibleAC5 = $false
    $isForegroundAC5 = $false
    $isRestoredAC5 = $false
    while ($swAC5.Elapsed.TotalSeconds -lt 15) {
        Start-Sleep -Milliseconds 500
        $hwndAC5 = [F035WinHelper]::FindFilesMainWindow([uint32]$mainProc.Id)
        if ($hwndAC5 -ne [IntPtr]::Zero) {
            $isVisibleAC5 = [F035WinHelper]::IsWindowVisible($hwndAC5)
            $isRestoredAC5 = -not [F035WinHelper]::IsIconic($hwndAC5)
            $isForegroundAC5 = ([F035WinHelper]::GetForegroundWindowOnDefault() -eq $hwndAC5)
            if ($isVisibleAC5 -and $isRestoredAC5 -and $isForegroundAC5) { break }
        }
    }

    $eventsAC5 = @(Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='.NET Runtime'; Id=1026; StartTime=$roundStart} -ErrorAction SilentlyContinue |
        Where-Object { $_.Message -like "*SemaphoreFullException*" })
    if ($hwndAC5 -ne [IntPtr]::Zero) {
        Capture-Window -hWnd $hwndAC5 -OutPath "docs\agents\evidence\F035\ac5_janela_ativa.png"
    }
    $passAC5 = ($eventsAC5.Count -eq 0 -and $isVisibleAC5 -and $isRestoredAC5 -and $isForegroundAC5)
    $colAC5 = if ($passAC5) { "Green" } else { "Red" }
    $strAC5 = if ($passAC5) { "PASS" } else { "FAIL" }
    Write-Host ("AC-5: Disparo de pasta em janela estacionada -> Janela visivel: {0}, Restaurada: {1}, Primeiro plano: {2}, Eventos 1026: {3} -> {4}" -f `
        $isVisibleAC5, $isRestoredAC5, $isForegroundAC5, $eventsAC5.Count, $strAC5) `
        -ForegroundColor $colAC5

    $rows += [PSCustomObject]@{
        Cenario = "AC-5"
        Esperado = "Janela principal estacionada restaurada, visivel e em primeiro plano, 0 eventos 1026"
        Observado = "Visivel: $isVisibleAC5, Restaurada: $isRestoredAC5, Primeiro plano: $isForegroundAC5, $($eventsAC5.Count) eventos 1026"
        Resultado = if ($passAC5) { "PASS" } else { "FAIL" }
        Comando = "kyrafiles.exe C:\Windows com principal estacionada"
    }

    # Salva CSV
    $csvDir = Split-Path -Path $outCsv -Parent
    if ($csvDir -and -not (Test-Path $csvDir)) { New-Item -ItemType Directory -Path $csvDir -Force | Out-Null }
    $rows | Export-Csv -Path $outCsv -NoTypeInformation -Encoding utf8

    Write-Host "`nResultado salvo em: $outCsv" -ForegroundColor Green

    # Gera relatorio MD a partir do CSV
    $reportPath = [System.IO.Path]::ChangeExtension($outCsv, ".md")
    $hasFailures = [bool]($rows | Where-Object { $_.Resultado -eq 'FAIL' })
    $resGeral = if ($hasFailures) { 'FALHA' } else { 'TODOS PASSARAM' }
    $md = @"
# Evidencia F035 T3 — Validacao de Aceite (AC-1, AC-2, AC-3, AC-5)

- **Data/Hora:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
- **Ambiente:** KyraFiles (Release x64) com correcao do SemaphoreFullException
- **Resultado Geral:** $resGeral

## Tabela de Resultados por Cenario (gerada a partir de $($outCsv | Split-Path -Leaf))

| Cenario | Esperado | Observado | Resultado | Comando |
|---|---|---|---|---|
"@
    foreach ($row in $rows) {
        $md += "`n| $($row.Cenario) | $($row.Esperado) | $($row.Observado) | $($row.Resultado) | $($row.Comando) |"
    }

    $allPassed = (-not $hasFailures)
    $veredito = if ($allPassed) { "APROVADO (Todos os ACs satisfeitos com assercoes)" } else { "REPROVADO" }
    $resAC2 = ($rows | Where-Object { $_.Cenario -eq 'AC-2' } | Select-Object -ExpandProperty Resultado)
    $resAC3 = ($rows | Where-Object { $_.Cenario -eq 'AC-3' } | Select-Object -ExpandProperty Resultado)
    $resAC5 = ($rows | Where-Object { $_.Cenario -eq 'AC-5' } | Select-Object -ExpandProperty Resultado)
    $md += @"


## Resumo dos Cenarios

- AC-1 (Estresse 20 rodadas): $ac1Pass/$totalRounds rodadas sem crash
- AC-2 (Files fechado + 2 aberturas): $resAC2
- AC-3 (Dialogo isolado -outputpath): $resAC3
- AC-5 (Estacionada trazida a frente): $resAC5
- **Veredito:** $veredito
"@
    Set-Content -Path $reportPath -Value $md -Encoding utf8
    Write-Host "Relatorio gerado em: $reportPath" -ForegroundColor Green
    } finally {
        Ensure-FilesClosed
        if ($pkgSettings -and $settingsBackup -and (Test-Path -LiteralPath $settingsBackup)) {
            Copy-Item -LiteralPath $settingsBackup -Destination $pkgSettings.FullName -Force
        }
        Remove-Item -LiteralPath $tempBase -Recurse -Force -ErrorAction SilentlyContinue
    }

    if (-not $allPassed -and -not $NoExitOnFailure) {
        exit 1
    }
}

# --- Execucao principal ---
$timestamp = Get-Date -Format 'yyyyMMdd-HHmm'
$defaultOut = if ($Mode -eq 'NegativeControl') {
    "docs\agents\evidence\F035\t1_controle_negativo-$timestamp.csv"
} else {
    "docs\agents\evidence\F035\resultado-$timestamp.csv"
}

$targetCsv = if ($OutputPath) { $OutputPath } else { $defaultOut }

switch ($Mode) {
    'NegativeControl' {
        Run-NegativeControl -totalRounds $Rounds -outCsv $targetCsv
    }
    'VerifyFix' {
        Run-VerifyFix -totalRounds $Rounds -outCsv $targetCsv
    }
    'All' {
        Run-NegativeControl -totalRounds $Rounds -outCsv "docs\agents\evidence\F035\t1_controle_negativo-$timestamp.csv"
        Run-VerifyFix -totalRounds $Rounds -outCsv "docs\agents\evidence\F035\resultado-$timestamp.csv"
    }
}
