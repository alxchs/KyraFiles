# F043 — Histórico de operações com desfazer — PLAN

Status: APROVADO (Claude, 25/10/2026). Spec aprovada pelo Alexandre em 24/10/2026 (ponto em aberto decidido: guardar 200 entradas por padrão, dados só locais). **Execução bloqueada** até o Claude liberar (Onda 3).

## 1. Arquitetura atual (lida no código em 25/10/2026)
- **CONFIRMED** `Utils/Storage/History/StorageHistoryWrapper.cs` (52 linhas): uma **pilha linear com um índice** (`index`, `histories`). `AddHistory` insere no índice e **apaga a cauda de "refazer"**; `GetCurrentHistory`, `CanUndo`, `CanRedo`, `DecreaseIndex`, `IncreaseIndex`. É **só em memória** (registrado como singleton em `AppLifecycleHelper.cs:404`; some ao fechar o app).
- **CONFIRMED** `StorageHistory` guarda `OperationType`, `Source` e `Destination` (listas de `IStorageItemWithPath`). **Não tem** hora, nome do usuário, resultado (sucesso/parcial/falha) nem identificador.
- **CONFIRMED** `StorageHistoryHelpers` desfaz/refaz **sempre o item do topo** (`GetCurrentHistory()`), chamando `StorageHistoryOperations.Undo(history)`/`Redo(history)`. Esse `Undo` aceita **qualquer** `IStorageHistory` como argumento, mas depende de `ShellFilesystemOperations(associatedInstance)`, ou seja, de uma página do shell (interface) para executar.
- **CONFIRMED** quem registra: `FilesystemHelpers` chama `App.HistoryWrapper.AddHistory(history)` quando `registerHistory == true` (copiar, mover, apagar, renomear, criar). **INFERRED:** exclusão definitiva e operações com `registerHistory == false` **não entram** no histórico hoje; T0 lista exatamente quais.
- **CONFIRMED** o Centro de Status (`StatusCenterViewModel`, `StatusCenterItem`) já lista as operações da sessão (cartões de cópia, mover, apagar), mas **sem desfazer por item e sem persistência**.

## 2. Abordagem
1. **`OperationHistoryEntry` (novo, puro):** `Id`, `Timestamp`, `Kind` (copiar, mover, apagar, renomear, criar), `Result` (sucesso, parcial, falha), `Reversible` (bool + motivo), `Sources` e `Destinations` como **texto de caminho + tipo** (não guarda objetos do WinRT), e uma **referência opcional** ao `IStorageHistory` vivo (só nesta sessão). A pilha atual de desfazer/refazer **não muda**: o histórico novo é uma **visão paralela** alimentada no mesmo ponto onde hoje se chama `AddHistory`, mais as operações **não reversíveis** (apagar definitivamente) com `Reversible = false`.
2. **`OperationHistoryStore`:** lista limitada (200 por padrão, configurável), mais nova primeiro; **persistência em arquivo JSON local** na pasta de dados do app (`operation-history.json`), gravada em segundo plano com escrita atômica (temporário e renomear); leitura no início, tolerante a arquivo corrompido (começa vazio e registra o motivo); **"Limpar histórico"** nas configurações. Só caminhos, nunca conteúdo; dados **só locais** (decisão da spec).
3. **Desfazer por entrada (AC-2 a AC-4):** `UndoEligibility` (puro, testável) decide se **uma entrada específica** ainda pode ser desfeita olhando o disco: para **mover** o destino precisa existir e a origem estar livre; **apagar** (Lixeira) o item precisa estar na Lixeira; **renomear** o nome novo precisa existir e o nome antigo estar livre (senão pergunta, AC-4); **criar** o item precisa existir. Se não puder, devolve o **motivo** e **nada é alterado** (AC-3). Quando puder, executa pelo `StorageHistoryOperations.Undo(entrada.Historico)` existente (FR-002: não cria outro mecanismo) e marca a entrada como "desfeita" **sem mexer no índice da pilha** quando não for a do topo. Entradas **restauradas do arquivo** (sem o `IStorageHistory` vivo) são **reconstruídas** a partir dos caminhos para o desfazer funcionar também depois de reiniciar; se a reconstrução não for possível, ficam `Reversible = false` com motivo.
4. **Tela:** `OperationHistoryDialog` (diálogo, mesmo padrão do `BulkRenameDialog`) com lista **virtualizada**, filtro por tipo, busca por nome (com atraso de digitação) e botão "Desfazer" por linha; abre pelo comando "Histórico de operações" (paleta de comandos e menu do Centro de Status). Entradas "apagar definitivamente" aparecem como **não reversíveis**, sem botão (AC-6).
5. **Operação grande vira uma entrada** (milhares de arquivos: uma linha com a contagem e os primeiros caminhos, o resto guardado só como contagem; guarda no máximo N caminhos por entrada, N configurável).
Opções descartadas: (a) reescrever a pilha para um modelo de várias entradas independentes (mexe no `Ctrl+Z` que todos usam hoje: risco de regressão); (b) persistir os objetos `IStorageHistory` (não são serializáveis); (c) banco de dados (excessivo para 200 linhas).

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001 (registrar com resultado) | ponto onde `FilesystemHelpers` chama `AddHistory` + `ShellOperationResult` para parcial/falha |
| FR-002 (reaproveitar o desfazer) | `StorageHistoryOperations.Undo(IStorageHistory)` existente |
| FR-003 (limite e limpar) | `OperationHistoryStore` + configurações |
| NFR-001 (sem atraso na operação) | gravação do arquivo em segundo plano; medição em T6 |
| AC-1, AC-5 | `OperationHistoryStore` (ordem, persistência) |
| AC-2, AC-3, AC-4 | `UndoEligibility` + `StorageHistoryOperations.Undo` |
| AC-6 | entrada com `Reversible = false` |
| AC-7 | lista virtualizada + filtro com atraso; medição em T6 |

