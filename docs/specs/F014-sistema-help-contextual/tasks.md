# F014 — Sistema de Ajuda (Help) Completo e Contextual — TASKS

Metodologia: Spec-Driven Development (`METODO.md` §6).
Critério: cada tarefa é atômica, executável e verificável por compilação/teste.

---

## Tarefas de Implementação

- [x] **T1 — Modelos e Catálogo de Dados de Ajuda:**
  - Criar `HelpContextId`, `HelpBadgeType`, `HelpShortcut`, `HelpTopic` em `src/KyraFiles.App/Data/Help/` ou `src/KyraFiles.App/Services/Help/`.
  - Escrever o repositório inicial de tópicos com badges editoriais explícitas para os diferenciais do KyraFiles vs Windows Explorer e inspirações no OneCommander.

- [x] **T2 — Serviço Central de Ajuda (`IHelpService` & `HelpService`):**
  - Implementar interface e serviço com suporte a tópicos, busca textual, chaveamento de contexto e notificação de eventos.
  - Registrar no contêiner de injeção de dependências (`IHost` / `App.xaml.cs`).

- [x] **T3 — Componente de Interface do Painel Lateral (`HelpPane.xaml`):**
  - Desenvolver o UserControl `HelpPane` com cabeçalho, campo de busca em tempo real, navegação por tópicos, cartões de diferenciais com badges coloridas e tabela/cards de atalhos.
  - Integrar no `MainPage.xaml` como painel acionável (similar ao `ShelfPane` / `InfoPane`).

- [x] **T4 — Ação de Teclado e Roteamento de Contexto (`OpenHelpAction` e F1):**
  - Atualizar `OpenHelpAction.cs` para invocar o `IHelpService` em vez de abrir link externo web.
  - Implementar detector de contexto ativo no `MainPage` / `CurrentInstanceViewModel` para abrir diretamente no tópico de Colunas, Detalhes, Renomeação, Abas, etc.

- [x] **T5 — Integração no Menu Clássico e Configurações:**
  - Adicionar atalho no `ClassicMenuBar.xaml` para abrir o Guia de Ajuda e Atalhos.
  - Adicionar aba "Ajuda e Recursos" nas Configurações (`SettingsPage.xaml` / `HelpPage.xaml`).

- [x] **T6 — Validação, Compilação e Evidências:**
  - Compilar solução via `mkfile` em Debug e Release x64 com 0 warnings e 0 errors.
  - Abrir app via `Open-KyraFiles.ps1`, testar abertura geral por F1, testar abertura contextual em modo colunas e captura de evidências físicas (armazenadas em `docs/agents/evidence/f014/`).
  - Atualizar `AGENT_CONTEXT.md` e `STATUS.md`.

