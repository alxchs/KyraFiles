# FILA — semáforo único do projeto (Claude e AGY)

Fonte única de "quem pega o quê" (METODO §22). Fica na `main`; quem pegar ou entregar atualiza
aqui (na própria branch, junto com o commit da tarefa) e o estado chega à `main` na integração.
O `AGENT_CONTEXT.md` mostra só o pacote atual e aponta para este arquivo.

Semáforo: 🟢 LIVRE · 🔴 OCUPADO · 🟡 EM REVISÃO · ⚪ CONCLUÍDO (na `main`, com hash).

Sincronizado em 2026-11-12 por Claude, a partir de `AGENT_CONTEXT.md` e do handoff
`2026-11-12_2130_agy_f092-t2.md` da AGY, do `git log` e dos `tasks.md`.

## 1. Em andamento

| Item | Dono | Branch | Semáforo | Observação |
|---|---|---|---|---|
| F090 (correção da escala) | Alexandre | `feature/f090-escala` | 🔴 OCUPADO | 1 commit sobre a `main`; build e teste com o Alexandre |
| F048 (Diff de arquivos) | — | — | 🟢 LIVRE | 0/7 tarefas; integração de diff externo configurado |

## 2. Pacotes da F092 (CONCLUÍDA)

| Pacote | Semáforo | Depende de | Pronto = |
|---|---|---|---|
| T0 diagnóstico e baseline | ⚪ feito | — | medições de baseline e diagnóstico técnico |
| T1 Bibliotecas por padrão | ⚪ feito | — | default true e preservação de profile existente |
| T2 Este Computador | ⚪ feito | — | nó navegável com ícone nativo e sem chevron se vazio |
| T1-fix teste tautológico | ⚪ feito | nada | parsing JSON real comprovando fallback e preservação |
| T3 Painel de Controle | ⚪ feito | nada | nó canônico com fallback gracioso para ms-settings |
| T5 toggles em Configurações | ⚪ feito | T3 | toggles em Settings > Folders e menu de contexto |
| T4a itens delegados (STA) | ⚪ feito | T3 | enumeração COM segura via STATask com timeout de 3s |
| T4b navegação de mídias e fallback | ⚪ feito | T4a | RecyclePath fallback e explorer.exe para mídias UPnP/DLNA |
| T4c atalhos NetHood na Rede | ⚪ feito | T3 | atalhos observando Shortcuts.CollectionChanged |
| T6 validação, medição e relatório | ⚪ feito | T3 a T5 | 39 testes lógicos verdes e relatório formal em RELATORIO.md |

Brief: `docs/agents/tasks/BRIEF-F092-T3-T5-revisao-T1-T2.md`.

## 3. Próximas specs (uma branch por spec)

| Spec | Assunto | Semáforo | Observação |
|---|---|---|---|
| F036 | Fila de cópias | ⚪ CONCLUÍDO | Fases A e B entregues na v4.3.14.0 (T0 a T9); 28 testes verdes, RELATORIO.md |
| F035 | Segunda instância | 🟢 LIVRE | 0/6 |
| F037 | Renomear em lote com prévia | ⚪ CONCLUÍDO | 8/8 tarefas entregues; prévia ao vivo, regex, numeração, swaps e desfazer atômico; RELATORIO.md |
| F043 | Histórico de operações | 🟢 LIVRE | 0/8 |
| F044 | Quick Look com espaço | ⚪ CONCLUÍDO | 8/8 tarefas entregues; sobreposição embutida, leitor multimodo, setas e fechar com Esc/Espaço; RELATORIO.md |
| F045 | Ações de projeto | 🟢 LIVRE | 0/8 |
| F047 | Limpeza de pastas de build | ⚪ CONCLUÍDO | 7/7 tarefas entregues; diálogo, medição, lixeira e proteção git; RELATORIO.md |
| F048 | Diff de arquivos | 🟢 LIVRE | 0/7 tarefas; integração de diff externo configurado (aguardando leitura do usuário) |
| F049 | Links, symlinks, junctions | 🟢 LIVRE | 0/8 |
| F056 | Formatos de cópia de caminho | ⚪ CONCLUÍDO | 6/6 tarefas entregues na v4.3.15.0; 13 testes verdes, RELATORIO.md |
| F057 | Atalhos de botões em diálogos | ⚪ CONCLUÍDO | 5/5 tarefas entregues na v4.3.16.0; 8 testes verdes, RELATORIO.md |
| F060 | SFTP, script, hex, divisão | 🟢 LIVRE | 13/22; `origin/feature/f060-m3-sftp` e `f060-m4-script` ainda fora da `main` |
| F026, F082, F083 | Resíduos de 1 tarefa cada | 🟢 LIVRE | 3/4, 17/18, 18/19 |
| F058, F062, F063, F070, F071 | Specs aprovadas em 12/11/2026 | 🟢 LIVRE | Conferir se `plan.md` e `tasks.md` estão aprovados antes de codificar (SDD) |

