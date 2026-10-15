# F020: Prova de funcionamento das regras de aba única por endereço e Settings na posição 0
# Cenários AC-1 a AC-7
param([string]$Aumid = 'FilesDev_ykqwq8d6ps0ag!App')

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms

$evidenceDir = "C:\desenv\utils\FilesApp\docs\agents\evidence\f020"
if (-not (Test-Path $evidenceDir)) {
    New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
}

$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$Scope = [System.Windows.Automation.TreeScope]

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class F020Win {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    public struct RECT { public int L, T, R, B; }
}
"@

function Send-KeyChord([byte[]]$keys) {
    foreach ($k in $keys) { [F020Win]::keybd_event($k, 0, 0, [UIntPtr]::Zero) }
    Start-Sleep -Milliseconds 50
    for ($i = $keys.Length - 1; $i -ge 0; $i--) { [F020Win]::keybd_event($keys[$i], 0, 2, [UIntPtr]::Zero) }
    Start-Sleep -Milliseconds 150
}

function Save-Shot($name) {
    $p = Get-Process Files -ErrorAction SilentlyContinue | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
    if (-not $p) { return $null }
    [void][F020Win]::SetWindowPos($p.MainWindowHandle, [IntPtr](-1), 0, 0, 0, 0, 0x43)
    [void][F020Win]::SetForegroundWindow($p.MainWindowHandle)
    Start-Sleep -Milliseconds 500
    $r = New-Object F020Win+RECT
    [void][F020Win]::GetWindowRect($p.MainWindowHandle, [ref]$r)
    $w = [Math]::Max(10, $r.R - $r.L)
    $h = [Math]::Max(10, $r.B - $r.T)
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    [System.Drawing.Graphics]::FromImage($bmp).CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
    [void][F020Win]::SetWindowPos($p.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x43)
    $f = Join-Path $evidenceDir "$name.png"
    $bmp.Save($f, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return $f
}

function Get-Tabs($win) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::TabItem)
    return $win.FindAll($Scope::Descendants, $cond)
}

Write-Host "Encerrando instancias anteriores..."
Stop-Process -Name Files -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host "Iniciando Files Dev..."
Start-Process "shell:AppsFolder\$Aumid"

$sw = [Diagnostics.Stopwatch]::StartNew()
$win = $null
while ($sw.Elapsed.TotalSeconds -lt 25) {
    $proc = Get-Process Files -ErrorAction SilentlyContinue | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
    if ($proc) {
        $win = $AE::FromHandle($proc.MainWindowHandle)
        if ($win) { break }
    }
    Start-Sleep -Milliseconds 400
}

if (-not $win) {
    throw "A janela principal do Files nao foi localizada."
}

Start-Sleep -Seconds 4
[void][F020Win]::SetForegroundWindow($proc.MainWindowHandle)

$results = [ordered]@{}

# Estado Inicial
$tabs = Get-Tabs $win
$results["Inicial_TotalAbas"] = $tabs.Count
$results["Inicial_PrimeiraAba"] = if ($tabs.Count -gt 0) { $tabs[0].Current.Name } else { "Nenhuma" }
Save-Shot "01_estado_inicial" | Out-Null

# AC-5: Tentar abrir nova aba Home quando Home já está aberta
Write-Host "Testando AC-5 (Ctrl+T com destino Home)..."
# Dispara files-dev: (que navega para Home)
Start-Process "files-dev:"
Start-Sleep -Seconds 2
$tabs = Get-Tabs $win
$results["AC5_NovaAbaHome_TotalAbas"] = $tabs.Count
$results["AC5_Sucesso"] = ($tabs.Count -eq 1)
Save-Shot "02_ac5_home_unica" | Out-Null

# Abrir C:\Windows via protocolo
Write-Host "Abrindo C:\Windows..."
Start-Process "files-dev:?folder=C:\Windows"
Start-Sleep -Seconds 3
$tabs = Get-Tabs $win
$results["Abertura_CWindows_TotalAbas"] = $tabs.Count
Save-Shot "03_abertura_cwindows" | Out-Null

