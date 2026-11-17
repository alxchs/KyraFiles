# tools/perf/verify_win_e_health.ps1
# Verificacao de saude do Win+E e supervisao Watchdog (FR-005, AC-6, AC-8 / F061)
# Sai com codigo 0 se OK, ou 1 se FALHA.
# OK = exatamente 1 KyraFiles.HotkeyService vivo, no maximo 1 KyraFiles.Watchdog vivo,
#      menos de 5 processos terminados ainda listados e mutex de instancia existente.

[CmdletBinding()]
param(
    [switch]$RunSelfTest,
    [hashtable]$Simulate = $null,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

function Evaluate-WinEHealth {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$AliveHotkeyCount,

        [Parameter(Mandatory = $true)]
        [int]$AliveWatchdogCount,

        [Parameter(Mandatory = $true)]
        [int]$TerminatedCount,

        [Parameter(Mandatory = $true)]
        [bool]$MutexExists
    )

    $reasons = [System.Collections.Generic.List[string]]::new()

    # 1. Exatamente 1 servico KyraFiles.HotkeyService vivo
    if ($AliveHotkeyCount -ne 1) {
        $reasons.Add("KyraFiles.HotkeyService vivos: $AliveHotkeyCount (esperado: exatamente 1)")
    }

    # 2. No maximo 1 KyraFiles.Watchdog vivo (0 ou 1 aceito)
    if ($AliveWatchdogCount -gt 1) {
        $reasons.Add("KyraFiles.Watchdog vivos: $AliveWatchdogCount (esperado: no maximo 1)")
    }

    # 3. Menos de 5 processos terminados listados
    if ($TerminatedCount -ge 5) {
        $reasons.Add("Processos terminados listados: $TerminatedCount (esperado: < 5)")
    }

    # 4. Mutex Global\KyraFiles_HotkeyService_InstanceMutex existe
    if (-not $MutexExists) {
        $reasons.Add("Mutex Global\KyraFiles_HotkeyService_InstanceMutex nao encontrado")
    }

    $isFailure = ($reasons.Count -gt 0)
    $isWarning = (-not $isFailure -and $AliveWatchdogCount -eq 0)
    $isOk = (-not $isFailure -and $AliveWatchdogCount -eq 1)

    $status = if ($isFailure) {
        "FALHA"
    } elseif ($isWarning) {
        "AVISO"
    } else {
        "OK"
    }

    return [PSCustomObject]@{
        IsOk = $isOk
        IsWarning = $isWarning
        Status = $status
        ExitCode = if ($isFailure) { 1 } else { 0 }
        Reasons = $reasons
    }
}

