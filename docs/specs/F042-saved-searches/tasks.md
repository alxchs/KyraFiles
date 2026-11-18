# F042 — Buscas salvas na barra lateral — TASKS

Status: APROVADAS no Lote 1 (`D-TRAIN-1`, 18/11/2026). Branch: `feature/train-v4.3.22`.

- [ ] **T1 — Núcleo puro (`SavedSearchDefinition`, `SavedSearchQueryEvaluator`, `SavedSearchStore`) (FR-001, FR-003; AC-1 a AC-5, AC-7).** Modelo AOT-safe (`JsonSerializerContext`), avaliação dinâmica de datas relativas com relógio injetável (AC-2), checagem de escopo inexistente (AC-3), persistência atômica, reordenação e testes unitários completos em `SavedSearchesTests.cs`.
- [ ] **T2 — Serviço e Seção na Barra Lateral (`ISavedSearchesService`, `SavedSearchSidebarItem`, `SectionType.SavedSearches`) (US1, US3; AC-1, AC-4, AC-5, AC-7).** Registro DI em `AppLifecycleHelper.cs`, menu de contexto (Editar/Remover sem apagar arquivos) e reordenação na barra lateral.
- [ ] **T3 — Botão "Salvar busca" e Execução Dinâmica no `SearchHeader` / `SidebarViewModel` (US1, US2; AC-1, AC-2, AC-3, AC-6, FR-002).** Botão no `ModernShellPage.xaml` (`SearchHeader`), aviso claro para escopo ausente com edição direta (AC-3), execução via provedor ativo (`Everything` / `Native`).
- [ ] **T4 — Build 0/0 + Validação Consolidada do Train v4.3.22 (NFR-001).** Provar build Release x64 sem avisos, testes verdes e validação em lote do Train.
