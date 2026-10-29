# Test runner for verify_win_e_health.ps1 (F061: FR-005, AC-6, AC-8)
# Run from repository root: pwsh -File tests/test-verify-win-e-health.ps1

$scriptPath = Join-Path $PSScriptRoot "..\tools\perf\verify_win_e_health.ps1"
if (-not (Test-Path $scriptPath)) {
    Write-Error "Script not found at $scriptPath"
    exit 1
}

Write-Host "Executando suite de testes de decisao para verify_win_e_health.ps1..." -ForegroundColor Cyan
& $scriptPath -RunSelfTest
$testExitCode = $LASTEXITCODE

if ($testExitCode -ne 0) {
    Write-Error "Falha nos testes simulados de verify_win_e_health.ps1 (exit code $testExitCode)"
    exit 1
}

Write-Host "
Testando simulacao direta de 2 servicos via switch -Simulate (deve dar FALHA)..." -ForegroundColor Cyan
& $scriptPath -Simulate @{ AliveHotkeyCount = 2; AliveWatchdogCount = 1; TerminatedCount = 0; MutexExists = $true } -Quiet
$twoServicesCode = $LASTEXITCODE

if ($twoServicesCode -eq 1) {
    Write-Host "[PASS] Caso 2 servicos vivos gerou saida FALHA (exit code 1) conforme esperado." -ForegroundColor Green
} else {
    Write-Error "[FAIL] Caso 2 servicos vivos deveria ter retornado exit code 1, obteve $twoServicesCode"
    exit 1
}

Write-Host "
Testando simulacao direta de 1 servico e 0 watchdog via switch -Simulate (deve dar AVISO com exit code 0)..." -ForegroundColor Cyan
& $scriptPath -Simulate @{ AliveHotkeyCount = 1; AliveWatchdogCount = 0; TerminatedCount = 0; MutexExists = $true } -Quiet
$noWdCode = $LASTEXITCODE

if ($noWdCode -eq 0) {
    Write-Host "[PASS] Caso 0 watchdog gerou saida AVISO (exit code 0) conforme esperado." -ForegroundColor Green
} else {
    Write-Error "[FAIL] Caso 0 watchdog deveria ter retornado exit code 0, obteve $noWdCode"
    exit 1
}

Write-Host "
Testando simulacao direta de 1 servico e 1 watchdog via switch -Simulate (deve dar OK com exit code 0)..." -ForegroundColor Cyan
& $scriptPath -Simulate @{ AliveHotkeyCount = 1; AliveWatchdogCount = 1; TerminatedCount = 0; MutexExists = $true } -Quiet
$okCode = $LASTEXITCODE

if ($okCode -eq 0) {
    Write-Host "[PASS] Caso 1 servico e 1 watchdog gerou saida OK (exit code 0) conforme esperado." -ForegroundColor Green
} else {
    Write-Error "[FAIL] Caso 1 servico e 1 watchdog deveria ter retornado exit code 0, obteve $okCode"
    exit 1
}

Write-Host "
Todos os testes de verify_win_e_health foram concluidos com exito!" -ForegroundColor Green
exit 0
