# F021 — Desarmar IndexService e Bloqueio de Recursos do KyraFiles — TASKS

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## Lista de Tarefas de Implementação e Verificação

- [x] **T01 — Encerrar instâncias ativas do IndexService**
  - Encerrar forçadamente qualquer processo `KyraFiles.App.IndexService.exe` rodando no Windows. (CONFIRMED: Processos 16340, 14176 e 20720 encerrados).
  - Fechar o `KyraFiles.exe` ativo para que seu supervisor legado não reinicie o processo. (CONFIRMED).

- [x] **T02 — Desarmar o arranque automático no AppLifecycleHelper**
  - Editar `src/KyraFiles.App/Helpers/Application/AppLifecycleHelper.cs` removendo a instanciação de background do `IndexedSearchProvider`. (CONFIRMED).

- [x] **T03 — Bloquear o processo e disponibilidade no IndexedSearchProvider**
  - Editar `src/KyraFiles.App/Services/Search/IndexedSearchProvider.cs`:
    - Desativar `_ = Task.Run(async () => await EnsureReadyAndConfiguredAsync(...))` no construtor. (CONFIRMED).
    - Alterar `public bool IsAvailable => false;` com comentário explicativo de bloqueio emergencial F021. (CONFIRMED).
    - Remover campo não utilizado `_initialConfigSent` para 0 warnings. (CONFIRMED).

- [x] **T04 — Blindar o roteamento de busca no SearchProviderFactory**
  - Em `src/KyraFiles.App/Services/Search/SearchProviderFactory.cs`, garantir que quando `indexProvider.IsAvailable` for falso, o motor caia diretamente para `Everything` (quando configurado) ou `Native`. (CONFIRMED).

- [x] **T05 — Desativar o teste destrutivo de 32 GB de RAM**
  - Em `tests/KyraFiles.App.Search.Index.Tests/VolumeIndexBenchmarkTests.cs`, marcar `Benchmark_FiveMillionEntries_MemoryAndQueryLatency` com `[Ignore("F021: Desativado temporariamente para prevenir estouro de memória")]`. (CONFIRMED).

- [x] **T06 — Compilar o executável Release oficial do KyraFiles**
  - Rodar o build Release de `src/KyraFiles.App/KyraFiles.App.csproj` (`-p:Configuration=Release -p:Platform=x64`). (CONFIRMED: 0 Warning(s), 0 Error(s)).

- [x] **T07 — Atualizar a instalação oficial e limpar artefatos debug/auxiliares**
  - Atualizar os binários em `%LOCALAPPDATA%\Programs\KyraFiles` usando `installer/Update-KyraFiles.ps1`. (CONFIRMED: 20 arquivos atualizados).
  - Remover qualquer binário ou pasta temporária de debug/auxiliar fora do local oficial instalado. (CONFIRMED: Pastas Debug removidas).

- [x] **T08 — Criar script de verificação automatizada `verify_no_index_service.ps1`**
  - Criar `tools/perf/verify_no_index_service.ps1` que inicia o app, mede processos e CPU por 10s e emite o veredito. (CONFIRMED).

- [x] **T09 — Executar o teste prático e medir resultados reais**
  - Executar o script `tools/perf/verify_no_index_service.ps1`. (CONFIRMED: ExitCode 0, IndexService = 0 processos, CPU em repouso = 0%, RAM Working Set = 45.63 MB).

- [x] **T10 — Atualizar governança no AGENTS.md e PLAYBOOK.md**
  - Registrar formalmente no `AGENTS.md` e `docs/agents/PLAYBOOK.md` que a execução do `verify_no_index_service.ps1` é pré-requisito mandatório antes de qualquer tarefa ser considerada concluída no KyraFiles. (CONFIRMED).
