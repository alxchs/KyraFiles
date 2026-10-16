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

## Notas para a tarefa atual (T17a a T17e: correções achadas pela medição da T17)

1. Leia antes: `tools/perf/README.md`, seção "F015 T17 — latência com snapshot" (números, perfil e diagnóstico). As linhas T17a a T17e do `tasks.md` da F015 dizem o "pronto" de cada uma.
2. **T17a e T17b primeiro, com teste que falha antes da correção.** Na T17b o teste tem de mostrar o arquivo de `A\B\` perdido no código atual.
3. Na T17a o resultado tem de ser **idêntico** ao do algoritmo antigo: mantenha o antigo num método de teste e compare os conjuntos de itens vivos.
4. Na T17c, o reconciliador da abertura e o do estouro são **o mesmo** por volume (não dois `Task.Run` concorrendo pelo lock).
5. Medição de aceite: `pwsh -File tools\perf\f015_latency.ps1` e `pwsh -File tools\perf\f015_trace_reconcile.ps1`, a saída colada no commit. O serviço tem de ser recompilado em Release (`mkfile r` na solução) antes, senão o script roda o binário velho do `AppX`.
6. A T17e **não muda código**: só mede e propõe.
