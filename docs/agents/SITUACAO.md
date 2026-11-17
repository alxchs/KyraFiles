# SITUAÇÃO DO PROJETO — para a AGY ler no começo de TODA tarefa

Atualizado pelo Claude em 26/10/2026, ~01h30. **Quem escreve o último trabalho atualiza este arquivo** (regra R13 da `ORDEM-RIGIDA-AGY.md`). Se este arquivo e o seu resumo de cabeça divergirem, vale este arquivo e a fonte (`spec.md`, `plan.md`, `tasks.md`).

## 1. Onde estamos
- **Publicado:** **v4.3.7 (26/10/2026)** na `main`, com **só a correção do crash da segunda instância (F035)**: build 0/0, Gate F021 ok, 20/20 rodadas com 5 abas (controle 3/20 com crash). Release no GitHub com instalador de 50,25 MB. **Não resolvidos nela:** AC-5, crash do `GridLayoutPage`, AC-2 e AC-4 (NOT TESTED). A v4.3.6 (24/10) foi publicada sem revisão.
- **Specs:** 22 aprovadas (F035 a F056). **Plan e tasks escritos:** F035, F036 (fases A, B e C), F037, F043, F044, F045, F047, F048, F049, F056. **Faltam:** F038, F039, F040, F041, F042, F046, F050, F051, F052, F053, F054, F055.
- **Código entregue por você e revisado:** F035 T1 a T3 (aceitas com ressalvas), F047 T0 a T2 e correções C1 a C3 (aceitas), F056 T1 (formatador). **Incompleto:** F056 T2 a T4 (solto no checkout, nunca compilou).
- **Trava de entrada de teste:** `Send-GuardedInput.ps1` provado (positivo, 4 negativos, modificador, com entrada real). **Integrado na `main` em 26/10/2026** (commit `601d63a89`, **sem mudar a versão**, exceção autorizada pelo Alexandre). Os 57 scripts antigos estão em `tools/perf/_desativados/`.
- **Cota:** `agy --print` só funciona com `gemini-3.8-flash-high` (Claude e GPT zerados até ~01/11). Janela de teste com tela só em janela de AWAY (`ALEXANDRE_AWAY=1`). Sandbox adiado.

## 2. O que deu CERTO (repita)
1. **Controle antes do corrigido:** a F035 só valeu porque o app instalado (sem correção) foi medido primeiro: 3 de 20 rodadas com crash. Depois a versão corrigida: 20 de 20 com as 5 abas.
2. **Conferir no log do Windows**, independente do script (achou o crash do `GridLayoutPage` que o script não contava).
3. **Testes por tabela e falha fechada** na F047 (verificador do git).
4. **Um commit por tarefa, branch própria, handoff com comandos e saídas.**
5. **Parar quando manda parar** (a ordem em passos fixos de 26/10 funcionou: ela restaurou o app, apagou o `bin` e escreveu DISCOVERY).
6. **Probe de cota antes de disparar** e registro em `~/.gemini/accounts/esgotada.txt`.

## 3. O que deu ERRADO (não repita)
1. **Entrada global sem trava** encheu o WhatsApp do Alexandre (24/10). Hoje só o helper.
2. **Resumo escrito de memória** com escopos trocados (F047, F044, F035, F046, F054) e uma lista de T0 errada **repetida depois de corrigida**; handoff citando um script inexistente (`tools/clean_build_dirs.ps1`).
3. **Estourar o orçamento e repetir a bateria** várias vezes, sobrescrevendo o CSV.
4. **Teste que não checa o que o AC diz** (AC-1 só contava crashes, não abas).
5. **Release publicada sem revisão** (v4.3.6), com evidência inválida (captura do VS Code como prova).
6. **Deixar o ambiente sujo:** registro do `KyraFiles` apontando para o worktree, `KyraFiles.exe` de teste aberto, `bin` e `%TEMP%\agy` sobrando.
7. **Código de interface commitado sem compilar** foi evitado: continue só commitando com build 0 erros e 0 avisos.
8. **Modelos fracos:** o GPT-OSS terminou sem saída e sem erro; o Flash errou CSV (cultura) e deixou o verificador git falhar aberto. Revisão do Claude é obrigatória.

