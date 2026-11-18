# F055 — Mapa do repositório — PLAN

Status: APROVADO no Lote 3 (`D-TRAIN-1`, 18/11/2026). Spec aprovada pelo Alexandre em 24/10/2026 (US3 diagrama de projetos adiado conforme recomendação §8).

## 1. Abordagem (Motor Puro em `KyraFiles.Shared/RepoMap/` + Diálogo em `KyraFiles.App`)

1. **`RepoMapAnalyzer` (somente leitura — FR-001, cancelável <= 1s — AC-7):**
   - Mapeia extensões para linguagens (tabela editável — FR-003), respeita `.gitignore` com toggle (AC-5), ignora binários e conta arquivos, linhas e bytes com somatório exato (AC-1, AC-2).
   - Pontos quentes (`churn × tamanho` nos últimos 12 meses) lidos em segundo plano via `git log --name-only --since="12 months ago"` (ou `LibGit2Sharp`) quando a pasta é repositório Git; fora de Git explica claramente e mantém o resumo por linguagem ativo (AC-3, AC-4).
   - Exportação direta para CSV e Markdown (FR-002).
2. **UI (`RepoMapDialog` + `OpenRepoMapAction` `[GeneratedRichCommand]`):**
   - Registrado em `AppDialogService.cs` (`DialogService`).
