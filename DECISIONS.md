# DECISIONS

Formato (MASTER_SPEC §37): Context, Problem, Options, Decision, Reason, Trade-offs, Consequences. Decisões maiores podem ir em `docs/decisions/`.

## D-001 — MASTER_SPEC.md é regra de desenvolvimento para Claude e AGY (28/09/2026)

- **Context:** o Alexandre entregou `Files_Custom_MASTER_SPEC.md` e pediu que valha para as próximas etapas, com Claude e AGY alinhados a partir do momento em que a AGY conseguir rodar o app oficial.
- **Decision:** o texto foi salvo sem alterações como `MASTER_SPEC.md` na raiz. `AGENTS.md` aponta para ele. Mudança no spec só com decisão registrada aqui.
- **Consequences:** o spec vale integralmente a partir do gate G0; antes disso valem só as restrições do G0 em `docs/agents/CONTINUITY.md`.

## D-002 — Papéis: spec × regras globais do Alexandre

- **Problem:** o spec (§20, §23) trata Claude como Chief/Architect e a AGY como executora; as regras globais do Alexandre proíbem o Claude de fazer o trabalho da AGY por falta de cota.
- **Decision:** prevalece o mais restritivo. Claude: arquitetura, briefs, critérios, revisão, docs. AGY: exploração, implementação, teste, build. Claude só escreve o código da AGY se ela falhar por motivo que não seja cota, após 5+ tentativas, avisando o Alexandre.
- **Consequences:** ver "Quando um agente para" em `docs/agents/CONTINUITY.md`.

## D-003 — Continuidade por arquivos e commits, não por conversa

- **Decision:** estado em `AGENT_CONTEXT.md`, `STATUS.md`, handoffs e task briefs em `docs/agents/`; checkpoints em commits locais. Protocolo completo em `docs/agents/CONTINUITY.md`.
- **Reason:** cota acaba sem aviso; o que só estava no chat se perde e o agente seguinte tenderia a preencher a lacuna com suposição.

## D-004 — Gate G0 antes de qualquer feature

- **Decision:** nada de F001+ antes do baseline (§16). Tarefa da AGY agora: compilar/rodar o Files oficial.

## D-005 — Como cada agente carrega estas regras

- **Decision:** `AGENTS.md` recebe uma seção curta no fim do arquivo (um único trecho, para facilitar merge com o upstream, §13). `.claude/CLAUDE.md` importa `@AGENTS.md` e `@AGENT_CONTEXT.md`. `GEMINI.md` na raiz repete o apontamento para a AGY.
- **Trade-off:** NOT TESTED se a AGY (`agy.exe`) lê `AGENTS.md`/`GEMINI.md` do projeto; até confirmar, o prompt de disparo da AGY deve mandar ler `AGENTS.md` e `AGENT_CONTEXT.md` explicitamente.

## D-006 — Repositório: fork `alxchs/FilesCustom` ancorado no upstream v4.2.9 (28/09/2026)

- **Context:** a pasta não era um repositório git; o spec (§13) pede fork sustentável.
- **Decision:** fork de `files-community/Files` criado em `https://github.com/alxchs/FilesCustom` (nome do §36). Local: `origin` = fork, `upstream` = Files oficial; `main` parte do commit `99951c66` (Build: v4.2.9), o único que bate com a árvore existente.
- **Reason:** dá diff real contra o upstream e permite `git merge upstream/main` depois; separa ajustes da AGY do código oficial.
- **Trade-offs:** GitHub não permite fork privado de repositório público; o fork é **público**. Nada foi enviado. Um `git push` publicaria `MASTER_SPEC.md`, `STATUS.md` etc.; só com confirmação do Alexandre (regra global). Alternativa: repositório privado sem vínculo de fork.
- **Consequences:** o histórico do upstream vem junto (baixado localmente). `core.symlinks=false` porque o `CLAUDE.md` do upstream é symlink.

## D-007 — Desempenho: a referência é o OneCommander (01/10/2026)

