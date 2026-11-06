# F089 - Tarefas (RASCUNHO, não aprovado). Um commit por tarefa. Branch: `feature/f089-fontes`, saindo da colab atualizada.

- [ ] **T0: Provar a causa (AC-1, AC-2)**
  - Log temporário do tipo e valor de `Resources["ContentControlThemeFontFamily"]` e de `App.Theme.FileArea.FontFamily` antes e depois da troca; registrar se a sidebar enxerga o valor.
  - Pronto: relatório em `docs/agents/` dizendo qual causa (1, 2 ou as duas). Remover o log antes do commit de código. Sem alteração de comportamento.
- [ ] **T1: Aplicar `FontFamily` de verdade (FR-1, FR-3, AC-1, AC-4, AC-5)**
  - `AppResourcesService`: gravar `new FontFamily(nome)`, com fallback seguro; `ThemeDictionaries` só se o T0 exigir.
  - Pronto: teste unitário com nome válido, vazio e inexistente; build 0/0.
- [ ] **T2: Fonte da área de arquivos nos três layouts (FR-2, AC-2)**
  - Mapear `ContentControlThemeFontFamily` nos `Resources` de Details, Grade e Colunas, mais `DataGridHeader`.
  - Pronto: sidebar inalterada; nome, data, tamanho e tipo na fonte da lista.
- [ ] **T3: Troca imediata e persistência (FR-3, AC-3)**
  - Conferir `ApplyResources()` e a inicialização em `AppThemeResourcesHelper`.
  - Pronto: trocar sem reiniciar e reabrir mantendo.
- [ ] **T4: Prova de tela e desempenho (AC-1 a AC-4, NFR-1)**
  - Capturas antes e depois em `docs/agents/evidence/f089/`; `verify_no_index_service.ps1`; navegação em pasta grande contra a `colab`.
  - Pronto: relatório de conclusão (§18.7) com comparativo de desempenho; nota de liberação; exe com data posterior ao commit.
- [ ] **T5: Corrigir a F009** — escrever em `F009-separate-fonts/` uma nota "substituída pela F089", sem apagar o histórico.

Regras: sem roubar foco; temporários em `%TEMP%\agy\<GUID>\...`; um commit por tarefa; sem push nem merge sem pedido do Alexandre.
