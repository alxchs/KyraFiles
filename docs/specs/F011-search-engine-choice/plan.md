# F011 — Motor de busca escolhido pelo usuário (F3 / Ctrl+F) — PLAN

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Arquitetura
- **Abstração:** `ISearchProvider` em `KyraFiles.App.Search`.
- **Implementações:** `NativeFilesSearchProvider` e `EverythingSearchProvider`.
- **Fábrica:** `SearchProviderFactory` selecionando a implementação a partir de `FoldersSettingsService.SearchEnginePreference`.
- **Roteamento:** `SearchAction` (F3 / Ctrl+F) invocando `ShellViewModel.SearchAsync` através do provedor ativo.