- **Context:** o Files Dev abriu (G0 parcial) e o Alexandre o achou lento em quase tudo: abrir o app, entrar em pastas, rolagem/miniaturas, cliques e menus. Ele quer substituir o OneCommander.
- **Decision (Alexandre):** a meta de desempenho é ficar melhor que o OneCommander nas mesmas ações. Comparar com o Files oficial não interessa.
- **Consequences:** desempenho vira trilha própria (F007), medida pelos scripts de `tools/perf/`, com linha de base em `tools/perf/README.md`. Cada otimização precisa mostrar o antes/depois com os mesmos scripts. Continua valendo D-002 (Claude define brief e critério; AGY implementa) e o gate G0.

## D-008 — Base migrada do upstream v4.2.9 para upstream/main (03/10/2026)

- **Context:** o `main` do fork no GitHub foi sincronizado com o upstream (127 commits à frente de v4.2.9, nenhum do Alexandre ou da AGY). O local estava em v4.2.9.
- **Decision (Alexandre):** trazer o upstream agora e ajustar. Tag de segurança `tag_v4.2.9.0_build-zero-warnings-abertura-appx-perf-onecommander_salvo` criada antes (publicada em origin).
- **Reason:** quanto mais tarde, mais conflito. Antes de F001+ a base precisa ser a atual.
- **Trade-offs:** o baseline do G0 e as medições de `tools/perf/` foram feitos no v4.2.9 e precisam ser refeitos nesta base. 7 conflitos, todos resolvidos com a versão do upstream (as supressões de warning da AGY nesses pontos foram descartadas e reavaliadas). Roslyn continua fixado em 5.6.0 porque o compilador do SDK 10.0.301 é 5.6.0 (o 5.9.0 do upstream dá CS9057).
- **Consequences:** voltar atrás = `git reset --hard tag_v4.2.9.0_build-zero-warnings-abertura-appx-perf-onecommander_salvo`.

## PENDING DECISION

(Dúvidas que a AGY encontrar enquanto o Claude estiver parado: contexto, opções, a escolha que ela faria e por quê.)

## D-009 — Requisitos adicionais: densidade, fontes, colunas do Explorer e motor de busca (03/10/2026)

- **Context:** o Alexandre pediu quatro itens de configuração: modo compacto (menor entrelinhamento), fonte das áreas fixas separada da fonte dos resultados, todas as colunas que o Windows Explorer oferece, e escolher se o F3 usa a busca do Files, o Agent Ransack ou o Everything, sem abrir a tela deles. Autorizou reordenar tudo.
- **Decision:** acrescentados ao `MASTER_SPEC.md` como §46 (F008 a F011), com briefs em `docs/agents/tasks/`. Nova ordem de execução no §46 e no `PLAYBOOK.md`. F006 passa a ser a infraestrutura de busca e a F011 é a escolha de motor.
- **Reason:** são pedidos expressos, logo têm "razão legítima" para configuração (§33). F008/F009 são pequenos e de baixo risco; F010 exige modelo dinâmico de colunas e a F011 depende de `ISearchProvider`.
- **Trade-offs:** o spec original (D-001) era "verbatim"; esta seção é a primeira alteração, registrada aqui. Colunas dinâmicas (F010) e busca externa (F011) aumentam o diff contra o upstream; mitigação: arquivos próprios e fases com gate do Claude.
- **Consequences:** a lista no §29 está superada pelo §46. Nenhuma configuração muda o comportamento padrão.
- **Fatos verificados em 03/10/2026:** F3 e Ctrl+F já estão em `SearchAction`; linha mínima do Details é 28 px; fonte global única `AppThemeFontFamily`; Everything 1.4.1.1032 e Agent Ransack 9.2.3425.1 (com `flpsearch.exe`) instalados. O que é INFERRED/NOT TESTED está marcado nos briefs.


## D-010 — Claude corrigiu 3 erros de compilacao do wip da F001 (03/10/2026)

