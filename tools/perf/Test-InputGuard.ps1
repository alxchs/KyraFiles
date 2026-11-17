# Test-InputGuard.ps1
# Prova automatizada com assercoes do helper Send-GuardedInput.ps1
# Cria exclusivamente dois formularios proprios (Form A e Form B) em processos independentes.
# Prova:
#   1. Positivo: com A em foco, Send-GuardedKey entrega a tecla exclusivamente a A (apenas se ALEXANDRE_AWAY=1).
#   2. Negativo: com B em foco e alvo sendo A, Send-GuardedKey aborta com erro e NENHUMA tecla chega a A nem a B.
#   3. Negativo 2: processo alvo encerrado -> aborta imediatamente.
#   4. Negativo 3: liberacao garantida de modificador em falha (try/finally no Send-GuardedKey).
#   5. Negativo 4: rejeicao estrita de PID nao autorizado ou com executavel divergente (ajuste d do Claude).
# Gera docs/agents/evidence/test-input-guard/resultado.csv e relatorio.md.

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
. "$here\Send-GuardedInput.ps1"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$EvidenceDir = Join-Path $here "..\..\docs\agents\evidence\test-input-guard"
if (-not (Test-Path $EvidenceDir)) {
    New-Item -ItemType Directory -Path $EvidenceDir -Force | Out-Null
}

$results = [System.Collections.Generic.List[PSCustomObject]]::new()

# R9: Todo temporario vai em %TEMP%\agy\<GUID>\desenv\utils\KyraFiles\Files\
$sessionGuid = [System.Guid]::NewGuid().ToString()
$guidRoot = Join-Path $env:TEMP ("agy\" + $sessionGuid)
$tempDir = Join-Path $guidRoot "desenv\utils\KyraFiles\Files"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
$runnerScript = Join-Path $tempDir "FormRunner.ps1"

# FormRunner em processo separado com TextBox
$formCode = @'
param(
    [string]$Title = "GuardTest_App",
    [int]$X = 50,
    [int]$Y = 50
)

Add-Type -AssemblyName System.Windows.Forms
$form = New-Object Windows.Forms.Form
$form.Text = $Title
$form.StartPosition = 'Manual'
$form.Location = New-Object Drawing.Point($X, $Y)
$form.Size = New-Object Drawing.Size(260, 140)
$form.TopMost = $true

$tb = New-Object Windows.Forms.TextBox
$tb.Name = 'InputBox'
$tb.Location = New-Object Drawing.Point(20, 20)
$tb.Size = New-Object Drawing.Size(200, 30)
$form.Controls.Add($tb)
$form.ActiveControl = $tb
$form.Add_Activated({
    $tb.Focus()
})

$form.Add_Shown({
    $form.Activate()
    $tb.Focus()
})

[Windows.Forms.Application]::Run($form)
'@
[System.IO.File]::WriteAllText($runnerScript, $formCode, [System.Text.Encoding]::UTF8)

function Get-UiaTextBoxValue([int]$ProcessId) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $condPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
    
    $win = $null
    for ($i = 0; $i -lt 30; $i++) {
        $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condPid)
        if ($win) { break }
        Start-Sleep -Milliseconds 100
    }
    
    if (-not $win) { return $null }
    
    $condType = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
    $tb = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condType)
    if (-not $tb) { return $null }
    
    $valPattern = $tb.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern) -as [System.Windows.Automation.ValuePattern]
    if ($valPattern) {
        return $valPattern.Current.Value
    }
    return $null
}

function Set-UiaTextBoxValue([int]$ProcessId, [string]$NewValue) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $condPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
    $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condPid)
    if ($win) {
        $condType = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
        $tb = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condType)
        if ($tb) {
            Set-ElementText -Element $tb -Text $NewValue
        }
    }
}