## 4. Defeitos conhecidos, ainda sem dono
- **Crash nativo com a prévia aberta** (relatado pelo Alexandre, 26/10 08:11:45): `Microsoft.UI.Input.dll`, `0xc0000409`, ↑↓ rápidos passando por um vídeo. Dump em `%LOCALAPPDATA%\CrashDumps\KyraFiles.exe.15768.dmp` (não apagar). Brief: `docs/agents/tasks/R-crashes-reproducao.md` (janela de AWAY 12:30-13:00 de 26/10).
- **Crash do `GridLayoutPage`** (`LookupConverter`/`Update_FileTagsUI`, 26/10 00:59:04): causa não determinada.
- **AC-5 da F035** (janela estacionada não volta ao primeiro plano): reprova também no app sem a correção; causa provável (INFERRED): o ramo `else` de `PerformNavigationAsync` não chama `BringToForegroundEx`.
- **AC-2 da F035:** o teste precisa de sessão limpa (`ContinueLastSession`).
- **Evidências inválidas de F030, F032, F033, F034, F029, F023** (v4.3.6): refazer com o helper.

## 5. Próximo passo da fila (ordem)
1. (feito) v4.3.7 publicada. 2. Reproduzir o crash do `GridLayoutPage` (sem alterar `src/`). 3. Retrabalho das evidências (janela de AWAY). 4. Medição das teclas seguradas. 5. F056 T2 a T4 (restore com o `--artifacts-path` do `mkfile`) e F047 T3. 6. Onda 2.

## 6. O que deu CERTO na v4.3.7 (acrescentado em 26/10/2026)
- **Release pequena e revisada:** só um arquivo de código; evidência com controle; instalador instalado de verdade e Gate F021 rodado; hash conferido contra a release; texto para o usuário sem jargão.
- **Restore da solução inteira** (`dotnet restore Files.slnx`) antes do build: o restore projeto a projeto falhou duas vezes (`KyraFiles.App.Server`, `KyraFiles.App.IndexService`).
- **O que deu ERRADO no caminho:** o build falhou por restore incompleto; o texto das notas anteriores era para desenvolvedor; faltou captura de tela de fumaça (NOT TESTED).

## 7. Janela de AWAY de 26/10 (12:30-13:00): resultado
- **Crash A (prévia, `Microsoft.UI.Input.dll`): NOT TESTED.** Todas as rodadas terminaram em `ABORTED_GUARD` (`GetForegroundWindow` = 0): zero tecla enviada. Causa provável (INFERRED): **sessão bloqueada ou tela apagada**. **Próxima janela: sessão desbloqueada, sem protetor de tela.** Branch `feature/crash-reproducao` (local, 2 commits, sem push) em `C:\desenv\utils\KyraFiles-crash`.
- **Crash B (`GridLayoutPage`): NOT TESTED** (acabou o tempo).
- **O que deu certo:** a trava de foco impediu qualquer tecla fora de janela válida; ambiente restaurado; dump preservado.
- **O que deu errado:** o build (6 min 23 s) consumiu a janela antes de se descobrir que não havia foco. **Regra nova:** antes do build, um **teste de foco de 10 s** (abrir uma janela própria e confirmar o foreground); sem foco, abortar e avisar, **sem compilar**.
- **Aprendido:** o `KyraFiles.Watchdog.exe` (parte do Win+E persistente) **reabre o KyraFiles** quando ele é fechado; para um teste limpo é preciso parar `KyraFiles.Watchdog.exe` e `KyraFiles.HotkeyService.exe` e depois religá-los (o Claude fez).

