# F037 — Renomear em lote com pré-visualização — PLAN

Status: APROVADO (Claude, 24/10/2026). Spec aprovada pelo Alexandre em 24/10/2026. **Execução bloqueada** até o Claude liberar (`docs/agents/ORDEM-RIGIDA-AGY.md`).

## 1. Arquitetura atual (lida no código em 24/10/2026)
- **CONFIRMED** `Actions/FileSystem/RenameAction.cs:47-52`: com mais de um item selecionado abre `BulkRenameDialogViewModel` pelo `IDialogService`; com um item, renomeia no lugar.
- **CONFIRMED** `ViewModels/Dialogs/BulkRenameDialogViewModel.cs` (63 linhas): **um único campo de nome-base** (sem ponto, validado por `IsValidForFilename`). `DoCommitRenameAsync` percorre `context.SelectedItems` e, para cada item, chama `ShellPage.FilesystemHelpers.RenameAsync(item, FileName + item.FileExtension, NameCollisionOption.GenerateUniqueName, true, false)`. Ou seja: **funciona, mas sem prévia, sem regras e sem conflito visível**; a numeração vem do `GenerateUniqueName` do sistema (`nome (2)`), não de uma regra.
- **CONFIRMED** o histórico aceita lote: `StorageHistory` guarda `Source` e `Destination` como **listas** (`Utils/Storage/History/StorageHistory.cs`) e `StorageHistoryOperations` tem `Undo`/`Redo`. **INFERRED**: `Undo` de um `FileOperationType.Rename` com várias entradas funciona; T0 confirma lendo `StorageHistoryOperations`.
- **CONFIRMED** a abertura do diálogo é registrada em `Services/App/AppDialogService.cs:30`.
- **CONFIRMED** F001 (blindagem de extensões duplas e dotfiles) **não foi feita** (está no backlog), e `item.FileExtension` é o que o diálogo usa hoje.

## 2. Abordagem
1. **`RenameRuleEngine` (puro, testável):** recebe a lista de nomes de entrada e uma lista ordenada de regras; devolve a lista `nome atual → novo nome` e os conflitos. Regras: localizar/substituir (texto e regex, com maiúsculas opcionais), numeração (início, passo, zeros), caixa (alta, baixa, título), data (do arquivo ou da foto), remover trecho, trocar espaços. Sem tocar o disco.
2. **`NameParts`:** separa nome e extensão **uma só vez** (dotfiles como `.gitignore` sem extensão; `a.tar.gz` com extensão dupla reconhecida por lista configurável). Entrega o pedido da F001 dentro da F037 (T1), e a F001 passa a reaproveitá-lo.
3. **Conflitos (FR-002):** (a) nomes iguais entre si; (b) nome que já existe fora da seleção; (c) nome inválido no Windows (`:`/`?`, terminar em ponto/espaço, nomes reservados); (d) caminho acima do limite. Linha em conflito fica marcada e "Aplicar" desabilita (AC-2).
4. **Aplicar em duas fases** para suportar troca de nomes (A→B e B→A): renomear primeiro para nomes temporários únicos e depois para os finais, **dentro de um só** `StorageHistory` (FR-003, AC-3). Falha de um item não aborta os demais; o histórico cobre só o que mudou (AC-6).
5. **Diálogo novo** reaproveitando o ponto de abertura atual: lista de regras à esquerda, prévia ao vivo à direita, botão "Aplicar". Prévia recalculada **fora da thread de UI** com cancelamento a cada tecla (NFR-001: 5.000 arquivos em ≤ 500 ms).
6. **Predefinições** (FR-004) salvas em configuração. **Edição como texto** (US4, decidida: entra) como segunda aba: um nome por linha; o número de linhas tem de bater, senão não aplica.
Opções descartadas: (a) manter o `GenerateUniqueName` do sistema para conflito (esconde o conflito do usuário e viola o AC-2); (b) renomear item a item registrando um histórico por item (desfazer não seria "tudo de uma vez").

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001 (prévia) | `RenameRuleEngine` + prévia no diálogo |
| FR-002 (conflitos) | `RenameRuleEngine` (validação) |
| FR-003 (desfazer em lote) | aplicar em duas fases + um `StorageHistory` com listas; `App.HistoryWrapper.AddHistory` |
| FR-004 | configuração do usuário (predefinições) |
| NFR-001 | prévia em segundo plano com cancelamento; medição em T5 |
| AC-4 (extensões) | `NameParts` |

**Arquivos a mexer:** novos `RenameRuleEngine`, `NameParts`, diálogo e viewmodel novos (ou substituição controlada do `BulkRenameDialog*`), `AppDialogService` (registro), `RenameAction` (só a chamada), `Strings/en-US`. **A NÃO mexer:** `FilesystemOperations.RenameAsync` (só chamar), `StorageHistoryOperations` (só ler, salvo se T0 provar que o `Undo` em lote falha; aí abrir `DISCOVERY` e parar), a F033.

## 4. Riscos
- **Perda de arquivo** por colisão no meio da troca de nomes: por isso duas fases com nomes temporários únicos e teste de troca A↔B.
- **Desfazer parcial**: testar falha no meio (arquivo em uso) e o desfazer.
- **Regex catastrófica** travando a UI: tempo limite na avaliação e prévia fora da thread de UI.
- **Fuso e data da foto:** a data EXIF pode faltar; definir regra de recuo (data de criação) e mostrar na prévia.

## 5. Estratégia de teste (por AC, com asserção)
Testes unitários do motor (AC-2, AC-4, AC-5, AC-7) com tabela de entradas e saídas, incluindo `a.tar.gz`, `.gitignore`, troca A↔B, nomes reservados e 5.000 itens (tempo). Teste de integração em pasta de teste em `%TEMP%\agy\<GUID>\...` (AC-3 aplicar e desfazer, AC-6 falha parcial com arquivo aberto pelo teste). Interface (diálogo, prévia, botão desabilitado) por UI Automation com o helper revisado ou Sandbox. Controle negativo: um lote com conflito **não** pode aplicar. `resultado.csv` em `docs/agents/evidence/F037/`.
