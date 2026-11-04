# Handoff: F052 Visão de repositório Git (Ajustes de Plan e Tasks pós-Revisão)

Data: 02/11/2026
Autora: AGY
Branch: `feature/f052-git-repo-view`

## Contexto e Estado Atual

Após análise detalhada dos 7 pontos levantados na revisão, os arquivos de planejamento da feature **F052 — Visão de repositório Git** (`plan.md` e `tasks.md` em `docs/specs/F052-git-repo-view/`) foram devidamente corrigidos e atualizados em estado de **RASCUNHO**. O código da F052 **não** foi iniciado, conforme estipulado.

### Resumo dos 7 Ajustes Realizados

1. **GetRepositoryHeadName e Diferença Real para T1 (CONFIRMED):**
   - Confirmada a existência prévia de `GetRepositoryHeadName(string? path)` em `LibGit2Service.cs:183`, `IVersionControlService.cs:69` e `GitHelpers.cs:34-35`.
   - Confirmado o uso em `ShellViewModel.cs:273` (`IsValidGitDirectory`) e `Win32StorageEnumerator.cs:45` (`isGitRepo`).
   - Confirmado o que a interface já exibe hoje: a barra de status (`StatusBar.xaml` e `DirectoryPropertiesViewModel.cs:187-233`) já exibe o branch ativo (`GitBranchDisplayName`), contadores de pendências/sincronização (`VcsStatusIndicator`) e flyout de checkout (`BranchesFlyout`), além do preview de pasta (`FolderPreviewViewModel.cs:72`).
   - O título da aba (`NavigationHelpers.cs:275-348`, `UpdateTabInfoAsync`) **não** exibe a branch hoje (apenas nome da pasta e hints ancestrais).
   - A T1 foi reduzida à diferença: estender `GetRepositoryHeadName` para suportar detached HEAD (SHA curto com 7 caracteres via `repository.Head.Tip.Sha[..7]`) e atualizar o título da aba de forma assíncrona pós-carregamento.

2. **Ponto Real de Filtragem (Correção de `ItemFilter` inexistente):**
   - Removida qualquer menção a classes inexistentes.
   - Apontados os locais reais onde `ShowHiddenItems` é aplicado:
     - `Win32StorageEnumerator.cs:40-59` (`ListEntries`): onde `userSettingsService.FoldersSettingsService.ShowHiddenItems` é checado na enumeração.
     - `ShellViewModel.cs:3295-3305` (`AddFileOrFolderAsync`): filtragem de itens capturados dinamicamente pelo watcher.
     - `ShellViewModel.cs:883-896`: escuta a mudança da propriedade em `FoldersSettingsService` e despacha `RefreshItems(null)`.

3. **Custo e Risco do `IsPathIgnored` (AC-5 e Casos de Borda):**
   - Custo zero se `HideGitIgnoredItems == false` (padrão desligado).
   - Reutilização de uma única instância de `Repository` por sessão de enumeração assíncrona (sem abrir instâncias repetidas de `Repository`).
   - Cache de prefixos ignorados na listagem (ex.: filhos sob `bin/`, `obj/` ou `node_modules/` ignorados sem nova chamada nativa).
   - Isolamento em threadpool de background no `Win32StorageEnumerator`.
   - Casos de borda cobertos: submódulos (gitlink protegido por try/catch); repositório corrompido (`LibGit2SharpException` tratada com fallback e sem crash); `git` fora do PATH (KyraFiles utiliza LibGit2Sharp embarcado e não depende do binário `git.exe` no PATH); pasta de rede (timeout defensivo sem travar I/O da interface).

4. **Lista Exaustiva de Arquivos a Mexer:**
   - Adicionadas ações: `ToggleHideGitIgnoredItemsAction.cs` e `OpenWorktreeFlyoutAction.cs` com `[GeneratedRichCommand]`.
   - Adicionados controles e XAML: `ClassicMenuBar.xaml`, `Toolbar.xaml`, `StatusBar.xaml`, `StatusBar.xaml.cs` e `DirectoryPropertiesViewModel.cs`.
   - Adicionadas strings de localização: `Resources.resw` (en-US e pt-BR).
   - Mapeados contratos, enumerador, viewmodels e testes unitários.

5. **Isolamento de Testes Unitários e Medição AC-5:**
   - Testes unitários com repositórios Git temporários gerados obrigatoriamente dentro de `%TEMP%\agy\<GUID>\KyraFiles\GitTestRepo\` e removidos no `[TestCleanup]`.
   - Medição do AC-5 definida utilizando o mesmo script comparativo de navegação em pasta grande contra o baseline da `colab` (§18.5) e execução mandatória de `pwsh tools\perf\verify_no_index_service.ps1`.

6. **Classificação dos Cenários de Interface (Regra 7 da Fila):**
   - AC-1, AC-2, AC-3 e AC-7 são cenários visuais de interface. Testes unitários cobrem a lógica de dados, mas os cenários de tela permanecem explicitamente marcados como `NOT TESTED` até a prova de execução visual real com o app aberto.

7. **Divisão de T3 em T3a e T3b:**
   - **T3a:** Consulta, modelo `GitWorktreeItem` e testes unitários em `LibGit2Service`, `GitHelpers` e `IVersionControlService` (um commit).
   - **T3b:** UI do Flyout de Worktrees (`OpenWorktreeFlyoutAction`, XAML do flyout no `StatusBar` / menus e navegação) (um commit).

## Arquivos Modificados
- `docs/specs/F052-git-repo-view/plan.md`
- `docs/specs/F052-git-repo-view/tasks.md`
- `docs/agents/handoffs/2026-11-02_2255_agy_f052-revisao-plan-tasks.md`

## Próximos Passos
- Aguardar aprovação do `plan.md` e `tasks.md` pelo Alexandre para dar início à execução das tarefas T1 a T4 na branch `feature/f052-git-repo-view`.

