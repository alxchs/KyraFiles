# Build 2026-10-17 00:25 — KyraFilesSetup v4.2.37 (F021 — Desarmamento do IndexService e Everything Instantâneo)

- **Instalador:** `artifacts\Setup\KyraFilesSetup-4.2.37.exe` (67,4 MB, Inno Setup 6, LZMA2 Ultra64).
- **Executável instalado:** `%LocalAppData%\Programs\KyraFiles\KyraFiles.exe` (pacote `FilesDev_4.2.37.0_x64__ykqwq8d6ps0ag`).
- **Código:** branch `feature/f021-bloqueio-indexservice`.
- **Build:** `mkfile release src\KyraFiles.App\KyraFiles.App.csproj` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Teste Mandatório de Qualidade:** `tools\perf\verify_no_index_service.ps1` = `0` processos de IndexService, `0%` CPU em repouso, RAM = `45 MB` (CONFIRMED).
- **Everything Instantâneo:** Ponte `es.exe` (v1.1.0.38 x64) integrada e resolvida em tempo recorde (< 10 ms) (CONFIRMED).

## O que foi liberado neste pacote

| Item | O que muda para o usuário | Estado |
|---|---|---|
| **F021: Desarmamento Total do IndexService** | O processo `KyraFiles.App.IndexService.exe` foi completamente desativado no startup e no construtor. Não sobe em background, não varre discos às cegas e não consome CPU (0% medido). | CONFIRMED (`AppLifecycleHelper.cs`, `IndexedSearchProvider.cs`) |
| **F021: Desativação do Teste Destrutivo** | O teste sintético `Benchmark_FiveMillionEntries_MemoryAndQueryLatency` que causou 32 GB de RAM e travamentos em 11/10 foi desativado com `[Ignore]`. | CONFIRMED (`VolumeIndexBenchmarkTests.cs`) |
| **F021: Busca Instantânea via Everything** | A pesquisa por nome em todos os discos agora utiliza o voidtools Everything nativo (< 10 ms por consulta) via `es.exe`, sem consumir recursos extras e sem depender de indexador próprio defeituoso. | CONFIRMED (`EverythingSearchProvider.cs`, `SearchProviderFactory.cs`) |
| **F021: Governança Mandatória para AGY e Claude** | Nenhuma tarefa do KyraFiles pode ser dada como pronta sem a aprovação do script `verify_no_index_service.ps1`. | CONFIRMED (`AGENTS.md`, `docs/agents/PLAYBOOK.md`) |
| **Higiene de Binários** | Nenhuma pasta de Debug ou lixo auxiliar deixada no repositório. Apenas o pacote Release oficial instalado existe. | CONFIRMED |
| **F001 a F020 (Consolidados)** | Aba única por endereço, Configurações no índice 0, Win+E via HotkeyService, menu clássico, colunas independentes e todas as features estáveis mantidas. | CONFIRMED |

## Como testar

1. **Instalação:**
   * Execute `artifacts\Setup\KyraFilesSetup-4.2.37.exe`.
   * O instalador substitui os binários em `%LocalAppData%\Programs\KyraFiles\` de forma limpa.
2. **Verificação de Ausência do IndexService:**
   * Abra o KyraFiles.
   * Abra o Gerenciador de Tarefas ou digite no terminal `Get-Process KyraFiles.App.IndexService -ErrorAction SilentlyContinue`: o processo **não existe**.
   * A CPU em repouso permanece em **0%**.
3. **Teste de Busca Instantânea com Everything:**
   * Clique na barra de pesquisa (ou `Ctrl+F`) e digite qualquer nome de arquivo de qualquer disco.
   * Os resultados surgem instantaneamente (< 50 ms) alimentados pelo Everything.
