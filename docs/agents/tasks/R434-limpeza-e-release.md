# R434 — Limpeza e Release 4.3.4

Ordem do Claude (arquiteto), a pedido do Alexandre em 22/10/2026 ~21h: limpeza dos desenvolvimentos + release 4.3.4 + instalador. Projeto: C:\desenv\utils\KyraFiles (trabalhe SÓ nele).

Antes de tudo: `ia check`, `ia claim agy "R434 limpeza e release 4.3.4"`. Crie a branch `feature/r434-limpeza-release` a partir da `main` atual (0ea9958db). Salve esta ordem como `docs/agents/tasks/R434-limpeza-e-release.md` (é brief de manutenção, não feature: não precisa de spec SDD). Commit local a cada passo verde.

## 1. Limpeza (só sujeira de desenvolvimento; nada que seja trabalho de outra IA ou do Alexandre)

Para cada item, registre no brief o que achou, o que removeu e o comando que comprova.

a. **Código**: procure instrumentação temporária que ficou das F024 a F031: `Stopwatch`/logs de tempo só para medição, `Debug.WriteLine` de diagnóstico, variáveis de ambiente de teste, código comentado de experimento. Remova só o que a spec/tasks da feature diz que era temporário ou que claramente é sonda de medição. Na dúvida, não remova: liste como DISCOVERY. Cada remoção: build Release 0/0.
   **Exceção, NÃO mexa:** as bandeiras da F029 (`F029Flag`, `ReadF029Flags`, `C:\FilesUXLab\f029_flags.txt`, `TryMoveRow`) em `DetailsLayoutPage.xaml.cs`. A F029 é trabalho em aberto do Claude (T1–T5); elas saem na T4 dela. Não apague o `f029_flags.txt` (vazio = comportamento da F028).
a2. **Tarefas desmarcadas que já foram feitas**: confira cada `[ ]` dos `tasks.md` das F023, F026 e F027 contra o código e a evidência. A F027 T1–T4 está implementada (`DetailsLayoutPage.xaml.cs`: `SelectionCheckbox_PointerEntered`) e medida (`docs/agents/evidence/F027/relatorio.md`): marque com a referência. F026 T3/T4: marque só se `docs/agents/evidence/F026/relatorio.md` tiver a tabela/verificação pedida; senão deixe e anote. F023 T11/T13/T14: deixe como estão e liste no handoff. **Não marque** F029, F030 T2, F032 T4 nem F033 T4: são do Claude agora (teste no app aberto).
b. **Pastas Debug de build**: apague `src\**\bin\x64\Debug` e `src\**\obj\x64\Debug` (e `bin\Debug` na raiz, se existir). Não mexa em Release.
c. **Laboratório da F024**: o pedido do Alexandre agora vale como a validação que o `tasks.md` da F024 exigia. Rode `tools\perf\cleanup_f024_lab.ps1`, prove `Test-Path C:\FilesUXLab\perf\1600` = False, atualize `docs/specs/F024-choque-performance-navegacao/test_artifacts_registry.md` (Removidos) e marque as 3 tarefas no `tasks.md` da F024. Não apague o resto de `C:\FilesUXLab` (laboratório do §17).
d. **Temporários**: `%TEMP%\agy\` — apague as pastas desta sessão/projeto que não forem evidência. Evidência citada em doc vai para `docs/agents/evidence/`. Confira também `D:\DriveD\Downloads` por `*.tmp`/arquivos gerados por teste deste projeto; só remova se tiver certeza que foi o teste.
e. **`debug.log`** solto na raiz do repositório: apague (é ignorado pelo git).
f. **Instaladores velhos** em `artifacts\Setup`: apague 4.3.1 e 4.3.2. **Mantenha o 4.3.3** (o Claude vai usá-lo para testar a atualização 4.3.3 → 4.3.4).
g. **Branches locais** `feature/f024-*` a `feature/f029-*`: só apague a local se provar que todo o conteúdo dela já está na `main` (squash: compare `git diff <branch> main -- <arquivos que a branch mexeu>`). Não apague nenhuma branch remota. Liste o resultado.
h. **NÃO mexa** no worktree `C:\desenv\utils\KyraFiles-upstream-base` (base do F025, com 4 alterações): só registre que existe.
i. **Cópias do app**: no fim, só a Release instalada em `%LOCALAPPDATA%\Programs\KyraFiles` deve estar registrada: `Get-AppxPackage KyraFiles*` com `InstallLocation` nessa pasta.

## 2. Versão
4.3.3 → **4.3.4** em `src/KyraFiles.App/Package.appxmanifest` (4.3.4.0) e `installer/KyraFilesSetup.iss` (MyAppVersion), como no commit 0ea9958db.

## 3. Build Release e instalador (mesmo procedimento da 4.3.3)
- `mkfile release src\KyraFiles.App\KyraFiles.App.csproj` = 0 Warning(s), 0 Error(s). Compile também `KyraFiles.HotkeyService.exe` e `KyraFiles.Watchdog.exe` em Release (`bin\Release`), 0/0.
- `.\Open-KyraFiles.ps1 -Configuration Release -NoLaunch` para o layout AppX; `installer\Generate-PackageManifest.ps1` (versão 4.3.4); ISCC → `artifacts\Setup\KyraFilesSetup-4.3.4.exe`. Informe tamanho e SHA256 (`Get-FileHash`) e compare o tamanho com o 4.3.3.
- Instale o 4.3.4 por cima do 4.3.3 instalado (silencioso), confirme código de saída 0, `Get-AppxPackage KyraFiles*` = 4.3.4.0 com InstallLocation em `%LOCALAPPDATA%\Programs\KyraFiles`, abra o app pelo protocolo/alias e capture a tela (Sobre mostrando 4.3.4.0). Gate F021: `pwsh tools\perf\verify_no_index_service.ps1` = 0.

## 4. Documentação
- `docs/releases/2026-10-22_build-KyraFilesSetup-4.3.4.md` no modelo das anteriores: caminho, SHA256, tamanho, commit, build, o que mudou desde a 4.3.3 (a limpeza), como testar, o que NÃO está testado. Só CONFIRMED com o comando. Inclua em "o que ainda está no exe": bandeiras de A/B da F029 inertes (lidas de `C:\FilesUXLab\f029_flags.txt`; sem o arquivo ou vazio = comportamento da F028), saem na próxima versão; F030 T2, F032 T4 e F033 T4 (teste no app aberto) NOT TESTED, com o Claude.
- `STATUS.md` (seção Release v4.3.4), `AGENT_CONTEXT.md` (NEXT ACTION: "Claude revisa e publica a release 4.3.4 no GitHub"), handoff `docs/agents/handoffs/2026-10-22_HHMM_agy_r434-limpeza-release.md`.

## 5. Integração e parada
Squash + rebase e `git merge --ff-only` na `main` local (fluxo dos hooks). Tag local `tag_v4.3.4_limpeza-dev-instalador-4.3.4_salvo`. **Não faça push, não crie release no GitHub**: a publicação é do Claude. `ia release` e pare.

---

## Log de Execução e Evidências da Limpeza

*(Preenchido durante a execução)*
