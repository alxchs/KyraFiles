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

- [x] **T3 — Configurações > Avançado** (`AdvancedViewModel.cs`)
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

- [x] **T7 — Diálogo de arquivos**
  - **Atende:** relato do Alexandre de 08/10/2026 (fora da spec F012; ver spec §6).
  - **Pronto quando:** com a opção ligada, o diálogo "Abrir" de um app comum (Bloco de Notas) abre o Files; desligada, volta o diálogo do Windows. Capturas.

- [ ] **T9 — Redirecionador de janelas do Explorer** (`ExplorerWindowRedirector.cs`, `ShellWindowsAutomation.cs`, `MainWindow.xaml.cs`, `App.xaml.cs`, `Win32Helper.Storage.cs`)
  - **Atende:** `FR-009`, `FR-010`, `FR-011`, `AC-1`, `AC-3`, `AC-4`, `AC-11`, `AC-12`. Plan §9.
  - **Pronto quando:** com o Files em execução e o gerenciador padrão ligado, `Win + E`, `explorer.exe <pasta>` e `explorer /select,<arquivo>` abrem no Files (o arquivo selecionado no terceiro caso) e a janela do Explorer não fica visível por mais de 100 ms; Painel de Controle continua no Explorer; com a opção desligada, nada é redirecionado. Script `tools/perf/f012_explorer_redirect.ps1` com os números.

- [ ] **T8 — Build 0/0, handoff e nota de liberação**
  - **Pronto quando:** `mkfile release` com `0 Warning(s)` e `0 Error(s)`, `git diff --check` limpo, `STATUS.md`, handoff e nota em `docs/releases/`.

## Estado em 09/10/2026 01:00 (Claude)

| Tarefa | Estado | Evidência |
|---|---|---|
| T1 | CONFIRMED | `pwsh tests\test-launcher-fallback.ps1`: todos PASS |
| T2 | CONFIRMED | `pwsh tools\Build-Native.ps1` gerou launcher e 4 DLLs; `mkfile release` = 0/0; layout AppX com os 5 binários |
| T3 | CONFIRMED (diálogo) | pela tela, com outro processo segurando a DLL carregada: desligar removeu as chaves CLSID e religar as recriou (`tools\perf\f012_advanced_settings.ps1 -FileDialog Off`/`On`; capturas 04 e 05). Antes da correção de 01:10, desligar falhava com `IOException ... being used by another process` (debug.log 00:57:02). Desligar o gerenciador padrão pela tela: NOT TESTED |
| T4 | CONFIRMED | idem T1 |
| T6 | NOT TESTED | `Win + E` e duplo clique; launcher direto CONFIRMED (Files navegou para a pasta) |
| T7 | OBSERVED | diálogo "Abrir" de outro processo abriu o Files na pasta inicial (`docs/agents/evidence/f012/02_dialogo_abrir_files.png`); retorno do arquivo NOT TESTED |
| T8 | parcial | build 0/0 e nota `docs/releases/2026-10-09_0100_build-F012-dialogo-e-gerenciador.md`; handoff pendente |

## Estado em 09/10/2026 07:30 (Claude)

Substitui a tabela acima nos itens que mudaram. Build com a correção da D-016: `mkfile r Files.slnx` = `0 Warning(s)`, `0 Error(s)`; pacote registrado por `Open-FilesDev.ps1 -NoLaunch`.

| Tarefa / AC | Estado | Evidência |
|---|---|---|
| T3 / AC-8 | CONFIRMED | pela tela, `tools\perf\f012_advanced_settings.ps1 -DefaultManager Off`: `Folder\shell\open\command` = `C:\WINDOWS\Explorer.exe`, chaves CLSID do diálogo removidas, opção do diálogo desligada e desabilitada, sem UAC (nenhum `consent.exe`); `-DefaultManager On -FileDialog On` religou tudo. Capturas 09 e 10 |
| T7 "Abrir" | CONFIRMED | `pwsh tools\perf\f012_file_dialog_return.ps1 -Mode Open` 3 de 3 PASS: `OpenFileDialog` de outro processo abriu o Files na pasta inicial, com uma aba só, e recebeu `OK\|...\arquivo de teste ação.txt` (caminho com espaço e acento). Captura 07. Antes da D-016: "Cancelar" |
| T7 "Salvar" | CONFIRMED (arquivo existente) | `-Mode Save` 1 de 1 PASS, captura 08. NOT TESTED: salvar com nome novo (o Files não tem caixa de nome de arquivo no modo diálogo) e app 32 bits |
| T7 desligado | CONFIRMED | com a opção desligada, o `OpenFileDialog` abriu a janela `#32770` do Windows e nenhum Files |
| Sessão do usuário | CONFIRMED | 5 usos seguidos do diálogo deixaram `LastSessionTabList` = `Settings \| D:\` (antes da D-016, cada uso acrescentava uma aba) |
| T6 / AC-2, AC-5 | CONFIRMED (shell) | `Start-Process <pasta>` levou a janela do Files já aberta para a pasta (4,0 s), sem processo novo de janela |
| T6 / AC-4 | FALHA parcial | `explorer.exe "<pasta>"` abre o Explorer do Windows. PENDING DECISION D-017 |
| T6 / AC-1 | NOT TESTED | `Win + E` precisa de teclado real (Alexandre) |
