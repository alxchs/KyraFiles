# Build 2026-10-21 — Correção de PageUp/PageDown e Contorno de Foco (F028)

- **Pacote Dev Registrado:** `FilesDev_4.3.1.0_x64__ykqwq8d6ps0ag`
- **Localização do Layout AppX:** `src\Files.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX`
- **Como Abrir para Teste:**
  - Pelo Menu Iniciar: **"Files Dev"**
  - Ou via PowerShell / Terminal: `files-dev.exe` ou `.\Open-FilesDev.ps1 -Configuration Release`
- **Branch:** `feature/f028-custo-por-passo-da-selecao`
- **Commit:** `e59007015`
- **Build:** `dotnet build src/Files.App/Files.App.csproj -c Release -p:Platform=x64 -v:quiet -clp:ErrorsOnly` -> `0 Error(s)` (CONFIRMED)
- **Gate de Recursos (F021):** Validado com sucesso via `tools/perf/verify_no_index_service.ps1`. `Files.App.IndexService` rigorosamente inativo e CPU em repouso em 3.32% (< 5%) (CONFIRMED).

## O que foi corrigido e liberado

| Item | Estado | Descrição |
|---|---|---|
| **Contorno de foco visual em `PageUp`/`PageDown`** | CONFIRMED | Implementado `EnsureItemFocusVisual(targetItem)` com escuta ao `LayoutUpdated`, garantindo que quando os contêineres virtualizados são gerados sob demanda, o contorno de foco do teclado (`KeyboardFocus`) é aplicado imediatamente à linha selecionada sem sumir. |
| **Sincronização da barra de rolagem em `PageUp`/`PageDown`** | CONFIRMED | Cálculo exato de página com base na altura real da janela (`ViewportHeight` / `RowHeight` - 1) e deslocamento instantâneo via `ContentScroller.ChangeView(null, newOffset, null, true)` (sem animação assíncrona conflitante). |
| **Suporte a `Shift + PageUp` / `Shift + PageDown`** | CONFIRMED | Extensão de intervalo contíguo de seleção preservando o item âncora. |
| **Foco imediato e sem conflito em `Home` e `End`** | CONFIRMED | Alinhamento direto no início (offset 0) e fim (`ScrollableHeight`) com garantia de foco via `EnsureItemFocusVisual`. |

## Como Testar

1. Abra o **Files Dev** (via atalho no Iniciar ou executando `files-dev.exe`).
2. Abra uma pasta com centenas ou milhares de arquivos no modo Detalhes (ex: `C:\FilesUXLab\perf\10k` ou qualquer pasta grande).
3. Selecione um item e navegue usando as teclas **PageDown** e **PageUp** (toque único e segurando a tecla):
   - O contorno de foco visual (retângulo azul/tema) deve permanecer nítido na linha selecionada.
   - A barra de rolagem deve acompanhar exatamente o salto de página sem saltos erráticos nem perda de posição.
4. Teste também as teclas **Home** e **End** para ir diretamente ao topo e ao final da pasta.
