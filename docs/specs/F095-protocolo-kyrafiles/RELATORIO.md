# F095 — Relatório de conclusão (Claude, 26/11/2026)

Re-auditoria de `a6fc46b0b` (correções P1 a P3 da auditoria v4.3.33) e fechamento da spec retroativa.

## Estado por AC
| AC | Estado | Evidência |
|---|---|---|
| AC-1 a AC-5 | CONFIRMED | `dotnet test tests/KyraFiles.App.Search.Index.Tests/... -c Release -p:Platform=x64` → `Aprovado! total: 327` (inclui os 6 `ProtocolUriParser_*`) |
| AC-4 | CONFIRMED (código) | `Program.cs` usa `ProtocolUriParser.Parse(...) is { Action: ProtocolAction.Cmd }`; o parser antigo de `?cmd=` saiu |
| AC-6 | INFERRED | auditoria de 25/11 leu o `.iss`; instalador não reexecutado hoje |
| AC-7 | CONFIRMED | `TextEncodingHelper_DetectEncoding_Windows1252AndUtf8` verde |
| AC-8 | INFERRED | código revisado (`PrependSampleStream` devolve a amostra e depois o resto); nenhuma origem sem seek disponível para teste |
| AC-9 | CONFIRMED (simulação) | as 4 trocas do script aplicadas em memória a `src/**/*.cs,*.cpp`: 8 arquivos alterados, 0 com `kyrafiles-preview-preview` ou `namespace kyrafiles-`. CD real nunca rodou: `NOT TESTED` |
| NFR-1 | CONFIRMED | `dotnet build src/KyraFiles.App/KyraFiles.App.csproj -c Release -p:Platform=x64` → `0 Erro(s)`; o aviso novo `CsWinRT1028` em `PrependSampleStream` foi eliminado (85 → 83 avisos, os restantes são anteriores) |

## Re-auditoria de `a6fc46b0b`
- P1 `Configure-AppxManifest.ps1`: **APROVADO**. Efeitos colaterais aceitáveis: a palavra-chave de ajuda `"kyrafiles"` (`HelpCatalog.cs`) e o nome de canal em `AppUpdate*Service.cs` também viram `kyrafiles-<canal>`, como no script do ancestral.
- P2 leitor único, constante única, condição duplicada, versão exata: **APROVADO**.
- P2 `ReadTextAsync` sem seek: **APROVADO**; ressalva: o laço respeita `maxLength` só por bloco (anterior à F095).
- P3 `f090_scale_test.ps1`: **APROVADO** (aborta com KyraFiles aberto; grava RatioW/RatioH).
- `Constants.cs` perdeu o BOM no commit; sem efeito no build.

## DISCOVERY
- Os testes `OperationQueueTests` e `OperationMemoryBenchmarkTests` gravam CSV em `docs/agents/evidence/F036/` a cada `dotnet test`, sujando a árvore (origem do stash `f036-csvs-from-test` e da P5). Corrigir: gravar só com variável de ambiente explícita.

## Desempenho
Sem mudança em caminho quente além da leitura da prévia (uma cópia de até 4 KB a mais só quando o stream não tem seek). Não medido.
