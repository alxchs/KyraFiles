# G1 C2: abre o diálogo de colunas do Explorer ("More..." no menu do cabeçalho) e lista nomes e
# categorias como aparecem, para conferir o idioma. Captura opcional.
#
# Uso: pwsh tools\perf\g1c2_columns_language.ps1 [-Capture <nome>]

param([string]$Capture = '')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class G1C2 {
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
 [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint f, UIntPtr e);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
 [StructLayout(LayoutKind.Sequential)] public struct R { public int L,T,Rr,B; }
}
'@
$A = [System.Windows.Automation.AutomationElement]; $S = [System.Windows.Automation.TreeScope]
function Find-ByName($root, [string[]]$names, [int]$ms = 6000) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $ms) {
        $e = $root.FindAll($S::Descendants, [System.Windows.Automation.Condition]::TrueCondition) | Where-Object { $_.Current.Name -in $names -and -not $_.Current.IsOffscreen } | Select-Object -First 1
        if ($e) { return $e }
        Start-Sleep -Milliseconds 200
    }
}

$p = Get-Process Files | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
$win = $A::FromHandle($p.MainWindowHandle)
if ([G1C2]::GetForegroundWindow() -ne $p.MainWindowHandle) {
    [G1C2]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero); [G1C2]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    [G1C2]::SetForegroundWindow($p.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 300
    [G1C2]::keybd_event(0x1B, 0, 0, [UIntPtr]::Zero); [G1C2]::keybd_event(0x1B, 0, 2, [UIntPtr]::Zero)
}
Start-Sleep -Milliseconds 400

# Botão direito no cabeçalho "Nome"
$header = Find-ByName $win 'Name', 'Nome'
if (-not $header) { throw 'Cabeçalho Nome/Name não encontrado (o Files está em modo Detalhes?).' }
$rc = $header.Current.BoundingRectangle
[G1C2]::SetCursorPos([int]($rc.X + $rc.Width / 2), [int]($rc.Y + $rc.Height / 2)) | Out-Null
[G1C2]::mouse_event(0x0008, 0, 0, 0, [UIntPtr]::Zero); [G1C2]::mouse_event(0x0010, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep 1
$more = Find-ByName $win 'More...', 'Mais...'
if (-not $more) { throw 'Item More.../Mais... não encontrado no menu do cabeçalho.' }
$more.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep 2

$texts = $win.FindAll($S::Descendants, [System.Windows.Automation.PropertyCondition]::new($A::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)) |
    Where-Object { -not $_.Current.IsOffscreen } | ForEach-Object { $_.Current.Name }
$probe = 'Dimensions', 'Dimensões', 'Date taken', 'Data de captura', 'Image', 'Imagem', 'Media', 'Mídia', 'Authors', 'Autores'
"Visíveis no diálogo: $(($texts | Where-Object { $_ -in $probe } | Select-Object -Unique) -join ', ')"

if ($Capture) {
    $dir = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'docs\agents\evidence\g1c2'
    New-Item -ItemType Directory -Force $dir | Out-Null
    $r = New-Object G1C2+R; [G1C2]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
    $b = [System.Drawing.Bitmap]::new($r.Rr - $r.L, $r.B - $r.T); $g = [System.Drawing.Graphics]::FromImage($b)
    $h = $g.GetHdc(); [G1C2]::PrintWindow($p.MainWindowHandle, $h, 2) | Out-Null; $g.ReleaseHdc($h)
    $b.Save((Join-Path $dir "$Capture.png")); "CAPTURA $Capture.png"
}
# Fecha o diálogo sem aplicar
[G1C2]::keybd_event(0x1B, 0, 0, [UIntPtr]::Zero); [G1C2]::keybd_event(0x1B, 0, 2, [UIntPtr]::Zero)
