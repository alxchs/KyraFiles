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

### UI WinUI 3 (`src/Files.App`)

- **`UIElement.Clip` só aceita `RectangleGeometry`** (12/10/2026: `PathGeometry` dá CS0266). Para recorte livre use a camada de Composition: `ElementCompositionPreview.GetElementVisual(el).Clip = compositor.CreateGeometricClip(compositor.CreatePathGeometry(new CompositionPath(CanvasGeometry.CreatePath(new CanvasPathBuilder(null)))))`. Modelo pronto: `Helpers/UI/ShapeGeometryHelper.cs`.
- Em `CanvasPathBuilder`, arco que contorna a curva para fora num desenho com Y para baixo é `CanvasSweepDirection.Clockwise`; `Counterclockwise` abaula para dentro da forma.
- Erros em cascata `XamlCompiler error WMC0001/WMC0110` (dezenas, "Unknown type ResourceString") quase sempre vêm de **um** erro de C# no projeto: ache primeiro `error CS`, corrija e recompile, antes de mexer em XAML.
- Não assine lambda em evento dentro de `Loaded` (dispara mais de uma vez): use método nomeado e `-=` antes de `+=`.

### Processo

- Uma tarefa = um commit `tipo(f015): resumo (Tn)`; marcar a linha da tarefa em `tasks.md` com data e a evidência (comando + número).
- Número de desempenho só com o comando que mediu; sem isso, "estimado, não medido".
- Mudança sem conteúdo (só linha em branco no fim do arquivo) não entra em commit.

## Notas para a tarefa atual (T5: `VolumeIndex` + `QueryEngine`)

1. **Casamento sobre UTF-8, sem decodificar por entrada.** O índice guarda o nome normalizado em UTF-8, mas `QueryMatcher.MatchesNormalized` recebe `ReadOnlySpan<char>`. Decodificar 5 milhões de nomes por consulta mata a meta de < 100 ms. Acrescente ao `QueryMatcher` um `MatchesNormalizedUtf8(ReadOnlySpan<byte>)`: substring em bytes é válida em UTF-8 (`IndexOf` de span); no curinga, `?` deve consumir **um ponto de código** (1 a 4 bytes), não um byte. Testes: os mesmos casos da T4 rodando nas duas versões, com resultado igual, mais um caso de `?` sobre `ç`.
2. Nomes num único `byte[]` grande + offsets (`int[]`), não um `byte[]` por entrada (cada array custa ~24 bytes de cabeçalho e uma referência para o GC).
3. Caminho montado subindo pelos pais **só para os resultados entregues**, nunca para todo candidato.
4. Lotes: 200 itens ou 30 ms no primeiro, depois 100 ms; teto com sinal de "limitado"; `CancellationToken` verificado a cada bloco de alguns milhares de entradas.
5. Microbenchmark fora dos testes unitários (ou marcado para não rodar sempre), com 5 milhões de entradas sintéticas; o commit traz o tempo até o primeiro lote e a RAM por milhão (`GC.GetTotalMemory(true)` e working set), com o comando.
