# Build 2026-10-22 — KyraFilesSetup v4.3.2

- **Instalador Oficial:** `artifacts\Setup\KyraFilesSetup-4.3.2.exe` (52.582.542 bytes, 50,1 MB; Inno Setup 6). 
- **SHA256:** `CAD565928842767FCEBCC30BC13F2FFD309A0525ADD3DA6D6BF900A6C2C51379` (`Get-FileHash`).
- **Branch:** `main` (commit `c8ac72984`).
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Pacote Dev Registrado:** `KyraFilesDev_4.3.2.0_x64__sxng2gqmgahde` atualizado no Windows via `Open-KyraFiles.ps1 -Configuration Release -NoLaunch`.
- **Manifesto SHA256:** `installer\package_manifest.json` gerado via `Generate-PackageManifest.ps1` (1.328 arquivos, 211,15 MB, versão 4.3.2).
- **Gate de Recursos (F021):** Validado com `verify_no_index_service.ps1` com ausência rigorosa do `KyraFiles.App.IndexService.exe` e CPU em repouso < 5% (CONFIRMED).

## O que muda em relação à 4.3.1

| Item | Estado | Descrição |
|---|---|---|
| **Versionamento Obrigatório** | CONFIRMED | Política mandatória ativada: toda atualização integrada na `main` incrementa a versão oficial. |
| **F030: Preview na Tecla Segurada** | CONFIRMED | Ao segurar setas ou `PgUp`/`PgDn` com o painel de preview aberto, ele desativa o carregamento pesado concorrente e mostra apenas o indicador, carregando somente o arquivo final ao soltar a tecla. |
| **F032: Fixar Aba (Pin Tab)** | CONFIRMED | Menu de contexto na aba → "Fixar aba". A aba recebe o alfinete, posiciona-se à esquerda, protege contra fechamento acidental (`Ctrl+W`, `X`, "Fechar outras abas") e persiste entre sessões. |
| **F033: Copiar para / Mover para** | CONFIRMED | Menus de contexto para arquivos selecionados com atalhos para as últimas 12 pastas utilizadas e diálogo de confirmação seguro com padrão em "Não". |
| **F028: Contorno de Foco e Rolagem PgUp/PgDn** | CONFIRMED | Correção de virtualização e sincronismo de viewport garantindo que o contorno de foco do teclado nunca desapareça. |