## 4. Concluído recentemente (na `main`)

| Item | Hash |
|---|---|
| F091, F093, F018, F092 (identidade, quick-ship, update, sidebar) v4.3.17 | ⚪ `v4.3.17` |
| Specs aprovadas, decisões resolvidas, brief F092 | ⚪ `5827968aa` |
| F052 visão Git | ⚪ `2e401a04f` |
| F090 escala (primeira passada) | ⚪ `1c1a02df5` |
| F089 fontes | ⚪ `fc2101f59` |

## 5. Registro (§22: uma linha por evento, a mais nova embaixo)

- `[FEITO] 5827968aa · Claude · 2026-11-12 · specs aprovadas, D-017/D-F036 resolvidas, brief F092`
- `[FEITO] revisão T2 · Claude · 2026-11-12 · código aprovado; navegação por clique INFERRED até a T6`
- `[FEITO] F092 · AGY · 2026-11-12 · T0 a T6 concluídas (Este PC, Painel de Controle, mídias, toggles, 39 testes verdes)`
- `[FEITO] F036-T2 · AGY · 2026-11-14 · OperationQueue implementado, 10 testes verdes, 10 rodadas de medição confirmando padrão 2`
- `[FEITO] F036-T3 · AGY · 2026-11-14 · Fila ligada a CopyItemsAsync, MoveItemsAsync e CopyItemsFromClipboard; IsQueued no StatusCenterItem; 13 testes verdes`
- `[FEITO] F091 · AGY · 2026-11-14 · renome completo de namespaces Files.* -> KyraFiles.* (1.450 arquivos), migração transparente de configurações/registro sem perda de dados, build verde`
- `[FEITO] F093 · AGY · 2026-11-14 · Quick-Ship.ps1 com modo rápido (-Fast) e completo (-Full), Inno Setup condicional, instalador em 15s e deploy local com gate F021`
- `[FEITO] F018-update-fix · AGY · 2026-11-14 · mapeamento e correção da notificação de atualização (SideloadUpdateService ativado em DI, link direto de download de release)`
- `[FEITO] F092-restyle · AGY · 2026-11-15 · reestilização da barra lateral 1A + 1B + ultracompacto (51 testes verdes, Gate F021 verde, instalador 85.78 MB)`
- `[FEITO] F091, F093, F018, F092 · AGY · 2026-11-15 · correções da revisão da Claude aplicadas, integrado na main e gerado release v4.3.17`
- `[PEGO] F037 · AGY · feature/f037-bulk-rename · renomear em lote com prévia ao vivo, conflitos, regex e desfazer atômico · não inclui renomear conteúdo de arquivos nem IA`
- `[FEITO] F037 · AGY · 2026-11-15 · renomear em lote com prévia ao vivo, conflitos, regex, swaps e desfazer atômico (8/8 ACs confirmados)`
- `[PEGO] F047 · AGY · feature/f047-build-folder-cleaner · limpeza de pastas pesadas de build com diálogo, medição, lixeira e proteção git · não inclui caches globais (~/.nuget, ~/.gradle)`
- `[FEITO] F047 · AGY · 2026-11-15 · limpeza de pastas pesadas de build com diálogo, medição, lixeira e proteção git (7/7 ACs confirmados)`
- `[PEGO] F044 · AGY · feature/f044-quick-look · prévia rápida instantânea com barra de espaço, sobreposição própria, navegação e metadados · não inclui edição nem anotações`
