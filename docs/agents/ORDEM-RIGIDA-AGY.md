# ORDEM DE TRABALHO RÍGIDA PARA A AGY — 24/10/2026 (Claude, arquiteto)

> Texto autocontido: o Alexandre cola **tudo abaixo da linha** na janela da AGY. Ela não precisa ler outro arquivo para saber as regras. Vale até o Claude escrever "LIBERADO" numa nova ordem.

---

## CONTEXTO (não discuta, execute)

A automação de teste que você escreveu enviou teclas para a janela em foco e **encheu o WhatsApp do Alexandre de mensagens**. Os scripts de `tools/perf/` usam `SendKeys`, `SendInput`, `keybd_event` e `mouse_event` sem conferir qual janela está em foco. Além disso, a v4.3.6 foi publicada sem a revisão do Claude, e as evidências de F030, F032, F033, F034 e F023 não foram refeitas com testes com asserção. Por isso, **a partir de agora você trabalha sob as regras abaixo, sem exceção**. Cada regra tem a consequência de uma violação: o Claude reverte o trabalho e o Alexandre é avisado.

## REGRAS RÍGIDAS (R1 a R12)

- **R1. Escopo único.** Você só faz as tarefas S1 a S5 desta ordem, na ordem. Não toque em `src/`, `installer/`, `docs/specs/` nem em nenhum arquivo fora de `tools/perf/` e `docs/agents/`. Qualquer outra ideia vira `DISCOVERY` num arquivo de handoff, não código.
- **R2. Branch.** Crie `feature/test-input-guard` a partir da `main` atual (v4.3.6). **Proibido** alterar ou commitar na `main`. Um commit por tarefa, formato `tipo(escopo): resumo`.
- **R3. Sem publicar.** Proibido `git push`, tag, merge, release, instalador, `gh release`. Você só faz commits locais na sua branch. O Claude revisa e o Alexandre autoriza o push.
- **R4. Nenhuma entrada global.** Até o Claude revisar o helper da S2, **é proibido rodar qualquer script que envie tecla ou clique global**, inclusive os antigos. Isso inclui `SendKeys`, `SendInput`, `keybd_event`, `mouse_event`, `SetCursorPos` e `SetForegroundWindow`. As provas da S3 usam somente janelas **que o próprio script cria**.
- **R5. Nunca outro app.** É proibido anexar a um `KyraFiles.exe`, Bloco de Notas, navegador ou qualquer processo que você não iniciou. É proibido tocar no app que o Alexandre usa, no WhatsApp ou em qualquer chat, e-mail ou editor.
- **R6. Sem texto livre por teclado.** Nunca digite caminho nem texto com `SendKeys`. Para preencher campo, use UI Automation (`ValuePattern`) ou argumento de linha de comando.
- **R7. Afirmar só com prova.** "Funciona" exige o comando e a saída colados no handoff. Sem prova, escreva `NOT TESTED`. Proibido ✔ escrito à mão; relatório de teste é **gerado** do CSV do teste.
- **R8. Dúvida para tudo.** Se algo for ambíguo, ou o resultado surpreender, **pare**, escreva `PENDING DECISION` em `DECISIONS.md` (contexto, opções, a escolha que faria) e aguarde. Não improvise e não "corrija o que parece errado" fora do escopo.
- **R9. Temporários.** Todo arquivo temporário vai em `%TEMP%\agy\<GUID>\desenv\utils\KyraFiles\Files\`. Nunca no Downloads, na Área de Trabalho nem solto no `%TEMP%`. Apague a pasta do GUID ao fim.
- **R10. Não matar processos alheios.** Nunca encerre `agy.exe --hub` de outra janela nem processos que você não iniciou.
- **R11. Uma tarefa por vez, e pare no fim.** Termine a S5 e **pare**. Não comece F035, F036 ou qualquer feature. A próxima ordem vem do Claude.
- **R12. Honestidade.** Se uma tarefa falhar, diga que falhou e mostre a saída. Dizer "concluído" sem prova é a violação mais grave desta ordem.

## TAREFAS

**S1 — Inventário (somente leitura).** Gere `docs/agents/evidence/test-input-guard/inventario.csv` com uma linha por arquivo de `tools/perf/` que contenha `SendKeys`, `SendInput`, `keybd_event`, `mouse_event`, `SetCursorPos` ou `SetForegroundWindow` (hoje são 54 arquivos). Colunas: arquivo, quais chamadas, se anexa a um processo que não iniciou (`Get-Process Files` sem checar PID próprio), se digita texto livre. Gere o CSV por script, não à mão. Critério: o número de linhas bate com o `git grep -l` do mesmo padrão (cole os dois comandos e as contagens).

**S2 — Helper com trava.** Crie `tools/perf/Send-GuardedInput.ps1` (único ponto de entrada global permitido do repositório) com:
- `Start-GuardedApp` que inicia o app de teste e guarda o PID;
- `Assert-ForegroundOwned -Pid <pid>`: compara `GetForegroundWindow()` com o PID guardado (inclui processos filhos do mesmo app); se não for do app, **lança erro** e o script termina com código ≠ 0;
- `Send-GuardedKey`, `Send-GuardedClick`: chamam `Assert-ForegroundOwned` **imediatamente antes** de cada evento, e nunca chamam `SetForegroundWindow`;
- apenas teclas virtuais (nada de texto livre); e `Set-ElementText` por UI Automation para campos;
- um tempo limite global e uma tecla de emergência (por exemplo, o script aborta se detectar o foco fora do app por duas verificações seguidas).
Critério: build do script sem erro de sintaxe (`[ScriptBlock]::Create`) e revisão do Claude.

**S3 — Prova do helper, só com janelas criadas pelo próprio teste.** Crie `tools/perf/Test-InputGuard.ps1`, que cria dois formulários próprios (A com caixa de texto, B com caixa de texto) e prova, com asserções e saída ≠ 0 se algo falhar:
- **Positivo:** com A em foco, `Send-GuardedKey` entrega a tecla ao A (a caixa de A muda).
- **Negativo:** com B em foco (e A ainda sendo o "app de teste"), `Send-GuardedKey` **aborta com erro** e **nenhuma** tecla chega a A nem a B (as duas caixas continuam vazias).
- **Negativo 2:** processo de teste que já encerrou: o helper aborta.
Gere `docs/agents/evidence/test-input-guard/resultado.csv` (uma linha por caso: esperado, observado, PASS/FAIL, comando) e o relatório **a partir do CSV**. Os formulários ficam num canto da tela e fecham sozinhos. Não use o Bloco de Notas nem nenhum app real.

**S4 — Quarentena e trava automática.**
- Mova **todos** os scripts do inventário (exceto o helper e `Test-InputGuard.ps1`) para `tools/perf/_desativados/` renomeando a extensão para `.ps1.desativado`. Nenhum é apagado; nenhum roda por engano.
- Crie `tools/perf/Test-NoRawInput.ps1`, que falha (código ≠ 0) se qualquer arquivo de `tools/perf/` (fora do helper e de `_desativados/`) contiver `SendKeys`, `SendInput`, `keybd_event`, `mouse_event`, `SetCursorPos` ou `SetForegroundWindow`. Cole a saída dele limpo, e a saída reprovando um arquivo de exemplo temporário.
- Registre no `docs/agents/PLAYBOOK.md` a regra: "todo script de teste que usa entrada global importa `Send-GuardedInput.ps1`; `Test-NoRawInput.ps1` roda antes de cada commit".

**S5 — Handoff e parada.** Crie `docs/agents/handoffs/2026-10-24_<hhmm>_agy_test-input-guard.md` com: o que foi feito por tarefa, os comandos e as saídas que provam cada critério, o `git log --oneline` da branch, o que **não** foi feito, e `DISCOVERY`/`PENDING DECISION` se houver. Atualize `AGENT_CONTEXT.md` na sua branch (OWNER: AGY, NEXT ACTION: "Claude revisa o helper"). **Pare.** Não faça push. Avise o Alexandre: "S1 a S5 prontas para a revisão do Claude".

## O QUE O CLAUDE CONFERE NA REVISÃO

1. `git grep -l -E 'SendKeys|SendInput|keybd_event|mouse_event|SetCursorPos|SetForegroundWindow' -- tools/perf` só acha o helper (e `Test-NoRawInput.ps1`, que cita os nomes como texto).
2. O teste negativo realmente reprova: as duas caixas vazias e saída ≠ 0.
3. Não existe `SetForegroundWindow` no helper.
4. O diff da branch não toca `src/`, `installer/` nem `docs/specs/`.
5. Nenhum processo `KyraFiles.exe` ou outro app foi anexado durante as provas.
6. Handoff com comandos e saídas; nenhum ✔ manual.

## R13. RELATÓRIO DE SITUAÇÃO (regra de 26/10/2026, pedido do Alexandre; vale a partir de agora)
**No começo de toda tarefa** leia `docs/agents/SITUACAO.md`. **No fim de toda tarefa, e também quando parar por cota ou por estouro de orçamento**, atualize esse arquivo e escreva no handoff, **nesta ordem**: (1) **o que deu certo** (com comando e saída); (2) **o que deu errado** (sem maquiar; erro seu, do teste ou do ambiente); (3) **o ponto em que o projeto está** (o que ficou pronto, o que falta, qual é o próximo passo e o que depende do Alexandre ou do Claude). Releia a fonte antes de escrever (lições 12 e 13). Resumo sem as três partes, ou escrito de memória, é violação.