- **Context:** o wip da AGY (`774f5eb64`) nunca tinha sido compilado e `mkfile release` falhou com 3 erros em `BaseGroupableLayoutPage.cs`. O disparo da AGY foi bloqueado pelo classificador do Claude Code mesmo com a permissao do Alexandre no chat, e ele nao soube criar a regra de permissao. O Alexandre queria um exe testavel.
- **Options:** (1) esperar a AGY; (2) Claude corrige so os erros de compilacao; (3) reabrir o exe antigo.
- **Decision:** opcao 2, autorizada explicitamente pelo Alexandre em 03/10/2026 ("Tem minha autorizacao"). Escopo: `using Windows.Storage;`, inicializar `ActiveRenameParts` com `FileNameParts` vazio (no campo e em `ResetRenameState`) e `[DynamicWindowsRuntimeCast(typeof(TextBox))]` em `RenameTextBox_SelectionChanged` (warning CsWinRT1034). Nada mais.
- **Reason:** a falha da AGY nao foi cota nem 5 tentativas; foi bloqueio de disparo. Excecao pontual e autorizada a regra "a AGY faz, o Claude nunca termina por ela".
- **Trade-offs:** os achados MAJOR de comportamento (tecla End destrava a extensao; `showExtensionDialog` suprimido em arquivo sem extensao) **continuam abertos** na AGY, em `docs/agents/reviews/2026-10-03_Claude_revisao-F001-wip.md`.
- **Consequences:** `mkfile release src\Files.App\Files.App.csproj` = 0 Warning(s), 0 Error(s) (03/10/2026 18:29). App aberto por `Open-FilesDev.ps1`.

## D-011 — F001: Correcao de comportamento (End, dialogo, Tab, clique e edge cases) (03/10/2026)

- **Context:** Revisao `docs/agents/reviews/2026-10-03_Claude_revisao-F001-wip.md` apontou 2 achados MAJOR restantes (End e clique fora da extensao destravavam a extensao; `showExtensionDialog` suprimido para arquivos sem extensao) e MINORs (Tab em arquivo sem extensao, `.user.js` na lista de compostas, cobertura de testes do helper).
- **Decision:**
  1. No `RenameTextBox_KeyDown`, a tecla `End` e `Down` limitam a selecao a `nameLen` quando a extensao estiver travada; em `SelectionChanged`, qualquer posicao do cursor alem de `nameLen` e truncada de volta para `nameLen` a menos que tenha havido clique de mouse explicitamente dentro dos limites da extensao (`PointerPressed` com medicao do texto da extensao).
  2. Em `CommitRenameAsync`, o dialogo de extensao so e suprimido se `IsExtensionDeliberatelyModified == true`. Arquivo sem extensao (`arquivo` -> `arquivo.exe`) preserva `IsExtensionDeliberatelyModified = false` e exibe o dialogo modal Sim/Nao.
  3. `Tab` em item sem extensao (`!ActiveRenameParts.HasExtension`) e no-op (`e.Handled = true`).
  4. `.user.js` removido de `CompoundExtensions` (mantendo apenas compressoes `.tar.*`).
  5. Casos de borda no helper: dotfiles com multiplos pontos (`.env.local`, `.tar.gz` sem prefixo) tratam o ultimo segmento como extensao (`.local`, `.gz`), permitindo editar o nome base e protegendo a extensao final. Nomes terminando com ponto (`arquivo.txt.`) tem extensao vazia. Testes do helper ampliados para 20 casos cobrindo todos os cenarios.
  6. `Tab` seleciona a extensao sem o ponto inicial para agilizar a substituicao direta.
- **Consequences:** `mkfile release src\Files.App\Files.App.csproj` resulta em 0 Warning(s) e 0 Error(s). Todos os 20 casos de teste passam com sucesso.



## D-012 — Spec-Driven Development (SDD) passa a ser a metodologia de features (04/10/2026)