function Invoke-SelfTests {
    Write-Host "=================================================================" -ForegroundColor Cyan
    Write-Host "  TESTES SIMULADOS DE DECISAO: verify_win_e_health (F061 / FR-005) " -ForegroundColor Cyan
    Write-Host "=================================================================" -ForegroundColor Cyan

    $testCases = @(
        @{
            Name = "Cenario sem supervisor (1 servico, 0 watchdog, 0 terminados, mutex OK)"
            Input = @{ AliveHotkeyCount = 1; AliveWatchdogCount = 0; TerminatedCount = 0; MutexExists = $true }
            ExpectedStatus = "AVISO"
            ExpectedExitCode = 0
        },
        @{
            Name = "Cenario padrao supervisionado (1 servico, 1 watchdog, 0 terminados, mutex OK)"
            Input = @{ AliveHotkeyCount = 1; AliveWatchdogCount = 1; TerminatedCount = 0; MutexExists = $true }
            ExpectedStatus = "OK"
            ExpectedExitCode = 0
        },
        @{
            Name = "Cenario 2 servicos vivos (anomalia de duplicacao / loop)"
            Input = @{ AliveHotkeyCount = 2; AliveWatchdogCount = 1; TerminatedCount = 0; MutexExists = $true }
            ExpectedStatus = "FALHA"
            ExpectedExitCode = 1
        },
        @{
            Name = "Cenario 0 servicos vivos (servico caiu e nao voltou)"
            Input = @{ AliveHotkeyCount = 0; AliveWatchdogCount = 1; TerminatedCount = 0; MutexExists = $false }
            ExpectedStatus = "FALHA"
            ExpectedExitCode = 1
        },
        @{
            Name = "Cenario 2 watchdogs vivos (duplicacao do supervisor)"
            Input = @{ AliveHotkeyCount = 1; AliveWatchdogCount = 2; TerminatedCount = 0; MutexExists = $true }
            ExpectedStatus = "FALHA"
            ExpectedExitCode = 1
        },
        @{
            Name = "Cenario acumulacao de processos terminados (>= 5 terminados)"
            Input = @{ AliveHotkeyCount = 1; AliveWatchdogCount = 1; TerminatedCount = 13000; MutexExists = $true }
            ExpectedStatus = "FALHA"
            ExpectedExitCode = 1
        },
        @{
            Name = "Cenario mutex ausente (hook desinstalado ou mutex perdido)"
            Input = @{ AliveHotkeyCount = 1; AliveWatchdogCount = 1; TerminatedCount = 0; MutexExists = $false }
            ExpectedStatus = "FALHA"
            ExpectedExitCode = 1
        }
    )

    $failed = 0
    foreach ($tc in $testCases) {
        $inputHash = $tc.Input
        $res = Evaluate-WinEHealth @inputHash
        $pass = ($res.Status -eq $tc.ExpectedStatus -and $res.ExitCode -eq $tc.ExpectedExitCode)
        if ($pass) {
            Write-Host "  [PASS] $($tc.Name) -> $($res.Status) (ExitCode $($res.ExitCode))" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] $($tc.Name) -> Obteve $($res.Status) (ExitCode $($res.ExitCode)), Esperava $($tc.ExpectedStatus) (ExitCode $($tc.ExpectedExitCode))" -ForegroundColor Red
            if ($res.Reasons.Count -gt 0) {
                Write-Host "         Motivos: $($res.Reasons -join '; ')" -ForegroundColor Yellow
            }
            $failed++
        }
    }

    Write-Host "=================================================================" -ForegroundColor Cyan
    if ($failed -eq 0) {
        Write-Host "SUCESSO: Todos os $($testCases.Count) casos de teste simulados passaram!" -ForegroundColor Green
        return 0
    } else {
        Write-Host "FALHA: $failed de $($testCases.Count) casos de teste falharam." -ForegroundColor Red
        return 1
    }
}

if ($RunSelfTest) {
    $exitCode = Invoke-SelfTests
    exit $exitCode
}

# Dados de execucao (reais ou simulados via -Simulate)
$aliveHotkeys = @()
$aliveWatchdogs = @()
$totalTerminated = 0
$mutexExists = $false
$serviceAgeStr = "N/A"
$serviceStartTimeStr = "N/A"

if ($Simulate) {
    $aliveHotkeyCount = if ($Simulate.ContainsKey('AliveHotkeyCount')) { [int]$Simulate['AliveHotkeyCount'] } else { 0 }
    $aliveWatchdogCount = if ($Simulate.ContainsKey('AliveWatchdogCount')) { [int]$Simulate['AliveWatchdogCount'] } else { 0 }
    $totalTerminated = if ($Simulate.ContainsKey('TerminatedCount')) { [int]$Simulate['TerminatedCount'] } else { 0 }
    $mutexExists = if ($Simulate.ContainsKey('MutexExists')) { [bool]$Simulate['MutexExists'] } else { $false }
    $serviceAgeStr = if ($Simulate.ContainsKey('ServiceAge')) { [string]$Simulate['ServiceAge'] } else { "simulado" }
    $serviceStartTimeStr = "simulado"
    $hotkeyPids = if ($aliveHotkeyCount -gt 0) { "(simulado: $aliveHotkeyCount)" } else { "nenhum" }
    $watchdogPids = if ($aliveWatchdogCount -gt 0) { "(simulado: $aliveWatchdogCount)" } else { "nenhum" }
} else {
    # Coleta de processos reais
    $hotkeyProcs = @(Get-Process -Name "KyraFiles.HotkeyService" -ErrorAction SilentlyContinue)
    $aliveHotkeys = @($hotkeyProcs | Where-Object { -not $_.HasExited })
    $termHotkeys = @($hotkeyProcs | Where-Object { $_.HasExited })

    $watchdogProcs = @(Get-Process -Name "KyraFiles.Watchdog" -ErrorAction SilentlyContinue)
    $aliveWatchdogs = @($watchdogProcs | Where-Object { -not $_.HasExited })
    $termWatchdogs = @($watchdogProcs | Where-Object { $_.HasExited })

    $aliveHotkeyCount = $aliveHotkeys.Count
    $aliveWatchdogCount = $aliveWatchdogs.Count
    $totalTerminated = $termHotkeys.Count + $termWatchdogs.Count

    # Checar mutex Global\KyraFiles_HotkeyService_InstanceMutex
    $mutexRef = [System.Threading.Mutex]$null
    try {
        $mutexExists = [System.Threading.Mutex]::TryOpenExisting("Global\KyraFiles_HotkeyService_InstanceMutex", [ref]$mutexRef)
        if (-not $mutexExists) {
            $mutexExists = [System.Threading.Mutex]::TryOpenExisting("Global\KyraFiles_HotkeyService_InstanceMutex", [ref]$mutexRef)
        }
    } catch {
        $mutexExists = $false
    } finally {
        if ($mutexRef) {
            $mutexRef.Dispose()
        }
    }

    # StartTime e idade do servico
    if ($aliveHotkeys.Count -gt 0) {
        try {
            $st = $aliveHotkeys[0].StartTime
            $serviceStartTimeStr = $st.ToString("dd/MM/yyyy HH:mm:ss")
            $diff = (Get-Date) - $st
            $serviceAgeStr = "{0:D2}d {1:D2}h {2:D2}m {3:D2}s" -f $diff.Days, $diff.Hours, $diff.Minutes, $diff.Seconds
        } catch {
            $serviceAgeStr = "desconhecido"
        }
        $hotkeyPids = ($aliveHotkeys | ForEach-Object { $_.Id }) -join ", "
    } else {
        $hotkeyPids = "nenhum"
    }

    if ($aliveWatchdogs.Count -gt 0) {
        $watchdogPids = ($aliveWatchdogs | ForEach-Object { $_.Id }) -join ", "
    } else {
        $watchdogPids = "nenhum"
    }
}

