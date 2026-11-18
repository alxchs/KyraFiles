# F051 — Busca de código no estilo ripgrep — PLAN

Status: APROVADO no Lote 3 (`D-TRAIN-1`, 18/11/2026). Spec aprovada pelo Alexandre em 24/10/2026.

## 1. Abordagem (Motor Próprio em `KyraFiles.Shared/CodeSearch/` + Diálogo em `KyraFiles.App`)

1. **`GitIgnoreMatcher` + `CodeSearchEngine` (puro, streaming, cancelável):**
   - Lê `.gitignore` (e padrões padrão como `.git/`, `bin/`, `obj/`, `node_modules/`) com opção `RespectGitIgnore` (AC-2).
   - Detecta arquivos binários (presença de byte `\0` nos primeiros 8 KiB) e arquivos acima do limite configurável (padrão 2 MB), contando-os em `SkippedBinaryOrLargeCount` (AC-5).
   - Abre arquivos com `FileShare.ReadWrite | FileShare.Delete` (FR-002: nunca bloqueia escrita externa).
   - Suporta UTF-8 (com e sem BOM) e UTF-16 LE/BE (AC-7) via `StreamReader(..., detectEncodingFromByteOrderMarks: true)`.
   - Valida regex **antes** de varrer e devolve mensagem clara se inválida sem iniciar a busca (AC-4).
   - Emite resultados incrementais (`file`, `lineNumber`, `lineSnippet`) via callback/`IAsyncEnumerable` com limite configurável e cancelamento em <= 1s (AC-1, AC-6, FR-001, NFR-001).
2. **Abrir na IDE na linha exata (AC-3, FR-003):**
   - Reutiliza `IDevToolsSettingsService.IDEPath` + formato configurável (`code -g "{arquivo}:{linha}"`, Rider, Notepad++).
3. **UI (`CodeSearchDialog` + `OpenCodeSearchAction` `[GeneratedRichCommand]`):**
   - Registrado em `AppDialogService.cs` (`DialogService`).
