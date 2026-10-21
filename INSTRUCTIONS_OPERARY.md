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
