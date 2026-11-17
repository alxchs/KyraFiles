# F062 — Diagnóstico de Consumo de Memória do Ecossistema na Página Sobre — TASKS

Status: **RASCUNHO PARA APROVAÇÃO**
Branch: `feature/f062-ecosystem-memory-about`
Worktree recomendado: `ia work f062-ecosystem-memory`

---

## Fases de Execução

- [ ] **F062-T1 — Núcleo de Diagnóstico e Testes Unitários (`KyraFiles.Shared`)**
  - Implementar `ProcessMemoryItem`, `EcosystemMemoryReport` e `EcosystemMemoryDiagnostics.cs`.
  - Criar testes unitários em `tests/KyraFiles.App.Search.Index.Tests/EcosystemMemoryDiagnosticsTests.cs`.
  - Critério de aceite: `dotnet test` passando 100%, cálculo exato de WorkingSet e PrivateBytes, proteção contra processos terminando em tempo real.

- [ ] **F062-T2 — ViewModel e Strings de Localização (`AboutViewModel.cs` e `Resources.resw`)**
  - Integrar propriedades observáveis e comandos em `AboutViewModel.cs`.
  - Adicionar chaves traduzíveis em `Strings/en-US/Resources.resw` (`EcosystemMemoryUsage`, `EcosystemMemorySummaryFormat`, `EcosystemMemoryDetails`, `CopyMemoryReport`, `ProcessMemoryActive`, `ProcessMemoryInactive`).
  - Critério de aceite: Build 0 erros e 0 avisos novos.

- [ ] **F062-T3 — Interface XAML na Página Sobre (`AboutPage.xaml`)**
  - Adicionar `SettingsCard` na `AboutPage.xaml` com ícone de desempenho.
  - Implementar flyout/diálogo de detalhes com lista discriminada de processos e botão de cópia.
  - Critério de aceite: Layout harmônico com os padrões do WinUI 3 e do KyraFiles, suporte a tema claro e escuro.

- [ ] **F062-T4 — Verificação e Prova de Funcionamento**
  - Conferir soma de memória contra o Gerenciador de Tarefas do Windows (`tasklist`).
  - Smoke test e captura de tela via `PrintWindow`.
  - Handoff e encerramento.

