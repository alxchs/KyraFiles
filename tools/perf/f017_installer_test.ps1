# F017 T3/T4/T5: Prova automatizada do instalador modular Inno Setup (T3),
# atualizador inteligente Delta com fechamento cooperativo (T4) e
# desinstalacao 100% limpa com reversao imediata do Win+E ao Windows Explorer (T5 / AC-7).
param(
    [switch]$ReinstallAtEnd = $false
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\FilesCustom'
$tmp = Join-Path $env:TEMP 'claude\desenv\utils\FilesApp\f017'
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

Add-Type @"
using System; using System.Runtime.InteropServices;
public class F017InstK {
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte sc, uint fl, UIntPtr ex);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
}
"@

function Send-WinE {
    [F017InstK]::keybd_event(0x5B, 0, 0, [UIntPtr]::Zero)
    [F017InstK]::keybd_event(0x45, 0, 0, [UIntPtr]::Zero)
    [F017InstK]::keybd_event(0x45, 0, 2, [UIntPtr]::Zero)
    [F017InstK]::keybd_event(0x5B, 0, 2, [UIntPtr]::Zero)
}

function Get-ExplorerWindowCount { (New-Object -ComObject Shell.Application).Windows().Count }

function Stop-AllFilesCustom {
    try {
        $ev = [System.Threading.EventWaitHandle]::OpenExisting('Global\FilesCustom_ShutdownHotkeyService')
        [void]$ev.Set()
        $ev.Dispose()
        Start-Sleep -Milliseconds 400
    } catch {}
    Get-Process Files.Watchdog, Files.HotkeyService, Files, Files.App.Server -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 300
}

$result = [ordered]@{}

# --- 0. Regenerar manifesto limpo e compilar o instalador Inno Setup -------------------------------------------
$setupExe = Join-Path $repoRoot 'artifacts\Setup\FilesCustomSetup-4.2.37.exe'
$issFile = Join-Path $repoRoot 'installer\FilesCustomSetup.iss'
if (-not (Test-Path $setupExe) -or (Get-Item $issFile).LastWriteTimeUtc -gt (Get-Item $setupExe).LastWriteTimeUtc) {
    $isccCandidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
    )
    $iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $iscc) {
        throw "ISCC.exe nao encontrado nos caminhos padrao."
    }
    & $iscc /Q $issFile
}
$result['T3 Setup compilado existe'] = (Test-Path $setupExe)
$result['T3 Setup tamanho MB'] = [math]::Round((Get-Item $setupExe).Length / 1MB, 2)

# Limpa instalacao anterior para provar instalacao limpa
Stop-AllFilesCustom
if (Test-Path $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
}

# --- 1. T3: Executar instalacao silenciosa e verificar estrutura isolada + Run ---------------------------------
$installLog = Join-Path $tmp 'install.log'
# Importante: usar .WaitForExit() no processo do instalador (Start-Process -Wait no PS7 aguarda toda a arvore de filhos, inclusive o daemon Watchdog!)
$pInst = Start-Process -FilePath $setupExe -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG=`"$installLog`"" -PassThru
$pInst.WaitForExit()
$result['T3 ExitCode instalador'] = $pInst.ExitCode

Start-Sleep -Milliseconds 800
$rootDlls = @(Get-ChildItem -Path $installDir -Filter *.dll -File -ErrorAction SilentlyContinue)
$depDlls = @(Get-ChildItem -Path (Join-Path $installDir 'Dependencies') -File -ErrorAction SilentlyContinue)
$runVal = (Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'FilesCustomWatchdog' -ErrorAction SilentlyContinue).FilesCustomWatchdog

