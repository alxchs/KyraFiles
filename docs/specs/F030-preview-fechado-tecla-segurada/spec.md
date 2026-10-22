# F030 — Preview fechado enquanto a tecla de navegação está segurada

Status: **APROVADA** (Claude, 21/10/2026, ideia do Alexandre: "se perceber que o usuário segurou o dedo em alguma tecla de navegação, fechar o preview até que ele solte a tecla").

## Contexto
Com o painel de preview/detalhes aberto, cada passo da seleção pode disparar a carga do preview do item (miniatura, manipulador de preview do Windows, detalhes). Hoje só há uma espera de 100 ms: com a tecla segurada, parte dos passos passa dela e carrega preview de itens que já ficaram para trás, roubando tempo da navegação.

## Requisitos
- **FR-1:** quando uma tecla de navegação da lista (↑, ↓, PgUp, PgDn) começa a se repetir (segurada), o painel de preview fecha o conteúdo: nenhuma carga em andamento continua e nenhuma nova começa.
- **FR-2:** ao soltar a tecla, o painel volta a mostrar o preview e os detalhes do item selecionado naquele momento, uma única vez.
- **FR-3:** um toque isolado (sem repetição) não fecha o painel; o comportamento atual continua.
- **FR-4:** a largura do painel e da lista não muda (o painel continua no lugar, vazio), para a lista não ser redesenhada por mudança de largura.

## Critérios de aceite
- **AC-1:** com o painel aberto, segurar ↓ 3 s: em quadros capturados durante a tecla o painel está vazio; 1 s depois de soltar mostra o preview do item final.
- **AC-2:** toque único em ↓ atualiza o preview como hoje.
- **AC-3:** `Down` segurado 5 s com o painel aberto: linhas alcançadas ≥ às do mesmo build com o painel fechado (dentro do ruído).

## Fora de escopo
Preview externo em janela (QuickLook/Peek): já tem espera de 150 ms desde a F028 e não é reaberto a cada passo.

## Decisão
"Fechar o preview" foi implementado como esvaziar o conteúdo do painel, não recolher o painel (FR-4). Recolher mudaria a largura da lista a cada aperto e soltura da tecla, e o redesenho da lista é exatamente o custo que se quer evitar. Se o Alexandre preferir recolher, muda a FR-4.

## Histórico
- 21/10/2026: criada.
- 21/10/2026 (plano): "vazio" (FR-1, AC-1) passa a ser o estado de carregamento já existente do painel: sem preview e sem detalhes, só o indicador de carregamento, que diz ao usuário que o preview volta ao soltar a tecla.
