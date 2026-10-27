# INSTRUCTIONS_OPERARY

Instruções para a executora (AGY/Gemini) neste repositório. Mantido pelo Claude na revisão de cada tarefa. Complementa `AGENTS.md`, `MASTER_SPEC.md` e a spec da feature em andamento; não os substitui.

## Base de Conhecimento e Padrões Sólidos

### Testes (`tests/Files.App.Search.Index.Tests`)

- **`dotnet test` não executa os testes deste projeto** (11/10/2026: "Zero tests ran", código de saída 5, embora a descoberta ache 15). Rode o executável gerado, que usa o Microsoft.Testing.Platform:
  `tests\Files.App.Search.Index.Tests\bin\x64\Release\net10.0-windows10.0.26100.0\Files.App.Search.Index.Tests.exe`
  Use `--list-tests` para listar. A evidência "N/N passou" cita essa saída (`total`, `succeeded`).
- Nome do teste começa pelo `AC-n` quando cobre um cenário de aceite (ex.: `AC15_BuscaAcao_MatchesExpectedVariants`).
- MSTest, `[TestClass] public sealed class`, `Assert.*`. Usings globais em `GlobalUsings.cs` do projeto de teste.

### Código da biblioteca de índice (`src/Files.App.Search.Index`)

- Cabeçalho `// Copyright (c) Files Community. Licensed under the MIT License.`, namespace por arquivo (`namespace X;`), tabs, comentários XML em português.
- Caminho quente sem alocação: `ReadOnlySpan<T>`, `ref readonly` sobre arrays de structs, nada de LINQ dentro do laço de consulta.
- `NameNormalizer.Normalize` (T4): FormD → tira `NonSpacingMark` → minúsculas invariantes → FormC. Limitações conhecidas, aceitas por ora: letras sem decomposição (`ø`, `ł`, `ß`, `æ`) não viram ASCII; marca combinante fora do BMP não é removida.
- `QueryMatcher` (T4): palavras separadas por espaço = E; termo **sem** curinga é substring; termo **com** `*`/`?` casa o nome **inteiro** (ancorado, como o Everything: `rel*` = começa com "rel"; `*.pdf` = termina em ".pdf"). `?` vale um `char` UTF-16.
- Arquivos novos com fim de linha CRLF (o Git avisa "LF will be replaced by CRLF": converta antes de commitar).
- **Nada de "para cada X, percorrer o índice inteiro"** (lição da T17, 16/10/2026: a passada 2 do `Reconciler` e o `MarkDeleted` de pasta eram pastas × itens e nunca terminavam no C: com 2,3 milhões de itens). Com mais de um alvo, junte os alvos num `HashSet`/`Dictionary` e faça **um** passe sobre o índice. Todo laço sobre `index.Count` dentro de outro laço precisa de justificativa no commit.
- **Lock do índice (`SyncRoot`) em fatias curtas.** Trabalho longo (reconciliação, revarredura) solta o lock a cada ~1.000 pastas. Lock segurado por minutos trava watcher, snapshot e a reconciliação da abertura.
- **Trabalho de fundo em prioridade baixa e sem tempestade**: thread em `THREAD_MODE_BACKGROUND_BEGIN`; evento repetido (estouro do watcher) só marca "pendente" e um único executor roda depois de um intervalo sem eventos.
- **Publique cada volume assim que ficar pronto**; nunca espere todos os discos para mostrar o primeiro.
- **Medição do serviço fora do pacote** usa `%LOCALAPPDATA%\FilesCustom\Index` (não a `LocalCache` do pacote). Para indexar do zero sem apagar o cache real, passe `--cache <pasta temporária>`.
- Perfil de CPU: `tools/perf/f015_trace_reconcile.ps1` (`dotnet-trace` já instalado). Antes de dizer de onde vem o CPU, rode o perfil.

### UI WinUI 3 (`src/Files.App`)

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

