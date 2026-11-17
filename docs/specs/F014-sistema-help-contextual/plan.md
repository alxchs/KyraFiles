# F014 — Sistema de Ajuda (Help) Completo e Contextual — PLAN

Metodologia: Spec-Driven Development (`METODO.md` §6).
Documento de plano técnico: define arquitetura, classes, MVVM, componentes de UI e fluxo de contexto.

---

## 1. Arquitetura Proposta

### 1.1. Camada de Dados e Serviços (`KyraFiles.App.Services`)
- **`HelpContextId` (Enum ou Constantes de string):**
  - `General` (Visão geral do KyraFiles)
  - `ColumnsLayout` (Navegação em Colunas / Miller Columns — *Inspirado no OneCommander*)
  - `DetailsLayout` (Modo Detalhes, redimensionamento independente e colunas extras)
  - `TabsAndPanes` (Abas inteligentes, divisão horizontal/vertical)
  - `BulkRename` (Renomeação em lote e proteção de nomes)
  - `ClassicMenuBar` (Barra de menus clássica Alt+F, Alt+V, etc.)
  - `SearchEngine` (Busca rápida interna e integração F3 com ferramentas externas)
  - `ExplorerReplacement` (Substituição transparente do Windows Explorer e Fallback)
  - `SettingsAndCustomization` (Personalização de layout, fontes e temas)
  - `KeyboardShortcuts` (Guia de atalhos de teclado rápidos)

- **`HelpTopic` (Modelo):**
  - `Id`: string/HelpContextId
  - `Title`: string
  - `Category`: string ("Navegação", "Produtividade", "Organização", "Sistema")
  - `Badge`: `HelpBadgeType` (`None`, `ExplorerAdvantage`, `OneCommanderInspired`, `PowerUser`)
  - `BadgeLabel`: string ("Diferencial vs Explorer", "Inspirado no OneCommander", "Recurso Exclusivo")
  - `Summary`: string
  - `Content`: string (formatação com tópicos, atalhos, dicas)
  - `RelatedShortcuts`: `IReadOnlyList<HelpShortcut>`
  - `Keywords`: `string[]`

- **`IHelpService` & `HelpService`:**
  - `IReadOnlyList<HelpTopic> GetAllTopics()`
  - `HelpTopic? GetTopicByContext(HelpContextId contextId)`
  - `void OpenHelp(HelpContextId contextId = HelpContextId.General)`
  - `event EventHandler<HelpRequestedEventArgs>? HelpRequested`

### 1.2. Camada de UI (User Interface)
Oferecer duas formas complementares de exibição:
1. **Painel Lateral / Drawer (`HelpPane` em `UserControls/Pane/HelpPane.xaml`):**
   - Integrado diretamente na área lateral da janela (ao lado ou alternando com InfoPane/ShelfPane).
   - Abre sem bloquear a interação com os arquivos, permitindo consultar e praticar os atalhos ao mesmo tempo!
   - Possui campo de pesquisa em tempo real, seletor de tópicos, lista de atalhos e destaques visuais (badges temáticas).
2. **Página Dedicada nas Configurações (`HelpPage.xaml` em `Views/Settings/HelpPage.xaml`):**
   - Disponível no menu lateral de configurações do app como central permanente de documentação.

### 1.3. Detecção de Contexto Ativo (`OpenHelpAction`)
No `OpenHelpAction`:
- Inspeciona o estado atual da janela:
  - Se o diálogo ativo for `BulkRenameDialog` → Contexto `BulkRename`.
  - Se a aba ativa estiver no layout de colunas (`FolderLayoutModes.ColumnView`) → Contexto `ColumnsLayout`.
  - Se a aba ativa estiver no layout de detalhes (`FolderLayoutModes.DetailsView`) → Contexto `DetailsLayout`.
  - Se a página atual for Configurações → Contexto `SettingsAndCustomization`.
  - Caso contrário → Contexto `General`.
- Invoca o `IHelpService.OpenHelp(contextId)`.

---

## 2. Conteúdo Inicial e Diferenciais Mapeados
O catálogo inicial de ajuda incluirá os seguintes tópicos prioritários:
1. **Miller Columns (Visualização em Colunas):** Explicação da navegação contínua por pastas com setas de teclado (`Left`/`Right`/`Up`/`Down`), histórico de navegação rápida e pré-visualização contínua. Badge: *Inspirado no OneCommander*.
2. **Redimensionamento Independente de Colunas e 300+ Propriedades da Shell:** As larguras das colunas não interferem umas nas outras e qualquer metadado do Windows pode ser exibido. Badge: *Diferencial vs Explorer*.
3. **Substituição Transparente do Explorer com Fallback:** Captura de `Win + E` e cliques duplos sem quebrar o sistema se o app for fechado. Badge: *Exclusivo KyraFiles*.
4. **Abas e Painéis Divididos (Dual Pane):** Organização horizontal e vertical de pastas na mesma janela. Badge: *Produtividade Avançada*.
5. **Busca Avançada com F3:** Integração rápida com indexadores e ferramentas externas. Badge: *Diferencial vs Explorer*.
6. **Renomeação em Lote Segura:** Padrões numéricos e substituição de termos com prevenção de colisões. Badge: *Produtividade Avançada*.

---

## 3. Impacto e Riscos
- **Impacto no Upstream:** Isolado em novos arquivos no namespace `KyraFiles.App.Services.Help` e `KyraFiles.App.UserControls.Pane`.
- **Risco de Conflito de F1:** Substitui com segurança o redirecionamento web externo no `OpenHelpAction.cs` pelo acionamento do serviço interno.

