# F083 — Identidade KyraFiles: Marca, Ícones do Windows e Imagem no About — TASKS

Estado: **CONCLUÍDO** (01/11/2026).
Branch: `feature/f083-kyrafiles-brand-and-icon`

---

## Fases de Execução

- [x] **Fase 1: Processamento de Imagens e Geração de Ícones**
  - [x] Copiar `KyraFiles.png` para os assets do app (`Assets/KyraLogo.png`, `Assets/KyraPet.png`, `Assets/AppTiles/Dev/`, `Release/`, etc.).
  - [x] Gerar novo `Logo.ico` multi-resolução de alta fidelidade (16x16 a 256x256) a partir da mascote Kyra.
  - [x] Substituir `Logo.ico` em `Assets/AppTiles/Dev/`, `Preview/` e `Release/`.
  - [x] Atualizar todos os tiles PNG do app em `Assets/AppTiles/Dev/` e `Release/` eliminando as pastinhas rosa antigas.

- [x] **Fase 2: Tela Sobre (About) e Exibição do Pet**
  - [x] Atualizar `AboutPage.xaml` para exibir a imagem da mascote Kyra (`BitmapIcon` com `ShowAsMonochrome="False"`) no cabeçalho do `SettingsCard`.
  - [x] Atualizar `AboutViewModel.cs` para garantir exibição do nome `KyraFiles`.

- [x] **Fase 3: Harmonização de Strings e Mensagens ao Usuário**
  - [x] Atualizar `Strings/en-US/Resources.resw` substituindo referências ao nome do app por "KyraFiles".
  - [x] Atualizar `Strings/pt-BR/Resources.resw` substituindo referências ao nome do app por "KyraFiles".
  - [x] Atualizar `Strings/es-ES/Resources.resw` e `Strings/de-DE/Resources.resw`.
  - [x] Atualizar `Package.appxmanifest` com `DisplayName="KyraFiles"` e `ShortName="KyraFiles"`.

- [x] **Fase 4: Validação, Testes e Integração**
  - [x] Compilar projeto completo via `mkfile` (0 erros).
  - [x] Executar suíte de testes unitários (`dotnet test`): 192 passaram, 0 falhas, 1 ignorado intencionalmente.
  - [x] Executar e validar gate obrigatório F021 (`verify_no_index_service.ps1`).
  - [x] Gerar relatório formal `RELATORIO.md`.
  - [ ] Integrar via `/commit-push` e `/merge` autorizados pelo Alexandre.
