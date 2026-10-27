# F043 — Histórico de operações com desfazer — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Hoje só o último movimento pode ser desfeito (`Ctrl+Z`) e não há como ver o que foi feito. Quem apagou, moveu ou renomeou sem querer precisa de um registro claro e de "desfazer" por item.

## 2. Histórias de usuário
- **US1 (P1)** Como usuário, quero ver uma lista das últimas operações (copiar, mover, apagar, renomear, criar) com hora, origem e destino.
- **US2 (P1)** Como usuário, quero desfazer uma operação específica da lista, quando ainda for possível.
- **US3 (P2)** Como usuário, quero filtrar por tipo e buscar por nome.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | movi 3 arquivos, renomeei 1 e apaguei 1 | abro o histórico | vejo 3 entradas, da mais nova para a mais antiga, com horário e caminhos |
| AC-2 | entrada "apagar" (Lixeira) | clico "Desfazer" | o arquivo volta ao lugar de origem |
| AC-3 | entrada "mover" cujo destino foi apagado depois | clico "Desfazer" | o app explica por que não dá e nada é alterado |
| AC-4 | entrada "renomear" | "Desfazer" | o nome antigo volta; se o nome antigo já está ocupado, pergunta antes |
| AC-5 | reinicio o app | abro o histórico | as operações recentes continuam (limite configurável) |
| AC-6 | entrada "apagar definitivamente" (sem Lixeira) | olho a entrada | aparece, marcada "não reversível", sem botão de desfazer |
| AC-7 | histórico com 500 entradas | rolo e filtro | continua fluido e filtra em até 300 ms |

## 4. Requisitos
- **FR-001** Registrar toda operação feita pelo app com resultado (sucesso, parcial, falha).
- **FR-002** Desfazer reaproveita o mecanismo atual de desfazer; não cria outro.
- **FR-003** Limite e "Limpar histórico" nas configurações.
- **NFR-001** Registrar não adiciona atraso perceptível à operação (medido nas cópias de F036).

## 5. Casos de borda
- Operações com milhares de arquivos (agrupar em uma entrada). Arquivos alterados depois da operação. Falha parcial.

## 6. Fora de escopo
- Registrar o que outros programas fizeram; refazer (redo) em lote.

## 7. Critérios de sucesso
- Recuperar um arquivo apagado por engano há 30 minutos em 3 cliques.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: guardar quantas entradas por padrão? Recomendação: 200, com dados só locais.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