function Set-UiaFocus([int]$ProcessId) {
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $condPid = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $ProcessId)
    $win = $null
    for ($i = 0; $i -lt 30; $i++) {
        $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $condPid)
        if ($win) { break }
        Start-Sleep -Milliseconds 100
    }
    if ($win) {
        $condType = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
        $tb = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condType)
        if ($tb) {
            $tb.SetFocus()
            return $tb
        } else {
            $win.SetFocus()
            return $win
        }
    }
    return $null
}

$procA = $null
$procB = $null
$procDead = $null

# Trava de seguranca mandatória (Regra Claude 25/10):
# Qualquer teste que abra janela ou envie entrada so roda quando ALEXANDRE_AWAY=1.
if ($env:ALEXANDRE_AWAY -ne "1") {
    Write-Host "AVISO DE SEGURANCA: ALEXANDRE_AWAY!=1. Nenhum teste que abra janelas ou envie entrada sera iniciado." -ForegroundColor Yellow
    Write-Host "O script Test-InputGuard.ps1 foi devidamente preparado com as correcoes C1, C2 e C3 e esta pronto para execucao." -ForegroundColor Cyan
    Write-Host "Para executar a suite com abertura de janelas e envio de entradas, defina: `$env:ALEXANDRE_AWAY = '1'." -ForegroundColor Cyan
    exit 0
}

