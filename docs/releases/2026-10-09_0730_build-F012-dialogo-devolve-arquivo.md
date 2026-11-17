# Build 2026-10-09 07:30 — Diálogo de arquivos devolve o arquivo escolhido (F012)

- **Executável:** `src\KyraFiles.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX\KyraFiles.exe`, registrado por `.\Open-KyraFiles.ps1 -NoLaunch` (pacote `FilesDev_4.2.37.0_x64__ykqwq8d6ps0ag`).
- **Código:** branch `feature/f012-explorer-replacement`, commit da correção D-016 (após `cd6040efa`).
- **Build:** `mkfile r Files.slnx` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED). Binários nativos sem mudança desde a nota de 01:00.

## O que foi liberado

| Item | O que muda para o usuário | Estado |
|---|---|---|
| Escolher arquivo pelo KyraFiles no diálogo "Abrir" | Selecionar o arquivo e fechar a janela do KyraFiles entrega o arquivo ao programa. Antes, o programa recebia "Cancelar" em parte das vezes. | CONFIRMED: `pwsh tools\perf\f012_file_dialog_return.ps1 -Mode Open`, 3 de 3 PASS (caminho com espaço e acento); captura `docs/agents/evidence/f012/07_dialogo_abrir_selecao.png` |
| Diálogo "Salvar" | Mesmo fluxo, escolhendo um arquivo existente. | CONFIRMED (1 de 1, captura 08). NOT TESTED: salvar com nome novo; app 32 bits |
| O diálogo não mexe mais nas suas abas | A janela de diálogo abre só com a pasta pedida. Antes, ela abria com todas as suas abas e, ao fechar, gravava a sessão com uma aba a mais a cada uso. | CONFIRMED: 5 usos seguidos e a sessão continuou `Configurações`, `D:\` |
| Diálogo mais rápido | Abre em ~2,6 s (antes 4 a 6 s) e devolve o arquivo ~0,4 s depois de fechar (antes 1,7 s). | OBSERVED (3 medições de cada, mesmo script) |
| Pasta aberta por fora não cai no diálogo | Enquanto o diálogo está aberto, abrir uma pasta pelo Windows vai para a janela principal do KyraFiles, não para a de diálogo. | INFERRED pelo código (`Window_Activated` não marca a instância de diálogo como ativa); NOT TESTED |
| Ligar e desligar o gerenciador padrão pela tela | Desligar volta pastas e `Win + E` para o Explorer e desliga junto o diálogo; religar volta tudo; sem pedir administrador. | CONFIRMED: `tools\perf\f012_advanced_settings.ps1 -DefaultManager Off` e `-DefaultManager On -FileDialog On`, conferindo o registro; capturas 09 e 10 |
| Opção do diálogo desligada | Programas voltam a usar o diálogo do Windows. | CONFIRMED: janela `#32770` do Windows, nenhum Files aberto |
| Abrir pasta pelo Windows (`start`, `Win + R`, duplo clique) | Abre na janela do KyraFiles já aberta. | CONFIRMED por `Start-Process <pasta>` (4,0 s) |
| `explorer.exe <pasta>` direto | **Continua abrindo o Explorer do Windows.** | CONFIRMED. Decisão pendente com o Alexandre: D-017 |
| `Win + E` | Deve abrir o KyraFiles. | NOT TESTED (precisa de teclado real) |

## Como testar

1. Feche o KyraFiles e rode `.\Open-KyraFiles.ps1`.
2. Em *Configurações > Avançado*, as duas opções ligadas (já estão nesta máquina).
3. Em qualquer programa, `Ctrl+O`: o KyraFiles abre só com a pasta. Selecione um arquivo e feche a janela do KyraFiles: o arquivo deve abrir no programa.
4. Abra o KyraFiles normalmente depois: suas abas devem ser as de antes.
5. `Win + E`: deve abrir o KyraFiles.

## Ainda não está neste exe

- Interceptar `explorer.exe <pasta>` (D-017, pendente).
- Botão "Abrir/Salvar" e caixa de nome de arquivo no modo diálogo (o upstream devolve a seleção ao fechar a janela).
- Correções da revisão do G1 (branch `agy/correcoes-g1`).
