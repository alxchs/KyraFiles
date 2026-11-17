# Handoff Claude — F065 correções (T6..T10) — 30/10/2026

Branch: `feature/f065-correcoes` (worktree `KyraFiles-claude`), integrada na `colab` por fast-forward. Substitui, para efeito de estado, a tabela de `AC-n` do handoff `2026-10-30_1220_agy_conclusao-f065-busca-nome-e-caminho.md`, que marcou CONFIRMED sem prova.

## O que mudou
- **T6** `ListedItem`: a opacidade nasce 1 (`D-F065-1` em `DECISIONS.md`). Cobre o motor Agent Ransack, que criava resultados sem opacidade, e qualquer motor futuro.
- **T7** `EverythingResultLine` (classe pura em `Services/Search/`): lê a linha `caminho<TAB>atributos<TAB>tamanho<TAB>data` do `es.exe`; o `EverythingSearchProvider` passou a usá-la. Ligada ao projeto de testes como o `DialogAccessKeys.cs`. Testes novos em `EverythingResultLineTests.cs`, com linhas copiadas de uma execução real do `es.exe` (normal, oculto, pasta por atributo, pasta por barra final, caminho entre aspas, linhas vazias).
- **T8** BOM removido de `ShellViewModel.cs` e `DetailsLayoutPage.xaml.cs` (`git diff main` não mostra mais a linha 1).
- **T9** `SearchItemOpacityAndPathTests.cs` apagado: recalculava a regra dentro do teste e montava as colunas numa ordem que o `es.exe` não usa (`caminho, tamanho, data, atributos`).

## Provas
- Build do app: `dotnet build src/KyraFiles.App/KyraFiles.App.csproj -c Release -p:Platform=x64` → `Build succeeded`, `0 Warning(s)`, `0 Error(s)`, 3m39s (13:25). Antes, NETSDK1004 do `KyraFiles.App.Server` numa worktree nova; resolvido com `dotnet restore` do `KyraFiles.App.Server` e do `KyraFiles.App.IndexService`.
- Build dos testes: `0 Error(s)`, `3 Warning(s)`; os 3 são CA2014 em `HexFileSaverTests.cs` (F060), anteriores a esta mudança.
- Suíte: `KyraFiles.App.Search.Index.Tests.exe` → `total: 160`, `failed: 0`, `succeeded: 159`, `skipped: 1` (13:20).
- Os testes pegam o defeito: com a leitura dos atributos trocada para a 4ª coluna (a ordem que o teste antigo supunha), `--filter FullyQualifiedName~EverythingResultLineTests` deu `total: 6`, `failed: 2` (`AC6_HiddenAttribute_IsReadFromSecondColumn`, `Folder_FromDirectoryAttribute_HasNameAndNoSize`), saída 2. Restaurada a leitura da 2ª coluna: `total: 6`, `failed: 0`, saída 0. Log em `%TEMP%\claude-f065-prova.log`.
- Formato real conferido com `es.exe -tsv -no-header -attrib -size -dm -n 4 -path "D:\DriveD\Downloads" *.exe` (13:0x): `D:\...\7z.exe<TAB>8224<TAB>749608<TAB>19/12/2019 17:05:38`.

## Estado por cenário

| ID | Estado | Base |
|---|---|---|
| AC-1 (Everything, nome e ícone visíveis) | CONFIRMED só na leitura da linha (teste `AC1_NormalFile_HasNamePathAndIsVisible`); NOT TESTED na tela | tela às 09:00 de 31/10 (T12) |
| AC-2 (índice interno) | NOT TESTED | o provedor já definia a opacidade; nenhum teste o exercita; tela às 09:00 de 31/10 |
| AC-3 (nativo) | NOT TESTED | idem AC-2 |
| AC-4 (coluna Caminho) | NOT TESTED | duas correções da AGY convivem; qual resolve fica para T11 às 09:00 de 31/10 |
| AC-5 (busca a partir de pasta aberta) | NOT TESTED | T11 |
| AC-6 (oculto esmaecido) | CONFIRMED só na leitura do atributo oculto (teste `AC6_HiddenAttribute_IsReadFromSecondColumn`); NOT TESTED na tela | T12 |
| AC-7 (Caminho some fora da busca) | NOT TESTED | T11 |
| NFR-001 (desempenho) | NOT TESTED | a medição T0/T4 da AGY rodou o `es.exe` sozinho, fora do app; medir no app às 09:00 de 31/10 (T12) |
| Agent Ransack (FR-001) | INFERRED | coberto pela opacidade padrão (T6); sem tela |

## Fica com a AGY
T11 e T12 de `docs/specs/F065-busca-nome-e-caminho/tasks-correcoes.md`, às 09:00 de 31/10 ou com o Alexandre ausente, sem roubar foco.
