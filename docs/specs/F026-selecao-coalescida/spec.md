# F026 — Navegação por teclado fluida: efeitos da seleção coalescidos (experimento)

Status: **APROVADA** pelo Alexandre em 20/10/2026 ("aceito o teste isolado"). Origem: F025 (`docs/agents/evidence/F025/relatorio.md`).

## Contexto
Medido (F025): segurando `Down`, o app processa só ~10 a 17 linhas/s das ~30 enviadas; o thread de UI fica 44% a 74% ocupado; o Files original se comporta quase igual. O Alexandre considera inutilizável. Outras aplicações nativas aguentam.

## Histórias
- **US1 (P1):** como usuário, ao segurar uma tecla de seta na lista, a seleção acompanha a repetição do teclado sem engasgar.

## Requisitos
- **FR-1:** Durante uma sequência rápida de mudanças de seleção, o app não atualiza os efeitos colaterais caros a cada passo (estado do toolbar, tamanho/contagem na barra de status). Eles são aplicados uma vez, logo depois que a seleção para de mudar.
- **FR-2:** A seleção em si (item destacado, foco, rolagem) continua sendo atualizada a cada passo.
- **FR-3:** O estado final do toolbar e da barra de status, depois que a seleção parou, é idêntico ao de hoje.
- **FR-4:** Nenhuma ação do usuário (Enter, Delete, F2, Ctrl+C...) pode agir sobre uma seleção desatualizada.
- **NFR-1:** A atraso máximo para aplicar os efeitos depois da última mudança é de 100 ms.

## Critérios de aceite
- **AC-1:** Segurar `Down` por 5 s na pasta de 1.600 itens: a linha alcançada é maior ou igual à do build sem a mudança, e o CPU do thread de UI é menor (medido por `tools\perf\f025_thr_hold.ps1`, A-B-A, mínimo 5 rodadas).
- **AC-2:** Depois de parar numa linha, em até 100 ms o toolbar e a barra de status mostram o estado dessa linha.
- **AC-3:** Pressionar Delete/Enter/F2 logo após a seta age sobre o item destacado, não sobre o anterior.
- **AC-4:** Ctrl+A, Shift+setas e clique continuam funcionando como antes.

## Fora de escopo
Trocar de framework; abas; tempo de abertura.
## Pontos `[NEEDS CLARIFICATION]`
Nenhum.
