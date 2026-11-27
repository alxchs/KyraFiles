# F096 — PLAN

Status: **APROVADO** (Claude, 27/11/2026).

## Arquitetura lida no código (CONFIRMED, 27/11/2026)
- `tests/KyraFiles.App.Search.Index.Tests/OperationQueueTests.cs` `Benchmark_TenRounds_GeneratesEvidenceCsv` grava `medicoes_t2_concorrencia.csv`.
- `OperationMemoryBenchmarkTests.cs` `MemoryBenchmark_TwoFilesTwoGiB_ThreeRounds_GeneratesCsvAndMeetsNfr001` grava `medicoes_t6_memoria.csv`.
- `PauseGateTests.cs` `PauseGate_OverheadBenchmark_OneThousandSmallFiles_ThreeRounds_GeneratesCsv` grava `medicoes_t7_sobrecarga.csv`.
- Os três sobem seis níveis a partir de `AppContext.BaseDirectory` até `docs/agents/evidence/F036`, e formatam `double` com a cultura atual (`F1`/`F2` em pt-BR sai com vírgula).
- Cada teste já tem uma pasta temporária própria em `%TEMP%\agy\<GUID>\...`, apagada no `finally`.

## Decisão
- Helper `TestEvidence.GetCsvPath(feature, fileName, tempFallbackDirectory)`: com `KYRAFILES_WRITE_EVIDENCE=1` devolve o caminho no repositório; sem, devolve `<pasta temporária do teste>\evidence\<feature>\<arquivo>` (apagado com ela).
- Formatação com `CultureInfo.InvariantCulture` (`AppendLine(IFormatProvider, ...)` e `string.Create`).
- Descartado: pular a gravação sem a variável (o `Assert` de arquivo gravado deixaria de exercitar a escrita).

## Arquivos
- Mexer: os três testes acima e `TestEvidence.cs` (novo).
- Não mexer: código do app, limites dos testes, planilhas commitadas.

## Teste por AC
- AC-1: `dotnet test` sem a variável e `git status --short` sem CSV.
- AC-2/AC-3: `dotnet test --filter` dos três testes com a variável ligada; conferir colunas das planilhas; depois restaurar as planilhas (fora de escopo regenerá-las).
- AC-4: total de testes verde.
