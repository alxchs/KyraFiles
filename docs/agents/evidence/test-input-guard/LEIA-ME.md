# Execução real do Test-InputGuard (ALEXANDRE_AWAY=1)

Data: 25/10/2026. Executor: Claude, com o Alexandre longe do teclado (autorizou: "Pode/rode"). Cópia do código em `C:\desenv\utils\FilesApp-guard` (branch `feature/test-input-guard`, commit `012406508`), sem tocar no checkout da AGY. Duração: 15 s. Estado do teclado depois: Shift, Ctrl e Alt soltos; nenhuma janela de teste sobrou.

| Caso | Resultado |
|---|---|
| Positivo_FocoAlinhado | **PASS**: a caixa A recebeu `k` (o helper entrega a tecla ao app de teste com o foco alinhado) |
| Negativo_FocoDesalinhado | **PASS**: com B de fato em primeiro plano, abortou citando o PID de B; as duas caixas vazias |
| Negativo_ProcessoEncerrado | PASS |
| Negativo_LiberacaoModificadorEmFalha | **FAIL, defeito do teste e não do helper**: B continuava em primeiro plano do caso anterior; o helper abortou na 1ª verificação ("Foco violado"), **antes** de o Shift descer, então o ponto de falha induzida nunca foi alcançado e a mensagem esperada (`FALHA_INDUZIDA_TESTE`) não apareceu. O `try/finally` continua **não provado**. Correção: o teste deve trazer A para a frente (como no caso positivo) e conferir a pré-condição antes de chamar o helper |
| Negativo_ProcessoNaoAutorizado | PASS |

Arquivos: `resultado-awaytest-2026-10-25.csv` e `relatorio-awaytest-2026-10-25.md` (gerado pelo próprio script).

## 2ª execução (25/10/2026, ~17h40, Alexandre longe do teclado; commit `6cf23b6ad`, helper idêntico ao da 1ª)
Executor: Claude. Duração: 76 s (a 1ª levou 15 s). Teclado depois: Shift, Ctrl e Alt soltos.

| Caso | Resultado |
|---|---|
| Negativo_LiberacaoModificadorEmFalha | **PASS**: a falha induzida **depois** de o modificador descer foi capturada (`FALHA_INDUZIDA_TESTE`) e os três modificadores voltaram a estar soltos. **Prova o `try/finally`.** |
| Negativo_FocoDesalinhado, Negativo_ProcessoEncerrado, Negativo_ProcessoNaoAutorizado | PASS |
| Positivo_FocoAlinhado | **FAIL por foco ocupado, e não por defeito do helper:** a janela em primeiro plano era do VS Code (PID 23456, várias janelas dele se revezando, com "Antigravity Settings" e "Auth Success"), então o helper **abortou sem enviar nenhuma tecla**, como projetado. O positivo **passou na 1ª execução** (a caixa A recebeu `k`) com o **mesmo** `Send-GuardedInput.ps1` (`git diff` do helper entre `012406508` e `6cf23b6ad` é vazio). |

**Leitura:** com as duas execuções, o helper está provado nos casos positivo e negativos e na liberação do modificador. O positivo é sensível a haver outra janela tomando o foco: **em prova real, só fora do horário de atividade de outras automações**, ou no Windows Sandbox.
