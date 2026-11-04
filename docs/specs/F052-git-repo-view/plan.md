# F052 — Visão de repositório Git — PLAN

Status: RASCUNHO (AGY, 02/11/2026). Spec.md APROVADA pelo Alexandre (24/10/2026). Responde "como", sem alterar o "o quê".

## 1. Arquitetura atual e verificação no código (CONFIRMED)

Toda afirmação técnica abaixo foi confirmada diretamente no código-fonte atual com os comandos indicados:

1. **Obtenção do HEAD e Branch já existente:**
   - `IVersionControlService.GetRepositoryHeadName(string? path)` (`src/KyraFiles.App/Data/Contracts/IVersionControlService.cs:69`) e sua implementação em `LibGit2Service.cs:183` (`src/KyraFiles.App/Services/Git/LibGit2Service.cs`) obtêm o nome do branch atual do repositório (`branch.FriendlyName`) usando `Task.Run` com LibGit2Sharp. *(CONFIRMED via `git grep -n "GetRepositoryHeadName"`)*.
   - `GitHelpers.GetRepositoryHeadName(path)` (`src/KyraFiles.App/Utils/Git/GitHelpers.cs:34-35`) atua como fachada estática. *(CONFIRMED)*.
   - `ShellViewModel.cs:273` consome `GitHelpers.GetRepositoryHeadName(gitDirectory)` para definir `IsValidGitDirectory = !string.IsNullOrEmpty(headName)`. *(CONFIRMED via `git grep -n "IsValidGitDirectory"`)*.
   - `Win32StorageEnumerator.cs:45` consome `GitHelpers.GetRepositoryHeadName(repoPath)` para definir `isGitRepo`. *(CONFIRMED)*.
2. **O que a branch já mostra na interface hoje:**
   - **Barra de status:** `StatusBar.xaml` (linhas 162, 176, 239, 249, 443, 500) e `DirectoryPropertiesViewModel.cs:187-233` (`UpdateGitInfo`). O app já exibe o branch ativo (`StatusBarViewModel.GitBranchDisplayName`), indicador visual de sincronização/pendências locais (`VcsStatusIndicator`) e flyout com lista de branches para checkout (`BranchesFlyout`). *(CONFIRMED)*.
   - **Painel de detalhes / Previews:** `FolderPreviewViewModel.cs:72` exibe `GitCurrentBranch` com `headName`. *(CONFIRMED)*.
   - **Título da aba:** Atualmente NÃO mostra a branch. Mostra apenas o nome do diretório e hints de ancestrais em caso de pastas homônimas (`NavigationHelpers.cs:275-348`, `UpdateTabInfoAsync` e `GetSelectedTabInfoAsync`). *(CONFIRMED)*.
3. **Ponto real onde itens são filtrados hoje (não existe `ItemFilter`):**
   - **Enumeração inicial da pasta:** `Win32StorageEnumerator.ListEntries` (`src/KyraFiles.App/Utils/Storage/Enumerators/Win32StorageEnumerator.cs:40-59`). É aqui que `userSettingsService.FoldersSettingsService.ShowHiddenItems` é lido e os atributos `FileAttributes.Hidden` / `FileAttributes.System` / `.` são filtrados antes de instanciar os itens. *(CONFIRMED via `git grep -n "ShowHiddenItems"`)*.
   - **Adição dinâmica de itens via watcher:** `ShellViewModel.AddFileOrFolderAsync` (`src/KyraFiles.App/ViewModels/ShellViewModel.cs:3295-3305`), onde `findData.dwFileAttributes` é testado contra `ShowHiddenItems`. *(CONFIRMED)*.
   - **Disparo de re-filtragem:** `ShellViewModel.SettingsService_FoldersSettingsService_PropertyChanged` (`src/KyraFiles.App/ViewModels/ShellViewModel.cs:883-896`) escuta alterações em propriedades de `FoldersSettingsService` e despacha `RefreshItems(null)`. *(CONFIRMED)*.
4. **Gerenciamento de Ações e Source Generator:**
   - As ações herdam de `ObservableObject` e interfaces de comando (`IToggleAction`, `IAction`) e são marcadas com o atributo `[GeneratedRichCommand]` (`src/KyraFiles.App/Actions/Show/ToggleShowHiddenItemsAction.cs:6`). *(CONFIRMED)*.
   - O gerador `CommandManagerGenerator.cs` (`src/KyraFiles.Core.SourceGenerator/Generators/CommandManagerGenerator.cs:11-25`) sintetiza o enum `CommandCodes` e registra os comandos automaticamente em `CommandManager`. *(CONFIRMED)*.
