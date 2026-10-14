# F020 — Aba única por endereço e aba de Configurações na primeira posição — TASKS

Metodologia: Spec-Driven Development (`METODO.md` §6).

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| **T1** | Implementar `NormalizeNavigationPath` e `FindExistingTabIndex` em `NavigationHelpers.cs` | FR-001, FR-002, AC-2 | Função compara caminhos com normalização de barras e case-insensitivity; encontra índice correto | Pendente de aprovação |
| **T2** | Modificar `AddNewTabByPathAsync` e `AddNewTabByParamAsync` para reaproveitar aba existente e forçar "Settings" no índice 0 | FR-001, FR-003, AC-1, AC-3, AC-4, AC-5 | Tentativa de abrir aba com endereço já aberto seleciona a aba existente; "Settings" é inserida ou movida para o índice 0 | Pendente de aprovação |
| **T3** | Redirecionar acionamento de Configurações em `SidebarViewModel.cs` para a aba no índice 0 | FR-004, AC-3 | Clicar no botão de configurações na barra lateral abre ou seleciona a aba 0 sem destruir a pasta ativa | Pendente de aprovação |
| **T4** | Adicionar verificação de duplicidade na navegação por barra de endereço e ação de duplicar aba | FR-005, AC-1, AC-6 | Navegar na barra de endereços para caminho já aberto em outra aba transfere o foco para ela | Pendente de aprovação |
| **T5** | Compilar release com zero warnings/erros, atualizar layout AppX e validar cenários AC-1..AC-7 no app | NFR-001, NFR-002, AC-1..AC-7 | `mkfile release` termina com `0 Error(s), 0 Warning(s)`, app abre e comprovações visuais capturadas | Pendente de aprovação |
| **T6** | Criar nota de liberação em `docs/releases/`, handoff e atualizar `STATUS.md` e `AGENT_CONTEXT.md` | `METODO.md` §6, §8 | Documentação completa com estados de evidência e commit local | Pendente de aprovação |
