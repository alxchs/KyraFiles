# Prepara uma janela para as medicoes de tecla segurada: traz para a frente (UI Automation), encaixa na tela
# e clica numa linha da lista para o foco ficar nela. Nada e enviado se a janela nao estiver em primeiro plano.
# Uso: pwsh -File prep_window.ps1 -WindowClass CabinetWClass [-NameLike '*1600*'] [-DetailsView]
#      pwsh -File prep_window.ps1 -ProcessName Files
param([string]$WindowClass, [string]$NameLike = '*', [string]$ProcessName, [switch]$DetailsView, [int]$Width = 1600)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type @"
using System; using System.Runtime.InteropServices;
public class PrepWin {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int hh, bool r);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, int x, int y, uint d, UIntPtr e);
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
  [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
  [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h, uint f);
}
"@
[PrepWin]::SetProcessDPIAware() | Out-Null
if ($WindowClass) {
    $cond = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::ClassNameProperty, $WindowClass)
    $el = [Windows.Automation.AutomationElement]::RootElement.FindAll([Windows.Automation.TreeScope]::Children, $cond) | Where-Object { $_.Current.Name -like $NameLike } | Select-Object -First 1
} else {
    $p = Get-Process $ProcessName | Where-Object MainWindowHandle -ne 0 | Select-Object -First 1
    $el = [Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
}
if (-not $el) { throw 'janela nao encontrada' }
$h = [IntPtr]$el.Current.NativeWindowHandle
$wa = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
[PrepWin]::ShowWindow($h, 9) | Out-Null
[PrepWin]::MoveWindow($h, $wa.X + 40, $wa.Y + 20, [math]::Min($Width, $wa.Width - 80), $wa.Height - 40, $true) | Out-Null
try { $el.SetFocus() } catch {}; [PrepWin]::SetForegroundWindow($h) | Out-Null; Start-Sleep -Milliseconds 600
if ([PrepWin]::GetForegroundWindow() -ne $h) { throw 'a janela nao veio para o primeiro plano; nada enviado' }
if ($DetailsView) { [System.Windows.Forms.SendKeys]::SendWait('^+6'); Start-Sleep -Milliseconds 800 }
$r = New-Object PrepWin+RECT; [PrepWin]::GetWindowRect($h, [ref]$r) | Out-Null
if ([PrepWin]::GetForegroundWindow() -ne $h) { throw 'a janela perdeu o primeiro plano; clique nao enviado' }
$pt = New-Object PrepWin+POINT; $pt.X = $r.L + [int](($r.R - $r.L) * 0.6); $pt.Y = $r.T + [int](($r.B - $r.T) * 0.45)
# o ponto do clique tem de pertencer a janela medida (outra janela por cima, ou sempre-no-topo, recebe o clique)
if ([PrepWin]::GetAncestor([PrepWin]::WindowFromPoint($pt), 2) -ne $h) { throw "o ponto do clique pertence a outra janela; clique nao enviado" }
[PrepWin]::SetCursorPos($pt.X, $pt.Y) | Out-Null
[PrepWin]::mouse_event(2, 0, 0, 0, [UIntPtr]::Zero); [PrepWin]::mouse_event(4, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 500
[PrepWin]::SetCursorPos($r.R - 20, $r.T + 15) | Out-Null   # ponteiro sobre a propria barra de titulo, longe das linhas
'{0} | foco: {1}' -f $el.Current.Name, [Windows.Automation.AutomationElement]::FocusedElement.Current.Name
