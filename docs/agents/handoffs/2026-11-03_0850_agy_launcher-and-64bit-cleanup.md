# Handoff: Consistência do Launcher KyraFiles, Padronização 64-bit e Purga de Nomes Legados

**Data/Hora:** 2026-11-03 08:50 (UTC-3)  
**Agente:** AGY (executora)  
**Branch:** `feature/launcher-rename-consistency`  
**Base:** `colab`

---

## 1. Contexto e Resolução dos 5 Itens do Alexandre

Em resposta à auditoria de nomes legados solicitada pelo Alexandre:

### Item 1 — Server & BackgroundTasks (`KyraFiles.App.*` -> `KyraFiles.App.*`)
- `<AssemblyName>` e `<RootNamespace>` atualizados para `KyraFiles.App.Server` e `KyraFiles.App.BackgroundTasks`.
- Namespaces alinhados em `UpdateTask.cs`, `AppInstanceMonitor.cs`, `Helpers.cs` e `Program.cs`.
- `Package.appxmanifest` atualizado em `EntryPoint`, `ActivatableClassId` e `OutOfProcessServer`.
- `KyraFiles.App.csproj` atualizado em `<CsWinRTIncludes>` e referências do `.winmd`.
- `EcosystemMemoryDiagnostics.cs`, `Program.cs`, scripts de dev e release ajustados para monitorar os novos processos.

### Item 2 — Padronização 64-bit dos Projetos Nativos C++
- Removidos projetos legados Win32 (`KyraFiles.App.OpenDialog.Win32.vcxproj`, `KyraFiles.App.SaveDialog.Win32.vcxproj`).
- `tools/Build-Native.ps1` ajustado para compilar exclusivamente em 64-bit (`x64`).
- Fontes C++ renomeados via `git mv`:
  - `FilesLauncher.cpp` -> `KyraFilesLauncher.cpp`
  - `KyraFilesOpenDialog.*` -> `KyraFilesOpenDialog.*`
  - `KyraFilesSaveDialog.*` -> `KyraFilesSaveDialog.*`
  - `FilesDialogEvents.*` -> `KyraFilesDialogEvents.*`
  - Filtros `.vcxproj.filters` e recursos `.rc` correspondentes devidamente atualizados.

### Item 3 — Preservação de `FileSystem*`
- Confirmado que referências a `FileSystem` (`FileSystemDialog`, `FilesystemOperations`, etc.) são termos de infraestrutura de Sistema Operacional e foram mantidos intactos.

### Item 4 — Purga de Sobras e Pastas de Build Legadas
- Purgados todos os artefatos antigos contendo `KyraFiles.App.*` em `artifacts/`, `bin/`, `obj/` e `src/KyraFiles.App/Assets/KyraFilesOpenDialog/`.
- `Assets/KyraFilesOpenDialog/` no build final contém estritamente os binários 64-bit da nova identidade:
  - `KyraFiles.App.Launcher.exe` (2.700.288 bytes)
  - `KyraFiles.App.OpenDialog64.dll` (496.128 bytes)
  - `KyraFiles.App.SaveDialog64.dll` (986.624 bytes)
  - `SetFilesAsDefault.reg` / `UnsetFilesAsDefault.reg`

### Item 5 — Documentação Legada `files-custom.html`
- Removidos os 5 arquivos legados `files-custom.html` em `de-DE`, `en-US`, `es-ES`, `pt-BR` e `features/`.
- Links de navegação em todos os arquivos `.html` de documentação atualizados para `kyrafiles.html`.

---

## 2. Evidência: Comparação de Arquivos Antes vs Depois

Listas geradas e comparadas em `scratch/`:
- **Total de arquivos antes:** 10.852
- **Total de arquivos depois:** 7.749 (redução de resíduos e pastas intermediárias)
- **Arquivos com nome velho (`\KyraFiles.App.`, `\FilesLauncher`, `files-custom.html`):**
  - **Antes:** 109 arquivos
  - **Depois:** **0 arquivos** (100% dos nomes antigos purgados do repositório e dos binários)

---

## 3. Evidências de Compilação e Testes

1. **Build Nativo C++ 64-bit (`tools/Build-Native.ps1 -Configuration Debug`):**
   - Sucesso com Exit Code 0 em todos os 5 projetos (Launcher, HotkeyService, Watchdog, OpenDialog64, SaveDialog64).
2. **Build C# .NET WinUI (`src/KyraFiles.App/KyraFiles.App.csproj` - Platform x64):**
   - `Build succeeded. 0 Warning(s), 0 Error(s).`
3. **Testes Unitários (`tests/KyraFiles.App.Search.Index.Tests/KyraFiles.App.Search.Index.Tests.csproj`):**
   - Execução: 194 testes no total (193 aprovados, 0 falhas, 1 pulado documentado).
4. **Contenção de Recursos:**
   - Nenhum processo `KyraFiles.App.IndexService` ativo.

---

## 4. Decisões Arquiteturais Registradas

- `D-LAUNCHER-1`: Unificação do nome do Launcher para `KyraFiles.App.Launcher.exe` e pasta `%LOCALAPPDATA%\KyraFiles\`.
- `D-64BIT-CLEANUP-1`: Eliminação do toolchain 32-bit (Win32), consolidação em x64 exclusivo e purga de nomes legados.
