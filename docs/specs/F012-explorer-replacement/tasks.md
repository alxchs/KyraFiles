# F012 — Tarefas de Implementação

Status: **APROVADAS** (Claude, 09/10/2026; revisadas pela D-015). Executor desta rodada: Claude, a pedido do Alexandre (AGY sem cota em 08/10/2026).
Branch: `feature/f012-explorer-replacement`
Metodologia: Spec-Driven Development (`METODO.md` §6).

Revisão de 09/10/2026: o trampolim C# da AGY (T1/T2 antigas) foi substituído pelo launcher C++ do upstream (D-015). O teste antigo (T4) não testava o fallback: só conferia que o `.exe` existia e terminava. As marcas "concluído" anteriores não valem mais.

---

- [x] **T1 — Launcher do upstream com fallback silencioso** (`src/Files.App.Launcher/FilesLauncher.cpp`)
  - **Atende:** `FR-003`, `FR-004`, `NFR-001`, `AC-6`.
  - **Pronto quando:** sem `MessageBox` e sem `runas` no caminho "Files desinstalado"; a reversão do registro termina antes de abrir o Explorer.

- [x] **T2 — Build dos projetos nativos** (`tools/Build-Native.ps1`)
  - **Atende:** `FR-004`, `FR-006`.
  - **Pronto quando:** o script gera `Files.App.Launcher.exe`, `Files.App.OpenDialog32/64.dll` e `Files.App.SaveDialog32/64.dll` em `src/Files.App/Assets/FilesOpenDialog`, e o `mkfile release` os leva para o layout AppX (conferido no disco).

- [ ] **T3 — Configurações > Avançado** (`AdvancedViewModel.cs`)
  - **Atende:** `FR-006`, `FR-008`, `AC-8`.
  - **Pronto quando:** ligar e desligar "gerenciador padrão" funciona sem UAC e o estado mostrado bate com o registro; "usar como diálogo de arquivos" liga, registra as DLLs existentes e registra falha no log em vez de engolir.

- [x] **T4 — Teste automatizado do fallback** (`tests/test-launcher-fallback.ps1`)
  - **Atende:** `AC-6`, `NFR-001`, `NFR-002`.
  - **Pronto quando:** com o Files "desinstalado" (`LOCALAPPDATA` vazio), o launcher termina sozinho, o Explorer abre a pasta pedida (caminho com espaço e acento) e o registro real não muda.

- [x] **T5 — Instalador e script de release**
  - **Atende:** `FR-007`, `AC-9`.
  - **Pronto quando:** o desinstalador reverte o registro; o script de release recusa pacote sem os binários nativos. (Desinstalador: AGY, 08/10; script de release: ajustado em 09/10.) NOT TESTED: desinstalação real.

- [ ] **T6 — Validação no Windows real**
  - **Atende:** `AC-1`, `AC-2`, `AC-4`, `AC-5`, `AC-8`.
  - **Pronto quando:** com a opção ligada nas Configurações, `Win + E`, `explorer.exe <pasta>` e abertura de pasta pelo shell abrem o Files; desligada, voltam ao Explorer. Capturas em `docs/agents/evidence/f012/`. A opção é desligada no fim do teste.

- [ ] **T7 — Diálogo de arquivos**
  - **Atende:** relato do Alexandre de 08/10/2026 (fora da spec F012; ver spec §6).
  - **Pronto quando:** com a opção ligada, o diálogo "Abrir" de um app comum (Bloco de Notas) abre o Files; desligada, volta o diálogo do Windows. Capturas.

- [ ] **T8 — Build 0/0, handoff e nota de liberação**
  - **Pronto quando:** `mkfile release` com `0 Warning(s)` e `0 Error(s)`, `git diff --check` limpo, `STATUS.md`, handoff e nota em `docs/releases/`.

## Estado em 09/10/2026 01:00 (Claude)

| Tarefa | Estado | Evidência |
|---|---|---|
| T1 | CONFIRMED | `pwsh tests\test-launcher-fallback.ps1`: todos PASS |
| T2 | CONFIRMED | `pwsh tools\Build-Native.ps1` gerou launcher e 4 DLLs; `mkfile release` = 0/0; layout AppX com os 5 binários |
| T3 | parcial | DLLs registram (exit 0) e as chaves batem com a detecção; alternar pela tela NOT TESTED (Firewall em primeiro plano bloqueou a automação) |
| T4 | CONFIRMED | idem T1 |
| T6 | NOT TESTED | `Win + E` e duplo clique; launcher direto CONFIRMED (Files navegou para a pasta) |
| T7 | OBSERVED | diálogo "Abrir" de outro processo abriu o Files na pasta inicial (`docs/agents/evidence/f012/02_dialogo_abrir_files.png`); retorno do arquivo NOT TESTED |
| T8 | parcial | build 0/0 e nota `docs/releases/2026-10-09_0100_build-F012-dialogo-e-gerenciador.md`; handoff pendente |
