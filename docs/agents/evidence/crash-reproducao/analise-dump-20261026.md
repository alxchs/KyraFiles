# Análise do dump do crash de 26/10/2026 08:11:45 (Files 4.3.7)

Autor: Claude. Ferramentas: `cdb` (Windows Kits x64) com símbolos públicos da Microsoft e `dotnet-dump`, **somente leitura**, sem abrir o app. Dump: `%LOCALAPPDATA%\CrashDumps\KyraFiles.exe.15768.dmp` (47,7 MB; **não apagar**). Saídas: `dump-20261026-0811-pilha-da-thread-que-caiu.txt` e `dump-20261026-0811-dotnet-dump-threads.txt` nesta pasta.

## O que o dump PROVA (CONFIRMED)
- **Não é exceção do código do KyraFiles.** É uma **falha rápida deliberada** (`FAST_FAIL_FATAL_APP_EXIT`, código `0xc0000409`) disparada por uma verificação interna do **WindowsAppSDK**: `Microsoft.UI.Input.dll`, componente do pacote `Microsoft.WindowsAppRuntime.2_2.5.1.0` (versão do arquivo 10.0.27200.1054).
- **HRESULT `0x8001010E` = `RPC_E_WRONG_THREAD`** ("a aplicação chamou uma interface empacotada para outra thread"). Está nos parâmetros da exceção.
- **Onde:** `CloseHelper<Microsoft::UI::Content::IContentSite>` chamado por `ContentExternalOutputLink::Dispose` ← destrutor ← liberação de objeto WinRT.
- **Quem chamou:** a **thread do coletor de lixo (Finalizer, thread 3)**. A pilha gerenciada é só `WinRT.IObjectReference.Finalize()` ← `System.GC.RunFinalizers()`. **Nenhum quadro de `KyraFiles.App`** aparece na pilha da thread que caiu.
- **Leitura:** um objeto WinRT ligado à interface (um "link de saída de conteúdo", usado quando um conteúdo XAML é hospedado em outra superfície) perdeu a última referência gerenciada e teve o **destrutor executado pelo finalizador numa thread que não é a da interface**. O componente do Windows reage derrubando o processo.

## O que o dump NÃO prova (INFERRED / não determinado)
- **Qual objeto de qual tela** foi finalizado: o dump não traz o tipo gerenciado.
- **A ligação com a prévia de vídeo** relatada pelo Alexandre. É compatível, **não confirmada**.
- Se é defeito do WindowsAppSDK 2.5.1, do CsWin32/CsWinRT, ou de uso do KyraFiles (objeto de interface criado e descartado sem liberação explícita na thread da interface).

## Pista no código (INFERRED, ainda sem teste)
`src/KyraFiles.App/UserControls/FilePreviews/MediaPreview.xaml.cs` traz o comentário *"The MediaPlayerElement isn't properly disposed by Windows so we set the source to null"* e, ao descarregar, só faz `PlayerContext.Source = null` e remove os eventos: **não libera explicitamente o `MediaPlayer`**. Trocar de item várias vezes em sequência cria e descarta vários `MediaPlayerElement`, deixando a liberação para o coletor de lixo. `HtmlPreview` (WebView2) e `ShellPreview` também criam hospedagens de conteúdo por item.

## O que falta para fechar
1. **Reproduzir** com a prévia aberta e ↑↓ rápidos passando por vídeos (brief `R-crashes-reproducao.md`), com a sessão **desbloqueada**; **controle** sem a prévia.
2. Se reproduzir: capturar um dump novo e, com ele, identificar o tipo gerenciado (`dumpheap`/`gcroot` no `dotnet-dump`).
3. **Candidatas de correção (não aplicar sem reproduzir):** liberar o `MediaPlayer` explicitamente na thread da interface em `MediaPreview_Unloaded`; evitar criar/descartar o elemento de mídia a cada item (reutilizar); atrasar a criação do player até a seleção estabilizar (a F030 já faz isso para tecla segurada). Verificar também se existe versão mais nova do WindowsAppRuntime.
4. Registrar o defeito como candidato a relato ao projeto WindowsAppSDK somente depois de reproduzir.

## 9. Correção do cenário pelo Alexandre (26/10/2026, depois da análise)

- **FACT (relato do Alexandre):** o vídeo **não chegou a tocar** na prévia. Ele navegou com a **seta para baixo pressionada continuamente** (tecla segurada, repetição automática), **primeiro para cima e depois para baixo**. O brief dizia "poucas teclas"; o cenário real é **tecla segurada com inversão de direção**.
- **Consequência para a hipótese (INFERRED, lido em `InfoPaneViewModel.cs` linhas 155 a 217 e `BaseLayoutPage.cs` 1401 a 1410):** a F030 fecha a prévia (`UnloadPreview`) quando `IsNavigationKeyHeld` vira verdadeiro e **carrega o item selecionado na soltura** (`ApplyCurrentSelection`). Ao segurar a tecla, soltar e segurar na direção oposta, o ciclo é: carrega a prévia do item onde parou (se for vídeo, cria o `MediaPlayerElement`, sem tocar) e, poucos décimos depois, a próxima tecla a descarrega. Há também uma janela antes da primeira repetição automática em que a flag ainda é falsa e o ramo de 100 ms (`Task.Delay`) pode carregar a prévia. Ou seja, **muitos cria-e-destrói de `MediaPlayerElement` sem tocar nada**, o que combina com o que o dump mostra (liberação de objeto de interface pelo finalizador). **Continua sem prova.**
- **Ajuste do teste de reprodução:** o helper deve **segurar** a tecla (repetição automática real, não toques isolados) por 1 a 3 s, soltar, inverter a direção e repetir, com a pasta cheia de vídeos curtos. Isso também mede a F030 de verdade (a medição de teclas seguradas que está pendente).
