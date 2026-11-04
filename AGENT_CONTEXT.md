# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F052 (Visão de repositório Git) com Plan/Tasks Aprovados; Ajuste visual de Settings concluído
CURRENT TASK   F052 plan e tasks revisados e aprovados pelo Alexandre, integrados na colab (commit 13938c870). Ajuste visual de KyraLogo sem cortes no rodapé de SettingsPage concluído na branch feature/kyrapet-settings-footer (commit cbe79ef24). Solução 100% verde com 194 testes unitários e 0 erros de build.
ACCEPTANCE     194 testes unitários (193 aprovados, 1 ignorado, 0 falhas); compilação 0 erros; colab sincronizada em 13938c870.
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA CICLO    MANDATÓRIO (29/10/2026): Cada spec, uma branch saindo da main/colab. Roteiro de testes executado. Devolução na colab antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO (24 e 29/10/2026): Em HIPÓTESE ALGUMA devolver na colab com performance pior que a colab. Sem lentidão.
REGRA COLAB    Em hipótese alguma iniciar nova spec sem colab atualizada. colab espelha a main exclusivamente por fast-forward (--ff-only).
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA          KyraFiles (31/10/2026): ordem de trabalho em docs/agents/FILA-KYRAFILES.md; Item 1 a 4 entregues; F086 entregue; Item 5 (F052 Visão de repositório Git) com plan e tasks aprovados, pronto para T1.
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY
CURRENT BRANCH feature/kyrapet-settings-footer
LAST DECISION  02/11/2026: Plan e Tasks F052 aprovados pelo Alexandre; Card de KyraLogo sem cortes adicionado ao SidebarView.Footer de SettingsPage.xaml.
NEXT ACTION    Iniciar codificação da T1 (F052) em feature/f052-git-repo-view ou integração do ajuste visual de Settings.
BLOCKER        Nenhum
LAST UPDATE    2026-11-03 00:05 - AGY (F052 aprovada na colab; ajuste de Settings concluído)
```