**Arquivos a mexer:** novos `OperationHistoryEntry`, `OperationHistoryStore`, `UndoEligibility`, diálogo e viewmodel; `FilesystemHelpers.cs` (só acrescentar o registro paralelo nos pontos onde já se registra, sem mudar a lógica); `AppDialogService` (registro do diálogo); comando novo; página de configurações (limite e limpar); `Strings/en-US`. **A NÃO mexer:** `StorageHistoryWrapper` (a pilha do `Ctrl+Z`), a lógica interna de `StorageHistoryOperations.Undo/Redo`, `Directory.Build.props`.

## 4. Riscos
- **Desfazer fora de ordem** pode conflitar com operações posteriores (ex.: desfazer o mover de A depois de A ter sido renomeado): por isso o `UndoEligibility` confere o disco **antes** e explica; nunca "tenta e vê".
- **Reconstrução após reiniciar** é a parte mais frágil (sem o objeto vivo): começa só com **mover, renomear e criar**; apagar para a Lixeira depende de achar o item na Lixeira pelo nome original (T0 investiga como o `Undo` atual o faz).
- **Privacidade:** o arquivo guarda caminhos que o usuário abriu/moveu; o limite, o "Limpar" e a opção de **desligar a persistência** têm de existir desde a primeira versão.
- **Concorrência:** operações simultâneas (F036) escrevem entradas ao mesmo tempo: o `Store` precisa ser seguro entre threads.
- **Desempenho:** 500 entradas com filtro não podem travar a interface (virtualização e filtro com atraso).

## 5. Estratégia de teste (por AC, com asserção)
- **Sem janela:** testes unitários do `OperationHistoryStore` (ordem, limite, serialização e leitura, arquivo corrompido, segurança entre threads) e do `UndoEligibility` em pasta de teste em `%TEMP%\agy\<GUID>\...` com arquivos gerados (AC-1, AC-3, AC-4, AC-5, AC-6); medição do filtro com 500 entradas (AC-7) sem interface.
- **O desfazer de verdade** (`StorageHistoryOperations.Undo`) exige uma página do shell: AC-2 e a prova ponta a ponta só em **janela de AWAY ou Sandbox**, com instância própria.
- **Controle negativo:** desfazer mover quando o destino foi apagado depois **não** pode alterar nada e deve dar o motivo. `resultado.csv` em `docs/agents/evidence/F043/`.
