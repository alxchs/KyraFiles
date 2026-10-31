# F065 — Busca: Nome e Caminho nas colunas — TASKS

Status: APROVADO (Alexandre, 30/10/2026: "aprovado e siga sem pausas com todas as pendencias"). Branch de execução: `feature/f065-execucao`, saindo da `colab` rebaseada sobre a `main`. Executora: AGY. Ler antes `docs/agents/REGRA-NAO-PULAR.md`.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| T0 | Medir a linha de base da busca `*.exe` em Downloads (tempo até o primeiro resultado e até o fim, 10 rodadas, instância própria, sem roubar foco) e anotar qual motor o Alexandre usa (configuração salva) | NFR-001 | números e comando no handoff | concluído (`ed1c54681`) |
| T1 | Reproduzir o Nome em branco por motor (Everything, índice, nativo) e provar a causa; só então definir `Opacity` no `EverythingSearchProvider`. Hipótese falsa = investigar o template, sem contorno | FR-001, AC-1..3, AC-6 | captura por motor com Nome e ícone legíveis; build 0/0 | concluído (`7ff393dc1`) |
| T2 | Descobrir por que o Caminho não aparece (preferência, largura, evento) e corrigir a causa comum | FR-002, FR-003, AC-4, AC-5, AC-7 | captura com Caminho separado do Nome, a partir de pasta já aberta | concluído (`4e5140fb1`) |
| T3 | Testes automatizados: resultado com `Opacity > 0` por provedor; item oculto esmaecido | AC-1..3, AC-6 | testes novos passam; suíte existente sem falhas | concluído (`47e2fece8`) |
| T4 | Repetir a medição da T0 e comparar | NFR-001 | igual ou melhor, com comando e saída | concluído (`c2f6e673d`) |
| T5 | Relatório de conclusão e handoff com estado por `AC-n` | todos | handoff `docs/agents/handoffs/` | concluído |

## Teste obrigatório
Um por `AC-n`, conforme o `plan.md`. Prova de tela só às 09:00 de 31/10 ou com o Alexandre ausente (METODO §16); sem `SetForegroundWindow`, sem `TopMost`. Prova que não puder rodar fica `NOT TESTED`, nunca `CONFIRMED`.

## Entrega
Build Release 0 avisos e 0 erros, um commit por tarefa, handoff por `AC-n`. Sem push, sem merge na `main` (a `main` recebe UM commit squash, ver `feedback_main-um-commit`). Remover branch e worktree depois de integrada na `colab`.
