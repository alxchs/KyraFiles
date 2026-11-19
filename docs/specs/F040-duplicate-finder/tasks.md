# F040 — Localizar arquivos duplicados — TASKS

Status: APROVADAS no Lote 2 (`D-TRAIN-1`, 18/11/2026).

- [x] **T1 — `DuplicateFinderEngine` & `DuplicateSelectionRules` em `KyraFiles.Shared/DuplicateFinder/` (FR-001, FR-002, FR-004, NFR-001; AC-1 a AC-7).** Pipeline tamanho → quick-hash → SHA-256 streaming 64 KiB, modo rápido rotulado, regras de seleção com invariante de nunca marcar a última cópia, tratamento de bloqueados/sistema e testes unitários completos (`DuplicateFinderTests.cs`).
- [x] **T2 — `DuplicateFinderDialog` + `OpenDuplicateFinderAction` (`[GeneratedRichCommand]`) (US1–US3; FR-003).** Diálogo com grupos, regras automáticas, contador de espaço recuperável, botão cancelar e exclusão exclusiva para a Lixeira com desfazer.
- [x] **T3 — Build Release x64 (0/0) + Validação em Lote do Train v4.3.23.**
