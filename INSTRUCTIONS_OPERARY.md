# INSTRUCTIONS_OPERARY

Instruções para a executora (AGY/Gemini) neste repositório. Mantido pelo Claude na revisão de cada tarefa. Complementa `AGENTS.md`, `MASTER_SPEC.md` e a spec da feature em andamento; não os substitui.

## Base de Conhecimento e Padrões Sólidos

### Disciplina de Git e Evidências Técnicas (10/11/2026)

- **Todo script citado como evidência num relatório fica versionado em `tools/` no mesmo commit do relatório.** Sem o script disponível e reproduzível no repositório, a afirmação é `INFERRED`, nunca `CONFIRMED`.
- **A mensagem de commit descreve só o que está no diff.** Confira sempre com `git diff --stat` ou `git show --stat` antes de fechar a mensagem do commit.
- **Itens de "Este Computador" sem letra de unidade (servidor de mídia DLNA, celular MTP) são pastas virtuais do Shell:** enumerar por `Shell.Application.NameSpace(17)` ou `IShellFolder` numa thread STA em segundo plano com timeout (`STATask`); `DriveInfo.GetDrives()` do .NET não os enxerga.

### Testes (`tests/KyraFiles.App.Search.Index.Tests`)

- **`dotnet test` não executa os testes deste projeto** (11/10/2026: "Zero tests ran", código de saída 5, embora a descoberta ache 15). Rode o executável gerado, que usa o Microsoft.Testing.Platform:
  `tests\KyraFiles.App.Search.Index.Tests\bin\x64\Release\net10.0-windows10.0.26100.0\KyraFiles.App.Search.Index.Tests.exe`
  Use `--list-tests` para listar. A evidência "N/N passou" cita essa saída (`total`, `succeeded`).
- Nome do teste começa pelo `AC-n` quando cobre um cenário de aceite (ex.: `AC15_BuscaAcao_MatchesExpectedVariants`).
- MSTest, `[TestClass] public sealed class`, `Assert.*`. Usings globais em `GlobalUsings.cs` do projeto de teste.

### Código da biblioteca de índice (`src/KyraFiles.App.Search.Index`)

- Cabeçalho `// Copyright (c) KyraFiles contributors. Licensed under the MIT License.`, namespace por arquivo (`namespace X;`), tabs, comentários XML em português.
- Caminho quente sem alocação: `ReadOnlySpan<T>`, `ref readonly` sobre arrays de structs, nada de LINQ dentro do laço de consulta.
- `NameNormalizer.Normalize` (T4): FormD → tira `NonSpacingMark` → minúsculas invariantes → FormC. Limitações conhecidas, aceitas por ora: letras sem decomposição (`ø`, `ł`, `ß`, `æ`) não viram ASCII; marca combinante fora do BMP não é removida.
- `QueryMatcher` (T4): palavras separadas por espaço = E; termo **sem** curinga é substring; termo **com** `*`/`?` casa o nome **inteiro** (ancorado, como o Everything: `rel*` = começa com "rel"; `*.pdf` = termina em ".pdf"). `?` vale um `char` UTF-16.
- Arquivos novos com fim de linha CRLF (o Git avisa "LF will be replaced by CRLF": converta antes de commitar).
- **Nada de "para cada X, percorrer o índice inteiro"** (lição da T17, 16/10/2026: a passada 2 do `Reconciler` e o `MarkDeleted` de pasta eram pastas × itens e nunca terminavam no C: com 2,3 milhões de itens). Com mais de um alvo, junte os alvos num `HashSet`/`Dictionary` e faça **um** passe sobre o índice. Todo laço sobre `index.Count` dentro de outro laço precisa de justificativa no commit.
- **Lock do índice (`SyncRoot`) em fatias curtas.** Trabalho longo (reconciliação, revarredura) solta o lock a cada ~1.000 pastas. Lock segurado por minutos trava watcher, snapshot e a reconciliação da abertura.
- **Trabalho de fundo em prioridade baixa e sem tempestade**: thread em `THREAD_MODE_BACKGROUND_BEGIN`; evento repetido (estouro do watcher) só marca "pendente" e um único executor roda depois de um intervalo sem eventos.
- **Publique cada volume assim que ficar pronto**; nunca espere todos os discos para mostrar o primeiro.
- **Medição do serviço fora do pacote** usa `%LOCALAPPDATA%\KyraFiles\Index` (não a `LocalCache` do pacote). Para indexar do zero sem apagar o cache real, passe `--cache <pasta temporária>`.
- Perfil de CPU: `tools/perf/f015_trace_reconcile.ps1` (`dotnet-trace` já instalado). Antes de dizer de onde vem o CPU, rode o perfil.

### UI WinUI 3 (`src/KyraFiles.App`)