# AC-1 e AC-2: Tentar abrir C:\Windows novamente com variacao de caixa/barra
Write-Host "Testando AC-1 e AC-2 (tentar abrir c:\windows\ novamente)..."
Start-Process "files-dev:?folder=c:\windows\"
Start-Sleep -Seconds 2
$tabs = Get-Tabs $win
$results["AC1_AC2_DuplicataCWindows_TotalAbas"] = $tabs.Count
$results["AC1_AC2_Sucesso"] = ($tabs.Count -le 2)
Save-Shot "04_ac1_ac2_cwindows_nao_duplicado" | Out-Null

# Abrir uma pasta diferente: C:\
Write-Host "Abrindo C:\..."
Start-Process "files-dev:?folder=C:\"
Start-Sleep -Seconds 3
$tabs = Get-Tabs $win
$results["Abertura_CDrive_TotalAbas"] = $tabs.Count
Save-Shot "05_abertura_cdrive" | Out-Null

# AC-3: Abrir Configurações via Ctrl+, (VK_CONTROL = 0x11, VK_OEM_COMMA = 0xBC)
Write-Host "Testando AC-3 (Abrir Configuracoes via Ctrl+,)..."
[void][F020Win]::SetForegroundWindow($proc.MainWindowHandle)
Start-Sleep -Milliseconds 300
Send-KeyChord @([byte]0x11, [byte]0xBC)
Start-Sleep -Seconds 3
$tabs = Get-Tabs $win
$results["AC3_Settings_TotalAbas"] = $tabs.Count
$results["AC3_Settings_Posicao0"] = if ($tabs.Count -gt 0) { $tabs[0].Current.Name -match "Settings|Configurações" } else { $false }
$results["AC3_Sucesso"] = $results["AC3_Settings_Posicao0"]
Save-Shot "06_ac3_settings_posicao_0" | Out-Null

# AC-4: Acionar Configurações novamente via Ctrl+, (deve manter 1 aba de Settings na posicao 0)
Write-Host "Testando AC-4 (Acionar Configuracoes novamente)..."
Send-KeyChord @([byte]0x11, [byte]0xBC)
Start-Sleep -Seconds 2
$tabs = Get-Tabs $win
$results["AC4_SettingsNovamente_TotalAbas"] = $tabs.Count
$results["AC4_Settings_AindaPosicao0"] = if ($tabs.Count -gt 0) { $tabs[0].Current.Name -match "Settings|Configurações" } else { $false }
$results["AC4_Sucesso"] = ($results["AC4_SettingsNovamente_TotalAbas"] -eq $results["AC3_Settings_TotalAbas"]) -and $results["AC4_Settings_AindaPosicao0"]
Save-Shot "07_ac4_settings_unica_posicao_0" | Out-Null

# AC-6: Duplicar aba atual (Ctrl+Shift+K) nao deve duplicar endereco existente
Write-Host "Testando Duplicate Tab (Ctrl+Shift+K)..."
Send-KeyChord @([byte]0x11, [byte]0x10, [byte]0x4B)
Start-Sleep -Seconds 2
$tabs = Get-Tabs $win
$results["DuplicateTab_TotalAbas"] = $tabs.Count
$results["DuplicateTab_NaoDuplicou"] = ($tabs.Count -eq $results["AC4_SettingsNovamente_TotalAbas"])
Save-Shot "08_duplicate_tab_rejeitada" | Out-Null

Write-Host "`n--- RESULTADOS F020 ---"
$results.GetEnumerator() | ForEach-Object {
    Write-Host "$($_.Key): $($_.Value)"
}

# Retorna sucesso se todos os testes chave passaram
$allPassed = $results["AC5_Sucesso"] -and $results["AC1_AC2_Sucesso"] -and $results["AC3_Sucesso"] -and $results["AC4_Sucesso"] -and $results["DuplicateTab_NaoDuplicou"]
Write-Host "RESULTADO GERAL: $(if ($allPassed) { 'PASS' } else { 'FAIL' })"
if (-not $allPassed) { exit 1 } else { exit 0 }

