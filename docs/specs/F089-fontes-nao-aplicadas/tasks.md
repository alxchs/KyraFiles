# F089 - Tarefas (APROVADO). Um commit por tarefa. Branch: `feature/f089-fontes`, saindo da colab atualizada.

- [x] **T0: Provar a causa (AC-1, AC-2)**
  - Diagnóstico CONFIRMED no código e documentado no plan.md: Resources recebia string direta em vez de objeto FontFamily, e TextBlockStyles impunha ContentControlThemeFontFamily quebrando a herança dos layouts de arquivos.
- [x] **T1: Aplicar `FontFamily` de verdade (FR-1, FR-3, AC-1, AC-4, AC-5)**
  - `AppResourcesService`: gravar `new FontFamily(nome)`, com fallback seguro para `StandardFont` e try/catch; atualizar dicionários ativos (`ThemeDictionaries`).
  - Pronto: gravação de objetos `FontFamily` implementada e blindada contra falhas ou strings vazias/inválidas.
- [x] **T2: Fonte da área de arquivos nos três layouts (FR-2, AC-2)**
  - Mapear estilos e fontes nos Resources de Details, Grade e Colunas, mais `DataGridHeader` para apontar para `{ThemeResource App.Theme.FileArea.FontFamily}`.
  - Pronto: sidebar inalterada usando a fonte da interface; nome, data, tamanho e tipo na fonte da lista.
- [x] **T3: Troca imediata e persistência (FR-3, AC-3)**
  - `AppThemeResourcesHelper` invoca `SetAppThemeFontFamily` e `SetAppThemeFileAreaFontFamily` sempre que configurados, e `ApplyResources()` força a reavaliação de temas.
  - Pronto: troca imediata e inicialização consistente na abertura do app.
- [x] **T4: Prova de tela e desempenho (AC-1 a AC-4, NFR-1)**
  - Homologada pelo Alexandre em 27/11/2026 ("F089 aprovada"), no app instalado.
  - Prova de tela no app em execução e compilação do pacote na máquina de build oficial (`Verde.ps1 -Mark`).
- [x] **T5: Corrigir a F009** — escrever em `F009-separate-fonts/` uma nota "substituída pela F089", sem apagar o histórico.

Regras: sem roubar foco; temporários em `%TEMP%\agy\<GUID>\...`; um commit por tarefa; sem push nem merge sem pedido do Alexandre.
