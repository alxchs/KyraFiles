# F012 / AC-3 e AC-10: abre localizações pelos mesmos caminhos que outros programas usam e conta
# quem mostrou o resultado (Files ou Explorer). "Mostrar na pasta" dos navegadores chama
# SHOpenFolderAndSelectItems; pastas especiais vêm de explorer.exe shell:<nome>.
#
# Uso: pwsh tools\perf\f012_show_in_folder.ps1
# Temporários em %TEMP%\claude\desenv\utils\FilesApp\f012 show.

$ErrorActionPreference = 'Stop'
$folder = Join-Path $env:TEMP 'claude\desenv\utils\FilesApp\f012 show\pasta ação'
New-Item -ItemType Directory -Force $folder | Out-Null
$file = Join-Path $folder 'baixado ação.txt'
Set-Content -LiteralPath $file -Value 'AC-3' -Encoding utf8
$log = "$env:LOCALAPPDATA\Packages\FilesDev_ykqwq8d6ps0ag\LocalState\debug.log"

Add-Type @'
using System; using System.Runtime.InteropServices;
public static class F012Show {
 [DllImport("shell32.dll", CharSet = CharSet.Unicode)] static extern int SHParseDisplayName(string name, IntPtr bc, out IntPtr pidl, uint sfgaoIn, out uint sfgaoOut);
 [DllImport("shell32.dll")] static extern int SHOpenFolderAndSelectItems(IntPtr folder, uint count, IntPtr[] items, uint flags);
 [DllImport("ole32.dll")] static extern void CoTaskMemFree(IntPtr p);
 [DllImport("ole32.dll")] static extern int CoInitializeEx(IntPtr r, uint c);
 // Como os navegadores fazem: a pasta-mãe e o arquivo a selecionar
 public static int ShowInFolder(string folder, string file) {
  CoInitializeEx(IntPtr.Zero, 2);
  IntPtr pf, pi; uint o;
  int hr = SHParseDisplayName(folder, IntPtr.Zero, out pf, 0, out o); if (hr < 0) return hr;
  hr = SHParseDisplayName(file, IntPtr.Zero, out pi, 0, out o); if (hr < 0) { CoTaskMemFree(pf); return hr; }
  hr = SHOpenFolderAndSelectItems(pf, 1, new[] { pi }, 0);
  CoTaskMemFree(pi); CoTaskMemFree(pf);
  return hr;
 }
}
'@
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$A = [System.Windows.Automation.AutomationElement]
function Get-ExplorerWindows { @($A::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.PropertyCondition]::new($A::ClassNameProperty, 'CabinetWClass'))) }

function Test-Open([string]$label, [scriptblock]$action, [string]$expect) {
    $logStart = (Get-Item $log).Length
    & $action
    Start-Sleep 7
    $fs = [IO.File]::Open($log, 'Open', 'Read', 'ReadWrite'); $null = $fs.Seek($logStart, 'Begin')
    $newLog = [IO.StreamReader]::new($fs).ReadToEnd(); $fs.Close()
    $lines = @($newLog -split "`r?`n" | Where-Object { $_ -match 'Explorer window' } | ForEach-Object { ($_ -split '\|', 3)[2] })
    $explorer = Get-ExplorerWindows | ForEach-Object { $_.Current.Name }
    $files = (Get-Process Files -ErrorAction SilentlyContinue | Where-Object MainWindowTitle | Select-Object -First 1).MainWindowTitle
    "$label | esperado: $expect | Files: '$files' | Explorer aberto: '$($explorer -join "', '")' | log: $($lines -join ' / ')"
    Get-ExplorerWindows | ForEach-Object { $_.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close() }
    Start-Sleep 1
}

Test-Open 'AC-3 Mostrar na pasta (SHOpenFolderAndSelectItems)' { "hr=$([F012Show]::ShowInFolder($folder, $file))" | Out-Null } 'Files, arquivo selecionado'
Test-Open 'AC-10 Lixeira' { Start-Process explorer.exe 'shell:RecycleBinFolder' } 'Files (Lixeira)'
Test-Open 'AC-10 Este Computador' { Start-Process explorer.exe 'shell:MyComputerFolder' } 'Files (This PC)'
Test-Open 'AC-10 Rede' { Start-Process explorer.exe 'shell:NetworkPlacesFolder' } 'Files (Network)'
Test-Open 'AC-10 Painel de Controle' { Start-Process control.exe } 'Explorer'
Test-Open 'AC-10 Bibliotecas' { Start-Process explorer.exe 'shell:Libraries' } 'Explorer'
