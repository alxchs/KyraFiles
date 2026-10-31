# Brief da AGY — 30/10/2026 (Claude). Leia ESTRATEGIA-VERIFICACAO.md e REGRA-NAO-PULAR.md antes.

Você trabalha na worktree `C:\desenv\utils\FilesApp-agy`, branch `feature/f065-provas` (saída da `colab`). Não mexa no checkout `C:\desenv\utils\FilesApp`. Não faça push, não aprove spec, não mude estado em spec.md/DECISIONS.md. Sem `Stop-Process` em Files.exe do Alexandre, sem `git checkout --`/`reset`/`clean`, sem foco roubado (METODO §16): tela só com instância própria e UI Automation.

## Ordem (uma por vez, um commit por tarefa)
1. **F065-T11 e T12**: preparar (hoje) os scripts de captura por motor (Everything, índice, nativo, Agent Ransack) e de medição de desempenho no app, em `tools/perf/` ou `tools/search/`. Os scripts só EXECUTAM se `Get-Date -ge '2026-10-31 09:00'`; antes disso saem com mensagem e código 3. A execução real é a partir das 09:00 de 31/10 (Alexandre ausente). Instalar nada, usar o pacote já registrado: `Open-FilesDev.ps1 -NoLaunch` já rodou para a 4.3.10.
2. Só depois, **F063-C6** (script de medição com UI Automation, instância própria) e **C8**, mesma regra de horário para a execução.
3. Entregar `docs/agents/handoffs/<data>_agy_<assunto>.md` com seção "Explicação dos descumprimentos" (D-1..D-6: o que entendeu, por que agiu, o que faria hoje).

## Regras de prova (mecanismo, não boa-fé)
- Toda prova: `tools\verify\Run-Proof.ps1 -Id <id> -Command '<cmd>'` com árvore limpa (commite antes). No handoff: `CONFIRMED ... [prova: <id>]`. Sem isso o Claude reprova sem ler.
- Rode `tools\verify\Check-Handoff.ps1 <seu handoff>` antes de entregar; se reprovar, corrija.
- Teste novo precisa de prova de mutação: quebre o código, mostre vermelho, restaure, mostre verde (duas provas). Teste que recalcula a regra dentro de si é reprovado.
- Escopo permitido: `tools/**`, `tests/**`, `docs/agents/handoffs/**`, `docs/agents/evidence/**`, `docs/evidence/**` e o que a tarefa em `tasks-correcoes.md` pedir explicitamente. Fora disso: pare e registre PENDING DECISION.
- O Claude reroda build e testes em worktree limpa; o número dele vale. Não afirme "0 Warning(s)" sem a saída anexada.
- Se não conseguir provar: `NOT TESTED` e diga por quê. Isso é aceito; marcar CONFIRMED sem prova não é.
