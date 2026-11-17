# F061 — Win+E sempre pronto — PLAN

Status: **APROVADO** (Claude, 28/10/2026).

## 1. Arquitetura atual (lida no código em 28/10/2026)

- `src/KyraFiles.Watchdog/main.cpp` (CONFIRMED, lido): mutex `Global\FilesCustom_Watchdog_InstanceMutex` (linha 15); laço que cria `KyraFiles.HotkeyService.exe` e espera o processo ou o evento `Global\FilesCustom_ShutdownHotkeyService` (linha 66); depois da morte espera **5000 ms se o código for 2, senão 200 ms** (linha 85); falha de `CreateProcess` espera 500 ms (linha 90). Sem contador, sem aumento de espera, sem log.
- `src/KyraFiles.HotkeyService/main.cpp` (CONFIRMED, lido): mutex de instância única (linha 150); se já existe, `return 0` (linha 154), **mesmo código de uma saída normal**; falha de janela retorna 1 (linha 184); falha do hook retorna `EXIT_HOOK_FAILED` = 2 (linha 194); uma thread fecha o serviço quando o evento de encerramento é sinalizado (linhas 136 a 144). O `hotkey.log` só recebe linhas nas falhas de janela e de hook.
- Autostart: chave `FilesCustomWatchdog` em `HKCU\...\Run` apontando para `KyraFiles.Watchdog.exe` (CONFIRMED por leitura do registro em 28/10/2026).
- Observação do incidente (OBSERVED): ~13 mil processos `KyraFiles.HotkeyService.exe`, quase todos `HasExited = True`, 1 thread, ~84 handles; criação a ~5 por segundo; o Watchdog em execução (2 listados no início).

## 2. Abordagem

Duas mudanças pequenas e independentes, mais uma verificação:

1. **Código de saída próprio** `EXIT_ALREADY_RUNNING = 3` no serviço (FR-001), com linha no log.
2. **Espera crescente no Watchdog** (FR-002/003/004/006): estado `consecutiveFastDeaths` e `delayMs`.
   - Vida do filho < 60 s: `delayMs = min(delayMs * 2, 30000)` (começa em 200); vida ≥ 60 s: `delayMs = 200`.
   - Código 3 ("já existe"): não recria; espera 2 s e verifica por `OpenMutexW` se o mutex do serviço ainda existe; só cria outro se sumiu.
   - Código 2 (hook falhou): mantém 5 s mínimo, sobe pela mesma regra.
   - Cada reinício grava linha no log com código, vida do filho e espera aplicada.
   - Nunca encerra por excesso de falhas (premissa); só desacelera.
3. **Verificação** `tools/perf/verify_win_e_health.ps1` (FR-005) e passo obrigatório na liberação (AC-8).

### Opções descartadas
- Parar o Watchdog depois de N falhas: viola a premissa de disponibilidade.
- Trocar o Watchdog por tarefa agendada: muda a arquitetura da F017; fica como alternativa se a T1 mostrar que o Watchdog é a fonte de múltiplas instâncias.
- Matar processos terminados da lista: o Windows os mantém por referência de terceiros; não é nosso.

## 3. Mapa requisito → código

| Requisito | Onde |
|---|---|
| FR-001 | `KyraFiles.HotkeyService/main.cpp` (ramo do mutex, linha ~154) e `LogLine` |
| FR-002, FR-003, FR-004, FR-006 | `KyraFiles.Watchdog/main.cpp` (laço de supervisão, linhas ~33 a 91) |
| FR-005 | novo `tools/perf/verify_win_e_health.ps1` |
| AC-8 | `docs/agents/PLAYBOOK.md` (lista de liberação) e modelo da nota de liberação |

## 4. Arquivos a mexer e a não mexer

**Mexer:** `src/KyraFiles.Watchdog/main.cpp`, `src/KyraFiles.HotkeyService/main.cpp`, `tools/perf/verify_win_e_health.ps1` (novo), `docs/agents/PLAYBOOK.md`, `DECISIONS.md`, `STATUS.md`.
**Não mexer:** `installer/*`, `ExplorerWindowRedirector.cs`, `MainWindow.xaml.cs`, o hook em si (função `LowLevelKeyboardProc`).

## 5. Riscos

- Mudar o Watchdog afeta todo login: testar em cópia isolada, nunca sobre os binários em uso, e com o serviço instalado intacto.
- Espera de 30 s pode atrasar o retorno do atalho depois de falhas repetidas; é o preço de não afogar o computador (AC-3 define o teto).
- Binários nativos exigem `tools\Build-Native.ps1` e Visual Studio com C++; confirmar antes de iniciar a T2.

## 6. Estratégia de teste por AC

| AC | Como |
|---|---|
| AC-1, AC-5, AC-6 | `verify_win_e_health.ps1` após subir o conjunto em pasta de teste isolada; iniciar um segundo serviço à mão e contar |
| AC-2, AC-4 | encerrar o serviço de teste e medir o tempo até o novo estar ativo (`Stopwatch`); depois forçar 60 s de vida e repetir |
| AC-3 | serviço de teste que falha na partida (variável de ambiente de teste ou executável substituto) e contagem de criações por minuto por 2 min |
| AC-7 | prova de tela só na janela combinada (METODO §16), sem roubar foco; o resto roda sem tela |
| AC-8 | o próprio comando de verificação na nota de liberação |

Temporários em `%TEMP%\claude\<GUID>\...`; nada fora disso.
