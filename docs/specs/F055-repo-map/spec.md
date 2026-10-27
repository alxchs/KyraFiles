# F055 — Mapa do repositório — SPEC

Status: APROVADA (Alexandre, 24/10/2026; pontos em aberto decididos pela Recomendação de cada um, DECISIONS.md D-BACKLOG-1). Fonte da verdade do **comportamento**. Sem tecnologia, arquivo ou classe aqui.

- [x] **APROVADA pelo Alexandre** (data: 24/10/2026)

## 1. Por quê
Quem faz arquitetura e manutenção precisa entender um repositório de relance: quanto de cada linguagem, onde estão os arquivos maiores e quais mudam mais. Hoje isso exige scripts soltos.

## 2. Histórias de usuário
- **US1 (P1)** Como arquiteto, quero um resumo da pasta: linhas por linguagem e os maiores arquivos.
- **US2 (P2)** Como arquiteto, quero os "pontos quentes": arquivos grandes que mudam muito no histórico do git.
- **US3 (P3)** Como arquiteto, quero ver as referências entre projetos de uma solução (`.sln`) como diagrama.

## 3. Cenários de aceite
| ID | Dado | Quando | Então |
|---|---|---|---|
| AC-1 | repositório com C#, XAML e Markdown | "Resumo do repositório" | tabela com arquivos, linhas e bytes por linguagem; total confere com a soma |
| AC-2 | resultado do resumo | clico em uma linguagem | lista seus arquivos, do maior para o menor |
| AC-3 | repositório com histórico | "Pontos quentes" | lista arquivos ordenados por (mudanças × tamanho), com o número de mudanças dos últimos 12 meses |
| AC-4 | pasta fora de git | peço pontos quentes | explica que precisa de git; o resumo por linguagem continua disponível |
| AC-5 | `.gitignore` | gero o resumo | itens ignorados não entram (com opção de incluir) |
| AC-6 | solução com 12 projetos | "Diagrama de projetos" | caixas e setas conforme as referências dos arquivos de projeto |
| AC-7 | análise em andamento | "Cancelar" | para em até 1 s |

## 4. Requisitos
- **FR-001** Somente leitura; nada é alterado no repositório.
- **FR-002** Resultado exportável (CSV e Markdown).
- **FR-003** Mapa de extensões para linguagens editável.
- **NFR-001** Resumo de 10 mil arquivos em até 5 s (medido); histórico do git em segundo plano.

## 5. Casos de borda
- Arquivos gerados e minificados; binários; submódulos; repositório raso (shallow).

## 6. Fora de escopo
- Métricas de qualidade (complexidade ciclomática); qualquer análise por IA.

## 7. Critérios de sucesso
- Responder "onde está o risco deste repositório" com uma lista de 10 arquivos, em menos de 1 minuto.

## 8. Pontos em aberto
- [DECIDIDO 24/10/2026, vale a recomendação: diagrama de projetos (US3) entra agora ou depois? Recomendação: depois; é o item de maior custo e menor frequência.]

## 9. Histórico
- 2026-10-24 — rascunho (Claude).
- 2026-10-24 — APROVADA pelo Alexandre ("Aprovo todas, mas solução de bugs e performance sempre terá prioridade"). Pontos em aberto resolvidos pela recomendação.
