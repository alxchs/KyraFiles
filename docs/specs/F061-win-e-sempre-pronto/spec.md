# F061 — Win+E sempre pronto e sem enxurrada de processos — SPEC

Status: **APROVADA** (Claude, 28/10/2026, a pedido do Alexandre: "Crie as SPECS e siga"). O ponto em aberto do §7 vira a tarefa T0 (decisão D-F061-1). Prioridade **P0** (bug que afeta o computador inteiro). Complementa a F017, sem contradizê-la.

## 1. Por quê

Em 28/10/2026 o Alexandre notou que o Win+E mostrava o Explorer do Windows antes de abrir o KyraFiles. A causa observada: havia cerca de **13 mil** processos `KyraFiles.HotkeyService.exe` (a maioria já terminada, mas ainda listada pelo Windows), criados a ~5 por segundo desde 23:48 de 27/10 (contagem por `Get-Process`; ao encerrar o Watchdog e o serviço, todos sumiram). Isso pesa no computador e pode atrasar ou derrubar o atalho. A causa raiz exata ainda **não está provada** (ver §7).

**Premissa permanente do projeto (pedido do Alexandre, 28/10/2026):** o Win+E abrir o KyraFiles é um recurso essencial. Ele precisa estar **sempre disponível, sempre rápido e nunca prejudicar o computador**. Toda mudança futura que mexa em instalador, atualizador, serviço de atalho ou Watchdog passa por esta garantia antes de ser liberada.

## 2. Histórias de usuário

- **US1 (P0):** Como usuário, quando aperto Win+E, o KyraFiles abre na hora e o Explorer do Windows nunca aparece, em qualquer momento depois do login.
- **US2 (P0):** Como usuário, o computador não pode ficar mais lento por causa do serviço de atalho: nada de processos se acumulando em segundo plano.
- **US3 (P0):** Como usuário, se o serviço de atalho cair, ele volta sozinho em até 1 segundo, **sem nunca** entrar num ciclo de recriação descontrolado.
- **US4 (P1):** Como usuário, quero poder confirmar com um comando que o Win+E está saudável (um serviço, um supervisor, sem sobra).

## 3. Cenários de aceite

| ID | Dado | Quando | Então |
|---|---|---|---|
| **AC-1** | Já existe um serviço de atalho ativo | O supervisor (Watchdog) inicia ou reinicia | O supervisor **não** cria outro serviço: existe exatamente 1 `KyraFiles.HotkeyService.exe` vivo e 1 `KyraFiles.Watchdog.exe` |
| **AC-2** | O serviço de atalho é encerrado à força | Passam até 1 segundo | Um novo serviço está ativo e o Win+E volta a abrir o KyraFiles (mantém o AC-3 da F017) |
| **AC-3** | O serviço morre várias vezes seguidas (ex.: falha repetida na partida) | Observo por 2 minutos | Nunca mais que 4 processos de serviço são criados por minuto, a espera entre tentativas cresce até 30 s, e o `hotkey.log` explica o motivo |
| **AC-4** | O serviço está saudável há mais de 60 s e morre uma vez | O Watchdog reinicia | A espera volta ao mínimo (reinício em até 1 s) |
| **AC-5** | Um segundo serviço é iniciado por fora (login, app, instalador) | Ele detecta que já existe um | Sai com um código próprio ("já existe") e o Watchdog entende que **não** é falha e não recria em ciclo |
| **AC-6** | Uso normal por 30 minutos depois do login | Conto os processos `KyraFiles.HotkeyService` e `KyraFiles.Watchdog` | Exatamente 1 de cada, sem sobras terminadas na lista |
| **AC-7** | o KyraFiles está fechado e aperto Win+E | A tecla é pressionada | o KyraFiles abre; o Explorer não aparece (mantém o AC-1 da F017) |
| **AC-8** | Qualquer liberação nova (exe) | Antes de entregar | O comando de verificação da saúde do Win+E passa e o resultado vai na nota de liberação |

## 4. Requisitos

