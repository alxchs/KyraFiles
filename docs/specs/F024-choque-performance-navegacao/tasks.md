# F024 — Lista de Tarefas (Tasks)

## Status: Bateria de Testes Executada — Pronto para Validação do Usuário

---

### T1 — Registro e Setup do Laboratório de 1.600 Arquivos
- [x] Criar `tools\perf\setup_1600_lab.ps1` para gerar exatamente 1.600 arquivos de teste em `C:\FilesUXLab\perf\1600` (Concluído: 1.600 arquivos gerados).
- [x] Registrar todos os caminhos gerados em `test_artifacts_registry.md` (Concluído).
- [x] Criar script de limpeza correspondente `tools\perf\cleanup_f024_lab.ps1` (Concluído).

### T2 — Automação da Bateria de Testes e Linha de Base (Antes)
- [x] Criar `tools\perf\f024_keyboard_nav_test.ps1` para simular e cronometrar navegação (Concluído).
- [x] Criar `tools\perf\f024_watcher_sync_test.ps1` para simular modificações concorrentes por processos externos (Concluído).
- [x] Diagnóstico de causas-raiz documentado (Concluído).

### T3 — Otimização de Carga e Paginação do ShellViewModel
- [x] Em `ShellViewModel.cs`, modificar o `intermediateAction` da enumeração:
  - Eliminar os múltiplos `Clear()` e `Reset()` thrasheando a UI a cada lote parcial.
  - Exibir o 1º lote para resposta visual imediata da tela.
  - Consolidar a entrada dos itens restantes sem congelar o dispatcher da UI. (Concluído)

### T4 — Navegação Rápida por Teclado nos Layouts
- [x] Em `DetailsLayoutPage.xaml.cs`:
  - Interceptar e tratar `VirtualKey.Home`, `VirtualKey.End`, `VirtualKey.PageUp`, `VirtualKey.PageDown` no `FileList_PreviewKeyDown`.
  - Aplicar seleção e navegação instantânea via `ScrollIntoView` calculando índice sem layout sequencial lento. (Concluído)

### T5 — Sincronismo Incremental do FileSystemWatcher
- [x] Em `ShellViewModel.cs`, otimização do queue flush:
  - Edições concorrentes (`Created`, `Deleted`, `Renamed`) são processadas e enviadas à UI imediatamente sem retenção desnecessária pelo sampler. (Concluído: 21 ms na criação concorrente de 5 arquivos e 30 ms na exclusão).

### T6 — Contenção de Repouso
- [x] Medição de repouso: O código C#/.NET repousa em 16 ms a 31 ms (0,3% a 0,6% de CPU). Constatado que 96% do consumo parado decorre da thread `DWM Compositor Thread` intrínseca ao motor WinUI 3 da Microsoft. (Concluído)

### T7 — Execução da Bateria de Testes Pós-Correção
- [x] Executar `tools\perf\f024_watcher_sync_test.ps1` (Concluído: 21 ms criação / 30 ms exclusão).
- [x] Executar `tools\perf\idle.ps1 -WaitS 5` (Concluído).
- [x] Correção de travamento no tratamento de protocolo `files-dev:` em `MainWindow.xaml.cs` (Concluído).

### T8 — Higiene Absoluta e Limpeza dos Artefatos de Teste
- [ ] Executar `tools\perf\cleanup_f024_lab.ps1` após validação do Alexandre.
- [ ] Comprovar que `C:\FilesUXLab\perf\1600` e todos os arquivos auxiliares de teste foram 100% removidos (`Test-Path` retornando `False`).
- [ ] Atualizar `test_artifacts_registry.md` marcando todos os itens como Removidos.

