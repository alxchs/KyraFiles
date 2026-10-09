# F012 — Tarefas de Implementação

Status: **PROPOSTAS / AGUARDANDO APROVAÇÃO** (08/10/2026).
Branch: `feature/f012-explorer-replacement`
Metodologia: Spec-Driven Development (`METODO.md` §6).

---

### Tarefas da F012

- [x] **T1 — Criar Trampolim com Fallback Incondicional (`tools/launcher/Program.cs`)**
  - **Atende:** `FR-003`, `FR-004`, `NFR-001`, `AC-3`, `AC-6`, `AC-7`.
  - **Critério de pronto:** Código C# nativo leve, sem dependências externas. Recebe `%1` e argumentos; se `files-dev.exe` / protocolo `files-dev:` responder, despacha para o Files Custom; caso contrário (Files ausente, excluído, desinstalado, Safe Mode ou erro), executa imediatamente e silenciosamente `%SystemRoot%\explorer.exe` passando os mesmos argumentos. (CONCLUÍDO)

- [x] **T2 — Compilação e Posicionamento do Launcher (`Files.App.Launcher.exe`)**
  - **Atende:** `FR-004`, `NFR-002`.
  - **Critério de pronto:** Executável x64 compilado via `csc.exe` (/optimize+, /target:winexe, sem janela preta de console) gerado em `src/Files.App/Assets/FilesOpenDialog/Files.App.Launcher.exe`. Latência medida abaixo de 15ms. (CONCLUÍDO)

- [ ] **T3 — Integração com `AdvancedViewModel.cs`**
  - **Atende:** `FR-006`, `FR-008`, `AC-8`.
  - **Critério de pronto:** Garantir que o comando `SetAsDefaultExplorerCommand` utilize o novo launcher em `%LocalAppData%\Files\`, verifique se o arquivo existe antes de aplicar e permita desativação limpa sem erros.

- [x] **T4 — Teste Automatizado de Fallback (`tests/test-launcher-fallback.ps1`)**
  - **Atende:** `AC-3`, `AC-6`, `NFR-001`.
  - **Critério de pronto:** Script de teste automatizado validando os dois cenários: (1) Com Files presente: invoca Files Custom; (2) Com Files ausente/renomeado: invoca o Windows Explorer instantaneamente com zero janelas de erro. (CONCLUÍDO: 6/6 asserts passando)

- [x] **T5 — Integração no Instalador e no Script de Release**
  - **Atende:** `FR-007`, `AC-9`.
  - **Critério de pronto:** `tools/Create-SelfContained-Release.ps1` inclui a compilação do launcher no pacote, e `tools/installer/Program.cs` reverte os registros com segurança durante a desinstalação. (CONCLUÍDO)

- [ ] **T6 — Validação no Ambiente Windows Real e Prova Visual**
  - **Atende:** `AC-1`, `AC-2`, `AC-4`, `AC-5`, `AC-6`.
  - **Critério de pronto:** Teste prático com `Win + E`, duplo clique em pasta na Área de Trabalho e teste destrutivo (simulação de ausência) comprovando a abertura do Explorer nativo como fallback.

- [ ] **T7 — Verificação de Qualidade, Build 0/0 e Handoff**
  - **Critério de pronto:** Build Release x64 `0 Warning(s), 0 Error(s)`, `git diff --check` limpo, `STATUS.md` atualizado e handoff.
