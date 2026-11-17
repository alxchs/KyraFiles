# Build 2026-10-22 — KyraFilesSetup v4.3.4

- **Instalador Oficial:** `artifacts\Setup\KyraFilesSetup-4.3.4.exe` (52.690.949 bytes, 50,25 MB; Inno Setup 6).
- **SHA256:** `7ED43A932EE51DFB3AA3965D0921715102A8422258712220136079F7365B803B` (`Get-FileHash`).
- **Branch:** `main` (integrado via squash/rebase da release r434).
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Pacote Dev Registrado:** `FilesDev_4.3.4.0_x64__ykqwq8d6ps0ag` atualizado no Windows via `Open-KyraFiles.ps1 -Configuration Release -NoLaunch`.
- **Manifesto SHA256:** `installer\package_manifest.json` gerado via `Generate-PackageManifest.ps1` (1.333 arquivos, 212,02 MB, versão 4.3.4).
- **Gate de Recursos (F021):** Validado com `verify_no_index_service.ps1` com ausência de `KyraFiles.App.IndexService.exe` e bloqueio confirmado (CONFIRMED).
- **Higiene de Binários e Ambiente:** Todas as pastas `Debug` sob `src\`, branches locais mescladas, instaladores legados e laboratório de teste foram limpos.

## O que muda em relação à 4.3.3

| Item | Estado | Descrição |
|---|---|---|
| **Limpeza Geral de Desenvolvimento (r434)** | CONFIRMED | Higienização completa pós-F031: exclusão de pastas `Debug` auxiliares, remoção do laboratório `C:\FilesUXLab\perf\1600`, limpeza de `debug.log` e expurgo de branches locais já integradas. |
| **Consolidação de Tarefas Concluídas** | CONFIRMED | Atualização de status em `tasks.md` de F024 (T8), F027 (T1–T4) e F026 (T3) com links de evidência e relatórios. |
| **Preservação de Extensibilidade F029** | CONFIRMED | Mantidas as estruturas e métodos de suporte A/B da F029 (`F029Flag`, `ReadF029Flags`, `TryMoveRow`) em `DetailsLayoutPage.xaml.cs`. |
| **Instalação e Validação do Update** | CONFIRMED | Atualização limpa da versão 4.3.3 para 4.3.4 em `%LOCALAPPDATA%\Programs\KyraFiles` comprovada com `Get-AppxPackage KyraFiles`. |
| **Versionamento Mandatório** | CONFIRMED | Incremento mandatório de versão aplicado: 4.3.3 → 4.3.4 em `Package.appxmanifest` e `KyraFilesSetup.iss`. |