**Funcionais**
- **FR-001:** O serviço de atalho, ao achar que já existe outra instância, sai com um código de saída próprio e registra uma linha no `hotkey.log`.
- **FR-002:** O Watchdog nunca recria o serviço em intervalo fixo e curto de forma indefinida: a espera cresce a cada morte rápida seguida (200 ms, 400 ms, 800 ms ... até 30 s) e volta ao mínimo depois de 60 s de vida saudável.
- **FR-003:** O Watchdog trata "já existe outra instância" como situação normal: espera e confere periodicamente, sem criar processo novo enquanto o serviço existente estiver vivo.
- **FR-004:** O Watchdog registra no `hotkey.log` (ou arquivo vizinho) cada reinício, com o código de saída do serviço e a espera aplicada.
- **FR-005:** Existe um comando de verificação (`tools/perf/verify_win_e_health.ps1`) que informa: quantos serviços e Watchdogs estão vivos, quantos processos terminados ainda constam na lista, se o hook responde e a idade do serviço.
- **FR-006:** O Watchdog **nunca desiste** de manter o serviço vivo (premissa de disponibilidade); ele só desacelera.

**Não funcionais**
- **NFR-001:** Teto de 4 criações de processo por minuto no pior caso (depois da aceleração inicial) — AC-3.
- **NFR-002:** Reinício em até 1 s para morte isolada — AC-2 e AC-4.
- **NFR-003:** O custo em repouso não aumenta: continua dentro do teto da F017 (< 3 MB de RAM por processo, CPU ~0%).
- **NFR-004:** Qualquer medição citada (contagem, tempo, RAM) vem de comando registrado.

## 5. Casos de borda

- Duas cópias do Watchdog no login (o segundo sai pelo mutex; não deve contar como falha).
- Evento de encerramento ligado (instalador/atualizador pedindo para fechar): o Watchdog e o serviço saem de forma limpa e **não** reiniciam enquanto o evento estiver ligado; ao desligar o evento, o supervisor volta a funcionar na próxima partida.
- Atualização em andamento (arquivo do serviço em uso): o Watchdog espera sem girar.
- Falha de `CreateProcess` (arquivo ausente ou bloqueado): mesma espera crescente.
- Explorer reiniciando (Win+E durante a troca): fora do escopo desta spec.

## 6. Fora de escopo

- Trocar o mecanismo de interceptação (continua o hook de baixo nível, DECISIONS D-021).
- Mudar o instalador (apenas a verificação do AC-8 entra no processo de liberação).
- Limpar processos terminados mantidos por terceiros (é comportamento do Windows).

## 7. Pontos em aberto

- **O que disparou o ciclo às 23:48 de 27/10?**
  - **Hipótese (a) [mecanismo CONFIRMED em 28/10/2026 via T0: 30 processos criados em 15 s com um serviço já vivo; o que fez existir um serviço vivo às 23:48 segue INFERRED, sem prova]:** Já existia um serviço de atalho vivo segurando o mutex `Global\KyraFiles_HotkeyService_InstanceMutex`. Quando o Watchdog iniciava uma nova instância de `KyraFiles.HotkeyService.exe`, a nova instância detectava `ERROR_ALREADY_EXISTS` no mutex e retornava `0` (`src/KyraFiles.HotkeyService/main.cpp:154`). O Watchdog tratava código 0 como término não-anômalo e reiniciava um novo processo após `Sleep(200)` (`src/KyraFiles.Watchdog/main.cpp:85`), gerando um ciclo contínuo de recriação a cada ~200 ms. Reproduzido de forma controlada em cópia isolada na T0: 30 processos distintos observados em 15 segundos (~2 processos/s amostrados; ciclo sustentado sem freio).
  - **Hipótese (b) [DESCARTADA por leitura de código em 28/10/2026]:** Se o evento `Global\KyraFiles_ShutdownHotkeyService` ficasse sinalizado, o Watchdog sairia imediatamente do loop de supervisão (`src/KyraFiles.Watchdog/main.cpp:36-39, 78-81`: `break;`), terminando seu próprio processo sem recriar nenhum serviço. Portanto, o evento de encerramento não poderia ter sustentado o ciclo de recriação.

## 8. Critérios de sucesso

- Reprodução controlada do ciclo (T1) com contagem de processos criados por minuto antes e depois.
- AC-1 a AC-8 com estado por cenário na nota de liberação.
- Premissa registrada em `DECISIONS.md` e no gate de liberação (AC-8).
