# F082 — Janela Modal de Configurações (Settings Modal Window) — SPEC

Estado: **EM EXECUÇÃO** (01/11/2026).
Solicitação do Alexandre: *"Está abrindo settings na última aba e o lugar do settings era sempre na primeira. Quero uma mudança. O Settings precisa abrir numa janela modal"*.

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Contexto e Motivação

1. Anteriormente, as Configurações do KyraFiles eram abertas como uma aba dentro do `TabControl` de navegação de arquivos (antiga F020).
2. Na prática, a abertura de Configurações como uma aba:
   - Polui a barra de abas de arquivos e compete por espaço com abas de pastas de trabalho.
   - Apresentava inconsistência de posicionamento na restauração de sessões anteriores (`LastSessionTabList`), abrindo por vezes na última posição em vez de manter uma âncora fixa.
   - Quebrava o fluxo mental de navegação, pois o usuário deseja ajustar preferências sem desviar ou perder o contexto da pasta ativa.
3. A mudança definitiva solicitada pelo usuário é transformar a experiência de Configurações em uma **Janela Modal Dedicada (`SettingsDialog` / `WindowEx`)**:
   - Não ocupa mais nenhuma aba na barra de navegação.
   - Abre de forma modal e centrada sobre a janela principal do aplicativo (`MainWindow`).
   - Bloqueia interações na janela principal enquanto a janela de configurações estiver aberta (comportamento clássico e robusto de diálogo modal Win32/WinUI 3).
   - Ao fechar a janela modal, o foco é devolvido imediatamente à janela principal e à pasta ativa.
   - Se o usuário acionar o comando de abrir configurações (`Ctrl+,`, menu ou sidebar) enquanto ela já estiver aberta, a janela modal existente é trazida para frente e focada.

---

## 2. Histórias de Usuário

- **US1 (P1 - Abertura em Janela Modal):** Como usuário, ao acionar as Configurações (via atalho `Ctrl+,`, Menu Clássico, botão da barra lateral ou atalhos de diálogo), quero que a tela de Configurações abra em uma janela modal dedicada centralizada sobre a aplicação, sem criar nenhuma aba nova.
- **US2 (P1 - Modalidade e Bloqueio Pai):** Como usuário, enquanto a janela modal de Configurações estiver aberta, a janela principal deve estar desabilitada para interação (`EnableWindow(false)`), garantindo foco exclusivo na configuração até o seu fechamento.
- **US3 (P1 - Retorno e Foco Limpo):** Como usuário, ao fechar a janela modal de Configurações (via botão `X` da janela ou atalho `Alt+F4`/`Esc`), a janela principal deve ser reabilitada instantaneamente e receber o foco na pasta onde eu estava trabalhando.
- **US4 (P1 - Instância Única e Navegação Direta):** Se a janela modal já estiver aberta e eu solicitar uma seção específica (por exemplo, Ajuda > Sobre, ou novo clique em Configurações), a janela modal existente deve ser focada e navegar diretamente para a seção pedida, sem abrir janelas duplicadas.
- **US5 (P2 - Limpeza da Barra de Abas):** Nenhuma sessão futura ou restauração de abas antigas deve criar uma aba para "Settings". A barra de abas fica 100% reservada para pastas e drives de arquivos.

---

## 3. Cenários de Aceite (Given / When / Then)

| ID | Dado (Contexto) | Quando (Ação) | Então (Resultado Esperado) |
|---|---|---|---|
| **AC-1** | O app está aberto com uma ou mais abas de pastas | O usuário aciona `Ctrl+,`, Menu Clássico (**Tools > Settings**) ou clica no ícone da Sidebar | A janela de Configurações é aberta como uma janela modal (`SettingsDialog`) sobre a janela principal; nenhuma aba é criada. |
| **AC-2** | A janela modal de Configurações está aberta | O usuário clica na janela principal do KyraFiles ao fundo | A janela principal não processa cliques (`EnableWindow(false)`) e a janela modal recebe o foco. |
| **AC-3** | A janela modal de Configurações está aberta | O usuário fecha a janela modal pelo botão `X` do topo | A janela fecha, a janela principal é reabilitada (`EnableWindow(true)`) e foca na aba de pasta ativa. |
| **AC-4** | A janela modal já está aberta na seção Geral | O usuário aciona **Help > About** ou passa parâmetro de página | A janela modal existente é trazida para frente e alterna a navegação para a página Sobre (`AboutPage`). |
| **AC-5** | O app é fechado e reaberto | A sessão anterior salva é restaurada pelo app | Nenhuma aba de Configurações é aberta na barra de abas; apenas abas de pastas de arquivos válidas são restauradas. |

---

## 4. Requisitos

### 4.1. Requisitos Funcionais (FR)
- **FR-001 (Classe `SettingsDialog` / `SettingsWindow`):** Implementar gerenciador de janela modal baseado em `WindowEx` para hospedar o `SettingsPage`.
- **FR-002 (Modalidade Win32):** Definir o proprietário (`GWL_HWNDPARENT`) como a `MainWindow` e utilizar `EnableWindow(mainWindowHandle, false)` na exibição e `EnableWindow(mainWindowHandle, true)` no fechamento.
- **FR-003 (Roteamento de Ações):** Atualizar `OpenSettingsAction` e `OpenAboutAction` para invocar `SettingsDialog.Show(...)` em vez de chamar `NavigationHelpers.AddNewTabByParamAsync` ou `OpenPathInNewTab("Settings")`.
- **FR-004 (Navegação Interna Desacoplada):** Ajustar `SettingsPage.xaml.cs` para suportar inicialização autônoma direta quando hospedada em `WindowEx` sem necessitar de `NavigationArguments.AssociatedTabInstance`.
- **FR-005 (Filtro na Restauração de Sessão):** Garantir que na restauração de abas de sessões anteriores (`LastSessionTabList`), identificadores `"Settings"` sejam descartados.

### 4.2. Requisitos Não Funcionais (NFR)
- **NFR-001 (Performance e Fluidez):** Abertura da janela modal em menos de 250 ms sem atrasos perceptíveis.
- **NFR-002 (Responsividade e DPI):** Ajustar as dimensões da janela modal para 960x680 lógicos, escalados pelo DPI da tela ativa, centralizada na janela mãe.
- **NFR-003 (Tema e Visual Consistente):** Suportar o tema ativo do sistema/aplicativo (Claro/Escuro/Mica/Acrylic) via `AppSystemBackdrop` e `IAppThemeModeService`.

---

## 5. Fora de Escopo
- Modificar o conteúdo das páginas de opções de configuração individuais.
- Alterar a persistência JSON das configurações do usuário.

