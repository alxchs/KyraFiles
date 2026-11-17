# Build 2026-10-24 — KyraFilesSetup v4.3.6

- **Instalador Oficial:** `artifacts\Setup\KyraFilesSetup-4.3.6.exe` (52.676.689 bytes, 50,24 MB; Inno Setup 6).
- **SHA256:** `AE8E9F32DC99CF216FD65B2603B30F6632973D7162FAA634FB650BB0BAF29B11` (`Get-FileHash`).
- **Branch:** `main` (integrado via squash/rebase das branches de feature T1, T2, T3 e T4).
- **Build:** `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` = `0 Warning(s)`, `0 Error(s)` (CONFIRMED).
- **Pacote Oficial Instalado:** `FilesDev_4.3.6.0_x64__ykqwq8d6ps0ag` instalado e registrado em `%LOCALAPPDATA%\Programs\KyraFiles` (CONFIRMED via `Get-AppxPackage KyraFiles`).
- **Manifesto SHA256:** `installer\package_manifest.json` gerado via `Generate-PackageManifest.ps1` (1.333 arquivos, 212,02 MB, versão 4.3.6).
- **Gate de Recursos (F021):** Validado com `verify_no_index_service.ps1` com ausência de `KyraFiles.App.IndexService.exe` e bloqueio confirmado (CONFIRMED).
- **Higiene de Binários:** Zero pastas `Debug` no repositório, restando apenas a versão Release instalada.

---

## O que muda em relação à 4.3.5

| Item | Estado | Descrição |
|---|---|---|
| **Tópico 1: Evidências F030, F032 e F033** | CONFIRMED | Evidências visuais registradas com sucesso: F030 (Preview aberto sob tecla segurada e fechado ao soltar), F032 (Fixar e desafixar aba com persistência), F033 (Copiar/Mover para no menu de contexto). Tarefas marcadas concluídas nas specs. |
| **Tópico 2: F034 — Lista sem Checkbox por Padrão** | CONFIRMED | Remoção física da caixa de seleção dos itens da lista e da grade via `x:Load="False"`. Opção desligada por padrão nas preferências de pastas. Economia de renderização por linha comprovada. |
| **Tópico 3: F029 — Setas Estilo Windows Explorer** | CONFIRMED | Fixado no código o comportamento vencedor da A/B test (navegação fluida de linha única `TryMoveRow` ativa por padrão para Up/Down, throttle de 1 quadro). Expurgo definitivo de bandeiras temporárias (`ReadF029Flags`, `f029_flags.txt`). |
| **Tópico 4: F023 — Busca Híbrida e Estresse de Recursos** | CONFIRMED | Bateria de 15 buscas consecutivas executada com delta nulo de kernel handles (3940 → 3933). Correção definitiva de Access Violation (`0xc0000005`) no Escape do Omnibar (`e.Handled = true`) e descarte explícito de `_suggestSearchCTS.Dispose()`. |
| **Tópico 4: Lições Arquiteturais do Everything** | CONFIRMED | Documento `docs/specs/F023-hybrid-fast-search/licoes_everything.md` elaborado detalhando MFT/USN Journal, arrays contíguos em RAM, busca SIMD e IPC sem locks para o futuro app independente. |
| **Versionamento Mandatório** | CONFIRMED | Incremento oficial aplicado: 4.3.5 → 4.3.6 em `Package.appxmanifest` e `KyraFilesSetup.iss`. |
