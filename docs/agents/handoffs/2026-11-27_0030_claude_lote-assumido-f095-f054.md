# Handoff — Claude, 27/11/2026: lote assumido (F095 fechada na main, F054 T1 na branch)

Pedido do Alexandre (26/11): "Assuma o máximo de specs"; merge local na `main` sem push autorizado para cada SPEC fechada; ordem do METODO §26.

## Na `main` (local, sem push)
- `f44ae60a8` reconciliação do controle de produção.
- `5651b6372` F095: spec/plan/tasks retroativos, re-auditoria de `a6fc46b0b` aprovada, aviso `CsWinRT1028` removido. Build Release x64 0 erros; 327 testes verdes. SPEC-0079 fica em `in_validation`: AC-6 (instalador) INFERRED, CD real NOT TESTED.

## Na branch `feature/F054-technical-preview` (`4c5ea3117`)
- T1: realce para JSON, YAML, INI/TOML, shell, SQL, Python; coluna de número de linha; paleta sem vermelho (também no `CssPreviewRenderer`).
- Correção de recurso: `BaseStorageFile.ReadTextAsync` agora para em `maxLength` (antes lia o arquivo inteiro mesmo com limite: 200 MB viravam ~400 MB de string). Contagem de linhas/palavras sem `Split`.
- 5 testes `PreviewCodeLanguagesTests` verdes. Tela NOT TESTED.
- Abertas: T0 (baseline, medir na 4.3.33 instalada), T2 (árvore), T3 (CSV), T4 (acompanhar/limite com aviso/FileShare), T5 (prova de tela e NFR-001).

## Não executado e por quê
- F063 C6 (`measure_f063_memory_restore.ps1`): minimiza/restaura a janela 10 vezes e pega a instância já aberta; restaurar ativa a janela (rouba foco). Rodar só com o Alexandre longe.

## DISCOVERY
- `OperationQueueTests`/`OperationMemoryBenchmarkTests` gravam CSV em `docs/agents/evidence/F036/` a cada `dotnet test` (sujam a árvore; origem da P5 e do stash `f036-csvs-from-test`). A restauração dos 3 CSVs foi negada pelo classificador; ficaram modificados, fora dos commits.
- Gate F021 passou, com avisos: CPU acima de 5% em repouso e pastas `src\*\bin\x64\Debug` de 25/11 (não criadas nesta sessão, exceto talvez `BackgroundTasks`, 26/11 23:17, do `mkfile d` que falhou).

## Próximo
F054 T0 → T2 → T3 → T4 → T5, depois merge local; em seguida F039.
