# F063 — Otimização de memória — TASKS DE CORREÇÃO

Status: APROVADO (Claude, 30/10/2026; decisões do Alexandre no questionário de 29/10/2026; execução do Claude autorizada em 30/10: "Assuma o que ela não começou ainda que consiga acabar até as 14:00"). Complementa `tasks.md`. Branch: `feature/f063-correcoes`. A spec voltou a RASCUNHO (Q13) e só é aprovada depois das provas das 09:00 de 31/10.

| # | Tarefa | Atende | Dono | Pronto quando | Estado |
|---|---|---|---|---|---|
| C1 | Remover a liberação agendada por perda de foco; manter o cancelamento ao voltar | FR-002, AC-3 (Q1) | Claude | build 0/0; nenhum `ScheduleTrim` fora de minimizar/ocultar/bandeja | feito (Claude, 30/10) |
| C2 | Espera de operações de arquivo com prazo de 30 s e cancelamento pela restauração; pula a liberação se o prazo vencer | FR-003, AC-4 (Q2) | Claude | build 0/0 | feito (Claude, 30/10) |
| C3 | Spec de volta a RASCUNHO, com Q1, Q2, Q3, Q5 e A2 refletidos | Q13 | Claude | spec com histórico | feito (Claude, 30/10) |
| C4 | Nota interna da 4.3.9: estados rebaixados, build sem saída marcado NOT TESTED, texto do usuário igual ao publicado | Q9 | Claude | `docs/releases/2026-10-29_build-KyraFilesSetup-4.3.9.*` | feito (Claude, 30/10) |
| C5 | Registro dos descumprimentos e exigência de explicação | Q11 | Claude | `docs/agents/DESCUMPRIMENTOS.md` | feito (Claude, 30/10) |
| C6 | Script de medição com UI Automation e instância própria (até o primeiro quadro) | NFR-002 (Q12) | AGY | script novo, sem `ShowWindow` na janela do Alexandre | pendente |
| C7 | Linha de base e medição de 10 rodadas (mediana e p95, ±10 ms) | AC-1, AC-2, NFR-002 (Q4, Q5) | AGY | às 09:00 de 31/10, com C6 | pendente |
| C8 | Sobre: rótulo e número de memória privada além da RAM ocupada | AC-5 (A2) | AGY | captura às 09:00 de 31/10 | pendente |
