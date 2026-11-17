# Pendências do KyraFiles — 26/10/2026, noite (Claude)

Fonte: `docs/agents/SITUACAO.md`, `docs/specs/BACKLOG-ORDEM-AGY.md` e as pastas de `docs/specs/`. Estados: NOT TESTED / INFERRED / CONFIRMED.

## A. Bugs e desempenho (prioridade máxima)
| # | Item | Estado | Depende de |
|---|---|---|---|
| A1 | Crash A (prévia + setas, `Microsoft.UI.Input.dll`) | **não reproduzido** em 120 rodadas com vídeos de 2 MB (18:10); cenário real: tecla segurada | Alexandre: tentar com vídeo de 98 MB ou esperar novo dump |
| A2 | Crash B (`GridLayoutPage`, `Update_FileTagsUI`) | NOT TESTED | janela de AWAY |
| A3 | F035 AC-5 (janela estacionada não volta ao primeiro plano; reprova também sem a correção) | bug sem dono | decisão: spec de bug |
| A4 | F035 AC-2 (sessão limpa) e AC-4 | NOT TESTED | AWAY |
| A5 | Medição das teclas seguradas contra o Explorer (`R-teclas-seguradas-medicao.md`); `Send-GuardedKeyHold` já existe | NOT TESTED | AWAY |
| A6 | Retrabalho das evidências de F030, F032, F033, F034, F029, F023 (v4.3.6, inválidas) | NOT TESTED | AWAY |
| A7 | F029 benchmark contra o Explorer e eleição da variante | pendente | AWAY |
| A8 | F023 T11 (memória da busca sob buscas contínuas) e F026 T4 (Ctrl+A e Shift+setas em pasta grande) | sem atividade | AWAY |
| A9 | `R-rede-baseline` (3 rodadas por variante) | brief entregue | medição, sem janela |

## B. Features com plan e tasks escritos (executáveis em ordem)
| Onda | Item | Falta |
|---|---|---|
| 1 | **F056** Copiar caminho em formatos | T2 a T4 soltas e nunca compiladas no checkout principal (branch `feature/f056-copy-path-formats`, 6 arquivos); T1 pronta |
| 1 | **F047** Limpeza de pastas de build | T3 a T6 (precisam de build completo e interface) |
| 1 | **F049** Links, symlinks e junctions | tudo; começa por controle negativo de segurança |
| 2 | F037 Renomear em lote com prévia; F044 Espaço = prévia rápida; F045 Ações de projeto; F048 Diff de arquivos | tudo (T0 liberadas) |
| 3 | F043 Histórico com desfazer; F036 Fila de cópia (fases A, B, C) | tudo; F036 T7 e T8 aguardam a decisão D-F036-1 |
| 1 | **F057 Atalhos nos botões dos diálogos (NOVA, 26/10)** | spec/plan/tasks escritos; **aguarda aprovação da spec** |

## C. Sugestões já aprovadas como spec, mas sem plan e tasks
F038 Áreas de trabalho nomeadas · F039 Mapa de uso de disco · F040 Localizador de duplicados · F041 Comparar e sincronizar pastas · F042 Buscas salvas (após F023) · F046 Busca profunda em rede (após a baseline A9) · F050 Terminal embutido (só se a medição justificar) · F051 Busca de código · F052 Visão de repositório git · F053 Coluna de codificação e fim de linha · F054 Prévia técnica (tarefa 0: medir a prévia atual) · F055 Mapa do repositório.

## D. Casa e decisões
- Push pendente (precisa da sua autorização): `claude/specs-backlog` (commits locais desta noite), `feature/crash-reproducao` (5e2360986 e anteriores, em `KyraFiles-crash`).
- Sem release nova sem autorização. Próxima versão só com A1 a A3 decididos ou com as correções revisadas.
- Cota da AGY: `--print` só com `gemini-3.8-flash-high` (Claude e GPT voltam ~01/11 17:48). Janelas interativas têm cota cheia.
- Você: aprovar a spec F057 e os dois pontos dela; decidir A1 e A3; aprovar push; D-F036-1.

## E. Lista de ordens para a AGY (já despachável, sem janela de tela)
1. **F057 T0** (somente leitura, `--print`/Flash serve): inventário das 17 caixas, botões, chaves do `.resw` e reaproveitamentos. *Só depois da sua aprovação da spec.*
2. **Ordens T0 já liberadas** (somente leitura, uma por vez, handoff por tarefa): F036, F037, F044, F045, F048, F049, F043.
3. **R-rede-baseline** (A9): CSV com 3 rodadas por variante, sem janela.
4. **F056 T2 a T4** num **hub interativo** (cota cheia; exige build do `KyraFiles.App`): partir do patch salvo, restore da solução inteira, build 0/0, um commit por tarefa.
5. **F047 T3 a T6**: build no hub; prova de interface só em AWAY.
6. **Não despachar** (precisam de AWAY): A2, A4 a A8, provas T3/T6 de qualquer feature.
Regras: R1 a R13, entrada global só pelo `Send-GuardedInput.ps1`, sem `src/` fora do escopo da tarefa, sem push.
