# Fila da AGY — gerenciada pelo Claude

Atualizado em 24/10/2026. Regra do Alexandre (24/10/2026): **solução de bugs e performance sempre tem prioridade** (DECISIONS.md D-BACKLOG-1). O Claude escreve plan e tasks, revisa cada entrega e só então libera o próximo item. A AGY executa na ordem, um item por vez, uma feature por branch.

## Regras de precedência (valem sempre)

1. **P0 — bug que derruba o app ou perde dado.** Para tudo o que não for P0.
2. **P1 — regressão ou pendência de performance.**
3. **P2 — evidência pendente de feature já feita** (prova com asserção; se reprovar, volta a ser bug P0/P1).
4. **P3 — feature nova**, por onda, só depois de a anterior estar `CONFIRMED`.

Bug novo achado durante uma feature vira item P0/P1 na hora e passa na frente. Item só sai da fila quando o Claude revisa e aceita.

## Fila atual

| # | Prioridade | Item | Estado | Próximo passo |
|---|---|---|---|---|
| 1 | P2→P0 se falhar | Evidência real de F030, F032, F033, F034 | AGY refazendo com testes com asserção (ordem de 24/10) | AGY entrega `resultado.csv` por feature; Claude revisa |
| 2 | **P0** | **F035** Segunda instância e abertura direta (crash real) | spec, plan e tasks aprovados | T1 a T6 em `docs/specs/F035-second-instance/tasks.md` |
| 3 | P1 | F029 Setas como no Explorer | branch local `feature/t3-f029-setas-explorer` | benchmark contra o Explorer, eleger a variante, remover bandeiras |
| 4 | P1 | F023 T11 (estresse de memória da busca) e F026 T4 | sem atividade | medição de memória sob buscas contínuas; Ctrl+A e Shift+setas em pasta grande |
| 5 | P1 | `R-rede-baseline` (medição para F046) | brief entregue | CSV com 3 rodadas por variante |
| 6 | P3 onda 1 | F056, F047, F049 | specs aprovadas; plan e tasks a escrever | Claude escreve quando o item 2 estiver `CONFIRMED` |
| 7 | P3 onda 2 | F037, F044, F045, F048 | idem | idem |
| 8 | P3 onda 3 | F043, F038, F052, F036 | idem | F036 exige medição de memória (cópia) |
| 9 | P3 onda 4 | F042 (após F023), F046 (após a baseline), F039, F040, F041 | idem | idem |
| 10 | P3 onda 5 | F051, F053, F054 (começa pela tarefa 0: medir a prévia atual) | idem | idem |
| 11 | P3 última | F055; F050 só se a medição de custo justificar | idem | idem |

## O que o Claude confere em cada entrega (checklist de revisão)

- Existe `resultado.csv` com uma linha por AC, e o relatório foi gerado dele (não escrito à mão).
- A captura mostra a janela do Files inteira e o estado pedido; não é de outra janela.
- O teste usou instância própria e o build certo (caminho, versão e commit registrados).
- Há controle negativo (o teste reprova onde deve).
- Build com 0 erros e 0 avisos; o diff só toca o que o plan permite.
- Para performance: medição antes e depois com os mesmos scripts (METODO §11).
- Para qualquer tamanho/memória: o comando que mediu.
- No fim: `FilesDev` de volta para o instalado, `bin` e GUID temporário apagados, uma cópia só do app.

## Regra de segurança da automação de teste (24/10/2026, incidente real)

Os scripts de teste enviavam teclas (`keybd_event`, `SendKeys`) para **a janela em foco**, sem conferir qual era. Em 24/10/2026 a automação encheu o WhatsApp do Alexandre de mensagens sem sentido (o `verify_f034.ps1` envia `C:\Windows{ENTER}` e atalhos como Ctrl+T, Ctrl+W e setas). Nada disso pode se repetir.

