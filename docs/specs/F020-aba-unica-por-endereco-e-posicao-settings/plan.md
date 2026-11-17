# F020 — Aba única por endereço e aba de Configurações na primeira posição — PLAN

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Arquitetura Atual

- **Gestão de Abas (`MainPageViewModel.AppInstances`):**
  - As abas ativas são armazenadas em `public static ObservableCollection<TabBarItem> AppInstances`.
  - Cada `TabBarItem` possui `NavigationParameter` do tipo `TabBarItemParameter`, que contém `InitialPageType` e `NavigationParameter` (`string` ou `PaneNavigationArguments`).
  - O conteúdo da aba (`ContentFrame.Content`) implementa `ITabBarItemContent`, cujo `TabBarItemParameter` reflete o caminho atual do painel ativo.
- **Criação de Abas (`NavigationHelpers.cs`):**
  - `AddNewTabByPathAsync(Type type, string? path, bool switchToNewTab, int atIndex = -1)`
  - `AddNewTabByParamAsync(Type type, object? tabViewItemArgs, int atIndex = -1, bool switchToNewTab = true)`
  - Atualmente, ambos os métodos inserem cegamente uma nova aba sem verificar se o endereço já está presente em `AppInstances`.
  - Quando `atIndex == -1`, a nova aba é inserida no final (`AppInstances.Count`).
- **Abertura de Configurações:**
  - `OpenSettingsAction.cs` chama `NavigationHelpers.OpenPathInNewTab("Settings", true)` ou `AddNewTabByParamAsync`.
  - `SidebarViewModel.cs` (ao clicar em Configurações sem modificador) chama `settingsShellPage.NavigateToSettings()`, que sobrescreve o conteúdo da aba de pasta ativa.

---

## 2. Abordagem Proposta

### 2.1. Normalização e Comparação de Caminhos
Criar em `NavigationHelpers`:
```csharp
public static string NormalizeNavigationPath(string? path)
```
- Se nulo ou vazio -> `"Home"`
- Remove barras finais redundantes (exceto raiz de unidade como `C:\`)
- Trata `"Settings"`, `"Home"`, `"ReleaseNotes"` como identificadores canônicos
- Comparação case-insensitive (`StringComparer.OrdinalIgnoreCase`)

### 2.2. Detecção de Aba Existente
Método auxiliar em `NavigationHelpers`:
```csharp
public static int FindExistingTabIndex(string? path)
public static int FindExistingTabIndex(object? navigationArg)
```
- Percorre `MainPageViewModel.AppInstances`
- Extrai o caminho representativo de cada aba (via `tab.NavigationParameter` ou `tab.TabItemContent?.TabBarItemParameter`)
- Compara com o caminho de destino normalizado
- Retorna o índice da aba correspondente (ou `-1` se não existir)

### 2.3. Roteamento Inteligente em `AddNewTabByPathAsync` e `AddNewTabByParamAsync`
1. **Se for "Settings":**
   - Forçar o índice alvo para `0`.
   - Se já existir uma aba de Settings (em qualquer índice `idx`):
     - Se `idx != 0`, mover para `0`: `MainPageViewModel.AppInstances.Move(idx, 0)`.
     - Definir `App.AppModel.TabStripSelectedIndex = 0`.
     - Retornar sem criar nova aba.
   - Se não existir:
     - Inserir no índice `0` (`atIndex = 0`).
2. **Se for outro caminho:**
   - Consultar `FindExistingTabIndex`.
   - Se encontrar aba existente no índice `idx`:
     - Se `switchToNewTab == true`, definir `App.AppModel.TabStripSelectedIndex = idx`.
     - Retornar sem criar nova aba.
   - Se não existir, prosseguir com a inserção normal.

### 2.4. Ajuste no Botão de Configurações da Barra Lateral (`SidebarViewModel.cs`)
- Ao clicar no item de Configurações na barra lateral:
  - Redirecionar para `NavigationHelpers.OpenPathInNewTab("Settings", true)`.
  - Isso garante que as Configurações sempre abram/foquem a aba no índice 0, sem sobrescrever a pasta atualmente aberta na aba ativa.

### 2.5. Navegação no Omnibar / Barra de Endereços
- Ao digitar um caminho ou navegar dentro de uma aba existente:
  - Se o caminho destino já estiver aberto em outra aba (`otherIdx != currentIdx`):
    - Selecionar a aba existente (`App.AppModel.TabStripSelectedIndex = otherIdx`), mantendo a integridade da regra "sempre abas com endereços distintos".

---

## 3. Arquivos Envolvidos

### Arquivos a Alterar:
1. `src/KyraFiles.App/Helpers/Navigation/NavigationHelpers.cs` (normalização, busca de duplicidade, inserção forçada de Settings no índice 0, movimentação de Settings existente).
2. `src/KyraFiles.App/ViewModels/UserControls/SidebarViewModel.cs` (roteamento do clique de Settings para a aba na posição 0).
3. `src/KyraFiles.App/Actions/Navigation/DuplicateSelectedTabAction.cs` (verificação de aba duplicada).
4. `src/KyraFiles.App/ViewModels/UserControls/NavigationToolbarViewModel.cs` (redirecionamento quando a barra de endereços recebe caminho já aberto).

### Arquivos a NÃO Alterar:
- `TabBar.xaml` e estilos visuais de controle.
- Páginas de configurações internas (`src/KyraFiles.App/Views/Settings/*`).
- Mecanismo de persistência de abas ao fechar/restaurar (`TabBarItemParameter.Serialize`).

---

## 4. Riscos e Mitigações

| Risco | Impacto | Mitigação |
|---|---|---|
| Mover a aba de Settings para 0 quebrar índices durante o carregamento de inicialização | Baixo | Só executar movimentação quando `AppInstances.Count > 0` e via `DispatcherQueue` UI thread |
| Falso positivo na comparação de caminhos (ex: subpasta vs pasta pai) | Médio | Normalização rigorosa com separador padrão e comparação estrita |
| Múltiplos cliques rápidos no ícone de Settings gerarem abas concorrentes | Baixo | A busca em memória é síncrona na UI thread antes do `Insert` |
