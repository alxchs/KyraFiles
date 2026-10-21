# F027 — Checkbox de seleção sem pintura por linha (experimento)

Status: **APROVADA** pelo Alexandre em 20/10/2026 ("Vamos fazer um teste, parando de tentar pintar o checkbox linha por linha").

## Contexto
Teste do Alexandre sobre o build F026 (tag `tag_v4.3.1_f026-selecao-coalescida-cache-reduzido_salvo`): setas, PgUp/PgDn, Home/End e scroll bem melhores; ainda há falhas de atualização visual (a referência da linha selecionada some em PgUp/PgDn; a tela esvazia ao rolar com o mouse). Meta de produto: ser pelo menos tão rápido quanto o Windows Explorer.

## Requisitos
- **FR-1:** Criar ou reciclar uma linha da lista, e mudar a seleção, não executa trabalho de pintura do checkbox daquela linha.
- **FR-2:** O checkbox continua funcional: ao passar o ponteiro, aparece com o estado correto (marcado se o item está selecionado) e clicar nele seleciona/desmarca.
- **FR-3:** O destaque da linha selecionada (realce do item) não muda.
- **FR-4:** O checkbox "selecionar tudo" do cabeçalho continua refletindo o estado.

## Critérios de aceite
- **AC-1:** Segurar `Down` e `PgDn` por 5 s na pasta de 1.600 itens: a linha alcançada é maior ou igual à da F026, e o CPU de UI é menor ou igual (`f025_hold_key.ps1`, `f025_thr_hold.ps1`, 5 rodadas).
- **AC-2:** Ao passar o ponteiro sobre o checkbox de uma linha selecionada, ele aparece marcado.
- **AC-3:** Clicar no checkbox de uma linha seleciona/desmarca o item.
- **AC-4:** O realce da linha selecionada continua visível durante e depois da rajada de teclas.

## Fora de escopo
Remover o recurso de checkbox; mudar `ItemsControl` aninhado; abas.
## [NEEDS CLARIFICATION]
Nenhum. Decisão consciente: com o experimento, o checkbox deixa de aparecer automaticamente em linhas selecionadas (só ao passar o ponteiro).
