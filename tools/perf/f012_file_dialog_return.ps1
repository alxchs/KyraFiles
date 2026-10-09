# F012 / T7: abre o diálogo "Abrir" (ou "Salvar") do Windows num processo separado, espera o Files
# aparecer como diálogo, seleciona o arquivo de teste por UI Automation, fecha a janela e confere
# o caminho devolvido ao app chamador.
#
# Uso: pwsh tools\perf\f012_file_dialog_return.ps1 [-Mode Open|Save] [-Capture <nome>]

param(
    [ValidateSet('Open', 'Save')] [string]$Mode = 'Open',
    [string]$Capture = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class F012Dlg {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
'@

$A = [System.Windows.Automation.AutomationElement]
$Scope = [System.Windows.Automation.TreeScope]

# Temporários do projeto ficam em %TEMP%\claude\<caminho do projeto>, fáceis de limpar
$work = Join-Path $env:TEMP 'claude\desenv\utils\FilesApp\f012 diálogo ação'
New-Item -ItemType Directory -Force $work | Out-Null
$target = Join-Path $work 'arquivo de teste ação.txt'
Set-Content -LiteralPath $target -Value 'F012 T7' -Encoding utf8
$result = Join-Path $work 'resultado.txt'
Remove-Item -LiteralPath $result -ErrorAction SilentlyContinue

# Cliente: processo separado, como um app comum faria
$client = @"
Add-Type -AssemblyName System.Windows.Forms
`$d = if ('$Mode' -eq 'Open') { [System.Windows.Forms.OpenFileDialog]::new() } else { [System.Windows.Forms.SaveFileDialog]::new() }
`$d.InitialDirectory = '$work'
`$d.AutoUpgradeEnabled = `$true
`$r = `$d.ShowDialog()
Set-Content -LiteralPath '$result' -Value ("`$r|" + `$d.FileName) -Encoding utf8
"@
$before = @(Get-Process Files -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
$sw = [Diagnostics.Stopwatch]::StartNew()
# -EncodedCommand: Start-Process tira as aspas duplas de -Command
$encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($client))
$cp = Start-Process pwsh -ArgumentList '-NoProfile', '-STA', '-EncodedCommand', $encoded -PassThru -WindowStyle Hidden

# Espera a instância nova do Files (o diálogo sempre abre instância nova)
$proc = $null
while ($sw.Elapsed.TotalSeconds -lt 30 -and -not $proc) {
    Start-Sleep -Milliseconds 300
    $proc = Get-Process Files -ErrorAction SilentlyContinue | Where-Object { $before -notcontains $_.Id -and $_.MainWindowHandle -ne 0 } | Select-Object -First 1
}
if (-not $proc) { $cp | Stop-Process -ErrorAction SilentlyContinue; throw 'O Files não abriu como diálogo em 30 s.' }
"Files como diálogo: PID $($proc.Id) em $([int]$sw.Elapsed.TotalMilliseconds) ms"
$win = $A::FromHandle($proc.MainWindowHandle)

# Item do arquivo na lista
$name = [IO.Path]::GetFileName($target)
$item = $null
while ($sw.Elapsed.TotalSeconds -lt 45 -and -not $item) {
    Start-Sleep -Milliseconds 300
    # O item da lista se chama "<nome>, <tipo>"
    $item = $win.FindAll($Scope::Descendants, [System.Windows.Automation.PropertyCondition]::new($A::ControlTypeProperty, [System.Windows.Automation.ControlType]::ListItem)) |
        Where-Object { $_.Current.Name -eq $name -or $_.Current.Name.StartsWith("$name, ") } | Select-Object -First 1
}
if (-not $item) { throw "Arquivo '$name' não apareceu no Files (a pasta inicial não é '$work'?)." }
$item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
Start-Sleep -Seconds 1

if ($Capture) {
    $dir = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'docs\agents\evidence\f012'
    $r = New-Object F012Dlg+RECT
    [F012Dlg]::GetWindowRect($proc.MainWindowHandle, [ref]$r) | Out-Null
    $bmp = [System.Drawing.Bitmap]::new($r.R - $r.L, $r.B - $r.T)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [F012Dlg]::PrintWindow($proc.MainWindowHandle, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc)
    $bmp.Save((Join-Path $dir "$Capture.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    "CAPTURA $(Join-Path $dir "$Capture.png")"
}

# Fechar a janela devolve a seleção ao app chamador. WindowPattern.Close não fecha a janela
# do WinUI; o botão Fechar da barra de título fecha.
$close = $win.FindFirst($Scope::Descendants, [System.Windows.Automation.PropertyCondition]::new($A::AutomationIdProperty, 'Close'))
$sw.Restart()
$close.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
if (-not $cp.WaitForExit(40000)) { $cp | Stop-Process; throw 'O app chamador não recebeu retorno em 40 s.' }
"Retorno ao app chamador em $([int]$sw.Elapsed.TotalMilliseconds) ms após fechar"
$got = (Get-Content -LiteralPath $result -Raw).Trim()
"RETORNO $got"
if ($got -eq "OK|$target") { 'PASS: caminho devolvido igual ao selecionado' } else { "FAIL: esperado 'OK|$target'"; exit 1 }
