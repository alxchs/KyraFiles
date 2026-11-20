# DECISIONS

Formato (MASTER_SPEC §37): Context, Problem, Options, Decision, Reason, Trade-offs, Consequences. Decisões maiores podem ir em `docs/decisions/`.

## D-001 — MASTER_SPEC.md é regra de desenvolvimento para Claude e AGY (28/09/2026)

- **Context:** o Alexandre entregou `MASTER_SPEC.md` e pediu que valha para as próximas etapas, com Claude e AGY alinhados a partir do momento em que a AGY conseguir rodar o app oficial.
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

- **Decision:** nada de F001+ antes do baseline (§16). Tarefa da AGY agora: compilar/rodar o KyraFiles oficial.

## D-005 — Como cada agente carrega estas regras

- **Decision:** `AGENTS.md` recebe uma seção curta no fim do arquivo (um único trecho, para facilitar merge com o upstream, §13). `.claude/CLAUDE.md` importa `@AGENTS.md` e `@AGENT_CONTEXT.md`. `GEMINI.md` na raiz repete o apontamento para a AGY.
- **Trade-off:** NOT TESTED se a AGY (`agy.exe`) lê `AGENTS.md`/`GEMINI.md` do projeto; até confirmar, o prompt de disparo da AGY deve mandar ler `AGENTS.md` e `AGENT_CONTEXT.md` explicitamente.

## D-006 — Repositório: fork `alxchs/KyraFiles` ancorado no upstream v4.2.9 (28/09/2026)

- **Context:** a pasta não era um repositório git; o spec (§13) pede fork sustentável.
- **Decision:** fork de `alxchs/KyraFiles` criado em `https://github.com/alxchs/KyraFilesCustom` (nome do §36). Local: `origin` = fork, `upstream` = KyraFiles oficial; `main` parte do commit `99951c66` (Build: v4.2.9), o único que bate com a árvore existente.
- **Reason:** dá diff real contra o upstream e permite `git merge upstream/main` depois; separa ajustes da AGY do código oficial.
- **Trade-offs:** GitHub não permite fork privado de repositório público; o fork é **público**. Nada foi enviado. Um `git push` publicaria `MASTER_SPEC.md`, `STATUS.md` etc.; só com confirmação do Alexandre (regra global). Alternativa: repositório privado sem vínculo de fork.
- **Consequences:** o histórico do upstream vem junto (baixado localmente). `core.symlinks=false` porque o `CLAUDE.md` do upstream é symlink.

## D-007 — Desempenho: a referência é o OneCommander (01/10/2026)

- **Context:** o KyraFiles abriu (G0 parcial) e o Alexandre o achou lento em quase tudo: abrir o app, entrar em pastas, rolagem/miniaturas, cliques e menus. Ele quer substituir o OneCommander.
- **Decision (Alexandre):** a meta de desempenho é ficar melhor que o OneCommander nas mesmas ações. Comparar com o KyraFiles oficial não interessa.
- **Consequences:** desempenho vira trilha própria (F007), medida pelos scripts de `tools/perf/`, com linha de base em `tools/perf/README.md`. Cada otimização precisa mostrar o antes/depois com os mesmos scripts. Continua valendo D-002 (Claude define brief e critério; AGY implementa) e o gate G0.

## D-008 — Base migrada do upstream v4.2.9 para upstream/main (03/10/2026)

- **Context:** o `main` do fork no GitHub foi sincronizado com o upstream (127 commits à frente de v4.2.9, nenhum do Alexandre ou da AGY). O local estava em v4.2.9.
- **Decision (Alexandre):** trazer o upstream agora e ajustar. Tag de segurança `tag_v4.2.9.0_build-zero-warnings-abertura-appx-perf-onecommander_salvo` criada antes (publicada em origin).
- **Reason:** quanto mais tarde, mais conflito. Antes de F001+ a base precisa ser a atual.
- **Trade-offs:** o baseline do G0 e as medições de `tools/perf/` foram feitos no v4.2.9 e precisam ser refeitos nesta base. 7 conflitos, todos resolvidos com a versão do upstream (as supressões de warning da AGY nesses pontos foram descartadas e reavaliadas). Roslyn continua fixado em 5.6.0 porque o compilador do SDK 10.0.301 é 5.6.0 (o 5.9.0 do upstream dá CS9057).
- **Consequences:** voltar atrás = `git reset --hard tag_v4.2.9.0_build-zero-warnings-abertura-appx-perf-onecommander_salvo`.

## PENDING DECISION

*(Nenhuma decisão bloqueante pendente em 18/11/2026.)*

## D-TRAIN-1 — Adoção de Release Trains (Lotes de 3–5 SPECs) e Centralização na HELOIZE-PC (18/11/2026)

