# Revisão do Claude — S5 da ordem rígida (test-input-guard)

Data: 25/10/2026. Revisor: Claude. Branch revisada: `feature/test-input-guard` (commits `8022e833c`, `7974c30ea`, `cc0c3b941`, `f0571def4`, `6bff9b121`). Checklist: `docs/agents/ORDEM-RIGIDA-AGY.md`.

## Veredito: ACEITA COM 3 CORREÇÕES. LIBERAÇÃO PARCIAL (ver §3).

## 1. Checklist (estado por item, com o comando que sustenta)

| # | Item | Estado | Evidência |
|---|---|---|---|
| 1 | `git grep` por `SendKeys|SendInput|keybd_event|mouse_event|SetCursorPos|SetForegroundWindow` em `tools/perf` fora de `_desativados/` só acha o helper e o verificador | **CONFIRMED** | `git grep -l -E '...' -- tools/perf ':!tools/perf/_desativados'` devolve só `Send-GuardedInput.ps1` e `Test-NoRawInput.ps1` |
| 2 | O verificador passa na árvore atual | **CONFIRMED** | `pwsh -File tools/perf/Test-NoRawInput.ps1` → `SUCESSO`, `exit 0` (executado pelo Claude, somente leitura) |
| 3 | O helper não chama nem cita `SetForegroundWindow` | **CONFIRMED** | `Select-String ... SetForegroundWindow` → 0 ocorrências em `Send-GuardedInput.ps1` |
| 4 | O diff não toca `src/`, `installer/` nem `docs/specs/` | **CONFIRMED** | `git diff --name-only main...feature/test-input-guard` → só `tools/perf`, `docs/agents/**`, `AGENT_CONTEXT.md` |
| 5 | Nenhum app alheio recebeu entrada | **INFERRED** | Os casos usam janelas próprias; o positivo ficou `NOT RUN`. Uma tentativa de anexar a um PowerShell real (PID 38244) foi só rejeitada, sem envio |
| 6 | Quarentena sem apagar | **CONFIRMED** | 57 scripts em `tools/perf/_desativados/*.desativado` via `git mv` |
| 7 | O positivo do helper entrega a tecla ao app de teste | **NOT TESTED** | Caso `NOT RUN (aguarda o Alexandre)`. **E o mesmo caso reprovou no 1º rodar** (`Caixa A=''`, 24/10 22:12): a causa nunca foi achada, o caso foi só marcado como não rodado |

## 2. Correções exigidas

**C1. `Negativo_FocoDesalinhado` passou pelo motivo errado.** O CSV diz: abortou com "Nenhuma janela em primeiro plano (GetForegroundWindow retornou 0 após 1 s)". O pedido era provar que, com a **janela B em foco**, o envio ao app A aborta e nenhuma das duas caixas recebe nada. O teste só provou o caso "sem janela em foco". Refazer com B de fato em primeiro plano (sem `SetForegroundWindow`).

**C2. `Negativo_LiberacaoModificadorEmFalha` não exercita o `try/finally`.** O teste chama `Send-GuardedKey -Modifier 0x10 -TargetPid 999999` (PID que não existe): a **primeira** asserção falha antes de qualquer tecla descer, então não havia modificador pressionado para liberar. O `GetAsyncKeyState` limpo ao fim não prova nada. É preciso induzir a falha **depois** de o modificador descer (por exemplo, um ponto de injeção de falha entre a descida do modificador e a tecla) e provar, no mesmo teste, que o modificador volta a estar solto.

**C3. Positivo sem causa raiz.** O primeiro rodar reprovou; a revisão mudou o caso para `NOT RUN` sem explicar a falha. Achar a causa (hipóteses a verificar por leitura de código: o formulário não era o foreground quando a tecla saiu; foco no controle `TextBox` ausente; a asserção passa mas a entrega usa o código de tecla errado; o `Test-InputGuard` compara a caixa antes de a mensagem ser processada) e deixar o caso **pronto para rodar com `ALEXANDRE_AWAY=1`**.

