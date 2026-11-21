# F094 — Busca Everything: escopo global e histórico — TASKS

Status: **CONCLUÍDAS** (T1 a T9) (21/11/2026). Branch: `feature/F094-everything-escopo-e-historico`.
Ajuste aprovado: T6 = 20 entradas guardadas e mostradas, com texto, escopo, motor e data (FR-013/FR-014, AC-16/AC-17).
Relatório de conclusão: `docs/specs/F094-everything-escopo-e-historico/RELATORIO.md`.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| T1 | Medir baseline: tempo até o primeiro resultado e até o fim de `*.exe` com Everything, na `main`, registrando o comando | NFR-001 | números anotados em `docs/specs/F094-.../perf.md` | [x] FEITO |
| T2 | Criar `EverythingQueryScope.Parse` em `KyraFiles.Shared/Helpers` + testes unitários (sem caminho; caminho com e sem aspas; com espaço sem aspas; barra final e `/`; UNC; pasta inexistente; `path:`/`parent:`; texto só com caminho; `c:` sem barra) | FR-002..004, AC-2..AC-5 | build 0/0 e testes verdes | [x] FEITO |
| T3 | `EverythingSearchProvider`: ignorar `request.Folder`, usar o analisador, `-path` entre aspas só com `ScopePath`; chamar `OnFallbackNotice` na queda (string localizada nova) | FR-001, FR-005, FR-007, AC-1, AC-6, AC-8 | build 0/0; motores Nativo/Índice intocados (diff) | [x] FEITO |
| T4 | Título da página de resultados com escopo real quando o motor é Everything ("todo o computador" ou a pasta) | FR-006, AC-7 | build 0/0; strings en-US e pt-BR | [x] FEITO |
| T5 | "Salvar busca" usa o escopo analisado com Everything | FR-012, AC-14 | build 0/0 | [x] FEITO |
| T6 | Sugestões: chave de deduplicação separada para histórico; comparação sem maiúsculas; limite 50 guardado / 10 exibido | FR-008, AC-9, AC-10 | teste unitário da lista (ordem, sem duplicata, várias entradas) verde | [x] FEITO |
| T7 | Ao digitar, até 3 buscas anteriores que contêm o texto aparecem primeiro | FR-010, AC-12 | build 0/0 | [x] FEITO |
| T8 | Remover item do histórico (botão no item) e "Limpar histórico de busca" em Configurações > Geral | FR-011, AC-13 | build 0/0; strings en-US e pt-BR | [x] FEITO |
| T9 | Validação no app aberto (UI Automation, sem roubar foco): AC-1..AC-15, reabrindo o app para AC-11/AC-13; medir NFR-001 com o script de T1; Gate F021; varredura de binários | todos | relatório de conclusão com estado por AC | [x] FEITO |

**Não inclui:** mudar escopo dos motores Nativo/Índice (DISCOVERY), ordenação do Everything, sincronizar histórico.
