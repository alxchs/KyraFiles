# Handoff: Relatório do Instalador 4.3.11, Purga de Resíduos (F088 Fase 0) e Destravamento de Dev

**Data/Hora:** 2026-11-04 00:00 (UTC-3)  
**Agente:** AGY (executora)  
**Branch:** `feature/f088-fase0-residuos`  
**Base:** `main` (commit `1fcf37136`)

---

## 1. Relatório Técnico do Commit do Instalador 4.3.11 (`1fcf37136`)

O commit `1fcf37136` reuniu na `main` o empacotamento oficial da versão 4.3.11 e as alterações de consolidação do Launcher único e projetos nativos 64-bit:

### O que o commit entregou:
1. **Instalador Oficial KyraFilesSetup 4.3.11 (`artifacts\Setup\KyraFilesSetup-4.3.11.exe`):**
   - Tamanho: 53.476.356 bytes (51,00 MB), variação de +1,30% em relação à v4.3.10 (+686 KB, dentro do teto de 5 MB / 10%).
   - SHA256: `1816647CA98466D93CC1F91C2835480D942D2F71A45EB1D46FCDE86067DB0B6D`.
   - Empacotado via Inno Setup 6 (`installer\KyraFilesSetup.iss`) cobrindo 1.235 arquivos com manifesto de integridade (`installer\package_manifest.json`).
   - Inclui binário `es.exe` (Everything Search CLI) para busca instantânea integrada.
2. **Correção Crítica de Registro AppX (HRESULT 0x80073CFB):**
   - O instalador desregistra preventivamente manifestos existentes em *Development Mode* preservando dados do usuário (`-PreserveApplicationData`) antes de executar `Add-AppxPackage -Register`, prevenindo conflitos com o Windows Deployment Server na reinstalação/atualização.
   - Encerramento preventivo de instâncias de `KyraFiles*` e `Files*` na instalação/atualização.
3. **Consolidação do Launcher Nativo e 64-bit Exclusivo:**
   - Padronização em `KyraFiles.App.Launcher.exe` e eliminação de toolchains Win32 duplicados.
   - Alinhamento de nomes em assemblies `KyraFiles.App.Server` e `KyraFiles.App.BackgroundTasks`.
4. **Nota de Release Registrada:**
   - `docs/releases/2026-11-03_build-KyraFilesSetup-4.3.11.md`.

---

## 2. Diagnóstico da Causa-Raiz: Travamento do Exe em `Open-KyraFilesDev.ps1`

- **Sintoma:** O script de desenvolvimento deixava o executável travado (`locked`), impedindo novas compilações ou gerando arquivos temporários `.locked.<guid>` sem conseguir sobrescrever os binários.
- **Causa-Raiz Encontrada:** 
  A linha 38 de `Open-KyraFilesDev.ps1` ainda usava `Get-Process Files, KyraFiles.App.Server, KyraFiles.App.IndexService`. Com a renomeação da identidade para `KyraFiles`, o processo principal em execução se chama `KyraFiles` (e o servidor COM `KyraFiles.App.Server`). Como o `Get-Process` buscava apenas `Files`, o processo `KyraFiles.exe` continuava em execução com handles abertos durante a rotina de cópia de arquivos para `$layoutDir`.
- **Solução Implementada:**
  - `Open-KyraFilesDev.ps1` agora encerra ativamente `KyraFiles`, `KyraFiles.App.Server`, `KyraFiles.App.IndexService` (mantendo os nomes legados por garantia retroativa).
  - Incluído `Wait-Process -Timeout 3` e `Start-Sleep -Milliseconds 500` para garantir liberação completa dos arquivos pelo Windows antes da cópia.
  - Limpeza automática de quaisquer arquivos residuais `*.locked.*` no diretório de layout.
  - Comentários e referências de documentação interna alinhados para `KyraFiles`.

---

## 3. Resolução Completa dos Resíduos de Nome Antigo (F088 Fase 0)