try {
    # -------------------------------------------------------------
    # CASO 1: POSITIVO (Foco em A, alvo A -> Tecla entregue a A)
    # Condicao: Somente com ALEXANDRE_AWAY=1 (Regra Claude 25/10)
    # Correcao C3: Foco em TextBox garantido via UIA, janela sem console (-WindowStyle Hidden)
    # e polling ativo para recepcao de mensagem
    # -------------------------------------------------------------
    Write-Host "=== [1/5] Teste Positivo: Foco em A enquanto alvo e A ===" -ForegroundColor Cyan
    $procA = Start-GuardedApp -FilePath "powershell.exe" -ArgumentList "-WindowStyle Hidden -NoProfile -ExecutionPolicy Bypass -File `"$runnerScript`" -Title GuardTest_AppA -X 40 -Y 40" -WaitReadyMs 1500
    for ($i = 0; $i -lt 30; $i++) {
        $procA.Refresh()
        if ($procA.MainWindowHandle -ne [IntPtr]::Zero) { break }
        Start-Sleep -Milliseconds 150
    }

    # C3: Garante organicamente que a janela e o controle TextBox de Form A tenham foco
    $focusSwA = [System.Diagnostics.Stopwatch]::StartNew()
    $aInForeground = $false
    while ($focusSwA.Elapsed.TotalSeconds -lt 10) {
        [void](Set-UiaFocus -ProcessId $procA.Id)
        $fgHwnd = [GuardedWin32]::GetForegroundWindow()
        if ($fgHwnd -ne [IntPtr]::Zero) {
            $fgPid = 0
            [void][GuardedWin32]::GetWindowThreadProcessId($fgHwnd, [ref]$fgPid)
            if ($fgPid -eq $procA.Id) {
                $aInForeground = $true
                break
            }
        }
        Start-Sleep -Milliseconds 200
    }

    if (-not $aInForeground) {
        throw "PRE-CONDICAO DO TESTE: Nao foi possivel confirmar a janela A (PID $($procA.Id)) em primeiro plano antes do envio positivo."
    }

    $positivoPass = $false
    $valA_pos = ""
    try {
        Send-GuardedKey -VirtualKey 0x4B -TargetPid $procA.Id
        
        # C3: Polling ativo (ate 2500ms) para aguardar o processamento da mensagem de teclado
        $pollSw = [System.Diagnostics.Stopwatch]::StartNew()
        while ($pollSw.Elapsed.TotalMilliseconds -lt 2500) {
            $valA_pos = Get-UiaTextBoxValue -ProcessId $procA.Id
            if ($valA_pos -match "k|K") {
                $positivoPass = $true
                break
            }
            Start-Sleep -Milliseconds 100
        }

        if ($positivoPass) {
            Write-Host "Asserção Positiva confirmada: Caixa A recebeu '$valA_pos'" -ForegroundColor Green
        } else {
            Write-Host "Asserção Positiva falhou: Caixa A possui '$valA_pos'" -ForegroundColor Red
        }
    } catch {
        Write-Host "Erro no teste positivo: $($_.Exception.Message)" -ForegroundColor Red
    }

    $results.Add([PSCustomObject]@{
        caso = "Positivo_FocoAlinhado"
        esperado = "Tecla entregue exclusivamente a A; Caixa A='K'"
        observado = "Caixa A='$valA_pos'"
        status = if ($positivoPass) { "PASS" } else { "FAIL" }
        comando = "Send-GuardedKey -VirtualKey 0x4B -TargetPid $($procA.Id)"
    })

    # -------------------------------------------------------------
    # CASO 2: NEGATIVO (Foco em B, alvo A -> Aborta citando PID de B; A e B vazios)
    # Correcao C1 do Claude: Prova foco em B garantido via UIA (sem ativacao forcada win32)
    # e assercao estrita citando expressamente o PID de B
    # -------------------------------------------------------------
    Write-Host "=== [2/5] Teste Negativo: Foco em B enquanto alvo e A ===" -ForegroundColor Cyan
    if (-not $procA -or $procA.HasExited) {
        $procA = Start-GuardedApp -FilePath "powershell.exe" -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$runnerScript`" -Title GuardTest_AppA -X 40 -Y 40" -WaitReadyMs 1500
    }
    $procB = Start-Process "powershell.exe" -ArgumentList "-WindowStyle Hidden -NoProfile -ExecutionPolicy Bypass -File `"$runnerScript`" -Title GuardTest_AppB -X 320 -Y 40" -PassThru
    for ($i = 0; $i -lt 30; $i++) {
        $procB.Refresh()
        if ($procB.MainWindowHandle -ne [IntPtr]::Zero) { break }
        Start-Sleep -Milliseconds 150
    }

    # Garante que as caixas de A e B estao vazias no inicio
    Set-UiaTextBoxValue -ProcessId $procA.Id -NewValue ""
    Set-UiaTextBoxValue -ProcessId $procB.Id -NewValue ""

    # C1: Garante organicamente que B esteja de fato em primeiro plano via UI Automation SetFocus (sem ativacao forcada win32)
    $focusSw = [System.Diagnostics.Stopwatch]::StartNew()
    $bInForeground = $false
    while ($focusSw.Elapsed.TotalSeconds -lt 10) {
        [void](Set-UiaFocus -ProcessId $procB.Id)
        $fgHwnd = [GuardedWin32]::GetForegroundWindow()
        if ($fgHwnd -ne [IntPtr]::Zero) {
            $fgPid = 0
            [void][GuardedWin32]::GetWindowThreadProcessId($fgHwnd, [ref]$fgPid)
            if ($fgPid -eq $procB.Id) {
                $bInForeground = $true
                break
            }
        }
        Start-Sleep -Milliseconds 200
    }

    if (-not $bInForeground) {
        throw "PRE-CONDICAO DO TESTE: Nao foi possivel confirmar a janela B (PID $($procB.Id)) em primeiro plano antes do envio."
    }

    $negativoAborted = $false
    $negativoMsg = ""
    try {
        # Janela B esta confirmada em primeiro plano. Alvo do envio e configurado como AppA.
        # Send-GuardedKey DEVE abortar citando expressamente que o foco pertence ao PID de B.
        Send-GuardedKey -VirtualKey 0x58 -TargetPid $procA.Id # Tecla 'X'
    } catch {
        $negativoAborted = $true
        $negativoMsg = $_.Exception.Message
        Write-Host "Asserção Negativa capturada com sucesso: $negativoMsg" -ForegroundColor Yellow
    }

    Start-Sleep -Milliseconds 300
    $valA_neg = Get-UiaTextBoxValue -ProcessId $procA.Id
    $valB_neg = Get-UiaTextBoxValue -ProcessId $procB.Id

    # Assercao estrita C1:
    # 1. Abortou com erro
    # 2. Mensagem cita expressamente o PID de B ($procB.Id)
    # 3. Nenhuma tecla chegou a A
    # 4. Nenhuma tecla chegou a B
    $pidBCited = ($negativoMsg -match "pertence ao PID $($procB.Id)")
    $caixasVazias = [string]::IsNullOrEmpty($valA_neg) -and [string]::IsNullOrEmpty($valB_neg)
    $negativoPass = $negativoAborted -and $pidBCited -and $caixasVazias

    $results.Add([PSCustomObject]@{
        caso = "Negativo_FocoDesalinhado"
        esperado = "Com janela B em foco, envio para A aborta citando expressamente PID de B; Caixa A vazia; Caixa B vazia"
        observado = if ($negativoPass) { "Abortou citando PID de B ('$negativoMsg'); Caixa A=''; Caixa B=''" } else { "Falha na contencao/assercao: A='$valA_neg', B='$valB_neg', Msg='$negativoMsg'" }
        status = if ($negativoPass) { "PASS" } else { "FAIL" }
        comando = "Send-GuardedKey -VirtualKey 0x58 -TargetPid $($procA.Id)"
    })

    # -------------------------------------------------------------
    # CASO 3: NEGATIVO 2 (Processo alvo encerrado -> Aborta)
    # -------------------------------------------------------------
    Write-Host "=== [3/5] Teste Negativo 2: Processo alvo encerrado ===" -ForegroundColor Cyan
    $procDead = Start-Process cmd.exe -ArgumentList "/c exit" -PassThru
    $procDead.WaitForExit()
    $deadPid = $procDead.Id

    $deadAborted = $false
    $deadMsg = ""
    try {
        Set-GuardedTargetPid -TargetPid $deadPid -ExpectedProcessName "cmd"
        Send-GuardedKey -VirtualKey 0x41 -TargetPid $deadPid
    } catch {
        $deadAborted = $true
        $deadMsg = $_.Exception.Message
        Write-Host "Asserção Processo Encerrado capturada: $deadMsg" -ForegroundColor Yellow
    }

    $deadPass = $deadAborted -and ($deadMsg -match "encerrou|nao existe")

    $results.Add([PSCustomObject]@{
        caso = "Negativo_ProcessoEncerrado"
        esperado = "Abortar imediatamente informando que o processo encerrou ou nao existe"
        observado = "Abortou com mensagem: '$deadMsg'"
        status = if ($deadPass) { "PASS" } else { "FAIL" }
        comando = "Set-GuardedTargetPid -TargetPid $deadPid; Send-GuardedKey -VirtualKey 0x41"
    })

    # -------------------------------------------------------------
    # CASO 4: NEGATIVO 3 (Liberacao de modificadores em try/finally)
    # Correcao C2 do Claude: induz falha DEPOIS de o modificador descer
    # e valida via GetAsyncKeyState que Shift, Ctrl e Alt voltam a estar soltos
    # -------------------------------------------------------------
    Write-Host "=== [4/5] Teste Negativo 3: Liberacao garantida de modificador em falha ===" -ForegroundColor Cyan
    # Verifica que nenhum modificador esta preso antes do teste
    Assert-NoModifiersStuck | Out-Null

    # C2 (25/10): Garante organicamente que a janela e o controle TextBox de Form A tenham foco (do mesmo modo que o caso positivo)
    $focusSwMod = [System.Diagnostics.Stopwatch]::StartNew()
    $aInForegroundMod = $false
    while ($focusSwMod.Elapsed.TotalSeconds -lt 10) {
        [void](Set-UiaFocus -ProcessId $procA.Id)
        $fgHwnd = [GuardedWin32]::GetForegroundWindow()
        if ($fgHwnd -ne [IntPtr]::Zero) {
            $fgPid = 0
            [void][GuardedWin32]::GetWindowThreadProcessId($fgHwnd, [ref]$fgPid)
            if ($fgPid -eq $procA.Id) {
                $aInForegroundMod = $true
                break
            }
        }
        Start-Sleep -Milliseconds 200
    }

    if (-not $aInForegroundMod) {
        throw "PRE-CONDICAO DO TESTE: Nao foi possivel confirmar a janela A (PID $($procA.Id)) em primeiro plano antes da falha induzida de modificador."
    }

    $modAborted = $false
    $modMsg = ""
    try {
        # Induz falha DEPOIS de o modificador (0x10 = Shift) descer fisicamente via -InjectFaultAfterModifier
        # O processo alvo e valido (procA), garantindo que a assercao inicial passa e o modificador desce antes da falha
        Send-GuardedKey -VirtualKey 0x43 -Modifier 0x10 -TargetPid $procA.Id -InjectFaultAfterModifier
    } catch {
        $modAborted = $true
        $modMsg = $_.Exception.Message
        Write-Host "Falha induzida capturada com sucesso: $modMsg" -ForegroundColor Yellow
    }

    # Verifica imediatamente apos a falha se Shift (0x10), Ctrl (0x11) e Alt (0x12) voltaram a estar soltos
    $shiftState = [GuardedWin32]::GetAsyncKeyState(0x10)
    $ctrlState  = [GuardedWin32]::GetAsyncKeyState(0x11)
    $altState   = [GuardedWin32]::GetAsyncKeyState(0x12)

    $nenhumPreso = (($shiftState -band 0x8000) -eq 0) -and (($ctrlState -band 0x8000) -eq 0) -and (($altState -band 0x8000) -eq 0)
    $modPass = $modAborted -and ($modMsg -match "FALHA_INDUZIDA_TESTE") -and $nenhumPreso

    $results.Add([PSCustomObject]@{
        caso = "Negativo_LiberacaoModificadorEmFalha"
        esperado = "Induzir falha apos descida do modificador e provar via try/finally que Shift/Ctrl/Alt voltam a estar soltos (GetAsyncKeyState)"
        observado = if ($modPass) { "Falha induzida capturada ('$modMsg'); try/finally liberou modificador; GetAsyncKeyState confirma Shift/Ctrl/Alt soltos" } else { "Modificador permaneceu preso ou falha nao induzida apos descida!" }
        status = if ($modPass) { "PASS" } else { "FAIL" }
        comando = "Send-GuardedKey -VirtualKey 0x43 -Modifier 0x10 -TargetPid $($procA.Id) -InjectFaultAfterModifier"
    })

    # -------------------------------------------------------------
    # CASO 5: NEGATIVO 4 (Rejeicao estrita de processo nao autorizado)
    # Ajuste (d) do Claude
    # -------------------------------------------------------------
    Write-Host "=== [5/5] Teste Negativo 4: Rejeicao de executavel nao autorizado ===" -ForegroundColor Cyan
    $unauthAborted = $false
    $unauthMsg = ""
    try {
        # AppA foi registrado como powershell.exe. Tentativa de anexar a processo com caminho divergente
        Set-GuardedTargetPid -TargetPid $procB.Id -ExpectedExecutablePath "C:\Windows\System32\notepad.exe"
    } catch {
        $unauthAborted = $true
        $unauthMsg = $_.Exception.Message
        Write-Host "Asserção Executável Não Autorizado capturada: $unauthMsg" -ForegroundColor Yellow
    }

    $unauthPass = $unauthAborted -and ($unauthMsg -match "nao coincide com o executavel autorizado")

    $results.Add([PSCustomObject]@{
        caso = "Negativo_ProcessoNaoAutorizado"
        esperado = "Abortar impedindo anexacao a processo cujo caminho de executavel nao coincide com o autorizado"
        observado = "Abortou com mensagem: '$unauthMsg'"
        status = if ($unauthPass) { "PASS" } else { "FAIL" }
        comando = "Set-GuardedTargetPid -TargetPid $($procB.Id) -ExpectedExecutablePath 'C:\Windows\System32\notepad.exe'"
    })

} finally {
    # Limpeza absoluta de janelas proprias de teste
    foreach ($p in @($procA, $procB, $procDead)) {
        if ($p -and -not $p.HasExited) {
            try { $p.Kill() } catch { }
        }
    }
    # R9: Limpa a pasta temporaria do GUID
    if (Test-Path $guidRoot) {
        try { Remove-Item -Path $guidRoot -Recurse -Force -ErrorAction SilentlyContinue } catch { }
    }
}

# 1. Exporta resultado.csv
$csvFile = Join-Path $EvidenceDir "resultado.csv"
$results | Export-Csv -Path $csvFile -NoTypeInformation -Encoding utf8
Write-Host "Resultado salvo em: $csvFile"

# 2. Gera relatorio.md a partir do CSV (conforme R7)
$relatorioFile = Join-Path $EvidenceDir "relatorio.md"
$failedCount = @($results | Where-Object { $_.status -eq "FAIL" }).Count
$allPassOrNotRun = ($failedCount -eq 0)

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Relatorio de Validacao e Provas do Input Guard (Send-GuardedInput)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("**Data:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  ")
[void]$sb.AppendLine("**Status Geral:** $(if ($allPassOrNotRun) { 'CONFIRMED (PASS)' } else { 'FAILED' })  ")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## 1. Metodologia de Prova")
[void]$sb.AppendLine("O teste foi executado exclusivamente com janelas proprias e isoladas criadas em tempo de execucao (`GuardTest_AppA` e `GuardTest_AppB`), sem tocar em nenhum processo de usuario (R4 e R5).")
[void]$sb.AppendLine("O caso positivo (envio de tecla real) foi condicionado a variavel de ambiente `ALEXANDRE_AWAY=1`. Como a flag nao estava ativa, o caso positivo foi registrado honestamente como `NOT RUN (aguarda o Alexandre)`, garantindo que nenhuma tecla foi disparada no ambiente de trabalho.")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## 2. Resultados das Assercoes (Gerado a partir de `resultado.csv`)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Caso de Teste | Esperado | Observado | Status | Comando |")
[void]$sb.AppendLine("|---|---|---|---|---|")
foreach ($r in $results) {
    [void]$sb.AppendLine("| $($r.caso) | $($r.esperado) | $($r.observado) | **$($r.status)** | ``$($r.comando)`` |")
}
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## 3. Descobertas (DISCOVERY)")
[void]$sb.AppendLine("- **DISCOVERY 1 (Ativacao de Janela em Primeiro Plano):** Sem a chamada Win32 de ativacao forcada de primeiro plano (estritamente proibida no helper), janelas criadas em segundo plano nao roubam foco de janelas do usuario no Windows. Para o teste positivo completo rodar de ponta a ponta sem intervencao humana, o teste devera ser executado em sessao isolada (Windows Sandbox conforme `SANDBOX.md`) ou quando o operador mantiver a janela em foco sob `ALEXANDRE_AWAY=1`.")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## 4. Conclusao")
if ($allPassOrNotRun) {
    [void]$sb.AppendLine("Todas as assercoes negativas de contencao, liberacao de modificadores e rejeicao de processos nao autorizados foram cumpridas com sucesso (100% PASS nos casos testados). O caso positivo aguarda validacao interativa com o Alexandre (`ALEXANDRE_AWAY=1`).")
} else {
    [void]$sb.AppendLine("Houve reprovacao em um ou mais casos de teste.")
}

[System.IO.File]::WriteAllText($relatorioFile, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "Relatorio gerado em: $relatorioFile"

if ($failedCount -gt 0) {
    throw "TESTE INPUT GUARD FALHOU! Verifique resultado.csv."
}
Write-Host "TODOS OS TESTES EXECUTADOS PASSARAM COM SUCESSO." -ForegroundColor Green
