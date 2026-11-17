# Passagem do KyraFiles para outra máquina (Claude, 06/11/2026)

Leia este arquivo primeiro na máquina nova. Ele junta o estado do projeto e a memória que o Claude tinha
nesta máquina. Essa memória fica fora do git, em `~/.claude/projects/<pasta>/memory/`, e não viaja com o clone.

Estado: conferido com `git rev-parse` e `git rev-list` em 06/11/2026. Reverifique antes de agir (§3 do METODO).

## 1. Primeiros passos na máquina nova

1. Clone: `git clone https://github.com/alxchs/KyraFilesCustom-private.git C:\desenv\utils\KyraFiles`.
   Use o mesmo caminho: as regras globais (hooks de git em `c:/desenv/`, temporários, `mkfile`) dependem dele.
2. Confira se o push desta máquina chegou: `git log --oneline -1 origin/main` deve mostrar
   `fix(core): purga residuos de nomes legados, fecha F088 Fase 0 e registra specs F089/F090`.
   Se mostrar `1fcf37136 feat(core): launcher unico...`, o push não foi feito e este arquivo não está no
   clone. Nesse caso, volte à máquina antiga.
3. O ambiente global do Alexandre não está no repositório. Traga da máquina antiga:
   - `~/.claude/CLAUDE.md`, `~/.dev-method/` (METODO.md, templates, git-hooks, ship/), `~/.gemini/GEMINI.md`;
   - `~/bin/` (`mkfile`, `ia`, `avisar`, `trocarConta.ps1`) e `~/.config/avisos/telegram.env`.
     Esse arquivo tem segredo: copie à mão, nunca pelo git;
   - o `includeIf` do `~/.gitconfig` que ativa os hooks em `c:/desenv/`;
   - perfis do Antigravity (`%LOCALAPPDATA%\antigravity-profiles\`). Cada perfil pede login manual uma vez;
   - Electron fixo em `C:\desenv\utils\electron-fixo\` e a variável `ELECTRON_OVERRIDE_DIST_PATH`.
     Isso vale só para projetos Electron, não para o KyraFiles.
4. Restaure a memória do Claude (seção 4 abaixo).
5. Compile com `mkfile d` na raiz (ou `Open-KyraFilesDev.ps1`) e só depois declare o ambiente pronto.

## 2. Estado do git (06/11/2026)

| Ref | Commit | Observação |
|---|---|---|
| `main` local | `fdc2bd894` + este arquivo (squash) | 1 commit à frente de `origin/main` |
| `colab` local | igual à `main` | |
| `origin/main` | `1fcf37136` | instalador 4.3.11 |
| `origin/colab` | `28ecad12a` | 2 commits atrás da `colab` |

- Todas as branches de trabalho (`feature/f052-git-repo-view`, `feature/kyrapet-settings-footer`,
  `feature/launcher-rename-consistency`, `feature/installer-4.3.11`, `claude/f086-xaml-fix`,
  `feature/f088-fase0-residuos`) já estão na `main`. Onde elas ainda diferem, a versão da `main` é a mais nova.
  Não faça rebase nem merge delas: specs novas saem de branch nova a partir da `colab`.
- `backup/*` e `archive/*` são cópias de segurança e só existem nesta máquina: não foram enviadas.
  `archive/agy-20261030` tem 33 commits da AGY que não estão na `colab`. Nunca apague `backup/*` nem
  `archive/*` sem aprovação do Alexandre.
- A worktree `KyraFiles-f086-xaml-fix` foi removida em 06/11. Ficou só a pasta principal.
- **Pendência conhecida do push da `colab`:** o `pre-push` exige exatamente 1 commit novo por push
  na `colab`/`main`. Em 03/11 ele barrou a `colab` com "0 commits novos", porque a `origin/main` já tinha
  os commits (INFERRED: o hook conta contra qualquer ref remota). A `colab` está 2 à frente da `origin/colab`.
  Para destravar é preciso ajustar o hook ou outro caminho aprovado pelo Alexandre. Nunca use `--no-verify`.

## 3. Onde o trabalho parou

- **Produto:** KyraFiles (fork do KyraFiles Community). Versão do pacote `4.3.11.0`. A identidade do pacote
  ainda é `KyraFiles` / `CN=Files`: isso é a F087, que está em rascunho e é de alto risco, porque o Windows
  trata o pacote como app novo.
- **F088, remoção do vínculo com o ancestral:** a Fase 0 (resíduos de nomes legados) está fechada.
  Decisões do Alexandre (03/11): zero referência visual ao KyraFiles Community; ficam só `LICENSE-MIT`,
  `LICENSE-MPL` e os cabeçalhos de copyright (249 arquivos são MPL-2.0 e continuam MPL). O renome do
  `namespace Files.*` (cerca de 1.219 arquivos) ficou para a F091.
  Pontos em aberto: o que conta como "referência visual" (só marca, textos, links e ícones, ou também
  o layout) e se o protocolo `kyrafiles-dev:` fica.
- **F089, fontes não aplicadas** (`docs/specs/F089-fontes-nao-aplicadas/`): spec, plan e tasks em
  RASCUNHO, esperando aprovação do Alexandre. É a próxima a ser executada pela AGY, em `feature/f089-fontes`.
  Achado: `F009-separate-fonts` tem `spec.md` e `plan.md` vazios e o `tasks.md` todo marcado como feito,
  mas o defeito continua presente.
- **F090, escala da interface** (`docs/specs/F090-escala-da-interface/`): RASCUNHO. Só começa depois
  de a F089 voltar para a `colab`.
- **F091, refatoração total** (pastas, classes, namespaces): ainda não escrita.
- **Descarte de 06/11:** uma edição em `AppThemeModeService.cs` estava corrompida (17 linhas trocadas por
  `n`) e foi descartada. O patch está em `docs/agents/descartes/`. Provavelmente era parte da investigação
  da F089. Reaplique só se fizer sentido, e escrito de novo, não pelo patch.
- Fila geral: `docs/agents/FILA-KYRAFILES.md`. Estado curto: `AGENT_CONTEXT.md`.

## 4. Memória do Claude para restaurar

Na máquina nova, o diretório de memória é `~/.claude/projects/<caminho-do-projeto-com-hífens>/memory/`
(para `C:\desenv\utils\KyraFiles` é `c--desenv-utils-KyraFiles`). Peça ao Claude, já na pasta do projeto:
"restaure a memória a partir de docs/agents/handoffs/2026-11-06_1900_claude_passagem-para-outra-maquina.md".
Ele grava um arquivo por item abaixo e o índice `MEMORY.md`.

### 4.1 Conferir relatórios da AGY (feedback)

Em 03/11/2026 a AGY declarou "build limpo" sem que o `KyraFiles.exe` tivesse sido regerado, e declarou
"0 ocorrências de nomes legados" com resíduos em workflows, `.gitignore`, script de dev e `.vcxproj`.
**Por quê:** o Alexandre tem pouca tolerância a isso. Relatório sem prova vale `INFERRED`.
**Como aplicar:** compare a data do exe/DLL em `bin/` com `git log -1 --format=%cd`. Para nomes antigos,
use `git grep -P '(?<![A-Za-z])Files\.App\.(Launcher|OpenDialog|SaveDialog|Server|BackgroundTasks)'`.
Sem o lookbehind há falso positivo, porque `KyraFiles.App.X` contém `KyraFiles.App.X`. Um commit citado no
relatório pode ter outro hash depois do squash: confira com `git diff --stat` contra a `backup/*-pre-merge`.

### 4.2 F088 e specs seguintes (project)

O conteúdo é o da seção 3: decisões da F088, IDs F089 (fontes), F090 (escala) e F091 (refatoração total),
e o achado da F009.
**Por quê:** a renomeação para KyraFiles ficou incompleta e causou "Application not found" ao abrir pastas.
**Como aplicar:** SDD primeiro. Nenhum código da F089/F090 sem spec, plan e tasks aprovados.

### 4.3 Estado do git (project)

O conteúdo é o da seção 2, com a pendência do `pre-push` na `colab`.
**Como aplicar:** reverifique com `git rev-parse --short main colab origin/main origin/colab`
(um ref por vez, se der erro) antes de qualquer push ou merge.
