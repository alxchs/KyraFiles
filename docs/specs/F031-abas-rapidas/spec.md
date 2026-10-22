# F031 — Troca de abas e nova aba mais rápidas

Status: **APROVADA** (Claude, 21/10/2026, pedido do Alexandre: "melhorar o tempo de troca entre abas e ao criar novas abas").

## Contexto
Não há medição registrada do tempo de troca de aba nem de abertura de aba nova. O Explorer do Windows 11 também tem abas e serve de referência na mesma máquina.

## Requisitos
- **FR-1:** trocar de aba (Ctrl+Tab, Ctrl+Shift+Tab, clique) mostra o conteúdo completo da aba de destino (lista desenhada, barra de endereço e barra de status corretas) em tempo menor ou igual ao do Explorer na mesma máquina.
- **FR-2:** abrir aba nova (Ctrl+T e botão "+") mostra a página inicial interativa em tempo menor ou igual ao do Explorer.
- **FR-3:** abrir uma pasta em aba nova (clique do meio, "Abrir em nova aba") mostra a lista da pasta em tempo menor ou igual ao do Explorer.
- **NFR-1:** memória privada com 10 abas abertas não cresce mais que 10% em relação ao build atual.
- **NFR-2:** nada muda no que cada aba guarda (pasta, histórico, seleção, rolagem).

## Critérios de aceite
- **AC-1:** linha de base medida antes de qualquer mudança (Files atual e Explorer), com script versionado, 10 repetições por cenário: troca de aba, aba nova, pasta em aba nova.
- **AC-2:** depois da mudança, mediana de cada cenário ≤ a do Explorer, ou, se o Explorer for inalcançável por custo do WinUI, redução ≥ 30% sobre a linha de base com o número registrado e a decisão com o Alexandre.
- **AC-3:** troca de aba preserva rolagem e seleção (conferência visual).

## Fora de escopo
Abas em janelas separadas (arrastar aba para fora).

## [NEEDS CLARIFICATION]
Nenhum. A meta numérica vem da linha de base (AC-1).

## Histórico
- 21/10/2026: criada.
