# Build 2026-10-24 — KyraFilesSetup v4.3.5

- **Instalador Oficial:** `artifacts\Setup\KyraFilesSetup-4.3.5.exe` (52.711.802 bytes, 50,27 MB; Inno Setup 6).
- **SHA256:** `71A8BC0E96745CA4EA698A8E225780F5334E16540C88683835B60D7F156BD879` (`Get-FileHash`).
- **Branch:** `main` (integrado via squash/rebase da release r435).
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Pacote Oficial Instalado:** `FilesDev_4.3.5.0_x64__ykqwq8d6ps0ag` instalado e registrado em `%LOCALAPPDATA%\Programs\KyraFiles` (CONFIRMED via `Get-AppxPackage KyraFiles`).
- **Manifesto SHA256:** `installer\package_manifest.json` gerado via `Generate-PackageManifest.ps1` (1.333 arquivos, 212,02 MB, versão 4.3.5).
- **Gate de Recursos (F021):** Validado com `verify_no_index_service.ps1` com ausência de `KyraFiles.App.IndexService.exe` e bloqueio confirmado (CONFIRMED).
- **Higiene de Binários:** Expurgadas todas as pastas `Debug` residuais sob `src\`, `tests\` e `tools\`, restando apenas a versão Release instalada.

## O que muda em relação à 4.3.4

| Item | Estado | Descrição |
|---|---|---|
| **Recomposição Completa de Pacote (r435)** | CONFIRMED | Recompilação total de binários nativos C++ (`KyraFiles.HotkeyService.exe`, `KyraFiles.Watchdog.exe`, `KyraFiles.App.Launcher.exe`, DLLs de diálogo Open/Save 32 e 64 bits), sincronização da receita AppX e geração de novo instalador oficial. |
| **Integridade de Otimizações Verificada** | CONFIRMED | Confirmada a preservação de 100% das otimizações ativas no código-fonte (F024 enumeração direta sem resets, F026 seleção coalescida, F027 checkbox preguiçoso, F028 limitação de quadros em navegação rápida, F029 setas como Explorer, F031 abas ultrarrápidas). |
| **Limpeza e Higienização de Testes** | CONFIRMED | Remoção de pastas `Debug` auxiliares sob todo o repositório (`src\`, `tests\`, `tools\`), prevenindo registros AppX acidentais apontando para checkouts de teste. |
| **Instalação e Validação do Update** | CONFIRMED | Atualização limpa da versão 4.3.4 para 4.3.5 em `%LOCALAPPDATA%\Programs\KyraFiles` comprovada com `Get-AppxPackage KyraFiles`. |
| **Versionamento Mandatório** | CONFIRMED | Incremento mandatório de versão aplicado: 4.3.4 → 4.3.5 em `Package.appxmanifest` e `KyraFilesSetup.iss`. |
