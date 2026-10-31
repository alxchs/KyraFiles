# F065 — Busca: Nome e Caminho nas colunas — SPEC

Status: APROVADA (Alexandre, 30/10/2026: "aprovado e siga sem pausas com todas as pendencias"). Fonte da verdade do **comportamento**.

## 1. Por quê
Queixa do Alexandre (30/10/2026, com captura de tela): "O resultado da busca não traz a coluna nome e path corretamente e separados". Na busca por `*.exe` em Downloads, a coluna Nome (e o ícone) aparece em branco e não existe coluna de Caminho; só data, tipo e tamanho aparecem.

## 2. Histórias de usuário (por prioridade)
- **US1 (P1)** Como usuário, quero ver o nome (com ícone) de cada resultado de busca para saber o que encontrei.
- **US2 (P1)** Como usuário, quero uma coluna Caminho separada do Nome para saber onde cada resultado está.
- **US3 (P2)** Quero que isso valha para qualquer motor de busca escolhido, e em qualquer pasta de onde eu busque.

## 3. Cenários de aceite

| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | Pasta aberta em lista de detalhes, motor Everything | busco `*.exe` | cada linha mostra ícone e nome do arquivo, legíveis e com a opacidade normal |
| AC-2 | idem, motor índice interno | busco `*.exe` | idem AC-1 |
| AC-3 | idem, motor nativo | busco `*.exe` | idem AC-1 |
| AC-4 | resultado de busca na lista de detalhes | vejo o cabeçalho | existe coluna Caminho, separada de Nome, com o caminho da pasta do item |
| AC-5 | busca feita a partir de uma pasta que já estava aberta | o resultado aparece | Nome e Caminho aparecem sem precisar trocar de pasta ou de layout |
| AC-6 | item oculto no resultado | vejo a linha | nome em tom esmaecido, mas legível |
| AC-7 | saio da busca para uma pasta normal | vejo o cabeçalho | a coluna Caminho some, como hoje |

## 4. Requisitos
- **FR-001** Todo resultado de busca, de qualquer motor, DEVE exibir nome e ícone visíveis.
- **FR-002** A lista de detalhes DEVE exibir coluna Caminho separada do Nome em resultados de busca.
- **FR-003** A coluna Caminho DEVE respeitar a preferência de largura e de ocultar do usuário, sem esconder o Nome.
- **NFR-001** Zero regressão de desempenho da busca (tempo até o primeiro resultado e até o fim, `*.exe` em Downloads), medido antes e depois com o mesmo script, no mesmo estado.

## 5. Casos de borda
- Resultado vazio; pasta de rede; caminho muito longo (corta com reticências, tooltip com o completo); itens com mesmo nome em pastas diferentes.

## 6. Fora de escopo
- Mudar a ordenação ou os filtros da busca; novas colunas.

## 7. Critérios de sucesso
- Captura de tela do Alexandre repetida com Nome, ícone e Caminho visíveis para cada motor.

## 8. Pontos em aberto
- Nenhum.

## 9. Histórico
- 30/10/2026 — criada e aprovada.
