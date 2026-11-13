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
| F092 | AGY | `feature/f092-native-sidebar-parity` | 🔴 OCUPADO | Uma spec por vez; integrar na `main` antes da próxima |
| F090 (correção da escala) | Alexandre | `feature/f090-escala` | 🔴 OCUPADO | 1 commit sobre a `main`; build e teste com o Alexandre |

## 2. Pacotes da F092

| Pacote | Semáforo | Depende de | Pronto = |
|---|---|---|---|
| T0 diagnóstico e baseline | ⚪ na branch | — | feito |
| T1 Bibliotecas por padrão | ⚪ na branch (`2581a3417`) | — | feito |
| T2 Este Computador | ⚪ na branch (`e69c7eb21`); revisão da Claude **aprovada** em 12/11 | — | prova de clique fica para a T6 |
| T1-fix teste tautológico | 🟢 LIVRE | nada | trocar `AC1_..._ResolutionLogic_...` por teste que pode falhar e anexar prova de AC-1 em atualização (brief §1) |
| T3 Painel de Controle | 🟢 LIVRE (próximo da AGY) | nada | 6 pontos do brief §2 + fallback `ms-settings:` |
| T5 toggles em Configurações | 🟢 LIVRE | T3 | dois toggles persistidos, textos em `Resources.resw` |
| T4a itens delegados (STA) | 🟢 LIVRE | T3 | sem travar a UI, timeout 3 s |
| T4b navegação de mídias e fallback | 🟢 LIVRE | T4a | aviso + "Abrir no Explorador" |
| T4c atalhos NetHood na Rede | 🟢 LIVRE | T3 | atalhos visíveis na seção Rede |
| T6 validação, medição e relatório | 🟢 LIVRE | T3 a T5 | gate F021, comparação com a `main`, prova de tela após 23:59 |

Brief: `docs/agents/tasks/BRIEF-F092-T3-T5-revisao-T1-T2.md`.

## 3. Próximas specs (só depois da F092 na `main`; uma branch por spec)

| Spec | Assunto | Semáforo | Observação |
|---|---|---|---|
| F036 | Fila de cópias | 🟢 LIVRE | Decisões resolvidas (D-F036-1/2/3); 2/18 tarefas; começar pela T2, com 10+ rodadas de medição |
| F035 | Segunda instância | 🟢 LIVRE | 0/6 |
| F037 | Renomear em lote com prévia | 🟢 LIVRE | 0/8 |
| F043 | Histórico de operações | 🟢 LIVRE | 0/8 |
| F044 | Quick Look com espaço | 🟢 LIVRE | 0/8 |
| F045 | Ações de projeto | 🟢 LIVRE | 0/8 |
| F047 | Limpeza de pastas de build | 🟢 LIVRE | 0/7 |
| F048 | Diff de arquivos | 🟢 LIVRE | 0/7 |
| F049 | Links, symlinks, junctions | 🟢 LIVRE | 0/8 |
| F056 | Formatos de cópia de caminho | 🟢 LIVRE | 0/6 |
| F060 | SFTP, script, hex, divisão | 🟢 LIVRE | 13/22; `origin/feature/f060-m3-sftp` e `f060-m4-script` ainda fora da `main` |
| F026, F057, F082, F083 | Resíduos de 1 tarefa cada | 🟢 LIVRE | 3/4, 4/5, 17/18, 18/19 |
| F058, F062, F063, F070, F071 | Specs aprovadas em 12/11/2026 | 🟢 LIVRE | Conferir se `plan.md` e `tasks.md` estão aprovados antes de codificar (SDD) |

## 4. Concluído recentemente (na `main`)

| Item | Hash |
|---|---|
| Specs aprovadas, decisões resolvidas, brief F092 | ⚪ `5827968aa` |
| F052 visão Git | ⚪ `2e401a04f` |
| F090 escala (primeira passada) | ⚪ `1c1a02df5` |
| F089 fontes | ⚪ `fc2101f59` |

## 5. Registro (§22: uma linha por evento, a mais nova embaixo)

- `[FEITO] 5827968aa · Claude · 2026-11-12 · specs aprovadas, D-017/D-F036 resolvidas, brief F092`
- `[FEITO] revisão T2 · Claude · 2026-11-12 · código aprovado; navegação por clique INFERRED até a T6`
