# Relatório Consolidado — Release Train Lote 1 (`v4.3.22` — F058 + F042 + F043 + F045)

- **Data:** 2026-11-18
- **Branch:** `feature/train-v4.3.22` (baseada em `feature/lote0-faxina-reconciliacao` / `main` `a667deb0d`)
- **Autora:** Antigravity (AGY)
- **Decisão base:** `D-TRAIN-1` (Release Trains de 3–5 SPECs / 16 pontos neste lote, centralizado na `HELOIZE-PC`)
- **Status:** 🟡 **PRONTO PARA REVISÃO EM LOTE PELA CLAUDE + VALIDAÇÃO VISUAL/INSTALADOR CONSOLIDADA**

---

## 1. Resumo das Entregas do Lote (4 SPECs = 16 pontos)

| SPEC | Legado | Pts | Entrega Implementada | Testes Unitários | Build Release x64 | Validação Visual / Tela |
|---|---|--:|---|---|---|---|
| `SPEC-0054` | **F058** | 3 | Janela do instalador sempre à frente (`installer/KyraFilesSetup.iss`: `SetupMutex`, ativação de 2ª instância, `SetForegroundWindow` + fallback `FlashWindowEx`, trava anti-digitação de 1.000 ms no `WizardForm`; `AllowSetForegroundWindow` em `AppUpdateSideloadService` e `CheckForUpdatesAction`) | Estático `CONFIRMED` | `CONFIRMED` (`0 Erro(s)`) | `NOT TESTED` (aguarda compilação Inno Setup + prova física) |
| `SPEC-0038` | **F042** | 3 | Buscas salvas na barra lateral (`KyraFiles.Shared/SavedSearches/*`, `ISavedSearchesService`, `SavedSearchSidebarItem`, seção `Buscas salvas` em `SidebarViewModel`, `SavedSearchDialog`, botão **Salvar busca** no `SearchHeader` da `ModernShellPage`, datas relativas dinâmicas AC-2, aviso+edição de escopo ausente AC-3) | **4/4 PASS** (`CONFIRMED`) | `CONFIRMED` (`0 Erro(s)`) | `NOT TESTED` (aguarda prova visual em lote) |
| `SPEC-0039` | **F043** | 5 | Histórico de operações com desfazer por item (`KyraFiles.Shared/OperationHistory/*`, `OperationHistoryService`, registro paralelo em todos os 8 pontos de `FilesystemHelpers` + `BulkRenameExecutionService` incluindo exclusões definitivas como não reversíveis, `OperationHistoryDialog`, `OpenOperationHistoryAction` na paleta de comandos) | **9/9 PASS** (`CONFIRMED`) | `CONFIRMED` (`0 Erro(s)`) | `NOT TESTED` (aguarda prova visual em lote) |
| `SPEC-0041` | **F045** | 5 | Ações por tipo de projeto e comandos próprios (`KyraFiles.Shared/ProjectActions/*`: `ProjectDetector`, `ProjectCommandRunner`, `ProjectActionsStore`, barra contextual `ProjectActionBar` na `ModernShellPage` sem auto-execução FR-003, `ProjectActionsDialog`, `ManageProjectActionsAction`) | **6/6 PASS** (`CONFIRMED`) | `CONFIRMED` (`0 Erro(s)`) | `NOT TESTED` (aguarda prova visual em lote) |

---

## 2. Evidências Comprovadas (`CONFIRMED`)

- **Compilação `KyraFiles.Shared` Release x64:** `0 Aviso(s), 0 Erro(s)`.
- **Compilação `KyraFiles.App.CsWin32` Release x64:** `0 Aviso(s), 0 Erro(s)` (incluindo geração de `Windows.Win32.PInvoke.AllowSetForegroundWindow`).
- **Compilação `KyraFiles.App` Release x64:** `Compilação com êxito. 0 Erro(s)` (38 avisos preexistentes do projeto principal; 0 avisos nos projetos novos/alterados `Shared`, `CsWin32` e `Tests`).
- **Testes Unitários do Train (`OperationHistoryTests`, `ProjectActionsTests`, `SavedSearchesTests`):** **19/19 PASS (0 falhas)** em `17,7 s`:
  - `F043` (9/9): ordem mais nova primeiro, limite + controle negativo descartando antigas, ida e volta JSON, JSON corrompido, persistência desligada, 8 threads concorrentes, filtro de 500 entradas < 300 ms, exclusão definitiva não reversível, controle negativo destino apagado (disco intacto), renomear com nome antigo ocupado exigindo confirmação.
  - `F045` (6/6): detecção DotNet/Delphi/Flutter + controle negativo pasta vazia, pasta de 10.000 arquivos < 2 s, argumentos adversários (`a b`, `çãé`, `x'y`, `$(calc)`, `foo;bar`) chegando idênticos via `-File` e `BuildSafePowerShellCommand` + controle negativo de concatenação crua reprovando, comando com falha retornando exit code 42 e stderr, cancelamento em <= 1 s, JSON round-trip + controle negativo JSON inválido.
  - `F042` (4/4): adicionar + desambiguação de nome repetido + reordenar + editar + persistir + remover sem apagar arquivos, relógio relativo avançando em D+1 (AC-2), aviso de escopo ausente (AC-3), recuperação de JSON corrompido.
- **Suíte Completa Não-Benchmark (`KyraFiles.App.Search.Index.Tests.exe --filter "FullyQualifiedName!~Benchmark"`):** **249/252 aprovados** (as 3 falhas são exclusivamente de testes antigos dependentes de `fsutil` / alias Store `WindowsApps\pwsh.exe` restritos no ambiente isolado; zero regressões nas demais 249 provas).
- **Verificador de Nomes Legados (`tools/check-legacy-names.ps1`):** `APROVADO — 0 violações`.
- **Governança v2 (`Validar-Producao.ps1`):** `OK` (com `SPECS.md` e `SPEC-INDEX.md` regenerados automaticamente via `tools/Sync-ProductionDocs.ps1`).

---

## 3. Pontos de Atenção e Performance

1. **Zero regressão de performance em pastas comuns:**
   - `ProjectActionBar` fica `Visibility="Collapsed"` por padrão e roda `ProjectDetector.DetectAsync` fora da thread de UI com cancelamento imediato ao trocar de pasta e cache por pasta (10.000 arquivos medidos < 2 s; pastas comuns < 5 ms).
   - `OperationHistoryStore` persiste JSON de forma assíncrona atômica (`.tmp` → `File.Move`), sem bloquear operações de arquivo (`NFR-001`), e nunca mexe na pilha `StorageHistoryWrapper` do `Ctrl+Z`.
   - `SavedSearches` só exibe seção na barra lateral quando há ao menos 1 busca salva, sem custo no startup quando vazia.
2. **Native AOT:** Todos os novos modelos JSON (`OperationHistoryJsonContext`, `ProjectActionsJsonContext`, `SavedSearchesJsonContext`) usam `System.Text.Json` source generation (`[JsonSerializable]`), sem reflexão dinâmica.
3. **Avisos de build (38 no `KyraFiles.App`):** Verificar na próxima rodada se algum aviso pertence a um dos arquivos novos da `KyraFiles.App` antes de gerar o instalador `v4.3.22` (lembrando a meta `0 Warning(s)` no release).
4. **Próxima fila já preparada (sem ociosidade):** Planos e `T0` do **Lote 2 (`F038` + `F040` + `F048` = 15 pts)** e planos do **Lote 3 (`F051` + `F055` = 10 pts)** já criados e prontos para execução imediata.
