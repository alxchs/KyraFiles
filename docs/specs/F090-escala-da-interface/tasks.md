# F090 - Tarefas. Um commit por tarefa. Branch: `feature/f090-escala`, saindo da `main`.

- [ ] **T0: Spike de viabilidade, sem entrega (AC-1, AC-4, AC-5, AC-7)**
  - Em branch descartável, aplicar a opção A (transformação com layout compensado) na raiz e medir: corte, faixas, popups, clique e arraste, região de arraste da barra de título (`DragZoneHelper.cs:30`).
  - Pronto: relatório em `docs/agents/` com capturas e a decisão A, B ou C.
- [x] **T1: Modelo e persistência (FR-1, FR-2, AC-3)**
  - `AppUiScalePercent` em `IAppearanceSettingsService` e `AppearanceSettingsService` (padrão 100%, persistido em settings).
- [x] **T2: Serviço de aplicação (FR-3, AC-1, AC-2, AC-6, AC-7)**
  - `IUiScaleService` e `UiScaleService` aplicando transformação visual com layout compensado na `MainWindow` e sincronização imediata em tempo real sem reiniciar o app.
- [x] **T3: Interface em Configurações (NFR-3)**
  - `SettingsCard` e `ComboBoxEx` em `AppearancePage.xaml`; `AppearanceViewModel` com opções (80%, 90%, 100%, 110%, 125%, 150%); strings en-US e pt-BR; nome acessível.
- [ ] **T4: Popups, diálogos e atalhos (AC-4, AC-5)** — escopo conforme a resposta às pendências 2 e 3 da spec.
- [ ] **T5: Prova de tela e desempenho (todos os AC, NFR-1)**
  - Capturas em 80%, 100% e 150% em `docs/agents/evidence/f090/`; `verify_no_index_service.ps1`; navegação em pasta grande contra a main.
  - Pronto: relatório de conclusão (§18.7) com comparativo de desempenho; nota de liberação; exe com data posterior ao commit.

Regras: sem roubar foco; temporários em `%TEMP%\agy\<GUID>\...`; sem push nem merge sem pedido do Alexandre.
