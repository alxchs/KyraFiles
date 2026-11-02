# F082 — Janela Modal de Configurações (Settings Modal Window) — TASKS

Estado: **CONCLUÍDO** (01/11/2026).
Branch: `feature/f082-settings-modal-window`

---

## Fases de Execução

- [x] **Fase 1: Especificação e Planejamento**
  - [x] Criar `spec.md` detalhando histórias de usuário, requisitos e cenários Given/When/Then.
  - [x] Criar `plan.md` mapeando a arquitetura Win32 modal e desacoplamento de abas.
  - [x] Criar `tasks.md` com o checklist de execução.

- [x] **Fase 2: Implementação da Janela Modal (`SettingsDialog.cs`)**
  - [x] Criar `src/Files.App/Views/Settings/SettingsDialog.cs` com suporte a `WindowEx`, modalidade Win32 (`GWL_HWNDPARENT`, `EnableWindow`), centralização, atalho Escape e gestão de ciclo de vida singleton.
  - [x] Ajustar `src/Files.App/Views/Settings/SettingsPage.xaml.cs` para suportar execução autônoma sem requerer `AssociatedTabInstance`.

- [x] **Fase 3: Roteamento de Ações e Limpeza de Abas**
  - [x] Atualizar `src/Files.App/Actions/Open/OpenSettingsAction.cs` para chamar `SettingsDialog.Show`.
  - [x] Atualizar `src/Files.App/Actions/Open/OpenAboutAction.cs` para chamar `SettingsDialog.Show(SettingsPageKind.AboutPage)`.
  - [x] Ajustar `NavigationHelpers.cs` para interceptar e redirecionar qualquer pedido de navegação para "Settings" para o `SettingsDialog`.
  - [x] Atualizar `AppLifecycleHelper.cs` e `MainPageViewModel.cs` para purgar e filtrar abas legadas de `"Settings"` da lista de restauração de sessão.

- [x] **Fase 4: Testes, Compilação e Gates**
  - [x] Compilar projeto completo via `mkfile`.
  - [x] Executar suíte de testes unitários (`dotnet test`): 192 passaram, 0 falhas, 1 pulado intencionalmente.
  - [x] Executar e validar gate obrigatório F021 (`verify_no_index_service.ps1`).
  - [x] Gerar relatório formal `RELATORIO.md`.
  - [ ] Integrar na `colab` via squash/rebase linear.
