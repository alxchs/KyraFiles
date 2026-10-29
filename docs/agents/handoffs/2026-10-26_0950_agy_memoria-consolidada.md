# Memória Consolidada do Projeto — Files Custom

**Data/Hora:** 2026-10-26 09:50  
**Autor:** AGY (Google Antigravity)  
**Objetivo:** Registro formal e completo do estado do projeto para permitir limpeza/compactação segura do chat sem perda de continuidade.  
**Referências Centrais:** `docs/agents/SITUACAO.md`, `AGENT_CONTEXT.md`, `MASTER_SPEC.md`, `docs/specs/BACKLOG-ORDEM-AGY.md`.

---

## 1. Estado Atual das Versões e Releases

| Versão | Data | Status | Escopo e Observações |
|---|---|---|---|
| **v4.3.7** | 26/10/2026 | **Publicada na `main`** | **Release de correção P0:** Contém estritamente a correção do crash de segunda instância ao abrir abas pelo Explorer (F035). Validada com controle (sem correção: 3/20 crashes; com correção: 20/20 PASS). Instalador de 50,25 MB no GitHub. Gate F021 aprovado. |
| **v4.3.6** | 24/10/2026 | Superada | Publicada anteriormente sem revisão completa. Evidências de F030, F032, F033, F034 e F023 foram marcadas como `INFERRED` devido a incidentes com scripts de entrada antigos. |
| **Infra de Testes** | 26/10/2026 | **Integrada na `main`** | Commit `601d63a89`: `Send-GuardedInput.ps1` e `Test-NoRawInput.ps1` integrados na `main` (exceção autorizada sem bump de versão). 57 scripts legados em `tools/perf/_desativados/`. |

---

## 2. Mapa das Worktrees e Branches Ativas

```text
C:/desenv/utils/FilesApp               -> branch feature/f056-copy-path-formats (checkout principal)
C:/desenv/utils/FilesApp-f035          -> branch feature/f035-second-instance (investigação e correções F035)
C:/desenv/utils/FilesApp-f047          -> branch feature/f047-build-folder-cleaner (limpador de build atômico)
C:/desenv/utils/FilesApp-specs-backlog -> branch claude/specs-backlog (especificações SDD, SITUACAO.md e fila)
C:/desenv/utils/FilesApp-upstream-base -> detached HEAD (baseline original de comparação)
```

---

## 3. O que foi Finalizado e Validado

1. **Trava de Segurança de Entrada (`test-input-guard`, Tarefas S1 a S5):**
   - Helper único `tools/perf/Send-GuardedInput.ps1` com asserção mandatória de foco (`Assert-ForegroundOwned`), garantia de soltura de modificadores via `try/finally`, validação estrita de PID e zero `SetForegroundWindow`.
   - Prova com janelas próprias validada (positivo sob AWAY, negativos com bloqueio imediato, medição `PostMessage`).
   - Verificador `Test-NoRawInput.ps1` em vigor (rejeita commits com entrada crua fora do helper).
2. **T0 (Análise Preliminar de Código) de 8 Features:**
   - F030, F032, F033, F034, F023, F036, F049 e F056 analisadas e documentadas com evidências (`CONFIRMED` / `INFERRED`).
   - F036 teve a baseline recalibrada honestamente para `OBSERVED, n = 3`.
3. **SDD Completo Aprovado:**
   - F035 (P0), F056, F047, F049 (Onda 1), F037, F044, F045, F048 (Onda 2), F036, F043 (Onda 3).
4. **F056 — T1 Concluída (`44aa312bd`):**
   - Classe central `PathFormatter.cs` em `Files.Shared` e testes unitários em `Files.App.Search.Index.Tests` verdes.
5. **F047 — T0 a T2 e C1 a C3 Concluídas:**
   - Estrutura de limpeza de pastas de build concluída com testes por tabela e falha fechada no verificador git.

---

## 4. O que Está em Andamento / Rascunhado

