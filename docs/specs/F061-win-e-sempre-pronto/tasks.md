# F061 — Win+E sempre pronto — TASKS

Status: **APROVADO** (Claude, 28/10/2026). Executar na ordem, T0 primeiro. Branch: `feature/f061-win-e-always-ready`.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| T0 | Reproduzir o ciclo de forma controlada, em pasta de teste isolada (cópia dos binários de `%LOCALAPPDATA%\Programs\FilesCustom` para `%TEMP%\claude\<GUID>\...`), nunca sobre os instalados: (a) iniciar um segundo serviço com o primeiro vivo; (b) analisar por leitura do código (NUNCA sinalizar o evento global `Global\FilesCustom_ShutdownHotkeyService`: ele derrubaria o serviço vivo do Alexandre). O caso (a) roda no máximo 15 segundos e termina com `Stop-Process` do Watchdog de teste antes dos filhos; pare antes se passar de 150 processos. Contar processos criados no caso (a) e decidir qual hipótese da spec §7 vale. | spec §7 | relato com comandos e contagens; hipótese marcada CONFIRMED ou descartada; spec §7 atualizada | concluído (28/10/2026: 30 processos em 15s na cópia isolada; mecanismo da hipótese a CONFIRMED, gatilho INFERRED; b descartada por leitura) |
| T1 | `verify_win_e_health.ps1`: conta serviços e Watchdogs vivos, processos terminados ainda listados, idade do serviço e se o mutex existe; saída de uma linha OK/FALHA + detalhes; sai com código 0/1. | FR-005, AC-6, AC-8 | rodado no estado atual (deve dar OK com 1 serviço) e no estado com 2 serviços (deve dar FALHA); saídas coladas no handoff | concluído (28/10/2026: verify_win_e_health.ps1 criado; OK em estado real com 1 serviço; FALHA no caso simulado de 2 serviços) |
| T2 | Serviço: `EXIT_ALREADY_RUNNING = 3` no ramo do mutex e linha no `hotkey.log`. | FR-001, AC-5 | `Build-Native.ps1` 0 erros; segundo serviço sai com código 3 e deixa linha no log | concluído e revisado pelo Claude (28/10/2026) |
| T3 | Watchdog: espera crescente (200 ms até 30 s), volta ao mínimo após 60 s de vida, trata código 3 sem recriar, nunca desiste, grava linha no log por reinício. | FR-002, FR-003, FR-004, FR-006, AC-1 a AC-4 | em pasta de teste: AC-2 (≤ 1 s), AC-3 (≤ 4 criações/min em 2 min), AC-4, AC-1 (1 serviço vivo); contagens no handoff | concluído (28/10/2026: AC-1 a AC-5 validados em cópia isolada; espera crescente 200ms->30s confirmada; código 3 sem recriação) |
| T4 | Passo AC-8 no `PLAYBOOK.md` (rodar `verify_win_e_health.ps1` e colar o resultado na nota de liberação); registrar a premissa em `DECISIONS.md` (D-0xx) e atualizar `STATUS.md`. | AC-8, US4 | texto revisado pelo Claude; premissa citada com a data de 28/10/2026 | concluído (28/10/2026: passo no PLAYBOOK; D-F061-1 já registrada; STATUS atualizado quando houver exe) |
| T5 | Prova de tela do AC-7 e do AC-2 reais (Win+E com Files fechado), só na janela combinada, sem roubar foco; restaurar o app instalado ao final. | AC-7, AC-2 | captura ou UI Automation; estado por AC no handoff | pendente |

## Regras da execução

- Nunca encerrar nem sobrescrever os binários instalados do Alexandre durante os testes; trabalhar só em cópia isolada.
- Ordem: T0 antes de qualquer código. Se T0 mostrar causa diferente das hipóteses, parar e pedir revisão da spec.
- Um commit por tarefa. BOM e CRLF preservados (conferir por bytes). Sem push.

## Teste obrigatório

Um por cenário (`AC-1` a `AC-8`), do tipo definido no `plan.md` §6.

## Entrega

Build nativo sem erro, comando de verificação passando, um commit por tarefa, handoff com estado por `AC-n` (CONFIRMED, OBSERVED, INFERRED ou NOT TESTED) e nota de liberação se gerar exe.
