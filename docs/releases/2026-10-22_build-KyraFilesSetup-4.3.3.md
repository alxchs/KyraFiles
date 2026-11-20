# Build 2026-10-22 — KyraFilesSetup v4.3.3

- **Instalador Oficial:** `artifacts\Setup\KyraFilesSetup-4.3.3.exe` (52.558.334 bytes, 50,1 MB; Inno Setup 6).
- **SHA256:** `A45F9D595C4E57CE0A4F0B9B62FFD20E852E8BCBE8D1436C9D532512307303F3` (`Get-FileHash`).
- **Branch:** `main` (integrado via squash/rebase da feature F031).
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Pacote Dev Registrado:** `KyraFilesDev_4.3.3.0_x64__sxng2gqmgahde` atualizado no Windows via `Open-KyraFiles.ps1 -Configuration Release -NoLaunch`.
- **Manifesto SHA256:** `installer\package_manifest.json` gerado via `Generate-PackageManifest.ps1` (1.328 arquivos, 211,15 MB, versão 4.3.3).
- **Gate de Recursos (F021):** Validado com `verify_no_index_service.ps1` com ausência de `KyraFiles.App.IndexService.exe` e CPU em repouso em 3,34% (CONFIRMED).
- **Higiene de Binários:** Cópias auxiliares de Debug removidas em conformidade estrita com as regras do Alexandre.

## O que muda em relação à 4.3.2

| Item | Estado | Descrição |
|---|---|---|
| **F031: Troca de Abas Ultrarrápida (SwitchTab)** | CONFIRMED | Mediana caiu de 1.116,7 ms para 456,1 ms (**redução de 59,1%**, superando a meta de 30%). Removido atraso artificial síncrono e adicionado fast-path com cabeçalho em cache sem consultas I/O ao disco. |
| **F031: Criação de Nova Aba Acelerada (NewTab)** | CONFIRMED | Mediana caiu de 526,4 ms para 287,9 ms (**redução de 45,3%**, superando a meta de 30%). |
| **F031: Preservação de Estado (AC-3 e NFR-1)** | CONFIRMED | Rolagem e seleção mantidas intactas ao alternar abas (`tab10_proof.png`). Memória com 10 abas abertas manteve estabilidade com acréscimo de apenas 0,08% (muito abaixo do teto de 10%). |
| **Versionamento Obrigatório** | CONFIRMED | Incremento mandatório de versão aplicado: 4.3.2 → 4.3.3. |