- **Context:** o Alexandre pediu "specs para metodologia SDD" a partir de agora (interpretado como Spec-Driven Development; o pedido veio escrito "ssd"). Até aqui cada feature tinha um brief único (TASK/ACCEPTANCE/ARQUIVOS) e o `MASTER_SPEC.md` como spec-mãe.
- **Decision:** `METODO.md` §6 reescrito: por feature, `docs/specs/<ID>-<nome>/` com `spec.md` (o quê e por quê, cenários `AC-n`, requisitos `FR-n`), `plan.md` (como) e `tasks.md` (o que a AGY executa); portões spec, plan, tasks, código; rastreabilidade por `AC-n` no handoff e na nota de liberação. Modelos em `~/.dev-method/templates/`, `Novo-Projeto.ps1` cria `docs/specs`. Primeira feature: F005.
- **Reason:** o brief misturava comportamento e implementação, e a verificação ficava sem critério numerado; a F001 mostrou o custo (defeitos de comportamento só achados na revisão).
- **Trade-offs:** mais três arquivos por feature e um portão a mais. Mitigação: specs curtas, briefs de exploração e medição continuam como estão, e features já aprovadas (F001) terminam pelo brief.
- **Consequences:** a AGY não implementa sem `tasks.md` aprovado. Descoberta no caminho: o projeto `tests/Files.App.UnitTests` citado nos briefs **não existe** (CONFIRMED por `git ls-files`), então teste unitário exige script `pwsh` ou projeto novo; decisão fica na T2 da F005.

## D-013 — Teste da função pura da F005 via script PowerShell (04/10/2026)

- **Context:** a tarefa T2 da F005 exige teste automatizado dos 6 cenários de alternância (AC-1 a AC-6) e registro em DECISIONS.md da escolha entre script `pwsh` e projeto novo `tests/Files.Custom.Tests`.
- **Problem:** o projeto de testes unitários não existe no repositório; criar projeto novo em `Files.slnx` introduz churn no build e risco de divergência com o upstream.
- **Options:** (1) Criar um projeto `tests/Files.Custom.Tests.csproj` adicionando-o à solution; (2) Criar script `tests/test-pane-toggle-helper.ps1` que compila dinamicamente a função com `Add-Type` e valida os casos em milissegundos, replicando o padrão já aprovado de `tests/test-rename-helper.ps1`.
- **Decision:** opção 2 (`tests/test-pane-toggle-helper.ps1`).
- **Reason:** atende 100% ao critério de aceite com diff mínimo, sem tocar em arquivos de projeto (.csproj / .slnx), mantendo compatibilidade total com upstream e execução instantânea.
- **Consequences:** os 6 casos AC-1..AC-6 são validados executando `pwsh -File tests/test-pane-toggle-helper.ps1` com 6/6 aprovados.

## D-015 — Projetos C++ do upstream voltam a ser compilados; F012 usa o launcher do upstream (09/10/2026)