# Avaliacao de saude
$eval = Evaluate-WinEHealth -AliveHotkeyCount $aliveHotkeyCount `
                            -AliveWatchdogCount $aliveWatchdogCount `
                            -TerminatedCount $totalTerminated `
                            -MutexExists $mutexExists

# Saida formatada: Primeira linha com status claro [OK], [AVISO] ou [FALHA]
if ($eval.Status -eq "OK") {
    Write-Host "[OK] Win+E supervisionado e saudavel: $aliveHotkeyCount servico vivo, $aliveWatchdogCount Watchdog vivo, $totalTerminated terminados, mutex ativo (idade do servico: $serviceAgeStr)" -ForegroundColor Green
} elseif ($eval.Status -eq "AVISO") {
    Write-Host "[AVISO] Win+E sem supervisor: se o servico cair, nao volta sozinho ($aliveHotkeyCount servico vivo, 0 Watchdog vivo, $totalTerminated terminados, mutex ativo, idade do servico: $serviceAgeStr)" -ForegroundColor Yellow
} else {
    $reasonsStr = $eval.Reasons -join "; "
    Write-Host "[FALHA] Win+E com anomalia: $reasonsStr" -ForegroundColor Red
}

if (-not $Quiet) {
    Write-Host "Detalhes:" -ForegroundColor Gray
    Write-Host "  KyraFiles.HotkeyService vivos:     $aliveHotkeyCount (PIDs: $hotkeyPids)" -ForegroundColor Gray
    Write-Host "  KyraFiles.Watchdog vivos:          $aliveWatchdogCount (PIDs: $watchdogPids)" -ForegroundColor Gray
    Write-Host "  Processos terminados listados: $totalTerminated" -ForegroundColor Gray
    Write-Host "  Idade do servico:              $serviceAgeStr (StartTime: $serviceStartTimeStr)" -ForegroundColor Gray
    Write-Host "  Mutex de instancia:            $(if ($mutexExists) { 'Presente (ativo)' } else { 'AUSENTE' }) (Global\KyraFiles_HotkeyService_InstanceMutex)" -ForegroundColor Gray
    if ($eval.Status -eq "AVISO") {
        Write-Host "Aviso de resiliencia:" -ForegroundColor Yellow
        Write-Host "  - Win+E sem supervisor: se o servico cair, nao volta sozinho" -ForegroundColor Yellow
    } elseif ($eval.Status -eq "FALHA") {
        Write-Host "Motivos da reprovacao:" -ForegroundColor Yellow
        foreach ($r in $eval.Reasons) {
            Write-Host "  - $r" -ForegroundColor Yellow
        }
    }
}

exit $eval.ExitCode

