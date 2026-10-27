#requires -RunAsAdministrator
<#
  Liga o recurso "Windows Sandbox" (Containers-DisposableClientVM). Rode UMA vez, num PowerShell **como administrador**.
  Criado pelo Claude em 25/10/2026. Contexto e regras: C:\Users\alxch\.dev-method\SANDBOX.md (METODO §14).

  Uso:
    pwsh -NoProfile -File tools\perf\sandbox\Habilitar-WindowsSandbox.ps1            # liga e avisa para reiniciar
    pwsh -NoProfile -File tools\perf\sandbox\Habilitar-WindowsSandbox.ps1 -Reiniciar # liga e reinicia em 90 s
#>
param([switch]$Reiniciar)
$ErrorActionPreference = 'Stop'

$f = Get-WindowsOptionalFeature -Online -FeatureName 'Containers-DisposableClientVM'
Write-Host "Estado atual do recurso: $($f.State)" -ForegroundColor Cyan

if ($f.State -eq 'Enabled') {
  Write-Host "Já está ligado. Se WindowsSandbox.exe ainda não existe, reinicie o computador." -ForegroundColor Green
  Get-Command WindowsSandbox.exe -ErrorAction SilentlyContinue | ForEach-Object { "WindowsSandbox.exe: $($_.Source)" }
  return
}

# antes de reiniciar: mostra o que está aberto e pode perder trabalho
Write-Host "`nProcessos que podem ter trabalho não salvo (feche ou salve antes de reiniciar):" -ForegroundColor Yellow
Get-Process Code, agy, Antigravity, pwsh, powershell -ErrorAction SilentlyContinue |
  Where-Object { $_.Id -ne $PID } |
  Group-Object ProcessName | ForEach-Object { "  {0,-12} {1} processo(s)" -f $_.Name, $_.Count }
$ram = (Get-Counter '\Memory\Available MBytes').CounterSamples[0].CookedValue
Write-Host ("`nMemória disponível agora: {0:N0} MB (o Sandbox pede cerca de 4096 MB; feche programas para os testes)." -f $ram)

$r = Enable-WindowsOptionalFeature -Online -FeatureName 'Containers-DisposableClientVM' -All -NoRestart
Write-Host "`nRecurso ligado. Reinício necessário: $($r.RestartNeeded)" -ForegroundColor Green

if ($Reiniciar) {
  Write-Host "Reiniciando em 90 segundos (cancele com: shutdown /a)." -ForegroundColor Yellow
  shutdown /r /t 90 /c "Windows Sandbox habilitado: reinicio necessario"
} else {
  Write-Host "Reinicie quando puder. Depois do reinício, confirme com:" -ForegroundColor Cyan
  Write-Host "  Get-Command WindowsSandbox.exe"
  Write-Host "e peça ao Claude para rodar a prova P1 (tools\perf\sandbox\Test-SandboxP1.ps1)."
}
