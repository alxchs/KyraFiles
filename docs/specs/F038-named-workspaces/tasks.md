# F038 — Espaços de trabalho com nome — TASKS

Status: APROVADAS no Lote 2 (`D-TRAIN-1`, 18/11/2026).

- [ ] **T1 — Núcleo puro (`NamedWorkspace`, `WorkspaceTabSnapshot`, `NamedWorkspaceStore`) em `KyraFiles.Shared/Workspaces/` (FR-001, FR-003; AC-1, AC-3, AC-4, AC-5).** Serialização AOT-safe, detecção de nome duplicado, diagnóstico de pastas ausentes e testes unitários em `NamedWorkspacesTests.cs`.
- [ ] **T2 — `INamedWorkspacesService` + `WorkspacesDialog` + `OpenWorkspacesAction` (`[GeneratedRichCommand]`) (US1, US2; FR-002; AC-2, AC-6).** Captura/restauração de abas, fixadas e painel duplo, modos "Substituir" e "Adicionar" sem fechar `MainWindow`.
- [ ] **T3 — Build Release x64 (0/0) + Validação em Lote do Train v4.3.23.**