- **`UIElement.Clip` só aceita `RectangleGeometry`** (12/10/2026: `PathGeometry` dá CS0266). Para recorte livre use a camada de Composition: `ElementCompositionPreview.GetElementVisual(el).Clip = compositor.CreateGeometricClip(compositor.CreatePathGeometry(new CompositionPath(CanvasGeometry.CreatePath(new CanvasPathBuilder(null)))))`. Modelo pronto: `Helpers/UI/ShapeGeometryHelper.cs`.
- Em `CanvasPathBuilder`, arco que contorna a curva para fora num desenho com Y para baixo é `CanvasSweepDirection.Clockwise`; `Counterclockwise` abaula para dentro da forma.
- Erros em cascata `XamlCompiler error WMC0001/WMC0110` (dezenas, "Unknown type ResourceString") quase sempre vêm de **um** erro de C# no projeto: ache primeiro `error CS`, corrija e recompile, antes de mexer em XAML.
- Não assine lambda em evento dentro de `Loaded` (dispara mais de uma vez): use método nomeado e `-=` antes de `+=`.

### Processo

- Uma tarefa = um commit `tipo(f015): resumo (Tn)`; marcar a linha da tarefa em `tasks.md` com data e a evidência (comando + número).
- Número de desempenho só com o comando que mediu; sem isso, "estimado, não medido".
- Mudança sem conteúdo (só linha em branco no fim do arquivo) não entra em commit.

### Como diagnosticar desempenho (lições da F024, 20/10/2026: leia antes de qualquer tarefa de perf)

A F024 terminou com um veredito grande ("WinUI 3 é inviável") sustentado por pouca prova. O Claude revisou e reabriu o caso (D-023, brief F025). O que ela errou vira regra:

1. **Sintoma do usuário é fato; causa é hipótese.** "Segurar `Down` congela" foi observado pelo Alexandre. "Foi o key repeat de 30 Hz saturando o Dispatcher" é palpite até haver trace. Escreva a causa como `INFERRED` e diga qual medição a confirmaria.
2. **Sem baseline não há conclusão.** Antes de culpar o framework, meça o **upstream puro** (`git merge-base HEAD upstream/main`) na mesma pasta, mesma máquina, mesmos scripts. Culpar o WinUI 3 só vale se o upstream também sofre.
3. **CPU de vida do processo não é CPU de uma ação.** `TotalProcessorTime` é acumulado desde o início do processo. Meça **a diferença** entre duas leituras, ao redor da ação. "1.068 s de CPU" sozinho não diz nada.
4. **Nomeie a métrica de memória.** `WorkingSet64` e `PrivateMemorySize64` são coisas diferentes; escreva qual usou e o comando.
5. **Um suspeito por vez, A/B antes de mudar código.** Troque uma coisa, rode o mesmo script, compare. Mudar três coisas e medir uma vez não prova nenhuma delas.
6. **Trace antes de causa.** Para travamento de UI: grave 5 s com WPR/ETW ou `dotnet-trace` (cpu-sampling) e liste as 10 funções mais quentes por thread. Só isso autoriza escrever "causa raiz".
7. **Nunca apague a prova.** Script de medição, saída e trace ficam no repositório (`tools/perf/`, `docs/agents/evidence/<feature>/`). Só o laboratório de arquivos de teste é descartável, e só depois da revisão. A F024 apagou os scripts e deixou números que ninguém consegue reproduzir.
8. **Não redefina a meta para ela passar.** A FR-004 pedia CPU parado ≤ 2%; a T6 reclassificou o problema como "culpa do compositor do DWM". Se a meta não é alcançável, escreva `NOT MET` com o número e abra uma `PENDING DECISION`. Mudar a meta é do Alexandre.
9. **Não decida o futuro do projeto.** "Encerrar/cancelar" e "diretriz para não insistir" são decisões do Alexandre. Seu trabalho termina em: número, comando, estado de evidência e, no máximo, uma proposta de próximo passo.
10. **Atalho de teclado: preserve os modificadores.** Ao tratar `Home`/`End`/`PgUp`/`PgDn` na mão, `Shift` estende a seleção e `Ctrl` só move o foco. O handler da F024 ignorou os dois (regressão). Todo handler de tecla vem com teste dos modificadores.
11. **Não remova proteção sem medir o efeito.** A F024 tirou `sampler.CheckNow()` do fluxo do watcher; isso baixa a latência de 5 arquivos mas piora o caso de muitas mudanças por segundo. Toda remoção de throttle exige o caso de carga alto no A/B.
12. **Documento e commit têm de dizer a mesma coisa.** A F024 escreveu "executável preservado" e depois o desinstalou; o commit "expurgo do registro" só tinha linhas em branco. Antes de afirmar estado do ambiente, confira (`Test-Path`, `Get-Process`) e cite o comando.