5. **Capacidades nativas do LibGit2Sharp no repositório:**
   - `repository.Ignore.IsPathIgnored(relativePath)`: consulta regras do `.gitignore` compiladas em C pelo libgit2. *(CONFIRMED)*.
   - `repository.Worktrees` (`WorktreeCollection`): enumera os worktrees vinculados ao repositório. *(CONFIRMED)*.
   - `repository.Info.IsHeadDetached` e `repository.Head.Tip.Sha[..7]`: detectam HEAD desacoplado e SHA curto. *(CONFIRMED)*.

---

## 2. Abordagem técnica

### 2.1 Título da aba e HEAD desacoplado (US2, AC-2, AC-4, AC-5, AC-6)
- **Diferença coberta na T1:** `GetRepositoryHeadName` em `LibGit2Service.cs` já obtém `branch.FriendlyName`. O ajuste consiste em:
  1. No `LibGit2Service.GetRepositoryHeadName`: verificar `repository.Info.IsHeadDetached`. Se estiver desacoplado (ou o nome for `"(no branch)"`), retornar o SHA curto do commit (`repository.Head.Tip.Sha[..7]`).
  2. Em `NavigationHelpers.cs` (`UpdateTabInfoAsync`): ao formatar o cabeçalho da aba, se a pasta estiver dentro de um repositório Git, compor o título como `{NomePasta} ({branch|sha})`.
  3. Para não violar o **AC-5**, a obtenção do nome/SHA ocorre em segundo plano assíncrono pós-navegação imediata, sem bloquear a transição de pastas. Se fora de repositório Git, o título exibe puramente `{NomePasta}` sem erro.

### 2.2 Ocultar itens ignorados pelo Git (US1, AC-1, FR-001)
- **Configuração:** Adicionar `HideGitIgnoredItems` (bool, default `false`) em `IFoldersSettingsService` e `FoldersSettingsService`.
- **Ação:** Criar `ToggleHideGitIgnoredItemsAction` (`IToggleAction` com `[GeneratedRichCommand]`, categoria `Show`).
- **Ponto de filtragem real:**
  - Em `Win32StorageEnumerator.cs:40-59`: se `HideGitIgnoredItems == true` e `isGitRepo == true`, testar itens contra o parser de ignore do repositório.
  - Em `ShellViewModel.cs:3295-3305` (`AddFileOrFolderAsync`): aplicar a mesma regra ao adicionar itens notificados pelo watcher.
  - Em `ShellViewModel.cs:883-896`: escutar `nameof(FoldersSettingsService.HideGitIgnoredItems)` para chamar `RefreshItems(null)`.

### 2.3 Desempenho e mitigação de custo do `IsPathIgnored` (AC-5)
- **Custo zero quando desligado:** A verificação de ignore não executa se `HideGitIgnoredItems` estiver desativado (comportamento padrão).
- **Sem repetição de instância `Repository`:** Abrir uma única instância de `Repository` por sessão de enumeração no enumerador assíncrono, nunca instanciando `new Repository()` item a item.
- **Cache local de pastas ignoradas:** Se um diretório for ignorado (ex.: `node_modules/`, `bin/`, `obj/`), registrar o caminho relativo no cache da listagem. Qualquer arquivo filho sob esse prefixo é ignorado imediatamente sem nova chamada nativa.
- **Isolamento de thread:** Toda a filtragem roda dentro de `Win32StorageEnumerator.ListEntries` na threadpool de background. A UI recebe apenas os lotes prontos via `dispatcherQueue.EnqueueOrInvokeAsync`.

