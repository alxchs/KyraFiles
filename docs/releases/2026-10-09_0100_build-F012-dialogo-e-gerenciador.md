# Build 2026-10-09 01:00 — Diálogo de arquivos e gerenciador padrão (F012)

- **Executável:** `src\Files.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX\Files.exe`, registrado por `.\Open-FilesDev.ps1` (pacote `FilesDev_4.2.37.0_x64__ykqwq8d6ps0ag`).
- **Código:** branch `feature/f012-explorer-replacement`, commit `0ac566f1c` mais os ajustes de teste e documentação deste mesmo dia.
- **Build:** `pwsh tools\Build-Native.ps1` (launcher e DLLs de diálogo; VS 2022, toolset v143, SDK 10.0.26100.0) e depois `mkfile release src\Files.App\Files.App.csproj` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED). O build nativo tem warnings que já vinham do código C++ do upstream (C4584 nos diálogos; C4311/C4302 num cast do launcher), sem warning novo.
- **Pré-requisito novo:** Visual Studio 2022 com "Desenvolvimento para desktop com C++" e ATL (instalado em 09/10/2026 com autorização do Alexandre; D-015).

## O que foi liberado

| Item | O que muda para o usuário | Estado |
|---|---|---|
| Diálogo de arquivos | *Configurações > Avançado > Usar o Files como diálogo para Abrir Arquivos* passa a ter as DLLs que registra (antes não existiam e a opção voltava sozinha para desligada). | CONFIRMED: as 4 DLLs estão no layout AppX; `regsvr32 /s /n /i:user` nelas = exit 0; as chaves `CLSID\{DC1C5A9C-...}` = `FilesOpenDialog class` e `CLSID\{C0B4E2F3-...}` = `FilesSaveDialog class` |
| Diálogo "Abrir" de outro programa abre o Files | Um `OpenFileDialog` comum (WinForms, outro processo) abriu uma janela do Files já na pasta inicial pedida (`C:\FilesUXLab`). | OBSERVED (`docs/agents/evidence/f012/02_dialogo_abrir_files.png`). NOT TESTED: escolher o arquivo e o caminho voltar ao programa; diálogo "Salvar"; app 32 bits |
| Ligar e desligar a opção pela tela | Funciona mesmo quando outro programa ainda tem o diálogo carregado (antes, desligar falhava com "arquivo em uso" e a chave voltava sozinha). | CONFIRMED (build de 01:25, mesma branch): `tools\perf\f012_advanced_settings.ps1 -FileDialog Off` removeu as chaves CLSID e `-FileDialog On` as recriou; capturas `04_dialogo_desligado.png` e `05_dialogo_religado.png` |
| Gerenciador padrão: launcher do upstream | Abrir pasta pelo Windows passa pelo launcher C++ do upstream (com `/select`, janelas do Explorer já abertas, pastas especiais) no lugar do trampolim C#. | CONFIRMED: `Files.App.Launcher.exe "C:\FilesUXLab\f012"` levou a janela do Files aberta para a pasta, sem abrir o Explorer |
| Fallback silencioso (AC-6) | Com o Files desinstalado, a pasta abre no Explorer, sem caixa de mensagem e sem pedir administrador. | CONFIRMED: `pwsh tests\test-launcher-fallback.ps1` = todos PASS (launcher terminou sozinho; Explorer abriu `F012 pasta com espaço`) |
| Máquina do Alexandre destravada | O registro apontava abrir pasta e `Win + E` para `%LocalAppData%\Files\Files.App.Launcher.exe`, que não existia. O launcher novo foi copiado para lá. | CONFIRMED (arquivo presente, 328.192 bytes). NOT TESTED: `Win + E` e duplo clique em pasta da Área de Trabalho |

## Medido

- Do pedido de abrir pasta até a janela do Files mudar: **4,7 s a 7,1 s** (OBSERVED, 2 medições). Linha do tempo: o launcher despacha em ~1,1 s (inclui espera de 500 ms do upstream); o protocolo `files-dev:` sobe um segundo processo do Files só para repassar o pedido à janela aberta, e isso leva ~3,5 s. Fica como `DISCOVERY` de desempenho (redirecionamento de instância única), não corrigido aqui.
- Fallback com o Files desinstalado: launcher termina em ~3,1 s (inclui a reversão do registro, uma única vez).

## Como testar

1. Feche o Files Dev e rode `.\Open-FilesDev.ps1`.
2. *Configurações > Avançado*: "Definir o Files como o gerenciador de arquivos padrão" ligado; depois "Usar o Files como diálogo para Abrir Arquivos".
3. Em qualquer programa (Bloco de Notas clássico, navegador), `Ctrl+O`: deve abrir o Files; escolha um arquivo e confira que ele abre no programa.
4. `Win + E` e duplo clique numa pasta da Área de Trabalho: devem abrir no Files.

## Ainda não está neste exe

- Correções da revisão do G1 (C1..C9, `docs/agents/tasks/G1-correcoes.md`, branch `agy/correcoes-g1`): valores crus da F010, defeitos do Agent Ransack, Everything sem `es.exe`, densidade.
- ARM64 dos diálogos (não compilado nesta máquina).

## Atenção

A nota anterior (`2026-10-07_2250_build-F010-...md`) marca como CONFIRMED itens que a revisão de 08/10 desmentiu (369 propriedades, Everything por IPC). Ver `docs/agents/reviews/2026-10-08_Claude_revisao-G1-F004-F011.md` na branch `claude/revisao-g1`.