- **Context:** O fluxo serial 1-a-1 (1 SPEC → 1 branch → 1 release → 1 validação visual) gerou 26 SPECs acumuladas em `in_validation` (120 pts) e overhead documental desproporcional para entregas pequenas. Além disso, o desenvolvimento foi centralizado integralmente na máquina `HELOIZE-PC`, com testes liberados o tempo todo.
- **Decision (Alexandre, 18/11/2026):**
  1. Adotar **Release Trains (Lotes de 3 a 5 SPECs / ~15–20 pts)** com 1 build Release x64 (`0/0`), 1 bateria consolidada de testes/UIA/Gate F021/Win+E e 1 instalador por lote (mantendo zero regressão de performance, evidência honesta e histórico linear na `main`).
  2. Executar **Lote 0 (Faxina)**: invalidar/arquivar **F015** (substituída por F021+F023), encerrar **F088** (absorvida pela F091), formalizar aprovação retroativa de **F014** e **F020**, reconciliar **F058**/**F070**, regenerar automaticamente `SPECS.md`/`SPEC-INDEX.md` via `tools/Sync-ProductionDocs.ps1` e consolidar a validação das SPECs antigas em lote único.
  3. Na `HELOIZE-PC`, testes podem ser executados o tempo todo (sem espera pelas 23:59, preservando a regra de não roubar foco da janela ativa de digitação).
- **Consequences:** Menos ciclos de cerimônia por ponto entregue, eliminação de ociosidade entre IAs e fechamento em massa do gargalo de validação sem perda de qualidade.

## D-F060-2 — F060 Onda B (SFTP e Script) movida para backlog final (18/11/2026)

- **Context:** Na F060 (`SPEC-0055`), a Onda A (`M1` busca por frase, `M2` editor hexadecimal, `M5` dividir/combinar arquivos) já está implementada e com 78/78 testes unitários aprovados na v4.3.21. A Onda B (`M3` SFTP e `M4` Motor de Scripts) dependia de definição das ADRs 007/008.
- **Decision (Alexandre, 18/11/2026):** Mover Onda B (SFTP + Script) para o backlog dos lotes finais, desbloqueando o fechamento da Onda A na validação em lote da v4.3.21.
- **Consequences:** F060 Onda A segue para validação visual consolidada sem depender de SFTP/Script.

## D-F090-1 — Escopo de T4 da F090: Popups (MenuFlyout/ToolTip/ContentDialog) e atalhos Ctrl++/Ctrl+- (11/11/2026 — DECIDIDA em 18/11/2026)

- **Context:** As janelas secundárias do app (`SettingsDialog`, `ToolbarCustomizationDialog`, `FilePropertiesHelpers`, `HexEditorWindow`) passaram a receber `IUiScaleService.ApplyScale`, cobrindo `FR-2` e a parte de janela de Configurações do `AC-4`. Ficam em aberto as pendências 2 e 3 de `docs/specs/F090-escala-da-interface/spec.md` (T4):
  1. **Menus, dicas e diálogos (`PopupRoot` / `MenuFlyout` / `ToolTip` / `ContentDialog`):** em WinUI 3, `PopupRoot` é irmão de `rootFrame` e não herda `Scale` aplicado na raiz da janela; escalar popups manualmente sem teste visual pode deslocar âncoras ou cortar menus na borda da tela.
  2. **Atalhos `Ctrl++` / `Ctrl+-`:** hoje `LayoutAction.cs` / `BaseLayoutPage.cs` já usam zoom de lista/grade (`ZoomIn` / `ZoomOut`).
- **Options:**
  - **(A) Conservadora (v1):** manter em v1 a escala aplicada em todas as janelas (`MainWindow`, `SettingsDialog`, `ToolbarCustomizationDialog`, `PropertiesWindow`, `HexEditorWindow`), documentando em `AC-4` que popups transitórios nativos seguem a escala do Windows até decisão pela Opção C do `plan.md`, e não sobrescrever `Ctrl++`/`Ctrl+-` do zoom da lista (ou usar atalho distinto, ex. `Ctrl+Alt++`/`Ctrl+Alt+-` se desejado).
  - **(B) Completa na T4:** implementar escalonamento de popups abertos via `VisualTreeHelper.GetOpenPopupsForXamlRoot` (Opção C do `plan.md`) e/ou reatribuir `Ctrl++`/`Ctrl+-` para escala global.
- **Decision (Alexandre, 18/11/2026):** **Opção (A) Conservadora (v1)** aprovada. Popups transitórios permanecem sob escala nativa do Windows na v1 e `Ctrl++`/`Ctrl+-` continuam preservados para o zoom da lista/grade.
- **Consequences:** T4 encerrada dentro do escopo v1; F090 segue apenas para prova visual consolidada no Lote 0.

## D-009 — Requisitos adicionais: densidade, fontes, colunas do Explorer e motor de busca (03/10/2026)

- **Context:** o Alexandre pediu quatro itens de configuração: modo compacto (menor entrelinhamento), fonte das áreas fixas separada da fonte dos resultados, todas as colunas que o Windows Explorer oferece, e escolher se o F3 usa a busca do KyraFiles, o Agent Ransack ou o Everything, sem abrir a tela deles. Autorizou reordenar tudo.
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
- **Consequences:** `mkfile release src\KyraFiles.App\KyraFiles.App.csproj` = 0 Warning(s), 0 Error(s) (03/10/2026 18:29). App aberto por `Open-KyraFiles.ps1`.

## D-011 — F001: Correcao de comportamento (End, dialogo, Tab, clique e edge cases) (03/10/2026)

- **Context:** Revisao `docs/agents/reviews/2026-10-03_Claude_revisao-F001-wip.md` apontou 2 achados MAJOR restantes (End e clique fora da extensao destravavam a extensao; `showExtensionDialog` suprimido para arquivos sem extensao) e MINORs (Tab em arquivo sem extensao, `.user.js` na lista de compostas, cobertura de testes do helper).
- **Decision:**
  1. No `RenameTextBox_KeyDown`, a tecla `End` e `Down` limitam a selecao a `nameLen` quando a extensao estiver travada; em `SelectionChanged`, qualquer posicao do cursor alem de `nameLen` e truncada de volta para `nameLen` a menos que tenha havido clique de mouse explicitamente dentro dos limites da extensao (`PointerPressed` com medicao do texto da extensao).
  2. Em `CommitRenameAsync`, o dialogo de extensao so e suprimido se `IsExtensionDeliberatelyModified == true`. Arquivo sem extensao (`arquivo` -> `arquivo.exe`) preserva `IsExtensionDeliberatelyModified = false` e exibe o dialogo modal Sim/Nao.
  3. `Tab` em item sem extensao (`!ActiveRenameParts.HasExtension`) e no-op (`e.Handled = true`).
  4. `.user.js` removido de `CompoundExtensions` (mantendo apenas compressoes `.tar.*`).
  5. Casos de borda no helper: dotfiles com multiplos pontos (`.env.local`, `.tar.gz` sem prefixo) tratam o ultimo segmento como extensao (`.local`, `.gz`), permitindo editar o nome base e protegendo a extensao final. Nomes terminando com ponto (`arquivo.txt.`) tem extensao vazia. Testes do helper ampliados para 20 casos cobrindo todos os cenarios.
  6. `Tab` seleciona a extensao sem o ponto inicial para agilizar a substituicao direta.
- **Consequences:** `mkfile release src\KyraFiles.App\KyraFiles.App.csproj` resulta em 0 Warning(s) e 0 Error(s). Todos os 20 casos de teste passam com sucesso.



## D-012 — Spec-Driven Development (SDD) passa a ser a metodologia de features (04/10/2026)

- **Context:** o Alexandre pediu "specs para metodologia SDD" a partir de agora (interpretado como Spec-Driven Development; o pedido veio escrito "ssd"). Até aqui cada feature tinha um brief único (TASK/ACCEPTANCE/ARQUIVOS) e o `MASTER_SPEC.md` como spec-mãe.
- **Decision:** `METODO.md` §6 reescrito: por feature, `docs/specs/<ID>-<nome>/` com `spec.md` (o quê e por quê, cenários `AC-n`, requisitos `FR-n`), `plan.md` (como) e `tasks.md` (o que a AGY executa); portões spec, plan, tasks, código; rastreabilidade por `AC-n` no handoff e na nota de liberação. Modelos em `~/.dev-method/templates/`, `Novo-Projeto.ps1` cria `docs/specs`. Primeira feature: F005.
- **Reason:** o brief misturava comportamento e implementação, e a verificação ficava sem critério numerado; a F001 mostrou o custo (defeitos de comportamento só achados na revisão).
- **Trade-offs:** mais três arquivos por feature e um portão a mais. Mitigação: specs curtas, briefs de exploração e medição continuam como estão, e features já aprovadas (F001) terminam pelo brief.
- **Consequences:** a AGY não implementa sem `tasks.md` aprovado. Descoberta no caminho: o projeto `tests/KyraFiles.App.UnitTests` citado nos briefs **não existe** (CONFIRMED por `git ls-files`), então teste unitário exige script `pwsh` ou projeto novo; decisão fica na T2 da F005.

## D-013 — Teste da função pura da F005 via script PowerShell (04/10/2026)

- **Context:** a tarefa T2 da F005 exige teste automatizado dos 6 cenários de alternância (AC-1 a AC-6) e registro em DECISIONS.md da escolha entre script `pwsh` e projeto novo `tests/KyraFiles.Custom.Tests`.
- **Problem:** o projeto de testes unitários não existe no repositório; criar projeto novo em `KyraFiles.slnx` introduz churn no build e risco de divergência com o upstream.
- **Options:** (1) Criar um projeto `tests/KyraFiles.Custom.Tests.csproj` adicionando-o à solution; (2) Criar script `tests/test-pane-toggle-helper.ps1` que compila dinamicamente a função com `Add-Type` e valida os casos em milissegundos, replicando o padrão já aprovado de `tests/test-rename-helper.ps1`.
- **Decision:** opção 2 (`tests/test-pane-toggle-helper.ps1`).
- **Reason:** atende 100% ao critério de aceite com diff mínimo, sem tocar em arquivos de projeto (.csproj / .slnx), mantendo compatibilidade total com upstream e execução instantânea.
- **Consequences:** os 6 casos AC-1..AC-6 são validados executando `pwsh -File tests/test-pane-toggle-helper.ps1` com 6/6 aprovados.

## D-015 — Projetos C++ do upstream voltam a ser compilados; F012 usa o launcher do upstream (09/10/2026)

- **Context:** em *Configurações > Avançado*, a opção de usar o KyraFiles como diálogo de arquivos não liga (relato do Alexandre, 08/10/2026). CONFIRMED: ela registra seis DLLs C++/ATL (`KyraFiles.App.OpenDialog*.dll`, `KyraFiles.App.SaveDialog*.dll`) que nunca existiram neste fork (`Get-ChildItem src\KyraFiles.App\Assets\KyraFilesOpenDialog`, layout AppX e LocalState sem nenhuma DLL), e o `catch { }` escondia a falha. Causa raiz: o commit `e3b8b0e38` (01/10/2026) tirou os 5 projetos C++ do `KyraFiles.slnx` (CONFIRMED por `git show 0e3c17ca4:KyraFiles.slnx`); o motivo provável (INFERRED, a mensagem do commit não diz) é que o Visual Studio 2022 da máquina não tinha C++ (CONFIRMED: sem `MSBuild\Microsoft\VC\v170\Platforms`; `vswhere -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64` vazio; nenhum `atlbase.h` no VS 2022, no VS x86, nos Windows Kits nem no `D:\`). Pelo mesmo motivo, a F012 (AGY) trocou o launcher C++ do upstream por um trampolim C# compilado com `csc`.
- **Options:** (1) manter o trampolim C# e desistir dos diálogos; (2) instalar C++ e ATL no VS 2022 e voltar a compilar os projetos do upstream; (3) baixar as DLLs prontas de uma release oficial do KyraFiles.
- **Decision:** (2), autorizada pelo Alexandre em 09/10/2026 (instalação de `Microsoft.VisualStudio.Workload.NativeDesktop`, `VC.Tools.x86.x64` e `VC.ATL`). Os projetos são compilados por `tools/Build-Native.ps1` (o `mkfile` só roda `dotnet build`, que não compila `.vcxproj`), com o toolset e o SDK mais novos instalados (o upstream pede v145, do VS 18). Na F012 vale o launcher do upstream (`src/KyraFiles.App.Launcher`), com dois ajustes para a AC-6: sem caixa de mensagem e sem UAC ao reverter o registro (o `UnsetFilesAsDefault.reg` desta branch só apaga chaves do HKCU), esperando a reversão antes de abrir o Explorer. O trampolim C# (`tools/launcher/Program.cs`) sai.
- **Reason:** o launcher do upstream já trata `/select`, janelas do Explorer já abertas, pastas especiais e o KyraFiles desinstalado; reescrevê-lo duplicava código e perdia essas regras. (3) traria binário de fora sem passar pelo nosso build.
- **Trade-offs:** o build completo passa a exigir o C++ do Visual Studio e um passo a mais (`tools/Build-Native.ps1`); o arquivo `KyraFilesLauncher.cpp` do upstream ganha um diff pequeno.
- **Consequences:** `tools/Create-SelfContained-Release.ps1` recusa gerar pacote sem os binários nativos no layout AppX.

## D-016 — Instância de diálogo de arquivos isolada da sessão do usuário (09/10/2026)

- **Context:** com o KyraFiles como diálogo "Abrir", escolher o arquivo e fechar devolvia "Cancelar" ao programa chamador em parte das vezes (CONFIRMED: teste direto com `-outputpath`, 2 falhas em 6; pelo `OpenFileDialog` de outro processo, falha na 1ª tentativa). Causa (CONFIRMED lendo `App.xaml.cs`, `MainWindow.xaml.cs`, `MainPageViewModel.cs`): a instância de diálogo restaurava a sessão de abas do usuário (o `-outputpath` só era lido depois da primeira navegação), e no fechamento salvava a sessão **antes** de ler a seleção. A cada uso a sessão do usuário ganhava uma aba (6 abas depois dos testes, 4 delas a pasta do diálogo). Além disso, o código do upstream saía sem sinalizar o evento `FILEDIALOG` quando não achava a aba atual, e o programa chamador espera esse evento sem prazo.
- **Options:** (1) só ler a seleção antes de salvar a sessão; (2) tratar a instância de diálogo como isolada: não restaura nem salva sessão, lê a seleção primeiro, sempre sinaliza o chamador, não "estaciona" em segundo plano e não recebe aberturas de outros programas.
- **Decision:** (2). Arquivos tocados além do `plan.md` da F012: `App.xaml.cs`, `MainWindow.xaml.cs`, `ViewModels/MainPageViewModel.cs`, `Helpers/Application/AppLifecycleHelper.cs` (só no caminho em que `App.OutputPath` está definido).
- **Reason:** (1) deixava a sessão poluída e o diálogo abrindo com as abas do usuário; o defeito é o mesmo nos dois sintomas.
- **Trade-offs:** diff pequeno em quatro arquivos do upstream.
- **Consequences:** 5 em 5 pelo teste direto e 3 em 3 pelo diálogo "Abrir", 1 em 1 pelo "Salvar", sessão inalterada (`tools/perf/f012_file_dialog_return.ps1`). O diálogo abre em ~2,6 s em vez de 4 a 6 s e devolve em ~0,4 s após fechar, em vez de 1,7 s.

## D-017 — RESOLVIDA (opção 3, implementada em `ExplorerWindowRedirector`): `explorer.exe <pasta>` direto (AC-4 da F012) (09/10/2026)

- **Context:** com o KyraFiles como gerenciador padrão, abrir pasta pelo shell (`start <pasta>`, `Win + R`, duplo clique) abre no KyraFiles (CONFIRMED: `Start-Process <pasta>` levou a janela do KyraFiles para a pasta). Mas `explorer.exe "<pasta>"` chamado direto abre o Explorer do Windows (CONFIRMED: janela `CabinetWClass` do processo `explorer`, nenhuma do KyraFiles). O `SetFilesAsDefault.reg` do upstream só troca `Folder\shell\open`, `Folder\shell\explore` e o `CLSID` do `Win + E`; uma chamada ao `explorer.exe` não passa por eles.
- **Options:** (1) aceitar: a AC-4 vale para `start` e `Win + R`, não para `explorer.exe` direto (o upstream é assim); (2) interceptar `explorer.exe` por `Image File Execution Options` (`Debugger`), que é invasivo, exige administrador e pode travar o shell se o launcher falhar; (3) outra forma ainda não estudada.
- **Escolha que o Claude faria:** (1), mudando a AC-4 na spec. **Pendente:** decisão do Alexandre.
- **Atualização (09/10/2026):** o Alexandre testou `Win + E` com teclado real e abriu o Explorer (AC-1 também falha; reproduzido por `SendInput`, com qualquer valor de `LaunchTo`). O verbo padrão em `Directory\shell` (técnica do OneCommander, vista nas chaves que ele deixou no HKCU) também não pega o `explorer.exe <pasta>` (testado e revertido; registro idêntico ao backup). O Alexandre pediu o comportamento do OneCommander e similares, sem administrador.
- **Decision:** opção (3): enquanto o KyraFiles estiver em execução, ele observa as janelas do Explorer que abrem (`SetWinEventHook`, fora do processo do Explorer), deixa a nova janela transparente, lê a localização e a seleção pela `ShellWindows`, fecha a janela e abre a localização no KyraFiles; localização que o KyraFiles não exibe volta a ficar visível. Protótipo (`SetWinEventHook` + `ShellWindows`): Win + E, `explorer.exe <pasta>` e `/select` redirecionados, janela do Explorer visível em no máximo 2 amostras de 10 ms.
- **Trade-offs:** só funciona com o KyraFiles em execução (com ele fechado, o Explorer abre, o que é seguro); se o KyraFiles cair com uma janela transparente pendente, essa janela fica invisível até ser fechada (mitigação: tempo máximo curto e restauração em qualquer falha).


## D-019 — Índice de busca próprio, em dois modos, sem duplicatas (F015) (11/10/2026)

- **Context:** a busca por nome usa o motor escolhido na F011. O ativo era o Agent Ransack (`SearchEnginePreference: 1`), que varre o disco a cada busca, só mostra resultado no fim e toca o disco item por item. Medido em 11/10/2026: 8,3 s em `C:\desenv` (86.954 arquivos), contra 5,9 s de um `Get-ChildItem -Recurse`. O Everything está instalado, mas o provedor depende do `es.exe` (ausente em `C:\Program Files\Everything\`) e cai em silêncio no motor nativo. Queixa do Alexandre: "extremamente lento, parecendo trabalho de principiante".
- **Options:** (A) componente com privilégio de administrador lendo a estrutura do NTFS (como o Everything); (B) índice sem privilégio, por varredura e vigilância de pastas; (C) os dois; (D) depender do Everything.
- **Decision:** (C), pelo Alexandre, que também descartou (D): "não podemos depender do Everything, precisamos fazer igual ou melhor". Regras dele: todos os discos; `subst` e links não podem gerar indexação nem resultado duplicado. Na revisão da spec: unidades de rede ficam fora do índice (busca por varredura na hora); link físico aparece uma vez, no local original.
- **Reason:** só (A) iguala o Everything em indexar e acompanhar mudanças com o app fechado; só (B) funciona sem administrador e em volumes não NTFS.
- **Trade-offs:** dois caminhos de indexação para manter; um componente instalado com UAC.
- **Consequences:** spec `docs/specs/F015-indice-de-busca/`. A escolha de motor da F011 deixa de valer para busca por nome; o F3 da F013 (Agent Ransack) continua. Numeração: o `AGENT_CONTEXT.md` da F014 cita uma "D-018" que não está neste arquivo; para não colidir, esta é a D-019.

## D-020 — Como instalar o modo completo do serviço de índice (F015 T1) (11/10/2026) — PENDING

**Estado:** PENDING — aguarda o teste elevado (Install-Spike.ps1 + Test-Spike.ps1) para confirmar a opção escolhida. O Claude atualiza esta decisão após rodar os scripts.

- **Context:** o modo completo do índice (F015) roda como SYSTEM para ler a MFT e o diário USN. o KyraFiles já tem `allowElevation` e `runFullTrust` no manifesto (`CONFIRMED`). O pacote de desenvolvimento é registrado com `Add-AppxPackage -Register` (layout, sem MSIX assinado).

- **Options:**
  - **(a) Serviço empacotado no MSIX** (`desktop6:Service`, capacidades `packagedServices` e `localSystemServices`): o serviço faz parte do próprio pacote; instalação automática junto com o app; remoção pelo desinstalador do app.
    - **Requer:** pacote assinado com certificado confiável na máquina-alvo (exigência da instalação de serviço SYSTEM via MSIX). O fluxo de desenvolvimento usa `Add-AppxPackage -Register` (layout solto, não assinado), que **NÃO instala serviços SYSTEM** — o registro de layout não processa `desktop6:Service/localSystem`. Ou seja: no fluxo `Open-KyraFiles.ps1` de desenvolvimento, a opção (a) não instala o serviço sem um MSIX assinado. Para uso em produção (pacote sideload com certificado autofirmado importado como root confiável) funciona normalmente no Windows 10 2004+.
    - **Desvantagem para desenvolvimento:** exige MSIX assinado ou certificado instalado; não funciona em dev com layout solto.
  - **(b) Instalador elevado separado** (`allowElevation` já presente): o KyraFiles inicia um processo elevado (`Install-Spike.ps1` ou equivalente) que copia o exe para `%ProgramFiles%\KyraFiles\IndexService\` e cria o serviço com `sc create`. Desinstalador remove os dois. Funciona com layout de desenvolvimento e com MSIX.
    - **Vantagem:** independente do sistema de empacotamento; funciona em qualquer cenário (dev, sideload, Store se aplicável).
    - **Desvantagem:** dois artefatos separados; o usuário vê um segundo UAC além do KyraFiles.

- **Recomendação da AGY (antes do teste):** **(b) instalador elevado separado**, pelos seguintes motivos:
  1. O fluxo de desenvolvimento `Open-KyraFiles.ps1` usa `Add-AppxPackage -Register` (layout não assinado), que não instala `localSystem` services — a opção (a) ficaria sem testar em dev.
  2. A opção (b) funciona em todos os cenários (dev, sideload, distribuição futura) com o mesmo código.
  3. O `allowElevation` já está declarado no manifesto; o KyraFiles pode usar `ShellExecute` com `runas` para o instalador, com UX clara para o usuário.
  4. Isola a lógica de instalação do serviço do código do app, facilitando atualização e remoção.

- **Spike construído:** `tools/spikes/f015-service/` — servidor `IndexServiceSpike.exe` (named pipe `KyraFiles.IndexService.Spike`, ACL para usuários autenticados, responde `pong:<identidade>`), cliente `IndexClientSpike.exe` (não elevado). Scripts: `Install-Spike.ps1` (elevado, para Claude disparar), `Uninstall-Spike.ps1` (elevado), `Test-Spike.ps1` (não elevado: `sc query` + ping).

- **Próximo passo para confirmar:** Claude dispara `Install-Spike.ps1` com `Start-Process pwsh -Verb RunAs`; Alexandre clica "Sim"; Claude lê o log e dispara `Test-Spike.ps1`; resultado do ping confirma a identidade SYSTEM. Depois, `Uninstall-Spike.ps1` remove o serviço e os arquivos.

- **Trade-offs da (b):** o usuário vê um prompt de UAC separado ao instalar e ao remover; o serviço não é removido automaticamente se o app for desinstalado sem passar pela UI de configurações (mitigação: instrução clara na UI e na nota de liberação).

- **Consequences:** implementação do modo completo (T15) usa o padrão (b) — script de instalação invocado pelo KyraFiles com `allowElevation`, copiando o exe para `%ProgramFiles%\KyraFiles\IndexService\` e criando o serviço com conta LocalSystem.

## D-021 — Win+E não pode ser capturado com `RegisterHotKey`; usar hook de teclado de baixo nível (F017 T1) (12/10/2026)

- **Context:** a F017 (`plan.md` §1.1) assumiu `RegisterHotKey(MOD_WIN, 'E')`. O Alexandre pressionou Win+E e abriu o Windows Explorer.
- **Problem:** medido em 12/10/2026 com P/Invoke: `RegisterHotKey(0, id, MOD_WIN, 'E')` retorna `false`, erro 1409 (`ERROR_HOTKEY_ALREADY_REGISTERED`), com e sem `MOD_NOREPEAT`; o controle Win+Y registra (erro 0). O `explorer.exe` é dono de Win+E. Com `KyraFiles.HotkeyService.exe` rodando (processo vivo), Win+E simulado abriu uma janela do Explorer e o KyraFiles não iniciou. O `main.cpp` ignora o retorno do segundo `RegisterHotKey`, então o serviço fica "vivo" sem atalho. O protocolo `kyrafiles-dev:?tab=Home` funciona sozinho (abriu o KyraFiles). Além disso, nada instala nem inicia o serviço (sem `Run`, sem instalador executado; `bin\Release\*.exe` só compilados).
- **Options:**
  - **(a) `WH_KEYBOARD_LL`** no HotkeyService: detecta Win(down)+E(down), engole o evento e dispara o protocolo. Sem alterar o registro; some ao encerrar o processo (reversão imediata, FR-007).
  - **(b) `HKCU\...\Explorer\Advanced\DisabledHotkeys = "E"`** e então `RegisterHotKey`. Exige reiniciar o Explorer, grava no registro e a reversão depende do desinstalador.
- **Decision:** (a).
- **Reason:** reversão imediata, nenhum estado persistente fora do `Run`, vale sem reiniciar o Explorer.
- **Trade-offs:** hook de baixo nível roda no caminho de toda tecla (o callback precisa ser mínimo: o Windows desativa o hook que passa de `LowLevelHooksTimeout`); antivírus podem desconfiar de hook global; precisa tratar o menu Iniciar abrindo ao soltar a tecla Win após engolir o E.
- **Consequences:** `plan.md` §1.1 e a tarefa T1 mudam de mecanismo (comportamento da spec inalterado). T1 volta a "pendente" até a prova real: Win+E físico/simulado abre o KyraFiles, e `explorer.exe` não ganha janela nova.

- **Adendo (12/10/2026):** o URI do protocolo passa a ser `kyrafiles-dev:` exato. `kyrafiles-dev:?tab=Home` (da spec/plan originais) cai no ramo do `MainWindow` que lê JSON de aba (`TabBarItemParameter.Deserialize`) e derruba o KyraFiles (`debug.log`: "'H' is an invalid start of a value"). Implementada por Claude por falta de cota da AGY (autorização permanente do Alexandre em 12/10/2026).

## D-022 — Fonte da verificação de atualização do KyraFiles (F018) (12/10/2026)

- **Context:** o menu Ajuda não permitia verificar versão; o `IUpdateService` do upstream é `Dummy` neste build.
- **Options:** (a) releases do GitHub de `alxchs/KyraFiles` (público); (b) o `package_manifest.json` da F017 T4 hospedado em outro lugar; (c) trocar o `IUpdateService`.
- **Decision:** (a), como constante `Constants.ExternalUrl.CustomReleasesApiUrl`. O repositório hoje não tem releases (`gh repo view`: `latestRelease: null`), então a tela mostra "Nenhuma versão foi publicada ainda" até a primeira publicação.
- **Reason:** não exige servidor próprio, funciona sem autenticação, a F017 T4 pode publicar a release e o manifesto no mesmo lugar.
- **Trade-offs:** limite de 60 requisições por hora por IP na API do GitHub (suficiente: só roda por clique). O `origin` do dia a dia é o repositório privado `KyraFiles-private`; as releases têm de sair do público.
- **Consequences:** mudar a fonte = mudar a constante. A F017 T4 reaproveita a mesma consulta para baixar o delta.

## D-023 — Reabrir o diagnóstico da F024: medir o upstream antes de decidir o futuro do projeto (F025) (20/10/2026)

- **Context:** a F024 (19/10/2026) terminou com o veredito da AGY "WinUI 3 é estruturalmente inviável; encerrar o projeto; não insistir". O Alexandre pediu ao Claude uma revisão sem tendência a acreditar na AGY. O Claude leu `AGENT_CONTEXT.md`, o handoff, `DIAGNOSTICO_PERFORMANCE_E_VIABILIDADE.md`, a spec/tasks da F024 e o diff de `ecb9c6d29`, `d7989509b`, `9c5e9ffa8`.
- **Problem:** o veredito não tem prova no repositório. (1) Os scripts de medição foram apagados em `d7989509b`; a AC-001 nunca teve número. (2) "1.068 s de CPU" é tempo acumulado de vida do processo, não custo de uma ação; "1.041 MB" não diz qual métrica. (3) As causas raiz (key repeat a 30 Hz, remontagem de aba, `INSTANCE_ACTIVE`) são inferências sem profiler. (4) Nunca se mediu o upstream puro na mesma pasta; o upstream também é WinUI 3 com o mesmo `ContentPresenter` por aba (`MainPage.xaml:255-258`). (5) O handler de Home/End/PgUp/PgDn ignora Shift/Ctrl (regressão) e Up/Down, o sintoma relatado, não foi tratado. (6) A remoção de `sampler.CheckNow()` no watcher não foi medida sob carga. (7) A meta de CPU parado (FR-004) foi redefinida como "culpa do DWM" sem prova. (8) Handoff e commit se contradizem sobre o estado do ambiente. (9) A AGY registrou como "diretriz definitiva" uma decisão que é do Alexandre.
- **Options:**
  - **(a) Aceitar o veredito e encerrar** o projeto.
  - **(b) Continuar a otimizar** às cegas, nova rodada de cirurgias.
  - **(c) Medir primeiro:** baseline do upstream puro + trace do nosso build, e só então decidir (F025, brief `docs/agents/tasks/F025-baseline-upstream-e-trace.md`).
- **Decision:** (c), aprovada pelo Alexandre em 20/10/2026. Nenhuma alteração de `src/` até o relatório. O veredito da F024 passa a `INFERRED` e a "diretriz de não insistir" fica **revogada**.
- **Reason:** custa pouco (só medição), é reversível e é a única opção que separa "framework" de "nosso diff". Os dois resultados possíveis são úteis: se o upstream também trava, o encerramento passa a ter prova; se não trava, existe um culpado achável por bisseção (F015 a F023). Já há referência parcial do upstream v4.2.9 em `tools/perf/README.md` (~610 MB de working set e 34% de um núcleo parado), mas ela não cobre teclado segurado nem troca de aba.
- **Trade-offs:** o app empacotado impede dois builds registrados ao mesmo tempo, então a medição é sequencial e mais lenta; compilar o upstream consome tempo de build; sem alterar `src/`, a regressão de Shift/Ctrl do handler continua no ramo até a decisão.
- **Consequences:** F025 vira a fase atual. A AGY executa o brief (P1 a P6) e entrega `docs/agents/evidence/F025/relatorio.md`. O Claude revisa. A decisão de continuar ou encerrar volta a ser `PENDING DECISION` do Alexandre com os números na mão. As lições da F024 foram registradas em `INSTRUCTIONS_OPERARY.md` ("Como diagnosticar desempenho") para a AGY não repetir.

## D-BACKLOG-1 — Aprovação das specs F035 a F056 e regra de prioridade (24/10/2026)

- **Context:** o Alexandre pediu ideias de valor, a pesquisa de concorrentes e as specs; Claude escreveu 22 rascunhos (`docs/specs/F035` a `F056`).
- **Problem:** havia pontos `[NEEDS CLARIFICATION]` e não havia regra de precedência entre bug, performance e feature.
- **Options:** (a) aprovar spec por spec; (b) aprovar todas com os pontos resolvidos pela recomendação escrita em cada uma; (c) adiar tudo.
- **Decision:** (b). Mensagem do Alexandre em 24/10/2026: "Aprovo todas, mas solução de bugs e performance sempre terá prioridade. Vc vai gerenciar a AGY nisso." Cada ponto em aberto vale a "Recomendação" da própria spec. Exceções que continuam condicionadas: F046 só começa depois da medição `R-rede-baseline`; F050 só sai do papel se a medição de custo do componente justificar; F054 começa pela tarefa 0 (medir a prévia atual).
- **Reason:** libera o planejamento sem pedir 22 respostas; a recomendação é o caminho mais conservador de cada spec.
- **Trade-offs:** aprovar spec não aprova código: `plan.md` e `tasks.md` ainda passam pelos portões do SDD.
- **Consequences:** regra permanente de fila: (1) bug que derruba o app ou perde dado, (2) regressão de performance, (3) pendências de evidência de feature já feita, (4) feature nova por onda. Feature nova só começa com a anterior `CONFIRMED` por teste com asserção. Fila e ordem da AGY em `docs/specs/BACKLOG-ORDEM-AGY.md`. Claude gerencia a AGY: escreve plan e tasks, revisa cada entrega e só então libera a próxima.

## D-F036-1 — Pausa na fila de cópias: o AC-1 da spec não é alcançável com o motor atual (24/10/2026) — RESOLVIDA (opção b, 25/10/2026)

- **Context:** a spec F036 (aprovada em 24/10/2026) promete, no AC-1, que "Pausar" numa cópia de 5 GB para em até 2 s e que "Retomar" continua do ponto em que parou.
- **Problem:** `FilesystemHelpers.CopyItemsAsync/MoveItemsAsync` entregam o trabalho ao **motor de cópia do shell do Windows** (`ShellFilesystemOperations`, com recuo para `StorageFile.CopyAsync`), que copia cada arquivo numa chamada só e aceita apenas cancelar. Sem controle do laço de cópia não há pausa no meio do arquivo (CONFIRMED pela leitura do código; a inexistência de pausa na interface do motor é INFERRED até o spike T8).
- **Options:** (a) só fila, cancelar, ETA e "tentar de novo" (sem pausa); (b) a e mais pausa **entre arquivos** (a fila alimenta o motor um item por vez); (c) b e pausa no meio do arquivo, trocando o motor por `CopyFile2` nos arquivos grandes.
- **Decision:** **pendente**. Recomendação do Claude: (b), com a fase (c) apenas como spike de medição (T8, fora do produto). Texto proposto para o AC-1 em dois níveis: nível 1 (garantido): a pausa vale depois do arquivo corrente e para ao terminar um item; nível 2 (só se o spike T8 provar): pausa no meio de um arquivo grande.
- **Reason:** (b) entrega a pausa para lotes (o caso comum, muitos arquivos) com risco baixo; (c) arrisca ACL, fluxos alternativos, propriedades, nomes longos e diálogos do motor do shell, sem prova de ganho.
- **Trade-offs:** com (b), uma cópia de **um** arquivo enorme não pausa no meio. Uma chamada ao motor por item tem sobrecarga (a ser medida na T7).
- **Consequences:** as tarefas T2 a T6 da F036 (fila, cancelar, ETA, tentar de novo, fechar o app) **não dependem** desta decisão e podem começar. T7 só depois da decisão; T8 é só medição. Se o Alexandre aprovar (b), o Claude reescreve o AC-1 da spec com data no histórico.

### Resolução de D-F036-1 (25/10/2026)

- **Decision:** **opção (b)**. O Alexandre instruiu: "testar algo que copiasse conjunto de bytes, se não der certo use a (b)". O teste foi feito (docs/agents/evidence/F036/spike-copia-por-blocos.md): a cópia por blocos funciona como mecanismo, mas **não passa na fidelidade** (perde atributos, Zone.Identifier e data de gravação) e é mais lenta; logo vale (b).
- **Fato novo:** CopyFile2 pausa e retoma com paridade de metadados com o motor do shell no teste local. A fase C passa a ser CopyFile2 e **depende do spike 2 (T8)**: outro disco, rede, nome longo, propriedades, EFS, esparso. Só então o Alexandre decide se entra.
- **Trade-offs mantidos:** com (b) um único arquivo enorme não pausa no meio; o AC-1 da spec ficou em dois níveis.
- **Consequences:** T1 concluída, T7 liberada pela decisão, T8 redefinida (spike 2 de CopyFile2). Spec F036 atualizada com data no histórico.

### Adendo de D-F036-1 (25/10/2026): resultado do spike 2
CopyFile2 provou paridade com o motor do shell nos testes feitos (entre discos, pausa dupla, cancelar, nome longo, esparso/compactado). Fica **recomendada** a fase C restrita a arquivos grandes, como feature separada depois de A+B; **decisão de entrar: do Alexandre**. Não testado: destino por SMB, rede real, EFS, propriedades, FAT/exFAT.

## D-F036-2 — Fase C da F036 aprovada: pausa no meio de arquivo grande por CopyFile2 (25/10/2026)

- **Context:** o spike 2 (docs/agents/evidence/F036/spike2-copyfile2-paridade.md) mostrou que CopyFile2 pausa e retoma, entre discos, com paridade de metadados com o motor do shell. Pergunta do Alexandre: o que acontece com um arquivo grande sem a fase C? Resposta: copia como hoje, o botão Pausar não pausa no meio. Decisão dele: "Melhor aplicar então".
- **Options:** (a) só A e B; (b) A, B e C restrita a arquivos grandes; (c) trocar o motor inteiro por CopyFile2.
- **Decision:** (b). A fase C entra, restrita (FR-005 a FR-007 da spec), implementada **depois** de A e B revisadas, na mesma feature, em commits separados.
- **Reason:** resolve exatamente o caso que A e B não resolvem (um arquivo único enorme), sem trocar o motor para o resto (que trata InUse, PropertyLoss, ACL, diálogos e elevação).
- **Trade-offs:** dois caminhos de cópia para manter; o arquivo grande passa a ser gravado em nome temporário e renomeado no fim; sem teste de destino por SMB, rede real, EFS, propriedades e FAT/exFAT (por isso o recuo ao motor atual nesses casos).
- **Consequences:** plan.md §9 e tasks T10 a T17. Escopo da fase C: arquivo (não pasta), a partir de 256 MiB por padrão (configurável), destino NTFS ou ReFS, sem elevação; mover só pelo caminho de arquivo grande quando for entre volumes (no mesmo volume o motor renomeia, instantâneo). Qualquer erro inesperado do CopyFile2 recua para o motor atual para aquele item.

## D-F036-3 — Política da fila: uma operação por destino (spec) ou simultaneidade configurável (25/10/2026) — RESOLVIDA (opção b, 25/10/2026)

- **Context:** a spec aprovada (FR-002 e AC-2) manda **serializar** cópias para o mesmo disco: a segunda espera a primeira terminar. O Alexandre, ao liberar a pasta de rede, pediu que se testassem "tarefas concorrentes, como uma cópia de um lote enquanto apaga outros arquivos".
- **Problem:** a premissa de que serializar é melhor **não se sustentou nas medições**: (1) NVMe local, baseline da AGY (n = 3): empate em duas rodadas e uma rodada atípica pesou a média; (2) NAS (n = 1): duas cópias de 256 MiB **juntas** foram **19% mais rápidas** que em sequência; copiar um lote e apagar 300 arquivos pequenos **ao mesmo tempo** foi **14% pior** que um depois do outro, com o apagar 4,9 vezes mais lento. Ver docs/agents/evidence/F036/spike3-nas.md.
- **Options:** (a) manter a spec: uma operação de cópia/movimento por destino, sempre; (b) **limite de simultaneidade configurável por destino (padrão 2)** para cópia e movimento, e **apagar e renomear fora da fila** (rodam à parte); (c) tudo simultâneo, sem fila.
- **Decision:** **pendente**. Recomendação do Claude: **(b)**, com o padrão a ser confirmado pela T2 (≥ 10 rodadas em NVMe, disco rotativo e NAS). Texto proposto para o AC-2: "com 2 cópias grandes para o mesmo disco, ambas progridem (até o limite configurado); a terceira espera na fila; apagar e renomear não esperam cópia".
- **Reason:** o indício aponta que serializar cópias grandes não ajuda no NAS e pode piorar, e que apagar muitos arquivos pequenos disputa com cópia de um jeito que a fila não resolve. Nenhuma medição ainda justifica serializar tudo.
- **Trade-offs:** (b) muda o comportamento prometido (AC-2); menos previsível que "um por vez"; exige configuração e medição.
- **Consequences:** até a decisão, a T2 (fila) pode ser implementada com **limite configurável, padrão 1** (comportamento da spec atual), sem travar a Onda 3; o padrão e o tratamento de apagar/renomear mudam depois, com um parâmetro. Se (b) for aprovada, o Claude reescreve FR-002 e AC-2 com data no histórico.

### Resolução de D-F036-3 (25/10/2026)
- **Decision:** **opção (b)**, escolhida pelo Alexandre ("3 = B"): limite de simultaneidade **configurável por destino, padrão 2**, para cópia e movimento; **apagar e renomear fora da fila**.
- **Consequences:** spec F036 (FR-002 e AC-2) reescrita com data no histórico; T2 e T3 ajustadas. O **padrão 2 é provisório**: a T2 deve medir com **no mínimo 10 rodadas** em NVMe local, disco rotativo e NAS (sozinho, juntos, copiar e apagar) e propor ajuste se a medição não sustentar. Evidência de apoio: docs/agents/evidence/F036/spike3-nas.md.
## D-F060-1 — Como o editor hexadecimal (F060 M2) salva

Data: 26/10/2026. Decidido por Claude (arquiteto); **aprovado pelo Alexandre em 26/10/2026** ("Essa parte é sua e eu aprovo").

- **Context:** o editor deve abrir e corrigir arquivos de vários GB sem carregá-los inteiros (HEX-001/009/010 da SPEC original).
- **Problem:** salvar no próprio arquivo arrisca corromper se cair no meio; salvar em cópia exige espaço livre igual ao arquivo.
- **Options:** (A) reescrever tudo numa cópia temporária e trocar; (B) gravar só os blocos alterados no próprio arquivo, com diário de recuperação ao lado; (C) permitir inserir/remover bytes (reescreve o resto do arquivo).
- **Decision:** B. A versão 1 só **substitui** bytes (o tamanho do arquivo nunca muda). Antes de salvar, confere tamanho e data de modificação; grava um diário (offsets e bytes antigos/novos), depois os blocos, depois apaga o diário; se o programa cair, o diário permite concluir ou desfazer na próxima abertura.
- **Reason:** não exige espaço extra, é rápido em arquivo grande e recuperável.
- **Trade-offs:** não insere nem remove bytes (fica para uma versão futura, com ADR própria); a gravação in-place depende do diário estar correto (por isso tem teste de queda no meio).
- **Consequences:** M2-T2 implementa B; inserir/remover fica fora do escopo da F060.

## D-F061-1 — Win+E é premissa permanente; o ciclo do Watchdog desacelera, nunca desiste

Data: 28/10/2026. Decidido por Claude (arquiteto), a pedido do Alexandre ("Essa deve ser uma premissa... Sempre ter esse recurso rápido").

- **Context:** em 28/10/2026 havia ~13 mil processos KyraFiles.HotkeyService.exe criados a ~5 por segundo pelo Watchdog; o Win+E mostrava o Explorer antes do KyraFiles.
- **Problem:** o Watchdog recria o serviço a cada 200 ms em qualquer saída diferente de 2, inclusive quando o serviço sai com código 0 por já existir outra instância. Causa do disparo às 23:48 de 27/10 ainda não provada.
- **Options:** (A) parar o Watchdog depois de N falhas; (B) espera crescente até 30 s, sem desistir, mais saída com código próprio para "já existe"; (C) trocar o Watchdog por tarefa agendada.
- **Decision:** B, mais um comando de verificação de saúde obrigatório em toda liberação. A é descartada porque viola a premissa de disponibilidade; C só se a T0 mostrar que o Watchdog gera várias instâncias.
- **Reason:** mantém o atalho sempre pronto sem afogar o computador.
- **Trade-offs:** depois de falhas repetidas o retorno pode levar até 30 s.
- **Consequences:** spec docs/specs/F061-win-e-sempre-pronto/; T0 prova a causa antes de qualquer código; AC-8 entra no PLAYBOOK.

## D-F065-1 — O item de lista nasce visível (opacidade 1)

Data: 30/10/2026. Decidido por Claude (arquiteto), na execução autorizada pelo Alexandre ("Assuma o que ela não começou ainda que consiga acabar até as 14:00").

- **Context:** na busca, Nome e ícone saíam invisíveis porque o motor Everything criava os resultados sem opacidade, e a opacidade do item nasce 0. A revisão de 30/10 achou o mesmo esquecimento no motor Agent Ransack (segunda ocorrência do mesmo sintoma).
- **Problem:** todo código que cria item de lista precisa lembrar de definir a opacidade; quem esquece produz linha invisível, sem erro nem aviso.
- **Options:** (A) definir a opacidade em cada motor que esquece; (B) o item nasce com opacidade 1 e cada criador só define quando quer esmaecer (oculto); (C) teste que lê o código-fonte e cobra a atribuição.
- **Decision:** B, mantendo a atribuição explícita no Everything (que esmaece ocultos).
- **Reason:** elimina a armadilha na origem, para os motores atuais e futuros; os 30 pontos que já definem a opacidade continuam iguais; os 6 que não definem são objetos auxiliares (menu de contexto, barra lateral, propriedades) que não viram linha da lista.
- **Trade-offs:** muda um arquivo do upstream (uma linha); um motor que esquecer o esmaecido de ocultos mostrará o oculto com opacidade normal, nunca invisível.
- **Consequences:** F065-T6. A prova de tela por motor (incluindo o Agent Ransack) fica para as 23:59 (F065-T12).


## D-LAUNCHER-1 — Nome único e canônico do Launcher e pasta de destino em %LOCALAPPDATA%

Data: 03/11/2026. Decidido por AGY (executora) a pedido do Alexandre.

- **Context:** após a renomeação da identidade para KyraFiles (F081-F086), ao clicar duas vezes em uma pasta o Windows dava "Application not found" e abria o Windows Explorer nativo como fallback. O registro chamava `%LOCALAPPDATA%\KyraFiles\KyraFiles.App.Launcher.exe`, caminho que não existia mais no disco. O `KyraFiles.App.csproj` copiava `KyraFiles.App.Launcher.exe`, os serviços de atualização (`AppUpdateSideloadService.cs` e `AppUpdateStoreService.cs`) e scripts de registro esperavam `KyraFiles.App.Launcher.exe`, e `Assets/KyraFilesOpenDialog/` estava desprovido do executável.
- **Problem:** inconsistência no nome do binário do Launcher e no diretório de destino do aplicativo no `%LOCALAPPDATA%` após a renomeação da solução para KyraFiles.
- **Options:** (A) Reverter o nome do executável e pasta para `KyraFiles.App.Launcher.exe` e `%LOCALAPPDATA%\KyraFiles`; (B) Unificar tudo sob a nova identidade: executável `KyraFiles.App.Launcher.exe` e pasta `%LOCALAPPDATA%\KyraFiles\`.
- **Decision:** Opção B. O executável canônico único é `KyraFiles.App.Launcher.exe`, instalado e registrado em `%LOCALAPPDATA%\KyraFiles\KyraFiles.App.Launcher.exe`.
- **Reason:** alinhamento completo com a identidade KyraFiles (F081+) estabelecida no commit `28ecad12a`, mantendo padrão consistente em todo o repositório, nos manifests, no instalador e nas chaves de registro do Windows.
- **Trade-offs:** exige atualização atômica de todas as referências em C#, C++, scripts `.reg` e projetos auxiliares.
- **Consequences:** todos os pontos identificados (csproj, `AppUpdate*Service.cs`, `AdvancedViewModel.cs`, `AboutViewModel.cs`, `ExplorerWindowRedirector.cs`, `KyraFilesLauncher.cpp`, `tools/Build-Native.ps1`, `installer/package_manifest.json` e `.reg` em `Assets/KyraFilesOpenDialog/`) passam a referenciar rigorosamente `KyraFiles.App.Launcher.exe` e `%LOCALAPPDATA%\KyraFiles\`.


## D-64BIT-CLEANUP-1 — Padronização 64-bit exclusiva dos projetos nativos C++ e purga de nomes legados

Data: 03/11/2026. Decidido por AGY (executora) com aprovação explícita do Alexandre.

- **Context:** projetos legados de 32-bit (Win32) ainda existiam em src/KyraFiles.App.OpenDialog/KyraFiles.App.OpenDialog.Win32.vcxproj e src/KyraFiles.App.SaveDialog/KyraFiles.App.SaveDialog.Win32.vcxproj, além de artefatos compilados de 32-bit em Assets/KyraFilesOpenDialog/ e rtifacts/. Além disso, fontes C++ mantinham prefixo KyraFiles* (KyraFilesLauncher.cpp, KyraFilesOpenDialog.*, KyraFilesSaveDialog.*), e assemblies Server e BackgroundTasks mantinham <AssemblyName> e <RootNamespace> como KyraFiles.App.*.
- **Problem:** resíduos legados de 32-bit geravam complexidade desnecessária e inconsistências de nomenclatura em relação à nova identidade KyraFiles x64.
- **Options:** (A) Manter suporte a 32-bit com projetos Win32; (B) Eliminar 32-bit completamente, padronizando a solução inteira como 64-bit (x64), renomear fontes e projetos nativos para KyraFiles* e purgar todos os artefatos legados KyraFiles.App.*.
- **Decision:** Opção B. Arquitetura unificada em 64-bit exclusivo.
- **Reason:** ecossistema moderno Windows opera em x64; simplifica o pipeline de build e elimina duplicação de DLLs nos pacotes.
- **Consequences:** removidos projetos .Win32.vcxproj; 	ools/Build-Native.ps1 compila apenas x64; fontes renomeados para KyraFilesLauncher.cpp, KyraFilesOpenDialog.*, KyraFilesSaveDialog.*, KyraFilesDialogEvents.*; KyraFiles.App.Server e KyraFiles.App.BackgroundTasks renomeados em assembly, namespace, manifesto e referências; 5 arquivos legados iles-custom.html removidos da documentação e links atualizados para kyrafiles.html.