13. **O exe de teste do Alexandre sai de `dotnet build src/Files.App/Files.App.csproj -c Release -p:Platform=x64` + `.\Open-FilesDev.ps1`.** O `mkfile` compila para `%TEMP%\Compiler` e falha no `Files.App.Server`: não atualiza o `AppX`.
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
18. **Restaure o ambiente sozinha ao fim:** registro do `FilesDev` (`Remove-AppxPackage -PreserveApplicationData` + `Add-AppxPackage -Register` do layout instalado), feche as instâncias de teste, apague `bin`/`obj` e `%TEMP%\agy`. Em 26/10 o Claude teve de restaurar e fechar um `Files.exe` órfão.
19. **Cota:** `--print` usa o login da CLI; se der 429, **pare** e escreva no handoff o que ficou pronto; não troque de conta nem insista.
20. **Build do app depende de dois projetos fora do `Files.slnx` (medido em 26/10/2026).** Antes de `mkfile d src\Files.App\Files.App.csproj`, rode `dotnet restore` de `src\Files.App.Server` e de `src\Files.App.IndexService` com `--artifacts-path` igual ao do `mkfile` (`%TEMP%\Compiler\c\desenv\utils\FilesApp\src\Files.App\<branch-com-hifens>`) e `dotnet build -c Debug -p:Platform=x64` do Server para gerar o .winmd. Sem isso: NETSDK1004. Linha de base: 25 avisos CS0618 em código gerado. Lógica pura vai em `Files.Shared/Helpers` e se testa com `dotnet test tests\Files.App.Search.Index.Tests`.
21. **Padrão validado na F060 M1-T1 (27/10/2026):** motor puro em `Files.Shared/Helpers` (`SplitEngine`), leitura em blocos de buffer fixo, CRC32 por `System.IO.Hashing.Crc32` (`GetCurrentHash` vem little-endian: inverta para o hexa padrão), manifesto JSON versionado por `JsonSerializerContext`, limpeza das partes no `catch`, temporários do teste em `%TEMP%\agy\<GUID>\...` apagados no `TestCleanup`. Referência de projeto nova no csproj de testes: **só a linha nova**.
23. **Avise o Alexandre no Telegram quando o trabalho parar esperando ação dele** (regra dura, `~/.dev-method/METODO.md` §17): `avisar --origem "FilesApp / AGY" "o que ele precisa fazer, curto e sem segredo"`. Vale para cota esgotada, bloqueio que você não resolve, decisão que é dele ou fim de lote que depende dele. Não avise por andamento rotineiro. Se o `avisar` falhar, diga no handoff e siga com o que não depende dele.
22. **Erros achados na revisão da M1-T1:** (a) a edição do `.csproj` pôs BOM e tirou a linha em branco final: confira o diff do csproj e não reescreva o arquivo inteiro; (b) o teste só checava `Crc32` "não vazio": todo hash/CRC precisa de **vetor conhecido** (CRC-32 de `123456789` = `CBF43926`) e de conferência independente por parte; (c) o gate F021 abriu o app instalado: rode só quando a tarefa mexer no app, nunca para lógica pura em `Files.Shared`.
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
5. Worktree do upstream fica ao lado do repositório (`..\FilesApp-upstream-base`), não em `%TEMP%`.
6. Relatório em `docs/agents/evidence/F025/relatorio.md`, cada afirmação com estado de evidência e comando. A palavra "inviável" não aparece sem número e trace.
7. Ao terminar: handoff, `AGENT_CONTEXT.md` atualizado e o Claude revisa antes de qualquer limpeza.

20. **Relatório de situação (regra R13, 26/10/2026):** começo de tarefa lê `docs/agents/SITUACAO.md`; fim de tarefa (ou parada por cota ou orçamento) atualiza o arquivo e escreve no handoff: o que deu certo, o que deu errado e o ponto do projeto, nessa ordem.