- **Context:** em *Configurações > Avançado*, a opção de usar o Files como diálogo de arquivos não liga (relato do Alexandre, 08/10/2026). CONFIRMED: ela registra seis DLLs C++/ATL (`Files.App.OpenDialog*.dll`, `Files.App.SaveDialog*.dll`) que nunca existiram neste fork (`Get-ChildItem src\Files.App\Assets\FilesOpenDialog`, layout AppX e LocalState sem nenhuma DLL), e o `catch { }` escondia a falha. Causa raiz: o commit `e3b8b0e38` (01/10/2026) tirou os 5 projetos C++ do `Files.slnx` (CONFIRMED por `git show 0e3c17ca4:Files.slnx`); o motivo provável (INFERRED, a mensagem do commit não diz) é que o Visual Studio 2022 da máquina não tinha C++ (CONFIRMED: sem `MSBuild\Microsoft\VC\v170\Platforms`; `vswhere -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64` vazio; nenhum `atlbase.h` no VS 2022, no VS x86, nos Windows Kits nem no `D:\`). Pelo mesmo motivo, a F012 (AGY) trocou o launcher C++ do upstream por um trampolim C# compilado com `csc`.
- **Options:** (1) manter o trampolim C# e desistir dos diálogos; (2) instalar C++ e ATL no VS 2022 e voltar a compilar os projetos do upstream; (3) baixar as DLLs prontas de uma release oficial do Files.
- **Decision:** (2), autorizada pelo Alexandre em 09/10/2026 (instalação de `Microsoft.VisualStudio.Workload.NativeDesktop`, `VC.Tools.x86.x64` e `VC.ATL`). Os projetos são compilados por `tools/Build-Native.ps1` (o `mkfile` só roda `dotnet build`, que não compila `.vcxproj`), com o toolset e o SDK mais novos instalados (o upstream pede v145, do VS 18). Na F012 vale o launcher do upstream (`src/Files.App.Launcher`), com dois ajustes para a AC-6: sem caixa de mensagem e sem UAC ao reverter o registro (o `UnsetFilesAsDefault.reg` desta branch só apaga chaves do HKCU), esperando a reversão antes de abrir o Explorer. O trampolim C# (`tools/launcher/Program.cs`) sai.
- **Reason:** o launcher do upstream já trata `/select`, janelas do Explorer já abertas, pastas especiais e o Files desinstalado; reescrevê-lo duplicava código e perdia essas regras. (3) traria binário de fora sem passar pelo nosso build.
- **Trade-offs:** o build completo passa a exigir o C++ do Visual Studio e um passo a mais (`tools/Build-Native.ps1`); o arquivo `FilesLauncher.cpp` do upstream ganha um diff pequeno.
- **Consequences:** `tools/Create-SelfContained-Release.ps1` recusa gerar pacote sem os binários nativos no layout AppX.

## D-016 — Instância de diálogo de arquivos isolada da sessão do usuário (09/10/2026)

- **Context:** com o Files como diálogo "Abrir", escolher o arquivo e fechar devolvia "Cancelar" ao programa chamador em parte das vezes (CONFIRMED: teste direto com `-outputpath`, 2 falhas em 6; pelo `OpenFileDialog` de outro processo, falha na 1ª tentativa). Causa (CONFIRMED lendo `App.xaml.cs`, `MainWindow.xaml.cs`, `MainPageViewModel.cs`): a instância de diálogo restaurava a sessão de abas do usuário (o `-outputpath` só era lido depois da primeira navegação), e no fechamento salvava a sessão **antes** de ler a seleção. A cada uso a sessão do usuário ganhava uma aba (6 abas depois dos testes, 4 delas a pasta do diálogo). Além disso, o código do upstream saía sem sinalizar o evento `FILEDIALOG` quando não achava a aba atual, e o programa chamador espera esse evento sem prazo.
- **Options:** (1) só ler a seleção antes de salvar a sessão; (2) tratar a instância de diálogo como isolada: não restaura nem salva sessão, lê a seleção primeiro, sempre sinaliza o chamador, não "estaciona" em segundo plano e não recebe aberturas de outros programas.
- **Decision:** (2). Arquivos tocados além do `plan.md` da F012: `App.xaml.cs`, `MainWindow.xaml.cs`, `ViewModels/MainPageViewModel.cs`, `Helpers/Application/AppLifecycleHelper.cs` (só no caminho em que `App.OutputPath` está definido).
- **Reason:** (1) deixava a sessão poluída e o diálogo abrindo com as abas do usuário; o defeito é o mesmo nos dois sintomas.
- **Trade-offs:** diff pequeno em quatro arquivos do upstream.
- **Consequences:** 5 em 5 pelo teste direto e 3 em 3 pelo diálogo "Abrir", 1 em 1 pelo "Salvar", sessão inalterada (`tools/perf/f012_file_dialog_return.ps1`). O diálogo abre em ~2,6 s em vez de 4 a 6 s e devolve em ~0,4 s após fechar, em vez de 1,7 s.

## D-017 — PENDING DECISION: `explorer.exe <pasta>` direto (AC-4 da F012) (09/10/2026)

- **Context:** com o Files como gerenciador padrão, abrir pasta pelo shell (`start <pasta>`, `Win + R`, duplo clique) abre no Files (CONFIRMED: `Start-Process <pasta>` levou a janela do Files para a pasta). Mas `explorer.exe "<pasta>"` chamado direto abre o Explorer do Windows (CONFIRMED: janela `CabinetWClass` do processo `explorer`, nenhuma do Files). O `SetFilesAsDefault.reg` do upstream só troca `Folder\shell\open`, `Folder\shell\explore` e o `CLSID` do `Win + E`; uma chamada ao `explorer.exe` não passa por eles.
- **Options:** (1) aceitar: a AC-4 vale para `start` e `Win + R`, não para `explorer.exe` direto (o upstream é assim); (2) interceptar `explorer.exe` por `Image File Execution Options` (`Debugger`), que é invasivo, exige administrador e pode travar o shell se o launcher falhar; (3) outra forma ainda não estudada.
- **Escolha que o Claude faria:** (1), mudando a AC-4 na spec. **Pendente:** decisão do Alexandre.
- **Atualização (09/10/2026):** o Alexandre testou `Win + E` com teclado real e abriu o Explorer (AC-1 também falha; reproduzido por `SendInput`, com qualquer valor de `LaunchTo`). O verbo padrão em `Directory\shell` (técnica do OneCommander, vista nas chaves que ele deixou no HKCU) também não pega o `explorer.exe <pasta>` (testado e revertido; registro idêntico ao backup). O Alexandre pediu o comportamento do OneCommander e similares, sem administrador.
- **Decision:** opção (3): enquanto o Files estiver em execução, ele observa as janelas do Explorer que abrem (`SetWinEventHook`, fora do processo do Explorer), deixa a nova janela transparente, lê a localização e a seleção pela `ShellWindows`, fecha a janela e abre a localização no Files; localização que o Files não exibe volta a ficar visível. Protótipo (`SetWinEventHook` + `ShellWindows`): Win + E, `explorer.exe <pasta>` e `/select` redirecionados, janela do Explorer visível em no máximo 2 amostras de 10 ms.
- **Trade-offs:** só funciona com o Files em execução (com ele fechado, o Explorer abre, o que é seguro); se o Files cair com uma janela transparente pendente, essa janela fica invisível até ser fechada (mitigação: tempo máximo curto e restauração em qualquer falha).


## D-019 — Índice de busca próprio, em dois modos, sem duplicatas (F015) (11/10/2026)

- **Context:** a busca por nome usa o motor escolhido na F011. O ativo era o Agent Ransack (`SearchEnginePreference: 1`), que varre o disco a cada busca, só mostra resultado no fim e toca o disco item por item. Medido em 11/10/2026: 8,3 s em `C:\desenv` (86.954 arquivos), contra 5,9 s de um `Get-ChildItem -Recurse`. O Everything está instalado, mas o provedor depende do `es.exe` (ausente em `C:\Program Files\Everything\`) e cai em silêncio no motor nativo. Queixa do Alexandre: "extremamente lento, parecendo trabalho de principiante".
- **Options:** (A) componente com privilégio de administrador lendo a estrutura do NTFS (como o Everything); (B) índice sem privilégio, por varredura e vigilância de pastas; (C) os dois; (D) depender do Everything.
- **Decision:** (C), pelo Alexandre, que também descartou (D): "não podemos depender do Everything, precisamos fazer igual ou melhor". Regras dele: todos os discos; `subst` e links não podem gerar indexação nem resultado duplicado. Na revisão da spec: unidades de rede ficam fora do índice (busca por varredura na hora); link físico aparece uma vez, no local original.
- **Reason:** só (A) iguala o Everything em indexar e acompanhar mudanças com o app fechado; só (B) funciona sem administrador e em volumes não NTFS.
- **Trade-offs:** dois caminhos de indexação para manter; um componente instalado com UAC.
- **Consequences:** spec `docs/specs/F015-indice-de-busca/`. A escolha de motor da F011 deixa de valer para busca por nome; o F3 da F013 (Agent Ransack) continua. Numeração: o `AGENT_CONTEXT.md` da F014 cita uma "D-018" que não está neste arquivo; para não colidir, esta é a D-019.
