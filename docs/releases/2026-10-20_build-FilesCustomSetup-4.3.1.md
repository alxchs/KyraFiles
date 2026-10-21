# Build 2026-10-20 — FilesCustomSetup v4.3.1

- **Instalador:** `artifacts\Setup\FilesCustomSetup-4.3.1.exe` (52.575.341 bytes, 50,1 MB; Inno Setup 6). SHA256 `9EF68F6A20FBDCF95E4BEF909C6C9F2482BE69FECE467567156D233F12C7B2B2` (`Get-FileHash`). Versão anterior 4.3.0: 52,7 MB (valor da nota de 18/10; diferença de ~0,1 MB).
- **Branch:** `feature/f025-diagnostico-baseline-upstream` (inclui as mudanças da F024).
- **Build:** `dotnet build src\Files.App\Files.App.csproj -c Release -m:1 --no-restore` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED). Foi preciso `dotnet restore` de todos os `.csproj` de `src` antes (a pasta `obj` estava vazia).
- **Nativos:** `Files.HotkeyService.exe` e `Files.Watchdog.exe` compilados com MSBuild em `bin\Release` (CONFIRMED, saída "->" dos dois projetos).
- **Pacote:** `Open-FilesDev.ps1 -Configuration Release -NoLaunch` registrou `FilesDev_4.3.1.0_x64__ykqwq8d6ps0ag`; `Generate-PackageManifest.ps1` gerou 1.328 arquivos, 211,12 MB.

## O que muda em relação à 4.3.0

| Item | Estado |
|---|---|
| Versão 4.3.0 para 4.3.1 (`.iss`, manifesto do pacote, `Package.appxmanifest`) | CONFIRMED |
| F024: enumeração sem resets repetidos, watcher sem `sampler.CheckNow()`, atalhos Home/End/PgUp/PgDn tratados na mão, correção do protocolo `files-dev:` | build OK; efeito de desempenho **NOT TESTED** |

## Limitações conhecidas (não ocultar)

- **Instalador não foi executado nem o app aberto nesta build.** Estado de UI: NOT TESTED.
- **Desempenho:** o Alexandre mediu lentidão (troca de aba, tecla segurada, abertura) na F024. Esta build **não** resolve isso; a causa está em investigação (F025, D-023).
- **Regressão conhecida:** Shift+End/Home/PgUp/PgDn não estende a seleção e Ctrl+tecla não só move o foco (handler da F024). INFERRED por leitura do código; falta teste.
- Gate de recursos (`verify_no_index_service.ps1`) não foi rodado nesta build.