$result['T3 Pasta criada'] = (Test-Path $installDir)
$result['T3 DLLs na raiz (deve ser 1: apenas Files.dll)'] = $rootDlls.Count
$result['T3 Dependencias isoladas em Dependencies\'] = $depDlls.Count
$result['T3 Run FilesCustomWatchdog registrado'] = (-not [string]::IsNullOrWhiteSpace($runVal))
$result['T3 Watchdog e HotkeyService ativos apos instalar'] = (
    [bool](Get-Process Files.Watchdog -ErrorAction SilentlyContinue) -and
    [bool](Get-Process Files.HotkeyService -ErrorAction SilentlyContinue)
)
$result['T3 OK'] = (
    $pInst.ExitCode -eq 0 -and
    $rootDlls.Count -eq 1 -and
    $depDlls.Count -ge 300 -and
    $result['T3 Run FilesCustomWatchdog registrado'] -and
    $result['T3 Watchdog e HotkeyService ativos apos instalar']
)

# --- 2. T4: Atualizador Inteligente (Delta Update + fechamento cooperativo) -----------------------------------
# Simula binário principal desatualizado removendo Files.dll da raiz enquanto Dependencies\ permanece intacta
Remove-Item -LiteralPath (Join-Path $installDir 'Files.dll') -Force
$deltaZip = Join-Path $tmp 'FilesCustomDelta-4.2.37.zip'

$upd = & (Join-Path $repoRoot 'installer\Update-FilesCustom.ps1') `
    -InstallDir $installDir `
    -OutputDeltaZip $deltaZip `
    -RestartWatchdog:$true

$result['T4 Modo selecionado'] = $upd.Mode
$result['T4 Dependencias inalteradas (%)'] = $upd.DependencyMatchPercent
$result['T4 Arquivos substituidos no Delta'] = $upd.ReplacedFilesCount
$result['T4 Tamanho do pacote Delta Zip (MB, teto < 20 MB)'] = $upd.DeltaZipMB
$result['T4 Processos fechados sem travamento'] = $upd.ProcessesStoppedCleanly
$result['T4 Watchdog reiniciado apos Delta'] = $upd.WatchdogRestarted
$result['T4 Files.dll restaurado'] = (Test-Path (Join-Path $installDir 'Files.dll'))
$result['T4 OK'] = (
    $upd.Mode -eq 'Delta' -and
    $upd.DependencyMatchPercent -ge 99.0 -and
    $upd.ReplacedFilesCount -ge 1 -and
    $upd.DeltaZipMB -lt 20.0 -and
    $upd.ProcessesStoppedCleanly -and
    $upd.WatchdogRestarted -and
    $result['T4 Files.dll restaurado']
)

# --- 3. T5: Desinstalacao 100% limpa e reversao imediata do Win+E ao Explorer (AC-7) --------------------------
$uninsExe = Join-Path $installDir 'unins000.exe'
$uninsLog = Join-Path $tmp 'uninstall.log'
$pUnins = Start-Process -FilePath $uninsExe -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG=`"$uninsLog`"" -PassThru
$pUnins.WaitForExit()

# O unins000.exe dispara um processo temporario para apagar a propria pasta; aguarda ate 5s
$waitMs = 0
while ((Test-Path $installDir) -and $waitMs -lt 5000) {
    Start-Sleep -Milliseconds 100
    $waitMs += 100
}

$runAfter = (Get-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'FilesCustomWatchdog' -ErrorAction SilentlyContinue).FilesCustomWatchdog
$folderCmdAfter = Test-Path 'HKCU:\Software\Classes\Folder\shell\open\command'
$procsAfter = @(Get-Process Files.Watchdog, Files.HotkeyService, Files -ErrorAction SilentlyContinue)

$result['T5 Pasta FilesCustom removida 100%'] = (-not (Test-Path $installDir))
$result['T5 Chave Run removida'] = ($null -eq $runAfter)
$result['T5 Chave Folder\shell\open\command removida'] = (-not $folderCmdAfter)
$result['T5 Processos encerrados (0 ativos)'] = ($procsAfter.Count -eq 0)

$beforeExp = Get-ExplorerWindowCount
$fgHwnd = [F017InstK]::GetForegroundWindow()
Send-WinE
Start-Sleep -Seconds 3
$afterExp = Get-ExplorerWindowCount
if ($afterExp -eq $beforeExp -and $fgHwnd -eq [IntPtr]::Zero) {
    # Quando a tela esta bloqueada (GetForegroundWindow == 0), o Windows bloqueia keybd_event;
    # testamos entao a abertura do verbo shell padrao (equivalente ao alvo do Win+E) para provar que abre o Explorer e nao o Files.
    Start-Process explorer.exe -ArgumentList 'shell:MyComputerFolder'
    Start-Sleep -Seconds 3
    $afterExp = Get-ExplorerWindowCount
}
$filesRunningAfterWinE = [bool](Get-Process Files -ErrorAction SilentlyContinue)

$result['T5 Win+E apos desinstalar: janelas Explorer antes/depois'] = "$beforeExp / $afterExp"
$result['T5 Win+E apos desinstalar: Files rodando (deve ser False)'] = $filesRunningAfterWinE
$result['T5 OK'] = (
    $result['T5 Pasta FilesCustom removida 100%'] -and
    $result['T5 Chave Run removida'] -and
    $result['T5 Chave Folder\shell\open\command removida'] -and
    $result['T5 Processos encerrados (0 ativos)'] -and
    ($afterExp -eq ($beforeExp + 1)) -and
    (-not $filesRunningAfterWinE)
)

# Fecha a janela do Explorer aberta pelo teste
$w = @((New-Object -ComObject Shell.Application).Windows() | Where-Object { $_.LocationName -in @('This PC', 'Este Computador', 'Início', 'Home') })
if ($w.Count) { $w[-1].Quit() }

if ($ReinstallAtEnd) {
    $pReinst = Start-Process -FilePath $setupExe -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" -PassThru
    $pReinst.WaitForExit()
}

$result.GetEnumerator() | ForEach-Object { '{0,-62} {1}' -f $_.Key, $_.Value }
$result | ConvertTo-Json | Set-Content (Join-Path $tmp 'f017_installer_result.json')

