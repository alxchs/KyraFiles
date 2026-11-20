# F082 — Janela Modal de Configurações (Settings Modal Window) — PLAN

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Arquitetura e Estratégia de Implementação

### 1.1. Ciclo de Vida da Janela Modal (`SettingsDialog.cs`)
Inspirado na arquitetura limpa de `ToolbarCustomizationDialog.cs` e `HexEditorWindow.cs`:
1. `SettingsDialog` será uma classe estática em `src/KyraFiles.App/Views/Settings/SettingsDialog.cs`.
2. Mantém referência `private static WindowEx? _settingsWindow;` e `private static SettingsPage? _settingsPage;`.
3. Método `Show(SettingsPageKind pageKind = SettingsPageKind.GeneralPage)`:
   - Se `_settingsWindow` estiver ativa:
     - Traz a janela para frente com `_settingsWindow.Activate()` e `PInvoke.SetForegroundWindow`.
     - Navega a página interna para `pageKind`: `_settingsPage?.NavigateTo(new SettingsNavigationParams { PageKind = pageKind });`.
     - Retorna.
   - Se não existir:
     - Cria `WindowEx(minWidth: 800, minHeight: 520)`.
     - Aplica `SystemBackdrop = new AppSystemBackdrop(true)`.
     - Ajusta dimensões (960 x 680 lógicos * DPI) e centraliza sobre a `MainWindow`.
     - Configura título `Strings.Settings.GetLocalizedResource()` e ícone da aplicação.
     - Aplica o tema ativo via `IAppThemeModeService`.
     - Adiciona a `SettingsPage` como conteúdo (ou dentro de um Frame).
     - **Modalidade Win32**:
       - `nint parentHwnd = MainWindow.Instance.WindowHandle;`
       - Se `parentHwnd != 0`:
         - `PInvoke.SetWindowLongPtr(new HWND(_settingsWindow.WindowHandle), WINDOW_LONG_PTR_INDEX.GWL_HWNDPARENT, parentHwnd);`
         - `PInvoke.EnableWindow(new HWND(parentHwnd), false);`
       - No evento `Closed` de `_settingsWindow`:
         - Se `parentHwnd != 0`:
           - `PInvoke.EnableWindow(new HWND(parentHwnd), true);`
           - `PInvoke.SetForegroundWindow(new HWND(parentHwnd));`
         - `_settingsWindow = null;`
         - `_settingsPage = null;`
     - Invoca `_settingsWindow.AppWindow.Show()` e `_settingsWindow.Activate()`.

### 1.2. Desacoplamento da `SettingsPage.xaml.cs`
Atualmente `SettingsPage.xaml.cs` espera `NavigationArguments.AssociatedTabInstance` no `OnNavigatedTo`.
Ao ser hospedada em uma janela modal independente:
- No `SettingsPage`:
  - Permitir carregar e navegar diretamente sem exigir `AssociatedTabInstance`.
  - Se `appInstance == null`, as atualizações de abas e toolbar de abas são ignoradas com segurança.
  - O método público `NavigateTo(SettingsNavigationParams navParams)` já existe e troca de aba perfeitamente.

### 1.3. Redirecionamento das Ações do Sistema
1. **`OpenSettingsAction.cs`**:
   - Atualizado para chamar `SettingsDialog.Show(pageKind)`.
   - Nenhuma chamada a `NavigationHelpers.OpenPathInNewTab("Settings")` ou `AddNewTabByParamAsync`.
2. **`OpenAboutAction.cs`**:
   - Atualizado para chamar `SettingsDialog.Show(SettingsPageKind.AboutPage)`.
3. **`NavigationHelpers.cs`**:
   - Se qualquer navegação tentar abrir `"Settings"` como caminho, redirecionar para `SettingsDialog.Show()` e não criar aba.
   - Ao desserializar `LastSessionTabList`, ignorar qualquer item apontando para `"Settings"`.

---

## 2. Arquivos Modificados / Criados

- `src/KyraFiles.App/Views/Settings/SettingsDialog.cs` (novo)
- `src/KyraFiles.App/Views/Settings/SettingsPage.xaml.cs` (suporte a execução modal sem `AssociatedTabInstance`)
- `src/KyraFiles.App/Actions/Open/OpenSettingsAction.cs` (invocação de `SettingsDialog.Show`)
- `src/KyraFiles.App/Actions/Open/OpenAboutAction.cs` (invocação de `SettingsDialog.Show`)
- `src/KyraFiles.App/Helpers/Navigation/NavigationHelpers.cs` (redirecionamento de chamadas residuais e descarte em restauração de sessão)

---

## 3. Critérios de Teste e Validação

1. **Compilação**: `mkfile r KyraFiles.slnx` sem erros.
2. **Suíte de Testes**: Execução de `dotnet test` com 100% de sucesso.
3. **Gate F021**: `pwsh tools\perf\verify_no_index_service.ps1` aprovado.
4. **Verificação de Regressão**: Nenhuma criação de abas ao acionar Configurações.

