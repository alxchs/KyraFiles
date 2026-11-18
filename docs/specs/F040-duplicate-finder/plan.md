# F040 — Localizar arquivos duplicados — PLAN

Status: APROVADO no Lote 2 (`D-TRAIN-1`, 18/11/2026). Spec aprovada pelo Alexandre em 24/10/2026.

## 1. Abordagem (Núcleo Puro em `KyraFiles.Shared/DuplicateFinder/` + Diálogo em `KyraFiles.App`)

1. **`DuplicateFinderEngine` (puro, streaming, cancelável):**
   - **Estágio 1 (Tamanho):** enumera pastas selecionadas (ignorando pastas configuradas como `.git`, `node_modules`, arquivos abaixo do tamanho mínimo e arquivos com atributo `FileAttributes.System` por padrão — AC-7, FR-004). Arquivos inacessíveis/em uso entram em `UnverifiedFiles` com o motivo sem interromper a varredura (AC-6). Agrupa por `Length` e descarta todos os grupos com apenas 1 arquivo (**NFR-001**: nunca lê disco de arquivo com tamanho único).
   - **Estágio 2 (Quick-Hash 4 KiB):** lê apenas os primeiros 4 KiB dos candidatos do mesmo tamanho para eliminar falsos positivos baratos.
   - **Estágio 3 (SHA-256 Streaming):** calcula SHA-256 em blocos fixos de 64 KiB (memória O(1) por arquivo, nunca carrega arquivo inteiro na RAM — NFR-001) para grupos sobreviventes.
   - **Modo rápido opcional (decisão §8):** `DuplicateCompareMode.NameAndSizeUnverified` rotulado explicitamente como *"não garantido"*.
   - **Cancelamento imediato (AC-5):** `CancellationToken` checado em cada estágio e bloco de leitura; nenhuma alteração em disco durante a análise.
2. **`DuplicateSelectionRules` (proteção FR-002 / AC-3 / AC-4):**
   - Regras: `KeepOldest`, `KeepNewest`, `KeepInFolder(preferredFolder)`.
   - Invariante rígida aplicada em `ToggleSelection` e `ApplyRule`: **sempre mantém ao menos 1 cópia desmarcada por grupo** (retorna `false` se o usuário tentar marcar a última cópia restante).
3. **Exclusão segura (FR-003):** envia somente os marcados para a **Lixeira** via `IFilesystemHelpers.DeleteItemsAsync(..., permanently: false, registerHistory: true)` (integrado automaticamente com o histórico da F043!).

## 2. Arquivos a mexer
- Novos: `src/KyraFiles.Shared/DuplicateFinder/DuplicateFinderEngine.cs`, `DuplicateSelectionRules.cs`; `src/KyraFiles.App/Dialogs/DuplicateFinderDialog.xaml(.cs)`, `ViewModels/Dialogs/DuplicateFinderDialogViewModel.cs`, `Actions/FileSystem/OpenDuplicateFinderAction.cs`; `tests/KyraFiles.App.Search.Index.Tests/DuplicateFinderTests.cs`.
- Editar: `src/KyraFiles.App/Services/App/AppDialogService.cs`.
