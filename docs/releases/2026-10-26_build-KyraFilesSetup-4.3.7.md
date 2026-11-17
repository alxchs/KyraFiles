# Build 2026-10-26 — KyraFilesSetup v4.3.7

- **Instalador oficial:** `artifacts\Setup\KyraFilesSetup-4.3.7.exe` (52.694.203 bytes, 50,25 MB; Inno Setup 6). A 4.3.6 tinha 50,24 MB (+17 KB).
- **SHA256:** `9A841441E1B352C338DB0151858C9CA1C6965E0B7053C1C7F496196976CF1555` (`Get-FileHash`).
- **Conteúdo:** só a correção da F035 (`src/KyraFiles.App/Program.cs`), mais versão, manifesto e documentação.
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` = 0 Warning(s), 0 Error(s) (CONFIRMED).
- **Pacote instalado:** `KyraFiles` 4.3.7.0 em `%LOCALAPPDATA%\Programs\KyraFiles` (CONFIRMED via `Get-AppxPackage KyraFiles`).
- **Manifesto:** `installer\package_manifest.json` (1.333 arquivos, 212,03 MB, versão 4.3.7).
- **Gate F021:** `verify_no_index_service.ps1` exit 0; IndexService inativo, CPU em repouso 1,66%, RAM 301,66 MB (CONFIRMED).

## Texto para quem usa o app (publicado na release do GitHub)

**Corrigido:** abrir o KyraFiles várias vezes seguidas, por exemplo clicando em várias pastas rapidamente, não fecha mais o programa de repente nem faz a pasta pedida se perder.

## Evidência técnica (não vai para o usuário)

| Item | Estado | Detalhe |
|---|---|---|
| Defeito reproduzido sem a correção | CONFIRMED | 4.3.6 instalado, 20 rodadas de 5 aberturas: 3 rodadas com `SemaphoreFullException` (log do Windows 1026 às 00:42:43, 00:43:01, 00:43:11) e só 3 das 5 abas abertas |
| Correção | CONFIRMED | 20 de 20 rodadas com as 5 abas abertas e 0 eventos 1026 (`docs/agents/evidence/F035/resultado-20261026-0054.csv`) |
| AC-2, AC-4 | NOT TESTED | AC-2 exige sessão limpa; AC-4 (abrir o `KyraFiles.exe` direto) é a T5 |
| AC-5 | INFERRED | Janela estacionada não volta ao primeiro plano; reprova também sem a correção; **não corrigido** |
| Crash do `GridLayoutPage` (00:59:04) | DISCOVERY | Causa não determinada; **não corrigido** |
| Captura de tela de fumaça | NOT TESTED | O Gate F021 abriu e mediu o app instalado, sem captura |
