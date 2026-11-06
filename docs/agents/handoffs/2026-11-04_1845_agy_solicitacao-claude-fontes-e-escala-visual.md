# Solicitação de Especificação — AGY para a Claude

- **Data / Hora**: 2026-11-04 18:45 (Horário de Brasília)
- **De**: Antigravity (AGY)
- **Para**: Claude
- **Status**: Aguardando elaboração e aprovação das specs (SDD) pela Claude para início da execução pela AGY.
- **Contexto**: Relato de bug e nova funcionalidade reportados diretamente pelo Alexandre com evidência em imagem (`media_1793826270192_ff9e76b1.png`).

---

## 1. Contexto e Origem da Demanda

O Alexandre reportou duas necessidades críticas na interface do KyraFiles:
> *"Não respeita fontes e senti falta de configuração para mudar a escala de visualização da tela."*

Na imagem fornecida pelo Alexandre:
1. Em **Configurações > Aparência**, a **Fonte** geral da interface foi configurada como **Corbel Light**, mas a barra lateral e navegação de pastas/unidades (*Windows-SSD (C:)*, *Data (D:)*, *Local Disk (R:)*) permaneceu utilizando a fonte padrão do sistema (*Segoe UI*).
2. A **Fonte da lista de arquivos** (*Files and columns font*) foi configurada como **Berlin Sans FB**, mas a listagem de arquivos nos detalhes/colunas (*Slideg.mp3*, *Slideg.mpeg*, etc.) permaneceu inalterada na fonte padrão do sistema.
3. Além disso, foi apontada a ausência de um controle dedicado de **escala de visualização da tela** (UI Scale / Zoom visual da interface).

---

## 2. Diagnóstico Técnico Realizado pela AGY

### Demanda 1: Bug — Fontes não são aplicadas / respeitadas

A investigação da AGY no código-fonte confirmou as seguintes causas-raízes:

1. **Atribuição incorreta de tipo em tempo de execução (`string` vs `FontFamily`):**
   - No arquivo `src/KyraFiles.App/Services/App/AppResourcesService.cs` (linhas 95–104):
     ```csharp
     public void SetAppThemeFontFamily(string contentControlThemeFontFamily)
     {
         Application.Current.Resources["ContentControlThemeFontFamily"] = contentControlThemeFontFamily;
     }

     public void SetAppThemeFileAreaFontFamily(string appThemeFileAreaFontFamily)
     {
         Application.Current.Resources["App.Theme.FileArea.FontFamily"] = appThemeFileAreaFontFamily;
     }
     ```
   - No WinUI 3 / XAML, as propriedades `Control.FontFamily` e `TextBlock.FontFamily` esperam instâncias do tipo `Microsoft.UI.Xaml.Media.FontFamily`.
   - Ao injetar uma `System.String` dinamicamente no `Application.Current.Resources` via C#, os bindings e referências `{ThemeResource}` falham na conversão implícita em tempo de execução, revertendo silenciosamente para a fonte de fallback do sistema.
   - **Correção necessária**: Instanciar e atribuir objetos `new FontFamily(fontFamilyName)` (utilizando `StringToFontFamilyConverter.ConvertToFontFamily` ou instanciação direta com fallback seguro).

2. **Precedência de `ThemeDictionaries` no WinUI 3:**
   - O recurso `ContentControlThemeFontFamily` é nativo do Windows App SDK e pertence aos `ThemeDictionaries` (`Default`, `Light`, `HighContrast`) de `XamlControlsResources`.
   - Modificar apenas na raiz de `Application.Current.Resources` é encoberto pelos dicionários temáticos no ciclo do `ApplyResources()` (`AppThemeModeService.cs`). É indispensável injetar a nova `FontFamily` também nos dicionários de tema ativos (`ThemeDictionaries`).

3. **Bloqueio de herança de fontes pelos estilos implícitos de `TextBlock`:**
   - Em `src/KyraFiles.App/Styles/TextBlockStyles.xaml`, todo `TextBlock` herda de `App.Theme.BaseTextBlockStyle`, que define explicitamente:
     ```xml
     <Setter Property="FontFamily" Value="{ThemeResource ContentControlThemeFontFamily}" />
     ```
   - Nos layouts de arquivos (`DetailsLayoutPage.xaml`, `GridLayoutPage.xaml`, `ColumnLayoutPage.xaml`), os blocos de texto (`ItemName`, metadados de coluna, data, tamanho, tipo) são `<TextBlock>` sem setter local de `FontFamily`.
   - Por haver um setter implícito no tipo `TextBlock`, o `FontFamily="{ThemeResource App.Theme.FileArea.FontFamily}"` colocado no container pai (`ListView x:Name="FileList"`) **não é herdado** pelos nós de texto internos.
   - **Correção necessária**: No recurso de cada página de layout (`DetailsLayoutPage.Resources`, `GridLayoutPage.Resources`, `ColumnLayoutPage.Resources`), mapear localmente `ContentControlThemeFontFamily` para `{ThemeResource App.Theme.FileArea.FontFamily}` ou prover o estilo implícito de `TextBlock` com vínculo à fonte da área de arquivos.

---

### Demanda 2: Nova Funcionalidade — Escala de Visualização da Tela (UI Scale)

1. **Estado Atual:**
   - O KyraFiles possui apenas `AppDensity` (`AppDensityKind`: Normal, Compact, UltraCompact) em `AppearanceSettingsService`, que altera apenas alturas fixas em pixels (`App.Theme.Sidebar.ItemHeight` entre 30 e 40px) e altura de linhas no Details.
   - Isso não redimensiona ícones, botões, abas, barras de navegação nem atende à necessidade de zoom/escala global da interface.

2. **Requisitos da Solução:**
   - Adicionar em **Configurações > Aparência** uma seção de **Escala da Interface** (*Display Scale / UI Scale*).
   - Permitir seleção em níveis comuns (ex.: 80%, 90%, 100%, 110%, 125%, 150%) ou controle contínuo, salvando a preferência em `AppearanceSettingsService`.
   - Aplicação da escala no elemento visual raiz (`MainWindow.Instance.Content` ou container principal) via transformações de escala do WinUI 3 (`UIElement.Scale` / `ScaleTransform` ou ajuste integrado de layout), garantindo renderização nítida e responsiva sem estourar as margens da janela.

---

## 3. O que solicitamos à Claude

Pedimos à Claude para assumir o papel de arquiteta/especificadora e preparar a estrutura de SDD:

1. **Definição dos IDs e Nomes das Especificações:**
   - Ajustar/completar o SDD de correção das fontes (ex.: revisão da F009 ou spec de estabilização `F009-font-fixes`).
   - Criar o pacote SDD para a Escala de Visualização de Tela (ex.: `F089-ui-scale` ou similar), com:
     - `spec.md` (requisitos funcionais, critérios de aceite, casos de borda).
     - `plan.md` (arquitetura, modelo de dados em `AppearanceSettingsService`, UI XAML em `AppearancePage.xaml`, serviço de aplicação).
     - `tasks.md` (tarefas granulares T1, T2, T3... seguindo rigorosamente o ciclo de desenvolvimento do projeto).

2. **Aviso de Liberação para Início:**
   - Assim que o `tasks.md` estiver pronto e aprovado pelo Alexandre, a Claude deve registrar a autorização para a AGY iniciar a codificação e os testes práticos na branch dedicada.

