# Build 2026-11-07 — KyraFilesSetup v4.3.12

- **Instalador:** `artifacts\Setup\KyraFilesSetup-4.3.12.exe` (53.641.735 bytes, 51,16 MB). Inno Setup 6.
- **SHA256:** `2009682CA23120114155961D8900883F034BB64DDFC93C9B40582638AF1CB4F8` (`Get-FileHash`).
- **Tamanho comparado à 4.3.11:** +165.379 bytes (53.641.735 contra 53.476.356), variação de +0,31% (dentro do teto de 5 MB / 10%).
- **Build:** `tools\Build-Native.ps1 -Configuration Release` + `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` → `0 Warning(s)`, `0 Error(s)`. Pacote: 1.240 arquivos, 179,45 MB (`installer\package_manifest.json`).
- **Conteúdo e Correções:**
  - Versão oficial atualizada para 4.3.12.0 (`Package.appxmanifest`, `KyraFilesSetup.iss`, `package_manifest.json`).
  - **Purga de vestígios legados:** remoção completa de executáveis e bibliotecas antigas (`Files.App.Server\`, `Files.App.BackgroundTasks.*`, `Files.App.OpenDialog*.dll`, `Files.App.SaveDialog*.dll`, `KyraFilesOpenDialog`, `*.locked.*`) e de todos os diretórios/artefatos de Debug no repositório.
  - **Inno Setup [InstallDelete]:** incorporada diretiva no instalador para purgar preventivamente quaisquer vestígios de binários legados no diretório de destino `{app}` durante a instalação/atualização.
  - **Manifesto de Integridade:** corrigido o cálculo do SHA256 em `installer\Generate-PackageManifest.ps1`, registrando os hashes SHA256 reais de todos os 1.240 arquivos em `package_manifest.json`.

## Estado

| Item | Estado | Base |
|---|---|---|
| Compilação nativa Release | CONFIRMED | `tools\Build-Native.ps1 -Configuration Release` exit 0 |
| Compilação C# WinUI Release | CONFIRMED | `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` exit 0 (0 Warning, 0 Error) |
| Geração Inno Setup 6 | CONFIRMED | `ISCC.exe installer\KyraFilesSetup.iss` exit 0 (90,39s) |
| Purga de vestígios antigos/Debug | CONFIRMED | Binários de Debug e executáveis legados purgados |
