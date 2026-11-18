# F051 — Busca de código no estilo ripgrep — TASKS

Status: APROVADAS no Lote 3 (`D-TRAIN-1`, 18/11/2026).

- [ ] **T1 — `CodeSearchEngine` + `GitIgnoreMatcher` em `KyraFiles.Shared/CodeSearch/` (FR-001, FR-002, NFR-001; AC-1, AC-2, AC-4, AC-5, AC-6, AC-7).** Testes unitários cobrindo UTF-8/BOM/UTF-16 com acentos, `.gitignore`, regex inválida, binários/grandes ignorados, cancelamento <= 1s e benchmark de 10k arquivos <= 3s.
- [ ] **T2 — `CodeSearchDialog` + `OpenCodeSearchAction` + abertura na IDE por linha (AC-3, FR-003).**
- [ ] **T3 — Build Release x64 (0/0) + Validação em Lote do Train v4.3.24.**