1. **Proibido** enviar tecla ou clique global sem antes confirmar, no mesmo instante, que `GetForegroundWindow()` pertence ao processo de teste (PID guardado ao iniciar). Se não pertencer, **aborta** o script com erro; não tenta "trazer para frente" e seguir.
2. Preferir a UI Automation (`InvokePattern`, `ValuePattern`, `SelectionItemPattern`) a teclas globais. Só usar teclado onde a UIA não alcança, sempre atrás da trava do item 1, e com mensagem na tela dizendo que a automação vai usar o teclado.
3. **Proibido** digitar texto livre (como um caminho) por `SendKeys`. Navegar pelo ValuePattern da barra de endereços ou pelo argumento de linha de comando do app de teste.
4. Um único helper compartilhado (`tools/perf/Send-GuardedKeys.ps1`) faz a trava; nenhum script chama `keybd_event` ou `SendKeys` direto. Revisar e apagar os scripts que chamam direto.
5. Rodar a automação com o Alexandre longe do teclado só quando ele avisar; nunca com WhatsApp, e-mail ou chat abertos em primeiro plano.
6. Teste de estado antes de tudo: a automação confere que a janela do teste está visível e inteira na tela; se algum diálogo de outro app ficar por cima, aborta.

## Estado dos planos (atualizado em 24/10/2026, noite)

| Onda | Item | spec | plan | tasks | Observação |
|---|---|---|---|---|---|
| 0 (P0) | F035 | aprovada | aprovado | aprovadas | bug do crash da segunda instância |
| 1 | F056, F047, F049 | aprovadas | aprovados | aprovadas | F049 começa por controle negativo de segurança (exclusão de link) |
| 2 | F037, F044, F045, F048 | aprovadas | aprovados | aprovadas | F037 já traz o `NameParts` pedido pela F001; F048 e F041 compartilham o `FolderComparer` |
| 3 | F036 | aprovada | aprovado nas fases A e B | aprovadas T0 a T6 | **T7 e T8 aguardam a decisão D-F036-1 do Alexandre** |
| 3 | F043, F038, F052 | aprovadas | a escrever | a escrever | |
| 4 | F042, F046, F039, F040, F041 | aprovadas | a escrever | a escrever | F042 após F023; F046 após a baseline de rede |
| 5 | F051, F053, F054 | aprovadas | a escrever | a escrever | F054 começa pela tarefa 0 |
| última | F055, F050 | aprovadas | a escrever | a escrever | F050 só se a medição justificar |

Todos os planos e tasks acima estão **bloqueados** até o Claude escrever "LIBERADO" depois de revisar a S5 da ordem rígida (`docs/agents/ORDEM-RIGIDA-AGY.md`). Teste que usa teclado ou mouse globais segue `~/.dev-method/SANDBOX.md` (METODO §14).

## Atualização de 25/10/2026 (revisão da S5)

S5 do `test-input-guard` **aceita com 3 correções** (parecer em `docs/agents/reviews/2026-10-25_s5-test-input-guard.md`). **Liberação parcial:** correções C1 a C4 e as T0 (somente leitura) de F036, F037, F044, F045, F047, F048, F049 e F056. **Segue bloqueado:** F035, retrabalho de evidências e qualquer tarefa com interface, até a prova do helper com `ALEXANDRE_AWAY=1`. Regra nova: teste que abre janela ou envia entrada só roda com `ALEXANDRE_AWAY=1`.

## Atualização de 25/10/2026 (tarde): liberação 2

Prova real do helper: 4 PASS e 1 FAIL (defeito do teste do modificador). **Liberado:** correção do teste C2 (sem rodar) e as tarefas **sem janela e sem modificador** da Onda 1, **uma feature por vez com revisão entre elas**: começa pela **F056 T1 a T4**. **Segue bloqueado:** tarefas com janela (F035 T1 e T3, provas de interface T5/T6 de cada feature), retrabalho de evidências e qualquer uso de modificador, até a próxima janela de AWAY (protocolo no parecer da S5). Em janela de AWAY a ordem de prioridade é: F035 (bug P0), depois retrabalho de evidências.

## Atualização de 25/10/2026 (tarde): teclas seguradas (P1 desempenho)

Nova medição P1 em `docs/agents/tasks/R-teclas-seguradas-medicao.md` (setas, Home e End seguradas em Detalhes, Ícones e Colunas, contra o Explorer e a v4.3.5). Roda em **janela de AWAY**, depois do `Send-GuardedKeyHold`. Na primeira janela de AWAY a ordem é: **F035 (bug P0), retrabalho de evidências, medição das teclas seguradas (P1)**. Só nasce a F058 se a medição mostrar lacuna.