1. **F056 (Copiar caminhos como...):**
   - **T1:** Commitada no repositório.
   - **T2 a T4:** Rascunho da integração visual (submenu de contexto no flyout, atalhos de teclado e configurações) salvo em patch de segurança (`%TEMP%\claude\...\backup-f056` e arquivos em edição no checkout principal).
2. **F035 (Segunda instância / abas):**
   - AC-1 resolvido e publicado na v4.3.7.
   - AC-2, AC-4 e AC-5 mapeados com pendências/limitações documentadas em `SITUACAO.md`.
3. **F047:**
   - Tarefa T3 pendente para fechamento.

---

## 5. Defeitos Críticos Conhecidos (Prioridade P0)

1. **Crash nativo com o Painel de Prévia aberto:**
   - Relatado pelo Alexandre (26/10 08:11:45).
   - Módulo: `Microsoft.UI.Input.dll`, código de exceção: `0xc0000409` (fail-fast / stack buffer overrun), disparado durante scroll/navegação rápida passando por arquivo de vídeo com prévia aberta.
   - Dump salvo e protegido em `%LOCALAPPDATA%\CrashDumps\Files.exe.15768.dmp` (não apagar!).
   - Roteiro de reprodução em `docs/agents/tasks/R-crashes-reproducao.md`.
2. **Crash do `GridLayoutPage`:**
   - Falha observada no log de eventos do Windows (`LookupConverter`/`Update_FileTagsUI`, 26/10 00:59:04). Causa raiz em investigação.

---

## 6. Ordem Rígida de Prioridades para as Próximas Sessões

Conforme acordado no backlog oficial (`BACKLOG-ORDEM-AGY.md` e `SITUACAO.md`):

1. **Prioridade 0 (Bugs Críticos de Crash):**
   - Reproduzir e diagnosticar o crash do `Microsoft.UI.Input.dll` (prévia com vídeo) e do `GridLayoutPage` conforme `R-crashes-reproducao.md` (somente em janela de AWAY, sem tocar em `src/`).
2. **Retrabalho de Evidências Reais (Janela de AWAY com `ALEXANDRE_AWAY=1`):**
   - Reexecutar com asserções estritas do helper os testes de F030, F032, F033, F034, F029 e F023.
3. **Prioridade 1 (Desempenho):**
   - Medição de navegação contínua de teclas seguradas (setas, Home e End em Detalhes, Ícones e Colunas) conforme `docs/agents/tasks/R-teclas-seguradas-medicao.md`.
4. **Onda 1 (Features Novas):**
   - Concluir F056 T2 a T4 (submenu no menu de contexto, atalho e configurações; restore via `Files.slnx`).
   - Concluir F047 T3.
   - Iniciar F049 (atenção estrita ao controle negativo de exclusão de links simbólicos).
5. **Onda 2 (Backlog):**
   - F037 (`NameParts` e renomeação), F044 (drag and drop assíncrono), F045, F048 (comparador de pastas).

---

## 7. Regras Mandatórias de Engenharia (Invioláveis)

* **Proteção da `main`:** Jamais alterar ou commitar diretamente na `main`. Todo trabalho nasce em branch `feature/<nome>` e a integração é feita via squash/rebase gerando commits coesos e lineares.
* **Bump de Versão:** Toda release/integração funcional na `main` exige incremento estrito do número de versão.
* **Controle e Evidências:** Nunca marcar testes como concluídos sem controle negativo e relatório gerado a partir de CSV.
* **Segurança de Entrada em Testes:** Zero chamadas cruas (`SendKeys`, `keybd_event`, etc.). Toda automação usa `Send-GuardedInput.ps1` com asserção mandatória de foco no processo de teste. Testes visuais somente com `ALEXANDRE_AWAY=1`.
* **Gate F021 e Higiene:** Nenhuma entrega é aceita sem validar `pwsh tools\perf\verify_no_index_service.ps1` (CPU < 5%, zero `IndexService` rodando). Nunca deixar binários em pastas temporárias fora do padrão `%TEMP%\agy\<GUID>\...`.
* **Preferências Visuais:** Jamais usar vermelho ou vermelho combinado com amarelo em ícones/artes geradas para o Alexandre.

