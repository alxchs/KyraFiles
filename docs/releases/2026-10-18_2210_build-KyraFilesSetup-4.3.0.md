# Build 2026-10-18 22:10 — KyraFilesSetup v4.3.0

- **Instalador:** `artifacts\Setup\KyraFilesSetup-4.3.0.exe` (52,7 MB, Inno Setup 6, LZMA2 Ultra64).
- **Executável instalado:** `%LocalAppData%\Programs\KyraFiles\KyraFiles.exe` (pacote `FilesDev_4.3.0.0_x64__ykqwq8d6ps0ag`).
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -m:1` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Layout AppX & Manifest:** `Open-KyraFiles.ps1 -Configuration Release -NoLaunch` + `Generate-PackageManifest.ps1` (CONFIRMED).
- **Gate de Recursos:** `tools\perf\verify_no_index_service.ps1` = `IndexService inativo` (CONFIRMED).

## O que está liberado nesta versão (v4.3.0)

| Item | Descrição | Estado |
|---|---|---|
| **Busca Híbrida & Ultrarrápida** | Integração autônoma do Everything com fallback inteligente para motor nativo assíncrono via `FileSystemEnumerable` e exclusão de diretórios ruidosos (`node_modules`, `.git`, etc.). Inclui `es.exe` direto no pacote. | CONFIRMED |
| **Arrasto Universal Win32 (CF_HDROP)** | Suporte completo de drag and drop universal para IDEs, compiladores Delphi VCL e aplicações clássicas adicionando `StorageItems` ao `DataPackage`. | CONFIRMED |
| **Ajustes de UI e Padrões** | Padrão de densidade compacta, formato de data/hora do sistema, sem agrupamento forçado por data na pasta Downloads. | CONFIRMED |
| **Estabilidade & Startup (F022)** | DI registrado para `IHelpService` e `HelpViewModel`, atalhos do instalador direcionados para o alias AppX registrado. | CONFIRMED |
| **Contenção de Recursos (F021)** | Desativação estrita do `IndexService` e proteção contra consumo excessivo de CPU em background. | CONFIRMED |