## 8. Análise do dump do crash A (26/10/2026, Claude, só leitura)
Crash **nativo e deliberado** do WindowsAppSDK (`Microsoft.UI.Input.dll`, `FAST_FAIL_FATAL_APP_EXIT`, HRESULT `0x8001010E` `RPC_E_WRONG_THREAD`): a **thread do finalizador do .NET** liberou um objeto WinRT (`ContentExternalOutputLink`) que só pode ser liberado na thread da interface. **Sem quadros do KyraFiles na pilha.** Ligação com a prévia de vídeo: compatível, **não confirmada**. Pista (INFERRED): `MediaPreview` só zera `Source` ao descarregar e não libera o `MediaPlayer`. Detalhes e plano: `docs/agents/evidence/crash-reproducao/analise-dump-20261026.md`. **Nenhuma alteração de código antes de reproduzir.**
## 9. Cenário real do crash A (26/10/2026, relato do Alexandre)
Vídeo **não tocou**; navegação com a **seta segurada continuamente**, subindo e depois descendo (inversão de direção). Hipótese (INFERRED, não testada): o ciclo soltar/segurar da F030 cria e destrói o `MediaPlayerElement` várias vezes sem tocar. O teste de reprodução precisa de **tecla segurada com repetição automática** (1 a 3 s, soltar, inverter), não de toques isolados. Detalhes: seção 9 de `analise-dump-20261026.md`.

## 10. F060 M1-T4: Ações, Diálogos e Status Center (27/10/2026, AGY)
- **O que deu CERTO:**
  - `SplitFileAction` e `CombineFilesAction` implementadas com `[GeneratedRichCommand]` e `IAction` em `src/KyraFiles.App/Actions/FileSystem/`.
  - Diálogos implementados via `DynamicDialogFactory` (`GetFor_SplitFileDialog` e `GetFor_CombineFilesDialog`) com presets (100 MB, 650 MB, 4 GB), campo livre com parser de unidades (`KB`, `MB`, `GB`, `GiB`, etc.), seletor de pasta (`ICommonDialogService`) e política de colisão no combine (`Refuse` [padrão], `Overwrite`, `Rename`).
  - Progresso e botão Cancelar integrados ao Status Center (`StatusCenterHelper.AddCard_Split`, `AddCard_Combine`, `UpdateCardStrings` dedicados, vinculados a `CancellationTokenSource`).
  - Menus de contexto em `ContentPageContextFlyoutFactory.cs` configurados estritamente para 1 arquivo selecionado ("Dividir em partes...") e para arquivos `.manifest.json` ou `.001` com manifesto adjacente ("Juntar partes...").
  - 38 strings de UI adicionadas em `src/KyraFiles.App/Strings/en-US/Resources.resw`.
  - Testes unitários `tests/KyraFiles.App.Search.Index.Tests` verdes: 85 passed, 0 failed, 1 skipped.
  - Compilação `mkfile d src\KyraFiles.App\KyraFiles.App.csproj` executada com sucesso: **0 erros e 0 avisos novos** (apenas os 25 avisos CS0618 de código gerado já conhecidos).
- **O que deu ERRADO:**
  - Build do `KyraFiles.App.Server` com `--artifacts-path` não encontrou o `.winmd` no diretório de saída esperado pelo target pós-build do próprio Server; resolvido executando `dotnet build src/KyraFiles.App.Server/KyraFiles.App.Server.csproj -c Debug -p:Platform=x64` sem `--artifacts-path` localmente (0 erros).
  - Avisos `CsWinRT1034` ao fazer cast para `ComboBoxItem` no DynamicDialog; resolvido lendo diretamente `SelectedIndex` via switch numérico, mantendo conformidade 100% trim/AOT-safe.
- **Ponto atual do projeto:**
  - M1-T1, M1-T2, M1-T3 e M1-T4 concluídas e verdes na branch `feature/f060-m1-split-combine`.
  - M1-T5 (prova de tela por UIA após as 23:59) pendente para execução pelo Claude.
  - App NÃO foi aberto (preservado para a M1-T5).

Vídeo **não tocou**; navegação com a **seta segurada continuamente**, subindo e depois descendo (inversão de direção). Hipótese (INFERRED, não testada): o ciclo soltar/segurar da F030 cria e destrói o `MediaPlayerElement` várias vezes sem tocar. O teste de reprodução precisa de **tecla segurada com repetição automática** (1 a 3 s, soltar, inverter), não de toques isolados. Detalhes: seção 9 de `analise-dump-20261026.md`.
