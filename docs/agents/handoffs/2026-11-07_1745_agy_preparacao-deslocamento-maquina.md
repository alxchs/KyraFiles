# Handoff: Preparação para Deslocamento de Máquina e Release v4.3.12

**Data/Hora:** 2026-11-07 17:45 (UTC-3)  
**Agente:** AGY  
**Branch:** `feature/v4.3.12-release-installer`  
**Commit:** `d5447de84` (`release(v4.3.12): instalador atualizado, purga de vestígios legados e regra contra espalhar binários`)  
**Base:** `main` (commit `4ee13e712`)

---

## 1. O que foi Concluído nesta Máquina

1. **Compilação Release Oficial (v4.3.12.0):**
   - Projetos nativos C++ x64 compilados via `tools\Build-Native.ps1 -Configuration Release` (0 erros, 0 avisos).
   - Aplicativo principal C# WinUI 3 compilado via `dotnet build src\KyraFiles.App\KyraFiles.App.csproj -c Release -p:Platform=x64` (0 erros, 0 avisos).
   - Versão `4.3.12.0` registrada em `src\KyraFiles.App\Package.appxmanifest`. Data/hora embutida no binário para a tela Sobre: `2026-11-07 07:47:45`.

2. **Geração do Novo Instalador v4.3.12:**
   - Corrigido o cálculo de SHA256 em `installer\Generate-PackageManifest.ps1` e gerado `installer\package_manifest.json` com hashes reais de todos os 1.240 arquivos do pacote (179,45 MB).
   - Gerado o instalador via Inno Setup 6: `artifacts\Setup\KyraFilesSetup-4.3.12.exe` (53.641.735 bytes, 51,16 MB, SHA256: `2009682CA23120114155961D8900883F034BB64DDFC93C9B40582638AF1CB4F8`).
   - Adicionada seção `[InstallDelete]` em `installer\KyraFilesSetup.iss` para purgar preventivamente executáveis legados de instalações anteriores (`KyraFiles.App.Server\`, `Assets\KyraFilesOpenDialog\KyraFiles.App.*`, `KyraFiles.App.BackgroundTasks.*`, etc.).
   - Nota de release documentada em `docs/releases/2026-11-07_build-KyraFilesSetup-4.3.12.md`.

3. **Purga de Vestígios Legados e Regra Dura contra Espalhar Binários:**
   - Limpeza de todos os diretórios `Debug` residuais no repositório (`bin\Debug`, `artifacts\Debug`, `src\*\bin\*\Debug`, `src\*\obj\*\Debug`, etc.).
   - Limpeza ativa dos binários obsoletos em `%LOCALAPPDATA%\Programs\KyraFiles`.
   - Adicionada **REGRA DURA DE NÃO PROLIFERAÇÃO E CONCENTRAÇÃO DE BINÁRIOS (07/11/2026)** em:
     - `AGENTS.md` (repositório local);
     - `~/.dev-method/METODO.md` (§12, comum com a Claude);
     - `~/.gemini/config/plugins/preferencias-alexandre/rules/AGENTS.md` (regras globais da AGY).

---

## 2. Estado do Git

- **Branch de Trabalho:** `feature/v4.3.12-release-installer`
- **Commit Local:** `d5447de84`
- **Working Tree:** Limpa (sem arquivos soltos ou modificações pendentes).
- **Atenção:** `git push` **NÃO** foi executado nesta sessão, conforme a regra rígida de autorização prévia do Alexandre. Para levar este trabalho para a outra máquina via repositório remoto, autorize o push da branch `feature/v4.3.12-release-installer` ou transfira o commit/patch.

---

## 3. Próximos Passos na Máquina Nova

1. **Na máquina nova:**
   - Se os commits forem sincronizados via push/pull: `git checkout feature/v4.3.12-release-installer` ou integrá-la na `main`/`colab`.
   - Testar o instalador `artifacts\Setup\KyraFilesSetup-4.3.12.exe` gerado.
2. **Próxima Tarefa da Fila (F089):**
   - Iniciar a spec **F089 (Fontes Não Aplicadas)** a partir da branch `feature/f089-fontes`.
   - A documentação de SDD já está preparada em `docs/specs/F089-fontes-nao-aplicadas/` (`spec.md`, `plan.md`, `tasks.md`).
