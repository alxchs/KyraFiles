# CONTEXTO DE DESENVOLVIMENTO

Estado atual, curto. Atualize ao mudar de fase/tarefa (METODO.md §1).

```text
PROJECT        Files Custom (derivado do Files Community). Regras: MASTER_SPEC.md; continuidade: docs/agents/CONTINUITY.md
OBJECTIVE      File manager moderno com UX mais previsível. Ver MASTER_SPEC.md §2 e §44
CURRENT PHASE  F083 (Identidade KyraFiles: Marca, Ícones do Windows e Mascote no About) concluída
CURRENT TASK   F083 validado: Logo.ico multi-resolução gerado a partir da mascote pet KyraFiles (D:\DriveD\Downloads\KyraFiles.png), todos os tiles de pasta rosa substituídos, AboutPage com imagem BitmapIcon da mascote Kyra, AppName e mensagens ao usuário em 4 idiomas (en-US, pt-BR, de-DE, es-ES) harmonizados para KyraFiles, 192 testes unitários passando (0 falhas), compilação com 0 erros via mkfile, F021 validado, push realizado e integrado na colab
ACCEPTANCE     192 testes unitários passando (0 falhas); compilação 0 erros via mkfile; F021 verificado com sucesso; RELATORIO.md criado; commit-push registrado no ledger; integrado na colab via ff-only
REGRA DA MAIN  PROIBIDO alterar direto na main. AGY e Claude criam branches próprias (feature/<nome>) e integram via squash/rebase.
REGRA CICLO    MANDATÓRIO (29/10/2026): Cada spec, uma branch saindo da main/colab. Roteiro de testes executado. Devolução na colab antes de iniciar próxima spec.
REGRA PERF     MANDATÓRIO (24 e 29/10/2026): Em HIPÓTESE ALGUMA devolver na colab com performance pior que a colab. Sem lentidão.
REGRA COLAB    Em hipótese alguma iniciar nova spec sem colab atualizada. colab espelha a main exclusivamente por fast-forward (--ff-only).
REGRA RELAT    Todo final de spec exige relatório de conclusão com detalhes importantes, pontos de atenção e comparativo de performance.
FILA          KyraFiles (31/10/2026): ordem de trabalho em docs/agents/FILA-KYRAFILES.md; Item 1 (F069) entregue; F072 entregue; Item 2 (F070) entregue; Item 3 (F071) entregue; Item 4 (F081) entregue; próximo: Item 5 (F052 Visão de repositório Git)
REGRA FOCO     MANDATÓRIO (29/10/2026): NUNCA roubar o foco da tela do usuário. Usar -NoLaunch e testes não-intrusivos.
REGRA VERSAO   MANDATÓRIO (21/10/2026): O número da versão DEVE ser incrementado a CADA atualização integrada na main.
DO NOT CHANGE  Nada na main diretamente. Trabalho somente em branch de feature.
OWNER          AGY
CURRENT BRANCH colab
LAST DECISION  01/11/2026: F083 (Nova identidade oficial KyraFiles com ícones gerados a partir do pet, substituição dos tiles rosa, imagem colorida no About e textos harmonizados)
NEXT ACTION    Aguardando novas orientações do usuário / fila KyraFiles
BLOCKER        Nenhum
LAST UPDATE    2026-11-01 16:45 - AGY (F083 concluído, testado, commit-push e merge na colab realizados)
```
