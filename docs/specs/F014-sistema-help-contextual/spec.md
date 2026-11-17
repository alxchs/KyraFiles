# F014 — Sistema de Ajuda (Help) Completo e Contextual — SPEC

Status: **PROPOSTA PARA APROVAÇÃO**
Metodologia: Spec-Driven Development (`METODO.md` §6).
Documento de especificação funcional: define **o quê e por quê**, independente de classes e detalhes internos de implementação (reservados para o `plan.md`).

---

## 1. Por quê

O KyraFiles evoluiu significativamente além de um simples visualizador de arquivos, incorporando recursos avançados de produtividade inspirados no OneCommander, Mac Finder (Miller Columns), Explorer clássico e ferramentas de desenvolvimento (substituição transparente de Explorer, busca externa, redimensionamento independente de colunas, menu clássico, abas inteligentes).

No entanto:
1. Atualmente, o atalho `F1` e o comando de Ajuda apenas disparam a URL externa da documentação web do KyraFiles Community no navegador, sem nenhuma experiência integrada.
2. Não existe documentação ou guia explicativo dentro do app para os recursos inovadores e avançados que excedem o Windows Explorer padrão ou que vieram do OneCommander.
3. Não há suporte a **ajuda contextual**: quando o usuário está em uma tela ou modo específico (ex.: Modo Colunas / Miller Columns, Renomeação em Lote, Abas Inteligentes, Menu Clássico, Barra de Ferramentas), ele não tem como pressionar `F1` e obter diretamente a explicação daquele contexto de trabalho.
4. Falta uma diretriz e estrutura sistêmica para que cada nova funcionalidade implementada no app (seja pela Claude ou pela AGY) já seja automaticamente documentada no catálogo de ajuda.

---

## 2. Histórias de Usuário

- **US1 (P1 — Painel Integrado de Ajuda):** Como usuário, quero consultar a ajuda completa dentro do próprio KyraFiles sem precisar abrir um navegador web externo, mantendo meu fluxo de foco.
- **US2 (P1 — Ajuda Contextual Direta com F1):** Como usuário, quando estiver interagindo com um modo ou tela específica (ex.: Visualização em Colunas, Renomeação em Lote, Gerenciamento de Abas, Barra de Menus Clássica), quero pressionar `F1` e ver o tópico correspondente àquela tela imediatamente aberto e em destaque.
- **US3 (P1 — Acesso Geral Duplo):** Como usuário, quero abrir a Ajuda Geral a qualquer momento através do Menu Clássico (`Ajuda > Guia do KyraFiles / Ajuda`), das Configurações (`Configurações > Ajuda e Recursos`) ou por atalho (`F1` na visualização padrão).
- **US4 (P1 — Destaque Explícito dos Recursos Avançados / Diferenciais):** Como usuário, quero identificar imediatamente quais recursos e atalhos **superam o Windows Explorer padrão** ou foram **inspirados no OneCommander / sistemas modernos** através de badges e seções dedicadas com linguagem humana e didática.
- **US5 (P1 — Busca Interna de Tópicos):** Como usuário, quero digitar termos no campo de busca do painel de ajuda (ex.: "colunas", "atalhos", "renomear", "substituir explorer", "git") e filtrar os tópicos e atalhos instantaneamente.
- **US6 (P2 — Regra de Continuidade Sistêmica):** Como arquiteto/desenvolvedor, quero que todo novo recurso adicionado ao KyraFiles no futuro tenha seu tópico de ajuda integrado na base do Help como critério de aceite (DoD - Definition of Done).

---

## 3. Cenários de Aceite (Given / When / Then)

| ID | Dado (Contexto) | Quando (Ação) | Então (Resultado Esperado) |
|---|---|---|---|
| **AC-1** | Usuário na visualização principal em Modo Detalhes / Grade | Pressiona `F1` | O painel lateral/diálogo de Ajuda abre no tópico Geral / Visão Geral. |
| **AC-2** | Usuário com uma pasta aberta no **Modo Colunas (Miller Columns)** | Pressiona `F1` | O painel de Ajuda abre automaticamente com o tópico **"Navegação em Colunas (Estilo OneCommander / Mac)"** selecionado e visível. |
| **AC-3** | Usuário no diálogo de **Renomeação em Lote (Bulk Rename)** | Pressiona `F1` ou clica no botão de ajuda | Abre o tópico detalhando as regras e sintaxe de renomeação em lote protegida. |
| **AC-4** | Usuário acessa o Menu Clássico (`Alt + H` > `Guia do Usuário e Atalhos`) | Clica no item | O painel de Ajuda abre com o índice completo e navegação por categorias. |
| **AC-5** | Usuário acessa as Configurações do KyraFiles | Clica na nova aba **"Ajuda e Recursos"** | Visualiza todos os tópicos, lista de atalhos e os destaques comparativos em relação ao Windows Explorer. |
| **AC-6** | Usuário pesquisa por "OneCommander" ou "Explorer" na busca do Help | Digita a palavra-chave | A lista de tópicos filtra e exibe os cards com a badge destacada de diferencial (ex: "Exclusivo KyraFiles / Inspirado no OneCommander"). |
| **AC-7** | Usuário navega pelo Help em qualquer tópico | Lê o conteúdo | O linguajar é didático, em português claro e objetivo, sem jargões de código ou stack traces desnecessários. |

---

## 4. Requisitos do Sistema

### 4.1. Requisitos Funcionais (FR)
- **FR-001:** O sistema DEVE fornecer um serviço centralizado de Help (`IHelpService` / `HelpManager`) que armazena os tópicos, categorias, tags e identificadores de contexto (`HelpContextId`).
- **FR-002:** O sistema DEVE suportar resolução dinâmica de contexto: ao disparar `F1` ou `OpenHelpAction`, o serviço avalia o contexto ativo da janela atual (Layout de Colunas, Modo Grade/Detalhes, Diálogo Modal Ativo, Aba de Configurações, etc.) e direciona para o `HelpContextId` correspondente.
- **FR-003:** O sistema DEVE apresentar badges editoriais em tópicos que representem diferenciais técnicos:
  - `[Diferencial vs Explorer]` (Ex.: Redimensionamento de coluna independente, todas as 300+ colunas, busca externa F3, substituição transparente com fallback).
  - `[Inspirado no OneCommander]` (Ex.: Miller columns, navegação rápida por teclado, densidade compacta, abas inteligentes).
- **FR-004:** O sistema DEVE permitir busca por texto livre em títulos, tags e corpo do texto dos tópicos de ajuda.
- **FR-005:** O sistema DEVE integrar o acesso ao Help em:
  1. Menu Clássico (`MenuBar > Ajuda > Guia do KyraFiles / Teclas de Atalho`).
  2. Atalho global de teclado `F1` roteado contextualmente.
  3. Página de Configurações (`Configurações > Ajuda & Atalhos`).
- **FR-006:** A arquitetura do catálogo de tópicos DEVE ser modular e tipada em C# / Resource / Markdown-like para permitir que Claude e AGY adicionem novos tópicos em cada PR/tarefa futura sem atrito.