### Lições da F028/F029 (21/10/2026, Claude)

13. **O exe de teste do Alexandre sai de `dotnet build src/KyraFiles.App/KyraFiles.App.csproj -c Release -p:Platform=x64` + `.\Open-KyraFiles.ps1`.** O `mkfile` compila para `%TEMP%\Compiler` e falha no `KyraFiles.App.Server`: não atualiza o `AppX`.
14. **Erro de XAML em massa (`WMC0001 Unknown type`, `WMC1509 No LocalAssembly`) costuma ser um erro de C#.** Rode com `-v:minimal` e procure `error CS`.
15. **Captura de tela por script precisa de `SetProcessDPIAware()`**, senão sai cortada com a escala de 150%. Encaixe a janela na área de trabalho e clique numa linha para o foco estar na lista (`tools/perf/prep_window.ps1`).
16. **Toda tecla ou clique sintético confere antes a janela em primeiro plano** (`GetForegroundWindow`). Um clique "na lista" caiu num VS Code de outro projeto. Os scripts F025/F028/F029 param se a janela medida sair da frente.
17. **Medição que toma teclado e mouse só roda com o computador ocioso** (`GetLastInputInfo`, `tools/perf/f029_ab.ps1` espera 60 s). O Alexandre usa a mesma máquina.
18. **Lista com cabeçalho fixo: posição de linha não é `índice × RowHeight`.** O cabeçalho cobre o topo do viewport. Leia a posição real de um contêiner realizado (`TryGetRowGeometry` em `DetailsLayoutPage`).
19. **Um log temporário de uma linha por evento resolve em minutos o que a captura não explica** (a F028 achou assim a rolagem extra de 7 px do foco nativo). Apague o log antes do commit.
20. **`Start-Sleep -Milliseconds 33` não dura 33 ms**: medido 89 ms em 21/10/2026 (depende da resolução do relógio do Windows naquele momento, que outros programas mudam). A coluna `presses` antiga era calculada, não contada. Compare só rodadas do mesmo lote, intercaladas, e use a contagem real de teclas enviadas.

### Segurança da automação, provas e medições (25/10/2026, Claude: padrões já validados, leia antes de qualquer teste)

