# Handoff — Claude, F012 e diálogo de arquivos (09/10/2026 01:10)

Branch `feature/f012-explorer-replacement` (checkout principal `C:\desenv\utils\FilesApp`), commits `0ac566f1c` e `5be8f220e`. Sem push.

O Claude escreveu o código desta rodada **a pedido explícito do Alexandre** (08/10/2026: "acabou a cota da agy. Continue o desenvolvimento"), por cima da regra "cota nunca vira trabalho do Claude".

## Causa do defeito relatado

"Usar o Files como diálogo para Abrir Arquivos" registrava seis DLLs C++/ATL que nunca foram compiladas neste fork: os projetos C++ saíram do `Files.slnx` em `e3b8b0e38`, e o VS 2022 da máquina não tinha C++ nem ATL. O `catch { }` escondia a falha e a detecção devolvia a chave para desligada. Detalhes e decisão: D-015.

## Feito

- C++ e ATL instalados no VS 2022 (autorizado pelo Alexandre; `vswhere -requires ...VC.Tools.x86.x64 ...VC.ATL` acha a instalação).
- `tools/Build-Native.ps1` compila launcher, OpenDialog e SaveDialog (x64 e Win32). Rodar antes do `mkfile` quando eles mudarem.
- Launcher C++ do upstream no lugar do trampolim C# da AGY, com fallback silencioso (sem `MessageBox`, sem UAC; espera a reversão do registro).
- `AdvancedViewModel`: registra só as DLLs existentes e loga falha do `regsvr32`.
- Teste de fallback reescrito (testa o comportamento, não só a existência do `.exe`).
- Máquina destravada: o registro apontava abrir pasta e `Win + E` para um launcher inexistente em `%LocalAppData%\Files`; o launcher novo foi copiado para lá.
- Pacote novo registrado por `Open-FilesDev.ps1`; DLLs registradas em HKCU (o diálogo está **ligado** nesta máquina).

## Estado por item

Ver a tabela "Estado em 09/10/2026" em `docs/specs/F012-explorer-replacement/tasks.md` e a nota `docs/releases/2026-10-09_0100_build-F012-dialogo-e-gerenciador.md`.

## Pendente

1. Alexandre: responder a notificação do Firewall do Windows aberta às 00:49 (`PickerHost FirewallNotificationDialogServer`); ela impede a automação de trazer o Files para a frente.
2. T3 pela tela: `pwsh tools\perf\f012_advanced_settings.ps1 -FileDialog Off` e `-FileDialog On`, com capturas (o script cancela o clique se o Files não estiver em primeiro plano).
3. T7: escolher um arquivo no diálogo do Files e conferir o retorno; diálogo "Salvar".
4. T6: `Win + E` e duplo clique em pasta (a tecla Windows simulada não teve efeito; precisa de teclado real).
5. `DISCOVERY` de desempenho: abrir pasta por fora leva 4,7 a 7,1 s, quase tudo no segundo processo do Files que só repassa o pedido (redirecionamento de instância única).
6. Correções do G1 continuam na branch `agy/correcoes-g1` (brief `docs/agents/tasks/G1-correcoes.md` na `claude/revisao-g1`).
