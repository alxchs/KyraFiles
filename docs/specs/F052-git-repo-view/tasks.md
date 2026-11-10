# F052 — Visão de repositório Git — TASKS

Status: CONCLUÍDO (AGY, 10/11/2026). Spec, Plan e Tasks executados e validados na branch `feature/f052-git-repo-view`.

Cada tarefa é pequena (um commit), cita o que atende e termina em algo verificável.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| T1 | Ajustar detecção de HEAD desacoplado (SHA curto) em `GetRepositoryHeadName` (`LibGit2Service.cs`) e atualizar o título da aba em `NavigationHelpers.cs` sem bloquear navegação | US2, AC-2, AC-4, AC-6, FR-001, FR-002 | Testes unitários de HEAD, branch e detached SHA passando; título da aba formata `{NomePasta} ({branch\|sha})`; AC-4 e AC-6 CONFIRMED; AC-2 confirmado | concluído |
| T2 | Implementar configuração `HideGitIgnoredItems`, ação `ToggleHideGitIgnoredItemsAction`, menus/toolbar, filtragem assíncrona em lote/cache no `Win32StorageEnumerator.cs` e strings de localização | US1, AC-1, FR-001, FR-002 | Teste unitário de filtro por `.gitignore` em repo temporário passando; filtragem integrada no enumerador; AC-1 confirmado | concluído |
| T3a | Implementar enumeração e consulta de worktrees em `LibGit2Service`, `GitHelpers` e `IVersionControlService` com modelo `GitWorktreeItem` e testes unitários | US3, AC-3, AC-7, FR-001 | Testes unitários com repo temporário em `%TEMP%\agy\<GUID>\...` validando lista de worktrees, tratamento de diretório inexistente e concorrência; lógica aprovada | concluído |
| T3b | Implementar UI do Flyout de Worktrees (`OpenWorktreeFlyoutAction`, XAML do flyout no `StatusBar` / menus e navegação ao clicar) | US3, AC-3, AC-7 | Ação registrada via `[GeneratedRichCommand]`; flyout funcional na interface; AC-3 e AC-7 confirmados | concluído |
| T4 | Medição de desempenho de abertura de pastas, validação de `verify_no_index_service.ps1`, captura das provas de tela (AC-1, 2, 3, 7) e confecção de `RELATORIO.md` | AC-1, AC-2, AC-3, AC-5, AC-7, NFR-001 | `verify_no_index_service.ps1` aprovado; CPU < 5%; todos os ACs no relatório com estado final comprovado em `RELATORIO.md` | concluído |

---

## Estratégia de execução e testes obrigatórios

1. **Repositórios de teste unitário (REGRA DURA):**
   - Criados exclusivamente em `%TEMP%\agy\<GUID>\KyraFiles\GitTestRepo\` (GUID único por lote/sessão).
   - Apagados rigorosamente no `[TestCleanup]`.
2. **Estado dos cenários de aceite de tela (Regra 7 da fila):**
   - **AC-1, AC-2, AC-3 e AC-7** envolvem visualização direta pelo usuário na interface gráfica (ocultação visual na lista de arquivos, visualização do título da aba, abertura e clique no flyout de worktrees).
   - Testes unitários cobrem a camada de serviço/dados, mas **os cenários de tela permanecem registrados como `NOT TESTED` nas tarefas T1 a T3b**, sendo transicionados para `CONFIRMED` apenas na **T4** mediante execução real do app e verificação física de interface (sem nunca roubar o foco da janela do VS Code).
3. **Medição de Desempenho (AC-5 / §18.5):**
   - Executar o mesmo script de abertura e contagem em repositório grande no baseline da `colab` e na branch da feature, comprovando ausência de regressão na navegação de pastas.
   - Executar `pwsh tools\perf\verify_no_index_service.ps1` com CPU em repouso (< 5%).

---

## Entrega
Compilar release (0 erros / 0 avisos novos), testes unitários passando, um commit por tarefa, handoff com o estado **por cenário** (`AC-n`: CONFIRMED, OBSERVED ou NOT TESTED) e confecção de `RELATORIO.md`. Sem push.
