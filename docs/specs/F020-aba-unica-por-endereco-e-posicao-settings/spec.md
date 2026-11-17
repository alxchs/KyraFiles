# F020 — Aba única por endereço e aba de Configurações na primeira posição — SPEC

Estado: **EM APROVAÇÃO** (14/10/2026). Pedido do Alexandre: *"Não permita abrir o mesmo endereço em duas abas diferentes. Abra a aba de configuracoes sempre na primeira posição"*.

Metodologia: Spec-Driven Development (`METODO.md` §6).

---

## 1. Por que

1. no KyraFiles atual, acionar "Abrir em nova aba", atalhos de teclado, linha de comando ou itens da barra lateral permite acumular múltiplas abas com o mesmo endereço (`C:\`, `C:\Windows`, `Home`, etc.), causando desorganização na navegação e desperdício de recursos.
2. A aba de Configurações ("Settings") era aberta na última posição da barra de abas ou substituía o conteúdo da pasta ativa quando clicada na barra lateral. Definir que a aba de Configurações abra sempre na primeira posição (índice 0) estabelece uma âncora fixa e previsível de navegação, sem destruir a pasta que o usuário estava visualizando.

---

## 2. Histórias de Usuário

- **US1 (P1 - Aba única por endereço):** Como usuário, ao tentar abrir um endereço (caminho local, rede ou página especial) que já esteja aberto em alguma aba existente, não quero que uma segunda aba seja criada; quero que o aplicativo alterne o foco para a aba existente.
- **US2 (P1 - Configurações na primeira posição):** Como usuário, ao abrir as Configurações (via atalho `Ctrl+,`, Menu Clássico, botão da barra lateral ou menu Ajuda), quero que a aba de Configurações seja posicionada sempre na primeira posição (extrema esquerda, índice 0).
- **US3 (P1 - Configurações única):** Como usuário, se as Configurações já estiverem abertas, qualquer nova solicitação para abrir Configurações deve apenas selecionar a aba existente no índice 0, nunca abrir uma segunda aba de Configurações.
- **US4 (P2 - Não perder pasta atual ao clicar em Configurações):** Como usuário, ao clicar no botão de Configurações na barra lateral (com ou sem modificador), não quero perder a pasta em exibição na aba ativa; o aplicativo deve abrir/focar a aba de Configurações na posição 0.

---

## 3. Cenários de Aceite (Given / When / Then)

| ID | Dado (Contexto) | Quando (Ação) | Então (Resultado Esperado) |
|---|---|---|---|
| **AC-1** | O app possui uma aba aberta no caminho `C:\Windows` | O usuário tenta abrir `C:\Windows` em nova aba (via menu de contexto, atalho ou comando) | Nenhuma aba nova é criada; a aba existente de `C:\Windows` é imediatamente selecionada. |
| **AC-2** | O app compara dois endereços para detecção de duplicidade | Caminhos com diferenças de caixa (`c:\windows` vs `C:\WINDOWS`) ou barra final (`C:\Windows\` vs `C:\Windows`) são avaliados | O sistema normaliza os caminhos e os considera idênticos, impedindo a duplicação. |
| **AC-3** | O app está com uma ou mais abas de pastas abertas e nenhuma aba de Configurações | O usuário aciona a abertura de Configurações (`Ctrl+,`, menu ou barra lateral) | A aba de Configurações é criada e inserida no índice 0 (primeira posição); as abas existentes são deslocadas para a direita e a aba de Configurações recebe o foco. |
| **AC-4** | A aba de Configurações já está aberta (no índice 0 ou reordenada) | O usuário aciona a abertura de Configurações novamente | Nenhuma nova aba é criada; se não estiver no índice 0, a aba de Configurações é movida para o índice 0 e recebe o foco. |
| **AC-5** | O app possui uma aba aberta em `Home` | O usuário aciona Nova Aba (`Ctrl+T` ou botão `+`) com destino padrão `Home` | O app foca na aba `Home` já existente em vez de criar uma segunda aba `Home`. |
| **AC-6** | O app possui a Aba 1 em `C:\` e a Aba 2 em `D:\` | Na Aba 2, o usuário tenta navegar (via barra de endereço ou atalho) para `C:\` | O app transfere o foco para a Aba 1 existente, preservando a Aba 2 em `D:\` e evitando duas abas simultâneas em `C:\`. |
| **AC-7** | O usuário fecha a aba de Configurações | O usuário clica no botão fechar da aba de Configurações | A aba fecha normalmente; ao ser reaberta posteriormente, abre novamente no índice 0. |

---

## 4. Requisitos

### 4.1. Requisitos Funcionais (FR)
- **FR-001 (Verificação de Endereço Único):** O serviço de navegação (`NavigationHelpers`) deve verificar, antes de instanciar ou inserir uma nova aba, se já existe uma aba ativa com o mesmo endereço normalizado. Se existir, ativa a aba existente e cancela a criação da nova aba.
- **FR-002 (Normalização de Caminho):** Implementar função de normalização de caminhos para comparação que desconsidere maiúsculas/minúsculas, barras finais redundantes e formate páginas especiais (`Home`, `Settings`, `ReleaseNotes`).
- **FR-003 (Inserção de Settings no Índice 0):** Ao adicionar ou navegar para a aba de Configurações, o índice de inserção/posicionamento deve ser forçado para `0`.
- **FR-004 (Roteamento de Configurações da Sidebar):** Clicar no botão `Settings` da barra lateral deve acionar a abertura/ativação da aba no índice 0, em vez de substituir a visualização da pasta atual.
- **FR-005 (Navegação Inter-abas com Endereço Existente):** Ao navegar na barra de endereços para um caminho que já está aberto em outra aba, focar na aba existente.

### 4.2. Requisitos Não Funcionais (NFR)
- **NFR-001 (Latência de Verificação):** A busca por abas duplicadas deve ser realizada em memória na coleção `AppInstances` em tempo menor que 1 ms.
- **NFR-002 (Zero Degradação Visual):** A transição de foco entre abas não deve apresentar flicker ou artefatos visuais.

---

## 5. Fora de Escopo
- Modificar o sistema de layout interno ou opções de configuração da página de Configurações.
- Desabilitar a capacidade do usuário de arrastar e reordenar abas manualmente após abertas (a regra aplica-se à abertura e ativação).
