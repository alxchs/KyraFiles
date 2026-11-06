# F089 - A fonte escolhida em Configurações é aplicada em toda a interface

Status: RASCUNHO para aprovação do Alexandre (Claude, 04/11/2026). Origem: relato do Alexandre com captura de tela e pedido da AGY (`docs/agents/handoffs/2026-11-04_1845_agy_solicitacao-claude-fontes-e-escala-visual.md`).
Substitui a F009 (`docs/specs/F009-separate-fonts/`), cuja `spec.md` e `plan.md` estão vazias (0 linhas) e cuja `tasks.md` está toda marcada como feita mesmo com o defeito presente.

## Problema
Em Configurações > Aparência, a "Fonte" foi trocada para Corbel Light e a "Fonte da lista de arquivos" para Berlin Sans FB. A barra lateral (unidades e pastas) e a lista de arquivos continuaram na fonte padrão do sistema.

## História de usuário (P1)
Como usuário, quero escolher a fonte da interface e a fonte da lista de arquivos e ver as duas mudarem em tudo, sem reiniciar.

## Cenários de aceite
- **AC-1** Dado que a fonte da interface é a padrão, quando escolho Corbel Light, então barra lateral, abas, barra de endereço, menus, diálogos e Configurações passam a usar Corbel Light imediatamente.
- **AC-2** Dado que escolho Berlin Sans FB como fonte da lista de arquivos, então nome, data, tamanho, tipo e cabeçalhos de coluna nos layouts Detalhes, Grade e Colunas usam Berlin Sans FB, e a barra lateral NÃO muda.
- **AC-3** Dado que fechei e reabri o app, então as duas escolhas continuam aplicadas desde a primeira tela.
- **AC-4** Dado que volto a "Padrão", então tudo volta à fonte original, sem reiniciar.
- **AC-5** Dado uma fonte que não está instalada (removida depois de escolhida), então o app abre normalmente com a fonte padrão e não trava.

## Requisitos
- **FR-1** A fonte da interface vale para todo texto, exceto a área de arquivos.
- **FR-2** A fonte da lista de arquivos vale para todo texto da área de arquivos e só para ele.
- **FR-3** A troca é imediata e persiste.
- **NFR-1** Sem piora de desempenho ao trocar de pasta nem ao abrir o app (§18.5).
- **NFR-2** Fonte inválida não derruba o app.

## Casos de borda
Fonte com nome inexistente; troca com várias abas e janelas abertas; modo de alto contraste; tema claro e escuro; ícones de símbolo (Segoe Fluent) não podem mudar.

## Fora de escopo
Tamanho da fonte (escala é a F090); fontes dos ícones.

## Critérios de sucesso
Captura de tela comparando antes e depois, com o app aberto, para AC-1 a AC-4.

## [NEEDS CLARIFICATION]
Nenhuma.
