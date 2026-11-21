# F051 — Busca de código no estilo ripgrep — TASKS

Status: CONCLUÍDAS (T1 a T3) (21/11/2026).
Relatório de conclusão: `docs/specs/F051-code-search/RELATORIO.md`.

- [x] **T1 — `CodeSearchEngine` + `GitIgnoreMatcher` em `KyraFiles.Shared/CodeSearch/` (FR-001, FR-002, NFR-001; AC-1, AC-2, AC-4, AC-5, AC-6, AC-7).** Testes unitários cobrindo UTF-8/BOM/UTF-16 com acentos, `.gitignore`, regex inválida, binários/grandes ignorados, cancelamento <= 1s e benchmark de 1.000 arquivos <= 3s (12/12 testes verdes).
- [x] **T2 — `CodeSearchDialog` + `OpenCodeSearchAction` + abertura na IDE por linha (AC-3, FR-003).**
- [x] **T3 — Build Release x64 (0/0) + Validação Gate F021 e nomes legados aprovados.**