1. **Configurações dos Projetos C++ Nativos (`.vcxproj`):**
   - Removidas todas as declarações de configurações `Win32` (`Debug|Win32` e `Release|Win32`) e seus blocos `ItemDefinitionGroup` dos 3 projetos:
     - `src/KyraFiles.App.Launcher/KyraFiles.App.Launcher.vcxproj`
     - `src/KyraFiles.App.OpenDialog/KyraFiles.App.OpenDialog.vcxproj`
     - `src/KyraFiles.App.SaveDialog/KyraFiles.App.SaveDialog.vcxproj`
   - `<RootNamespace>` do Launcher atualizado de `FilesAppLauncher` para `KyraFilesAppLauncher`.
2. **Workflows de CI/CD (`.github/workflows/` e `.github/scripts/`):**
   - Atualizados os caminhos obsoletos em 7 arquivos:
     - `.github/workflows/cd-sideload-preview.yml`
     - `.github/workflows/cd-sideload-stable.yml`
     - `.github/workflows/cd-store-preview.yml`
     - `.github/workflows/cd-store-stable.yml`
     - `.github/workflows/ci.yml`
     - `.github/workflows/format-xaml.yml`
     - `.github/scripts/Configure-AppxManifest.ps1`
   - `Files.slnx` -> `KyraFiles.slnx`
   - `src\KyraFiles.App\KyraFiles.App.csproj` -> `src\KyraFiles.App\KyraFiles.App.csproj`
   - `src\KyraFiles.App.Launcher\KyraFiles.App.Launcher.vcxproj` -> `src\KyraFiles.App.Launcher\KyraFiles.App.Launcher.vcxproj`
   - `tests\KyraFiles.InteractionTests\...` -> `tests\KyraFiles.InteractionTests\...`
   - `src/KyraFiles.App.Controls` e `tests/KyraFiles.App.UITests` corrigidos no XAML styler.
3. **Regras de Ignorados (`.gitignore`):**
   - Padrões adicionados para novos executáveis e arquivos de lock: `KyraFiles.HotkeyService.exe`, `KyraFiles.Watchdog.exe`, `*.locked.*`.
4. **Diretrizes e Árvore de Código (`AGENTS.md`):**
   - Árvore de `/src` e `/tests` atualizada para refletir todos os projetos reais `KyraFiles.*`.
   - Comandos de compilação msbuild atualizados de `Files.slnx` para `KyraFiles.slnx`.

---

## 4. Evidências de Validação e Testes

1. **Build Nativo C++ 64-bit (`tools/Build-Native.ps1 -Configuration Release`):**
   - Compilação dos 5 projetos nativos C++ em x64: `0 Warning(s)`, `0 Error(s)`.
   - Binários gerados e copiados para `src\KyraFiles.App\Assets\KyraFilesOpenDialog`:
     - `KyraFiles.App.Launcher.exe` (336.896 bytes)
     - `KyraFiles.App.OpenDialog64.dll` (133.120 bytes)
     - `KyraFiles.App.SaveDialog64.dll` (182.272 bytes)
2. **Build C# .NET WinUI (`dotnet build src/KyraFiles.App/KyraFiles.App.csproj -c Release -p:Platform=x64`):**
   - `Build succeeded. 0 Warning(s), 0 Error(s).`
3. **Bateria de Testes Unitários (`dotnet test tests/KyraFiles.App.Search.Index.Tests/...`):**
   - Total: 194 testes
   - Sucedidos: **193 aprovados**
   - Falhas: **0**
   - Ignorados: 1 (Benchmark justificado F021)
   - Tempo: 1m 38s
4. **Contenção de Processos:**
   - Nenhum processo `KyraFiles.App.IndexService` ativo.

---

## 5. Próximos Passos Desbloqueados

Com a Fase 0 da F088 concluída e o script de desenvolvimento destravado:
- **Opção A:** Prosseguir com **F088 Fase 1** (Identidade do pacote, Publisher e migração de pastas).
- **Opção B:** Iniciar a execução da **F052 (Visão de Repositório Git)** conforme tarefas já aprovadas em `docs/specs/F052-git-repo-view/tasks.md`.

