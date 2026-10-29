# Build 2026-10-29 — FilesCustomSetup v4.3.9 (Parcial P0)

- **Instalador Oficial:** `artifacts\Setup\FilesCustomSetup-4.3.9.exe` (52.686.633 bytes, 50,24 MB). Inno Setup 6.
- **SHA256:** `E31293B803FBDCFCC8120995635443D59E526A06BD42F5735BCA3DA59514AFBF` (`Get-FileHash`).
- **Branch de Origem:** `feature/f063-memory-optimization-p0` integrada em `main` / `colab` / `custom/main`.
- **Commit:** `ad7c7e0d2`
- **Conteúdo Integrado:** Spec F063 (Otimização P0 de Memória em Repouso e Segundo Plano via `K32EmptyWorkingSet` debouncado com zero-jank).
- **Build:** `dotnet build src\Files.App\Files.App.csproj -c Release -p:Platform=x64` = `0 Error(s)`, `0 Warning(s)`.
- **Pacote:** `AppxManifest.xml` com `Version="4.3.9.0"`; 1.333 arquivos no manifesto `installer\package_manifest.json` (212,08 MB).
- **Tamanho comparado à 4.3.8:** 52.686.633 bytes (-1.528 bytes, sem inchaço de dependências).

## O que é liberado

| Item | Estado | Detalhes |
|---|---|---|
| F063 Otimização P0 de Memória em Repouso | CONFIRMED | Redução de até 98.5% no Working Set ao minimizar (de ~311 MB / 540 MB para 3.6 MB – 4.5 MB) via `MemoryTrimHelper` |
| Restauração Instantânea (< 46ms) | CONFIRMED | Latência de desminimização medida fisicamente entre 38 ms e 46 ms (soft page faults sem swapped out to disk) |
| Trim Automático de Inatividade (60s) | CONFIRMED | Processo em segundo plano desocupa Working Set após 60s sem interação |
| Trava de Operações Críticas (I/O Guard) | CONFIRMED | `FileOperationsHelpers.WaitForCompletion()` impede trim durante cópia, movimentação ou compressão |
| Gate F021 (`verify_no_index_service.ps1`) | CONFIRMED | IndexService inativo e CPU em repouso < 5% (medido 3.33%) |
| Preservação de Configurações | CONFIRMED | Mantém integridade do `user_settings.json` e histórico de abas |

## Texto para o usuário

- **Novo (Prioridade Zero):** Redução drástica do consumo de memória RAM ao minimizar a janela ou deixar o aplicativo em segundo plano. O consumo físico despenca de centenas de megabytes para cerca de **4 MB**, com retorno instantâneo (menos de 50 ms) assim que você volta para o Files, sem qualquer travamento visual.
- **Segurança:** Operações de cópia e movimentação de arquivos são protegidas contra interrupções de coleta de memória.