### 2.4 Casos de borda da Spec (§5)
1. **Submódulos:** Diretórios com ponteiro `.git` em arquivo (gitlink). O LibGit2Sharp resolve repositórios filhos nativamente; no entanto, se o submódulo estiver não inicializado ou sem commit, um bloco `try/catch` protege a leitura, tratando o diretório como pasta comum sem lançar exceções.
2. **Repositório corrompido (`LibGit2SharpException`, `CorruptObjectException`):** Captura defensiva de exceções. Se o repositório estiver corrompido, a visão Git degrada graciosamente (retorna null, não exibe branch no título e não oculta itens), mantendo a listagem normal de arquivos intacta.
3. **`git` fora do PATH:** O KyraFiles utiliza `LibGit2Sharp` (wrapper C# sobre libgit2 nativo compilado), NÃO depende do executável `git.exe` no PATH do Windows. Portanto, toda a funcionalidade opera de forma autônoma sem requerer Git instalado na máquina do usuário.
4. **Pasta de rede (UNC / discos mapeados):** Operações em compartilhamentos de rede estão sujeitas a timeouts de I/O. A descoberta de repositório e leitura de worktrees opera em background assíncrono com tolerância a falhas; se o compartilhamento SMB demorar a responder, a navegação de arquivos nunca é bloqueada.

### 2.5 Worktrees do Repositório (US3, AC-3, AC-7)
- **T3a (Consulta e modelo):**
  - Adicionar contrato `GetRepositoryWorktrees(string? path)` em `IVersionControlService`, `GitHelpers` e `LibGit2Service`.
  - Retornar coleção imutável de `GitWorktreeItem(string Name, string Path, string? BranchName, bool IsCurrent, bool IsLocked)`.
  - Proteger contra worktree deletado em disco (`Directory.Exists(w.Path) == false`) e worktree aberto por outro agente (somente leitura, FR-001).
- **T3b (UI do Flyout):**
  - Criar `OpenWorktreeFlyoutAction` com `[GeneratedRichCommand]` e categoria `Git`.
  - Integrar o flyout de Worktrees na interface (`StatusBar.xaml` e/ou menu da toolbar).
  - Listar worktrees disponíveis com caminho e branch ativa. Clicar em um item navega a aba atual (ou abre nova aba com Ctrl/clique do meio).

---

## 3. Mapa spec → código

| Requisito | Onde é atendido no código |
|---|---|
| US1, AC-1 | `FoldersSettingsService.HideGitIgnoredItems`, `ToggleHideGitIgnoredItemsAction`, filtro em `Win32StorageEnumerator.cs:40-59` e `ShellViewModel.cs:3295` |
| US2, AC-2, AC-4, AC-5, AC-6 | `LibGit2Service.GetRepositoryHeadName` (suporte a detached HEAD via SHA curto), `NavigationHelpers.UpdateTabInfoAsync` |
| US3, AC-3, AC-7 | `LibGit2Service.GetRepositoryWorktrees` (T3a), `OpenWorktreeFlyoutAction`, flyout XAML e `DirectoryPropertiesViewModel` (T3b) |
| FR-001 | Operações de consulta estritamente read-only (`repository.Ignore`, `repository.Head`, `repository.Worktrees`) |
| FR-002 | `Task.Run` com cache de branch e repositório por caminho |
| NFR-001 | Medição de abertura de pastas com script comparativo contra a colab (§18.5) |

---

## 4. Lista exaustiva de arquivos a mexer

- **Serviços de Git e Contratos:**
  - `src/KyraFiles.App/Data/Contracts/IVersionControlService.cs` (adicionar métodos de worktrees e ignorados)
  - `src/KyraFiles.App/Services/Git/LibGit2Service.cs` (detached HEAD em `GetRepositoryHeadName`, enumeração de worktrees)
  - `src/KyraFiles.App/Utils/Git/GitHelpers.cs` (fachada estática para worktrees e ignorados)
  - `src/KyraFiles.App/Data/Items/GitWorktreeItem.cs` (novo modelo de dados do worktree)
- **Configurações:**
  - `src/KyraFiles.App/Data/Contracts/IFoldersSettingsService.cs` (`HideGitIgnoredItems`)
  - `src/KyraFiles.App/Services/Settings/FoldersSettingsService.cs` (persistência de `HideGitIgnoredItems`)
- **Ações e Comandos:**
  - `src/KyraFiles.App/Actions/Show/ToggleHideGitIgnoredItemsAction.cs` (nova ação de alternância)
  - `src/KyraFiles.App/Actions/Git/OpenWorktreeFlyoutAction.cs` (nova ação de abertura do flyout)
- **Listagem e Filtragem de Arquivos:**
  - `src/KyraFiles.App/Utils/Storage/Enumerators/Win32StorageEnumerator.cs` (filtro de itens ignorados em background com cache)
  - `src/KyraFiles.App/ViewModels/ShellViewModel.cs` (reação a alterações da configuração e filtro no `AddFileOrFolderAsync`)
- **Interface Visual e Menus:**
  - `src/KyraFiles.App/Helpers/Navigation/NavigationHelpers.cs` (formatação da branch/SHA no título da aba em `UpdateTabInfoAsync`)
  - `src/KyraFiles.App/UserControls/Menus/ClassicMenuBar.xaml` (adicionar item de menu "Ocultar ignorados pelo Git")
  - `src/KyraFiles.App/UserControls/Toolbar.xaml` (adicionar botão/toggle na toolbar)
  - `src/KyraFiles.App/UserControls/StatusBar.xaml` e `StatusBar.xaml.cs` (Flyout de Worktrees e binding)
  - `src/KyraFiles.App/Data/Models/DirectoryPropertiesViewModel.cs` (coleção de worktrees e comandos do flyout)
- **Localização:**
  - `src/KyraFiles.App/Strings/en-US/Resources.resw` (textos, tooltips e labels em inglês)
  - `src/KyraFiles.App/Strings/pt-BR/Resources.resw` (textos, tooltips e labels em português)
- **Testes:**
  - `tests/KyraFiles.App.Search.Index.Tests/LibGit2RepoViewTests.cs` (testes unitários com repo temporário)

---

## 5. Riscos e regressões a vigiar

- **Degradação de navegação em pastas grandes:** O `IsPathIgnored` só roda se habilitado explicitamente, usa cache de prefixos de diretório e roda na thread de enumeração sem bloquear a UI.
- **Worktree apontando para diretório excluído:** `Directory.Exists` verificado antes de disponibilizar o item no flyout para evitar falhas de navegação.
- **Concorrência com outros agentes (AC-7):** Apenas leitura das pastas e referências de worktrees, sem invocar comandos de mutação git.
- **Atraso de rede em diretórios UNC:** Tratamento de timeout e exceções para garantir que compartilhamentos SMB continuem navegáveis.

---

## 6. Estratégia de teste e isolamento

### 6.1 Isolamento de arquivos de teste (REGRA DURA)
- Todo teste unitário que cria ou manipula repositório Git usará diretório temporário restrito em `%TEMP%\agy\<GUID>\KyraFiles\GitTestRepo\` (com GUID gerado por execução de teste).
- O diretório é rigorosamente removido no método anotado com `[TestCleanup]`. Nunca criar pastas de teste em `Downloads`, `Documentos` ou raiz de `%TEMP%`.

### 6.2 Medição comparativa do AC-5 (§18.5)
- O AC-5 não é uma estimativa genérica. Deve ser medido executando o mesmo script de abertura e contagem de itens em um repositório grande antes (baseline na `colab`) e depois (na branch da feature).
- Execução mandatória de `pwsh tools\perf\verify_no_index_service.ps1` com CPU em repouso (< 5%) e ausência de processos anômalos.

### 6.3 Classificação de estado dos ACs (Regra de Interface)
- Conforme regra 7 da fila: **AC-1, AC-2, AC-3 e AC-7 são cenários visuais de interface.**
- Testes unitários/lógicos aprovam a camada de dados e enumeração, mas **o cenário de aceite da spec permanece como `NOT TESTED` até a execução real com o aplicativo aberto e comprovação física de tela.**

| Cenário | Tipo de verificação | Como se prova | Estado inicial |
|---|---|---|---|
| AC-1 | Lógica + Interface | Teste unitário de `IsPathIgnored` com pasta ignorada + Prova de tela no app aberto ocultando/exibindo itens | Lógica unitária provada; AC de tela `NOT TESTED` até inspeção visual |
| AC-2 | Lógica + Interface | Teste de detecção de branch + Prova de tela do título da aba mostrando `{NomePasta} ({branch})` | Lógica unitária provada; AC de tela `NOT TESTED` até inspeção visual |
| AC-3 | Lógica + Interface | Teste unitário de enumeração de worktrees + Prova de tela do flyout aberto listando os worktrees | Lógica unitária provada; AC de tela `NOT TESTED` até inspeção visual |
| AC-4 | Lógica / Unitário | Teste unitário: diretório fora de repositório retorna null sem exceção e título permanece inalterado | `CONFIRMED` via teste unitário |
| AC-5 | Performance | Medição comparativa de abertura de pasta contra o baseline da `colab` (§18.5) e `verify_no_index_service.ps1` | `CONFIRMED` via script de medição |
| AC-6 | Lógica / Unitário | Teste unitário: repositório com HEAD desacoplado retorna os 7 caracteres do SHA do commit | `CONFIRMED` via teste unitário |
| AC-7 | Lógica + Interface | Teste unitário de enumeração concorrente + Prova de tela de worktree de outro agente listado | Lógica unitária provada; AC de tela `NOT TESTED` até inspeção visual |