1. **Entrada global (teclado e mouse) só passa por `tools/perf/Send-GuardedInput.ps1`.** Nunca `SendKeys`, `SendInput`, `keybd_event`, `mouse_event`, `SetCursorPos` nem `SetForegroundWindow` fora dele (`tools/perf/Test-NoRawInput.ps1` reprova). Motivo: em 24/10/2026 a automação enviou teclas para a janela em foco e encheu o WhatsApp do Alexandre. A trava confere o foco **antes de cada evento** e aborta se a janela em foco não for do processo de teste que o próprio script iniciou.
2. **Teste que abre janela ou envia entrada só roda com `ALEXANDRE_AWAY=1`** (o Claude define no disparo, com orçamento de tempo). Fora disso: compilar, testes unitários, testes de arquivo e medições sem janela. Janela nova também rouba o foco de quem digita.
3. **Teste sem asserção não é teste.** Cada AC vira uma linha de `resultado.csv` (esperado, observado, PASS/FAIL, comando); o relatório é **gerado do CSV**; cada feature tem **controle negativo** (o teste reprova onde deve). Captura de tela é anexo, nunca a prova.
4. **Um teste negativo que passa por outro motivo é falso positivo.** Lição do `Negativo_FocoDesalinhado` (passou por "nenhuma janela em foco", e não por "janela B em foco") e do teste do modificador (falhou antes de o modificador descer, então não exercitou o `try/finally`). **Estabeleça a pré-condição de forma explícita e confira a mensagem de erro exata**, não só "abortou".
5. **Estado de evidência honesto.** `CONFIRMED` exige comando **executado** e saída; afirmação baseada só em leitura de código sobre comportamento em execução é `INFERRED`. `OBSERVED, n = 3` não vira "provado". Uma rodada atípica puxa a média: reporte as rodadas, não só a média.
6. **Medição de desempenho.** Execuções **intercaladas** entre os métodos (para o cache do disco afetar todos por igual); arquivo maior que a memória livre para chegar perto do disco; no mínimo **10 rodadas** quando a variação é grande (NAS, disco rotativo, antivírus ativo). Em máquina com pouca memória livre, registre a memória disponível na hora.
7. **Erros de script que já custaram resultado inválido (não repita):** (a) uma função que devolve **uma só linha** devolve texto, e `[-1]` pega o **último caractere**; devolva sempre array (`,@(...)`); (b) comparar hash pelo último caractere "passa" sempre: compare o hash **completo**; (c) apagar arquivos entre rodadas **sem `-Force`** deixa os ocultos, e o método seguinte herda atributos e datas; (d) leia colunas de CSV **pelo nome**, nunca por posição; (e) um script que apaga o CSV de resultados ao reiniciar perde o que já foi medido: acrescente, não sobrescreva.
8. **Dados de teste só onde foi autorizado.** Local: `%TEMP%\agy\<GUID>\...` (apague ao fim). Rede: só dentro da pasta que o Alexandre liberou. Origem real de arquivo **somente leitura**; nunca gerar dado fora da pasta permitida. Teste que usa o shell do Windows (`Shell.Application`, `IFileOperation`) pode abrir **diálogo no desktop do Alexandre** e travar o script: isole em processo filho com limite de tempo, com `FOF_NOERRORUI`.
9. **Cópia de arquivos no Windows (medido na F036).** `CopyFile2` pausa (`COPYFILE2_PROGRESS_PAUSE`) e retoma (`COPY_FILE_RESUME_FROM_PAUSE`) no meio do arquivo, também entre discos e para o NAS; o contador do callback é cumulativo entre retomadas. Gravar com **nome temporário e renomear no fim** evita arquivo incompleto com o nome final. Cópia por blocos feita à mão **perde** atributo oculto, `Zone.Identifier` e data de gravação. Destino sem fluxos alternativos (NAS) descarta o `Zone.Identifier` **em silêncio** em `CopyFile2` e `File.Copy`. Esparso e compactado são perdidos por todos os métodos. O motor do shell falha em caminho acima de 260 caracteres; `CopyFile2` com `\\?\` funciona.
10. **Fila e concorrência: não presuma que serializar é melhor.** No NAS, duas cópias grandes juntas foram **19% mais rápidas** que em sequência; copiar um lote e apagar 300 arquivos pequenos **ao mesmo tempo** foi **14% pior** que um depois do outro. Meça antes de fixar política.
11. **Build:** `mkfile r <csproj>` grava a saída em `%TEMP%\Compiler\...`; apague ao fim. App empacotado (MSIX) não convive com dois registros; registre o layout do teste e **restaure o instalado** no fim; uma cópia só do app no disco.
### Lições de 25 e 26/10/2026 (Claude): erros reais da AGY, leia antes de qualquer tarefa

12. **Releia a fonte antes de afirmar.** Em 26/10 o resumo dela trocou o escopo de F047 ("descompactação atômica"; é limpar pastas de build), F044 ("drag and drop"; é a barra de espaço), F035 ("abrir pelo Explorer"; é abrir duas vezes seguidas), F046 e F054, e **repetiu** a lista errada de T0 mesmo depois de corrigida. Resumo e handoff saem **depois de reler `spec.md`, `plan.md` e `tasks.md`**, nunca de memória. Se não leu, escreva "não verificado".
13. **Respeite o orçamento de tempo e pare.** A F035 passou de 75 min e **repetiu a mesma bateria várias vezes**, sobrescrevendo o `resultado.csv`. Rode **uma execução por critério**; não repita para "dar verde". Reprovou: investigue a causa; não rode de novo esperando outro resultado.
14. **Todo critério precisa do controle.** AC-5 reprovou e **nunca foi rodado no build sem correção**, então não se sabe se o defeito é da correção, do teste ou anterior. O controle (build **sem** a mudança) vem **antes**. O app instalado (v4.3.6) é o controle pronto.
15. **Teste precisa checar o que o AC diz, não um substituto.** AC-1 pede "as 5 pastas aparecem"; o teste só contou eventos 1026. AC-2 pede "uma janela com 2 pastas"; o CSV diz "4 abas" e ninguém explicou. Meça o que o critério exige (abas, janelas) por UI Automation.
16. **Um arquivo de resultado por execução**, com data e hora no nome; nunca sobrescreva evidência. Relatório gerado do CSV da mesma execução.
17. **Não copie arquivo de outra branch para a sua** (o `Send-GuardedInput.ps1` foi duplicado na F035): peça ao Claude para integrar na ordem certa (a `test-input-guard` entra na `main` antes).
18. **Restaure o ambiente sozinha ao fim:** registro do `KyraFiles` (`Remove-AppxPackage -PreserveApplicationData` + `Add-AppxPackage -Register` do layout instalado), feche as instâncias de teste, apague `bin`/`obj` e `%TEMP%\agy`. Em 26/10 o Claude teve de restaurar e fechar um `KyraFiles.exe` órfão.
19. **Cota:** `--print` usa o login da CLI; se der 429, **pare** e escreva no handoff o que ficou pronto; não troque de conta nem insista.
20. **Build do app depende de dois projetos fora do `KyraFiles.slnx` (medido em 26/10/2026).** Antes de `mkfile d src\KyraFiles.App\KyraFiles.App.csproj`, rode `dotnet restore` de `src\KyraFiles.App.Server` e de `src\KyraFiles.App.IndexService` com `--artifacts-path` igual ao do `mkfile` (`%TEMP%\Compiler\c\desenv\utils\KyraFiles\src\KyraFiles.App\<branch-com-hifens>`) e `dotnet build -c Debug -p:Platform=x64` do Server para gerar o .winmd. Sem isso: NETSDK1004. Linha de base: 25 avisos CS0618 em código gerado. Lógica pura vai em `KyraFiles.Shared/Helpers` e se testa com `dotnet test tests\KyraFiles.App.Search.Index.Tests`.
21. **Padrão validado na F060 M1-T1 (27/10/2026):** motor puro em `KyraFiles.Shared/Helpers` (`SplitEngine`), leitura em blocos de buffer fixo, CRC32 por `System.IO.Hashing.Crc32` (`GetCurrentHash` vem little-endian: inverta para o hexa padrão), manifesto JSON versionado por `JsonSerializerContext`, limpeza das partes no `catch`, temporários do teste em `%TEMP%\agy\<GUID>\...` apagados no `TestCleanup`. Referência de projeto nova no csproj de testes: **só a linha nova**.
23. **Avise o Alexandre no Telegram quando o trabalho parar esperando ação dele** (regra dura, `~/.dev-method/METODO.md` §17): `avisar --origem "KyraFiles / AGY" "o que ele precisa fazer, curto e sem segredo"`. Vale para cota esgotada, bloqueio que você não resolve, decisão que é dele ou fim de lote que depende dele. Não avise por andamento rotineiro. Se o `avisar` falhar, diga no handoff e siga com o que não depende dele.
22. **Erros achados na revisão da M1-T1:** (a) a edição do `.csproj` pôs BOM e tirou a linha em branco final: confira o diff do csproj e não reescreva o arquivo inteiro; (b) o teste só checava `Crc32` "não vazio": todo hash/CRC precisa de **vetor conhecido** (CRC-32 de `123456789` = `CBF43926`) e de conferência independente por parte; (c) o gate F021 abriu o app instalado: rode só quando a tarefa mexer no app, nunca para lógica pura em `KyraFiles.Shared`.
### Lições de 1 e 2/11/2026 (Claude): rename para KyraFiles, build, deploy e Git. Erros reais, leia antes de qualquer tarefa

Os nomes de projeto neste arquivo (`KyraFiles.App`, `KyraFiles.slnx`) são de antes do rename: hoje são `src\KyraFiles.App`, `KyraFiles.slnx` etc.

24. **Rename não é busca-e-troca.** Depois de renomear projeto, pasta ou assembly, ache **quem consome o nome**: (a) `ms-appx:///<Assembly>/...` em XAML (o `Generic.xaml` dos Controls ficou com `KyraFiles.App.Controls` e o app **abria e fechava em 2 s** com `0x802B000A`, `E_XAMLPARSEFAILED`); (b) caminho de executável no código (`IndexServiceProcessManager` procurava `KyraFiles.App.IndexService.exe` com o projeto já gerando `KyraFiles.App.IndexService.exe`: a busca indexada não subiria); (c) `Include` de `.winmd`/DLL em `.csproj` (o `AssemblyName` do Server continuou `KyraFiles.App.Server`, mas o `Include` foi trocado e o build quebrou); (d) nome de processo em monitor de memória, instalador e scripts; (e) pastas `%LOCALAPPDATA%\Files` no guarda de quedas e no atualizador. Verificação, a rodar **antes** de dizer "concluído": `git grep -n "ms-appx:///Files\|Files\.App\.\(IndexService\|Controls\)" -- src tests installer tools`.
25. **Build com 0 erros não prova rename.** A F084/F085 foi dada como concluída sem abrir o app. Rename só é `CONFIRMED` com o app aberto (no horário de teste de tela, regra §16). Antes disso: `NOT TESTED`, escrito assim no relatório.
26. **Diagnóstico de "abre e fecha":** `Start-Process kyrafiles.exe -PassThru` e leia `ExitCode` em hexa (`0x802B000A` = XAML); `Get-WinEvent Microsoft-Windows-AppModel-Runtime/Admin` mostra criação e destruição do contêiner; o `debug.log` está em `%LOCALAPPDATA%\Packages\KyraFiles_sxng2gqmgahde\LocalState`. Ache o erro antes de mexer em código.
27. **`AssemblyName` que ainda é `Files.*` de propósito:** `KyraFiles.App.Server` (o manifesto aponta `KyraFiles.App.Server\KyraFiles.App.Server.exe`) e `KyraFiles.App.BackgroundTasks` (`ActivatableClass`). Mude **assembly, manifesto, `.csproj` e código juntos** (spec F088), nunca só um deles.
28. **O App precisa do `.winmd` do Server dentro de `src\KyraFiles.App.Server\bin\x64\<Config>\...\win-x64\KyraFiles.App.Server.winmd`** (`CsWinRTInputs`). Se apagou `src\*\bin`, rode antes do `mkfile`: `dotnet build src\KyraFiles.App.Server\KyraFiles.App.Server.csproj -c <Config> -p:Platform=x64`. O Server agora está no `KyraFiles.slnx` (o restore da solução o inclui; vale no lugar do passo de restore manual do item 20).
29. **`mkfile r` não empacota Server nem indexador no layout do app** (o `.csproj` só os copia de `bin` dentro do projeto). Para instalar use `pwsh tools\Deploy-Release.ps1`: ele copia os dois de `%TEMP%\Compiler\...\bin`, apaga restos do nome antigo e inicia o Watchdog. Não copie arquivo na mão.
30. **Depois do deploy, registre o pacote:** `Add-AppxPackage -Register <pasta>\AppxManifest.xml -ForceApplicationShutdown -ForceUpdateFromAnyVersion`. Manifesto novo sem registro deixa `Executable` e alias antigos (o `KyraFiles.exe` não existia mais). Alias atual: `kyrafiles.exe`; a identidade do pacote ainda é `KyraFiles` até a F087.
31. **Saída do `mkfile` é enorme** (25 avisos `CS0618` de código gerado se repetem 3 vezes). Nunca cole inteira: filtre por `Select-String -Pattern ' error |Warning\(s\)|Error\(s\)|Build succeeded|FAILED'`. Linha de base: 25 avisos `CS0618`, todos em `*.g.cs`, 0 erros.
32. **Git, uma entrega = um commit.** O `pre-push` recusa em `main`, `master` e `colab` merge commit e mais de 1 commit novo (novo = ainda não está na `main` remota). Nunca `--no-verify`. Force só com autorização literal do Alexandre e `--force-with-lease=<ref>:<sha>`. Não use `git branch -f` na branch em checkout (use `git switch` e `merge --ff-only`). Mensagem de commit com here-string `@'...'@` (um array de linhas vira uma linha só).
33. **Não deixe trabalho só em branch ou worktree.** Em 2/11/2026 havia 8 worktrees e 14 branches com trabalho que nunca voltou para a `colab` (instalador A3, medições F063/F065), em pastas cujo `.git` apontava para um caminho renomeado. Ao encerrar: devolva na `colab` ou escreva no handoff o hash e o motivo. Antes de apagar branch: `git bundle create <arquivo> --branches` e `git cherry <colab> <branch>`.
34. **Checkout compartilhado:** `ia check` antes de editar. Arquivo que mudou sem ser você (ex.: `csproj`, `Package.appxmanifest`) **não se reverte**: pergunte. Não rode `git switch` nem build com a árvore suja de outra IA.
35. **Mudança de empacotamento só com spec:** `WindowsPackageType`, `Identity Name`, alias e protocolo mudam como o Windows trata o app (Win+E, associações, pasta de dados). Entram na F087, não como ajuste solto.
36. **Instância única (F086):** um processo principal por sessão (mutex `Local\KyraFiles-<ambiente>-MainInstance`); abertura nova redireciona para ele. Nunca crie processo novo para "abrir em nova janela" nem para a opção `OpenTabInExistingInstance`. Medição: `tools/perf/measure_f086_instances.ps1` (a fazer), só fora do horário do Alexandre, sem roubar foco.
37. **Estado do que foi feito é `NOT TESTED` até alguém abrir o app.** Handoff diz por `AC-n`: F086 e a abertura do app após o rename estão `NOT TESTED` em 2/11/2026.