**C4. Documentação.** `medicao-postmessage.md` se contradiz: a tabela diz que `WM_KEYDOWN/UP` foi **aceito** (texto 'aa'); a análise diz que foi **ignorado**. Corrigir para o que a medição mostra e dizer por quê. O handoff descreve o `Negativo_FocoDesalinhado` como "foco fora do app alvo" sem registrar que foi "nenhuma janela em foco": corrigir a descrição.

**Observação (não bloqueia):** `Get-GuardedOwnedPids` considera só o processo e os **filhos diretos**; é falha segura (recusa), mas pode recusar um app legítimo. Registrar como limitação.

## 3. Liberação (regra do dia 25/10/2026)

**Regra nova, vale para toda automação:** qualquer teste que **abra janela** ou **envie entrada** só roda quando `ALEXANDRE_AWAY=1`, porque janelas novas também roubam o foco de quem está digitando.

**LIBERADO agora** (somente leitura, medição fora do app e correções de código sem rodar teste de janela):
- C1 a C4 (escrever as correções, **sem rodar** o teste de janelas).
- As tarefas **T0** (confirmação do plano no código, somente leitura) de F036, F037, F044, F045, F047, F048, F049 e F056. A T0 da F036 inclui medição com `robocopy` em `%TEMP%` (sem janela, sem entrada).

**CONTINUA BLOQUEADO até o Alexandre rodar a prova com `ALEXANDRE_AWAY=1` e eu revisar o resultado:** F035 (a T1 abre janelas), o retrabalho de evidências (R-retrabalho) e qualquer tarefa com interface.

## 4. Prova real com entrada (25/10/2026, Alexandre longe do teclado)
Executada pelo Claude (`docs/agents/evidence/test-input-guard/LEIA-ME.md`): **4 PASS e 1 FAIL**. O positivo passou (a caixa A recebeu `k`); o negativo de foco passou com a janela B **de fato** em primeiro plano, citando o PID de B; processo encerrado e executável não autorizado passaram. O **único FAIL** é `Negativo_LiberacaoModificadorEmFalha`, e a causa é do **teste**: B seguia em primeiro plano, o helper abortou na 1ª verificação (antes do Shift descer) e o ponto de falha induzida nunca foi alcançado. O helper tem `try/finally` correto pela leitura do código, mas o **comportamento ainda não está provado**. Teclado depois do teste: Shift, Ctrl e Alt soltos.

### Decisão de liberação (revisão 2)
- **Helper aceito para uso sem modificadores** (teclas simples, cliques dentro da janela, preenchimento por UIA).
- **Uso com modificador (Ctrl, Shift, Alt) segue bloqueado** até o teste C2 ser corrigido (trazer A para a frente antes de chamar o helper) e rodar com sucesso numa janela de AWAY.
- **Protocolo de janela de AWAY:** qualquer teste que abra janela ou envie entrada roda **só** quando o Alexandre disser que vai sair ("saindo por N minutos"). Nesse momento o Claude dispara a AGY com `ALEXANDRE_AWAY=1` no ambiente e um **orçamento de tempo** (a AGY para os testes de janela ao fim do orçamento). Fora da janela, a AGY trabalha em código, testes unitários, testes de arquivo e medições sem janela.

## 5. Atualização (25/10/2026, ~17h45): modificador provado
2ª prova real: `Negativo_LiberacaoModificadorEmFalha` **PASS** (try/finally provado). O positivo reprovou na 2ª execução por foco ocupado pelo VS Code (a trava abortou sem enviar: comportamento correto) e havia passado na 1ª com o mesmo helper. **O bloqueio de uso com modificadores (Ctrl, Shift, Alt) está levantado.** O helper fica aceito integralmente. Detalhes: `docs/agents/evidence/test-input-guard/LEIA-ME.md`.