## Atualização de 25/10/2026 (18h): cota da AGY
**Grupo Gemini:** semanal, renova em 28/10 por volta de 12:49. **Grupo Claude:** renovou-se em janela de 5 h, esgotou de novo às ~18:00, volta por volta de 22:48. Trabalho solto da F056 T2 a T4 **salvo em patch** (`%TEMP%\claude\...\backup-f056`), mas **não commitado** no checkout dela. Opções: (A) esperar a janela do grupo Claude; (B) modelo do 3º grupo (`gpt-oss-120b-medium`), mais fraco; (C) trocar de conta com `trocarConta` (fecha **todas** as janelas da AGY, inclusive a do pdfreaderpro), só com autorização do Alexandre. Plan e tasks da **F043** escritos (Onda 3).

## Atualização de 25/10/2026 (noite): Sandbox adiado
O Alexandre adiou o Windows Sandbox (1 GB de RAM livre contra os ~4 GB que ele pede). **Ninguém usa o Sandbox por enquanto.** Prova de interface continua só em janela de AWAY (Files do Alexandre fechado, `ALEXANDRE_AWAY=1`, helper com trava de foco). Os scripts de `tools/perf/sandbox/` ficam prontos, não testados.


## Atualização de 25/10/2026 (23h): cota e disparos
- **`agy --print` segue sem cota nos grupos Claude (até ~01/11 17:47) e GPT-OSS**, mesmo depois de o Alexandre trocar a conta **do aplicativo**: o `--print` usa o login da **CLI** (`antigravity-cli`), que não acompanha. Só `gemini-3.8-flash-high` responde. As janelas **interativas** (hubs) usam a conta nova do aplicativo e têm cota cheia: tarefas pesadas (F056 T2-T4, build do `Files.App`) são para **colar nelas**, não para o `--print`.
- Disparados às 23h: **F035 T1-T3** (AWAY, worktree `FilesApp-f035`) e **F047 C1-C3** (sem janela, worktree `FilesApp-f047`), ambos com Flash.
- **Erro de registro da AGY a corrigir:** D-F036-3 é a opção (b) (limite configurável, padrão 2, apagar/renomear fora da fila), e não "fila única serializada".

## Atualização de 26/10/2026 (noite): F057
Nova spec **F057 Atalhos de teclado nos botões das caixas de diálogo** (pedido do Alexandre com a captura da caixa Excluir item): spec, plan e tasks em docs/specs/F057-dialog-button-shortcuts/, **aguardando aprovação**. Entra na Onda 1, depois da F056. Lista completa de pendências e ordens para a AGY: docs/agents/PENDENCIAS-20261026.md.


## Atualização de 26/10/2026 (noite, 2): F057 aprovada e F058 nova; regra de teste de tela
- **F057 APROVADA com alcance global (17 caixas).** Plan e tasks atualizados; **T1 pronta** (8 testes), **T2 escrita**, build em andamento; T3 (tela) só depois das 23:59.
- **F058 (nova): janela do instalador e da atualização sempre à frente**, instância do modelo de qualquer projeto em ~/.dev-method/templates/spec-instalador-janela-frente.md (METODO §15). Spec em docs/specs/F058-installer-window-to-front/, aguardando aprovação. A medição das teclas seguradas, se gerar spec, passa a ser **F059**.
- **Regra de teste de tela (METODO §16):** só depois das 23:59 ou com o Alexandre longe; nunca roubar o foco nem trazer janela para frente (sem SetForegroundWindow, AttachThreadInput, TopMost); preferir UI Automation; senão NOT TESTED.

## Atualização 26/10/2026 (noite) — F060

- **F060 (diferenciais: Dividir/Juntar, editor hexadecimal, SFTP, script)**: spec PROPOSTA a partir do arquivo enviado pelo Alexandre, já recortada pelo que existe (`docs/specs/F060-diferenciais-sftp-script-hex-split/matriz-situacao.md`). Sync, renomeio em lote e fila **não** entram (F041, F037, F036). Aguarda aprovação e a escolha do primeiro módulo (Q1). Nenhuma tarefa liberada para a AGY.
