# F052 — Visão de repositório Git — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
O app já mostra ações e o estado de Git. Falta o que o fluxo com worktrees pede: esconder o que o `.gitignore` ignora, mostrar a branch atual onde se está olhando e alternar entre worktrees do mesmo repositório.

## 2. Histórias de usuário
- **US1 (P1)** Como desenvolvedor, quero esconder os itens ignorados pelo git na lista.
- **US2 (P1)** Como desenvolvedor, quero ver a branch atual no título da aba.
- **US3 (P2)** Como desenvolvedor, quero listar os worktrees do repositório e abrir um deles na aba atual ou numa nova.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | repositório com `bin/` ignorado | ligo "Ocultar ignorados pelo git" | `bin/` e demais itens ignorados somem; desligar os traz de volta |
| AC-2 | aba num repositório na branch `main` | olho a aba | o título mostra o nome e a branch |
| AC-3 | repositório com 3 worktrees | "Worktrees" | lista os 3 com caminho e branch; clicar abre o escolhido |
| AC-4 | pasta fora de repositório | olho a aba | nada de branch, sem erro |
| AC-5 | repositório muito grande | abro | o estado do git não atrasa a abertura da pasta (carrega depois) |
| AC-6 | HEAD solto (detached) | olho a aba | mostra o hash curto em vez de uma branch |
| AC-7 | worktree sendo usado por outro agente | listo | aparece normalmente, sem ação que o modifique |

## 4. Requisitos
- **FR-001** Somente leitura: nada aqui altera o repositório.
- **FR-002** Consultas ao git em segundo plano, com cache curto.
- **NFR-001** Custo na abertura de pasta medido com e sem a visão ligada (F024 como referência).

## 5. Casos de borda
- Submódulos; repositório corrompido; `git` ausente do PATH; pasta de rede.

## 6. Fora de escopo
- Criar/remover worktrees e branches (o fluxo `ia work` já existe).

## 7. Critérios de sucesso
- Trocar de worktree de um repositório com 2 cliques.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: mostrar a branch só no título da aba, ou também no cabeçalho/barra de status? Recomendação: título da aba e barra de status.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
