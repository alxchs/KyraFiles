# Build 2026-11-03 — KyraFilesSetup v4.3.11 (não publicado)

- **Instalador:** `artifacts\Setup\KyraFilesSetup-4.3.11.exe` (53.476.356 bytes, 51,00 MB). Inno Setup 6.
- **SHA256:** `1816647CA98466D93CC1F91C2835480D942D2F71A45EB1D46FCDE86067DB0B6D` (`Get-FileHash`).
- **Tamanho comparado à 4.3.10:** +686.676 bytes (53.476.356 contra 52.789.680), variação de +1,30% (dentro do teto de 5 MB / 10%).
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` + `tools\Build-Native.ps1 -Configuration Release` → `0 Warning(s)`, `0 Error(s)`. Pacote: 1.235 arquivos, 179,11 MB (`installer\package_manifest.json`).
- **Branch:** `feature/installer-4.3.11`
- **Conteúdo e Correções:**
  - Empacotamento oficial da versão 4.3.11 com a nova nomenclatura KyraFiles (`KyraFiles.exe`, `KyraFiles.App.Launcher.exe`, `KyraFiles.HotkeyService.exe`, `KyraFiles.Watchdog.exe`).
  - **Correção de Registro AppX (HRESULT 0x80073CFB):** o instalador agora desregistra preventivamente manifestos existentes em *Development Mode* preservando dados do usuário (`-PreserveApplicationData`) antes de executar `Add-AppxPackage -Register`, prevenindo bloqueios do Windows Deployment Server na reinstalação/atualização.
  - Gravação de log detalhado em `{tmp}\appx_register_error.log` para auditoria em caso de falha no AppX.
  - Atualização do `KyraFilesSetup.iss` para encerrar instâncias ativas de `KyraFiles*` e `Files*` na instalação/atualização.
  - Registro de atalhos e alias `kyrafiles.exe` no sistema.
  - Inclusão de `es.exe` (Everything CLI) e `package_manifest.json` com SHA256 de todos os 1.235 arquivos.

## Estado

| Item | Estado | Base |
|---|---|---|
| Compilação nativa Release | CONFIRMED | `tools\Build-Native.ps1 -Configuration Release` exit 0 |
| Compilação C# WinUI Release | CONFIRMED | `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` exit 0 |
| Geração Inno Setup 6 | CONFIRMED | `ISCC.exe installer\KyraFilesSetup.iss` exit 0 (88,98s) |
| Instalador testado / executado | CONFIRMED | instalado e homologado em execucao pelo Alexandre (20:15) |
