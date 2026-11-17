# F029 — Setas no mínimo como no Explorer do Windows

Status: **APROVADA** (Claude, 21/10/2026, a pedido do Alexandre: "teclas no mínimo concorrendo com Windows Explorer").

## Contexto
Depois da F028, o PgUp/PgDn ficou bom, mas o Alexandre sente o Explorer do Windows "nitidamente mais rápido e fluido" nas setas. Medido com o mesmo script (`Down` segurado 5 s, pasta de 1.600 itens, ~106 teclas reais por causa da resolução do relógio do Windows): o KyraFiles chega a 74 a 86 linhas; a lista nativa do Windows (WinForms) chega a 106. **O Explorer nunca foi medido com o mesmo método**, e é ele a referência pedida.

## Histórias
1. Como usuário, ao segurar ↓ ou ↑ numa pasta grande, quero que a seleção ande pelo menos tão rápido quanto no Explorer, com a lista rolando junto e a linha selecionada sempre visível.
2. Ao soltar a tecla, a seleção para onde está, sem continuar andando sozinha.

## Requisitos
- **FR-1:** com ↓ ou ↑ segurado, o número de linhas percorridas em 5 s é maior ou igual ao do Explorer do Windows na mesma pasta, mesma janela e mesmo gerador de teclas.
- **FR-2:** depois de soltar, a seleção anda no máximo 1 linha a mais (sem fila acumulada).
- **FR-3:** durante a tecla segurada, a linha selecionada e a barra de rolagem aparecem em todos os quadros capturados, andando só para frente.
- **NFR-1:** a CPU do processo por 5 s de tecla segurada não sobe em relação ao build de 21/10 09:20 (mediana de 5 rodadas).
- **NFR-2:** nada muda no comportamento de um toque isolado (seleção, foco, preview, toolbar, barra de status).

## Critérios de aceite
- **AC-1 (FR-1):** `f025_hold_key.ps1`, 5 rodadas de `Down`: mediana de linhas do KyraFiles ≥ mediana do Explorer (`-WindowClass CabinetWClass`), mesma pasta e mesmo tamanho de janela.
- **AC-2 (FR-2):** coluna `backlog_steps_after_release` ≤ 1 em todas as rodadas.
- **AC-3 (FR-3):** folha de contato de `Down` segurado com a linha selecionada visível em todos os quadros.
- **AC-4 (NFR-1):** CPU mediana ≤ a do build de 21/10 09:20 (Down: 7,0 s).
- **AC-5 (NFR-2):** um toque em ↓ atualiza toolbar e barra de status (conferência visual).

## Casos de borda
Pasta pequena (sem rolagem); início e fim da lista; Shift+↓ (estende a seleção); janela pequena.

## Fora de escopo
PgUp/PgDn (F028); modos Grade e Colunas; cache de propriedades em segundo plano (avaliar depois, se ainda faltar).

## [NEEDS CLARIFICATION]
Nenhum. A meta numérica sai da medição do Explorer (T1), não de chute.

## Histórico
- 21/10/2026: criada.
