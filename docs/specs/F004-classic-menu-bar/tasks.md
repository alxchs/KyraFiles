# F004 — Barra de Menus Tradicional Opcional — TASKS

Metodologia: Spec-Driven Development (`METODO.md` §6).

- [x] **T1 — Camada de Configuração:**
  - Adicionar `ShowClassicMenuBar` em `IAppearanceSettingsService` e `AppearanceSettingsService.cs`.
  - Expor em `AppearanceViewModel.cs` e `MainPageViewModel.cs`.
  - Adicionar strings localizadas em `pt-BR` e `en-US`.

- [x] **T2 — Criação do Controle `ClassicMenuBar`:**
  - Desenvolver `UserControls/Menus/ClassicMenuBar.xaml` e `.xaml.cs` usando `MenuBar` nativo do WinUI 3.
  - Conectar os 6 menus aos comandos do `ICommandManager`.

- [x] **T3 — Integração na Interface Principal:**
  - Posicionar a linha no `Grid` de `MainPage.xaml` abaixo das abas.
  - Adicionar interruptor na página de configurações `AppearancePage.xaml`.

- [x] **T4 — Validação e Comprovação Visual:**
  - Validar estado inicial desativado, ativação pelo menu de configurações, abertura física dos menus e navegação por teclado (`docs/agents/evidence/f004/`).