## (Encerrada) Notas da tarefa T17a a T17e (correções achadas pela medição da T17)

1. Leia antes: `tools/perf/README.md`, seção "F015 T17 — latência com snapshot" (números, perfil e diagnóstico). As linhas T17a a T17e do `tasks.md` da F015 dizem o "pronto" de cada uma.
2. **T17a e T17b primeiro, com teste que falha antes da correção.** Na T17b o teste tem de mostrar o arquivo de `A\B\` perdido no código atual.
3. Na T17a o resultado tem de ser **idêntico** ao do algoritmo antigo: mantenha o antigo num método de teste e compare os conjuntos de itens vivos.
4. Na T17c, o reconciliador da abertura e o do estouro são **o mesmo** por volume (não dois `Task.Run` concorrendo pelo lock).
5. Medição de aceite: `pwsh -File tools\perf\f015_latency.ps1` e `pwsh -File tools\perf\f015_trace_reconcile.ps1`, a saída colada no commit. O serviço tem de ser recompilado em Release (`mkfile r` na solução) antes, senão o script roda o binário velho do `AppX`.
6. A T17e **não muda código**: só mede e propõe.

## Notas para a tarefa atual (F025: baseline do upstream e trace, só medição)

1. Leia, nesta ordem: `docs/agents/tasks/F025-baseline-upstream-e-trace.md` (o brief com os passos P1 a P6), `DECISIONS.md` D-023, a seção "Como diagnosticar desempenho" acima e `tools/perf/README.md`.
2. **Você não altera `src/`.** Se achar um bug, registre como `DISCOVERY` no relatório.
3. Os scripts da F024 estão no git: `git show ecb9c6d29:tools/perf/<nome>`. Restaure com prefixo `f025_` e parâmetro `-Exe`.
4. Dois builds do app empacotado não convivem registrados ao mesmo tempo: meça um, desregistre, meça o outro e anote a ordem.
5. Worktree do upstream fica ao lado do repositório (`..\KyraFiles-upstream-base`), não em `%TEMP%`.
6. Relatório em `docs/agents/evidence/F025/relatorio.md`, cada afirmação com estado de evidência e comando. A palavra "inviável" não aparece sem número e trace.
7. Ao terminar: handoff, `AGENT_CONTEXT.md` atualizado e o Claude revisa antes de qualquer limpeza.

20. **Relatório de situação (regra R13, 26/10/2026):** começo de tarefa lê `docs/agents/SITUACAO.md`; fim de tarefa (ou parada por cota ou orçamento) atualiza o arquivo e escreve no handoff: o que deu certo, o que deu errado e o ponto do projeto, nessa ordem.

21. **Nada é pulado sem prova ou autorização (regra dura, 29/10/2026):** leia `docs/agents/REGRA-NAO-PULAR.md` antes de começar e antes de cada handoff. Todo item pedido é executado e provado, ou avaliado com prova de que não se aplica, ou perguntado ao Alexandre (so a resposta dele libera). Pulo autorizado exige a frase "PULADO COM AUTORIZACAO DO ALEXANDRE em ..." com citacao literal, no handoff, no commit, na nota de liberacao e na spec.

## Protocolo Anti-Reincidência: Purga Total de "Files" (16/11/2026, REGRA DURA)

Origem: a F091 foi declarada "zero resíduos" com um verificador que só olhava `Files.(App|Shared|Core|...)` em `src/`, `tests/` e 2 harnesses, ignorando `.md`. Auditoria do Claude achou 86 linhas `Files.*` e 749 de outras variantes fora disso. Meta nova do Alexandre: **nenhuma sobra, desvinculação completa do KyraFiles.**

### Erros cometidos (não repetir)
1. **Provou com a própria régua.** O verificador foi escrito pela mesma IA que fez o trabalho, com escopo menor que a afirmação. "Zero" só vale para o que a busca cobre.
2. **Padrão estreito.** Procurou só `Files.X`; esqueceu `KyraFiles`, `KyraFiles`, `KyraFiles`, `KyraFilesOpenDialog`, "KyraFiles", "KyraFiles contributors", URL `kyrafiles`.
3. **Escopo estreito.** Só `src/tests`; esqueceu `installer/`, `tools/`, `.github/`, `.iss`, `.vcxproj`, `.idl`, `.rgs`, `.xaml`, `.resw`, `.json`, `.yml`, `.csproj`, `.props`, `.slnx`, `.gitignore`, `crowdin.yml`.
4. **Esqueceu nomes de arquivo e pasta**, e não só conteúdo (`Assets\KyraFilesOpenDialog`, `KyraFiles_*.png`).
5. **Esqueceu texto visível ao usuário** (runtime, `.resw`, manifesto, instalador, tela Sobre).
6. **Lista de exceções aberta e sem justificativa.** Exceção vira esconderijo.
7. **Declarou CONFIRMED sem repetir o comando de ponta a ponta** no estado final da branch.
8. **Copiou "0 erros / 4 aprovados" de relatório anterior** sem rodar de novo (METODO §7 e disciplina de verificação, item 5).

### Regras
- **R1. Verificador antes do trabalho.** Primeiro amplie `tools/check-legacy-names.ps1` e rode: ele DEVE falhar com a lista completa atual. Esse é o backlog. Trabalho termina quando sai 0. Commit separado, antes de qualquer renomeação.
- **R2. Padrão por conteúdo, sem diferenciar maiúscula:** `(?<![A-Za-z0-9])Files(?![A-Za-z0-9]*System)` mais as variantes juntas, em TODOS os arquivos versionados (`git ls-files`), sem `:!docs` e sem `:!*.md`. Palavra comum em inglês ("files", "Files" como "arquivos") não é marca: tratar em lista de **falsos positivos nominal** (arquivo + trecho + motivo), nunca por glob.
- **R3. Também por nome:** `git ls-files` e as pastas, com o mesmo padrão. Renomear com `git mv`.
- **R4. Também binário e gerado:** depois do build autorizado, varrer `AppxManifest.xml`, `.exe/.dll` (`FileDescription`, `ProductName`, nome do arquivo), saída do instalador e `package_manifest.json`. Fonte gerada não se edita: corrija o gerador/entrada.
- **R5. Lista de exceções fechada**, em `tools/legacy-names-allowlist.json` (arquivo, trecho exato, motivo). Só duas categorias aceitas: (a) licença/atribuição exigida; (b) o que o Alexandre decidir manter (ver `DECISIONS.md`). Exceção sem motivo = falha do verificador.
- **R6. Fatiar a refatoração gigante.** Um pacote por commit coeso, por categoria (ver ordem abaixo); build e verificador no fim de cada pacote, nunca só no final.
- **R7. Compatibilidade antes de renomear identidade persistente.** Antes de trocar qualquer CLSID, nome de pipe, mutex, chave de registro, AppUserModelID, nome de pacote, protocolo URI, nome de processo ou pasta de dados: registrar em `DECISIONS.md` o que muda e a migração. Nunca trocar GUID por "limpeza".
- **R8. Prova no estado final, não no meio.** Relatório lista: comando exato, saída literal, commit (`git rev-parse HEAD`), árvore limpa (`git status --short` vazio). Afirmação sem isso é `INFERRED`.
- **R9. Auditoria independente.** Ao fechar, a AGY entrega o comando de varredura e o Claude o roda com um padrão próprio. Divergência = devolvido.
- **R10. Aviso de build.** Este é o Heloize-PC: build só com autorização do Alexandre (`<YouNeedDo>`). Sem build, os ACs de binário ficam `NOT TESTED`, nunca `CONFIRMED`.
- **R11. Resíduo no disco do usuário.** Todo nome antigo purgado do repo precisa de `[InstallDelete]`/limpeza no instalador e de varredura em `%LOCALAPPDATA%`, `%APPDATA%`, `HKCU\Software`, tarefas agendadas e atalhos (regra de contenção de binários).

### Ordem dos pacotes
1. Verificador ampliado + allowlist vazia (falha esperada) + inventário em `docs/specs/<ID>/inventario.md`.
2. Nomes de pasta e arquivo (`git mv`) e referências a eles.
3. Componentes nativos (`OpenDialog`, `SaveDialog`, `Launcher`: `.idl`, `.h`, `.rgs`, `.vcxproj`) e workflows `.github/`.
4. `installer/` e `tools/` (inclui `tools/installer/Program.cs`, `Create-SelfContained-Release.ps1`, spikes, `perf/`, `_desativados`).
5. Textos: comentários "KyraFiles: Fxxx" (substituir por "KyraFiles: Fxxx" ou remover), strings de runtime, `.resw`, manifesto, Sobre.
6. Código de migração de dados legados: aplicar a decisão do Alexandre (manter / manter por N versões / remover).
7. `.md` e `docs/`: ver decisão sobre histórico.
8. Relatório final (F091 corrigido) e auditoria do Claude.

### Limite jurídico (não é negociável pela AGY)
`LICENSE-MIT` exige manter o aviso "Copyright (c) 2018 - present KyraFiles contributors" e a permissão; `LICENSE-MPL` vale para os arquivos MPL. Essas atribuições ficam e são a única exceção da categoria (a). Remover o aviso viola a licença.

### Modo acelerado (16/11/2026, Alexandre: app fora de produção; commit 539a85108 mantido na main)
- O commit `539a85108` fica. A correção segue por cima dele, em commits pequenos (pode ser na `main`, a pedido do Alexandre; registrar no ledger).
- **Sem usuários legados:** remover o código de migração de dados `Files`/`KyraFiles`/`KyraFiles contributors` (`UserSettingsMigrationService`, `LegacyNamespacePrefix`, migração de registro em `LayoutPreferencesDatabase`/`FileTagsDatabase`, `[InstallDelete]` do nome antigo que não tenha mais o que apagar). Ajustar os testes de migração (apagar os que testam só o legado).
- **`.md` e `docs/`:** substituição mecânica por script versionado (`tools/purge-names.ps1`), com a mesma lista de padrões do verificador, aplicada em lote e conferida por diff por categoria. Renomear com `git mv` os arquivos `*KyraFiles*` de `docs/releases`. Não tocar `LICENSE-MIT`, `LICENSE-MPL`, `.github/NOTICE.md`.
- **Pacotes 1, 2 e 3 viram um só commit de verificador + inventário;** depois um commit por categoria (nativo/`.github`, `installer`+`tools`, textos/`.resw`, migração, docs). Ainda vale R1 (verificador falhando antes), R8 e R9.
- **Atenção a identidade persistente (R7) continua:** CLSID/GUID não muda; só o nome simbólico.
- Build: pedir autorização uma vez ao fim, para o conjunto, em vez de por pacote.
