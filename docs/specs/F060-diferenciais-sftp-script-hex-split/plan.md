# F060 — PLAN

Status: **APROVADO** (Claude, 26/10/2026). Spec: `spec.md` (aprovada). Estados: CONFIRMED (li o código e rodei algo), INFERRED (deduzido), NOT TESTED.

## 1. Arquitetura atual (lida no código)
- Lógica pura testável fica em `src/Files.Shared/Helpers/` (ex.: `PathFormatter.cs`, `ChecksumHelpers.cs`, que já tem `CreateMD5/SHA1/SHA256/SHA384/SHA512` e um uso de `Crc32`). CONFIRMED (grep em 26/10/2026).
- Testes unitários rodam em `tests/Files.App.Search.Index.Tests` (13 testes do `PathFormatter` passaram com `dotnet test --filter PathFormatter`). CONFIRMED. Arquivo de `Files.Shared` entra nos testes pelo mesmo mecanismo já usado pela F056/F057 (T0 de cada módulo confirma o vínculo no csproj).
- Ações de menu: `[GeneratedRichCommand]` + `IAction` em `src/Files.App/Actions/...`; o gerador cria `Commands.<Nome>` sozinho (F056 compilou sem registro manual). CONFIRMED.
- Menu de contexto: `ContentPageContextFlyoutFactory.cs`. Configurações: `GeneralSettingsService` + `GeneralViewModel` + `GeneralPage.xaml`. CONFIRMED (F056).
- Progresso e cancelamento: Status Center (`StatusCenterViewModel`, `StatusCenterItem`, `StatusCenterHelper`). INFERRED (só li os nomes); a T0 de M1 confirma o contrato.
- Busca: `src/Files.App/Utils/Storage/Search/FolderSearch.cs` e índice `src/Files.App.Search.Index` (F023). INFERRED; a T0 de M5 mapeia onde entram filtros.
- FTP: FluentFTP 55.0.0 em `src/Files.App.Storage/Ftp/*`. SFTP: nenhuma ocorrência. CONFIRMED.
- Regras de build: `mkfile d src\Files.App\Files.App.csproj`; antes, `dotnet restore` de `Files.App.Server` e `Files.App.IndexService` à parte com o `--artifacts-path` do `mkfile` (não estão no `Files.slnx`). CONFIRMED (26/10/2026).

## 2. Ordem de entrega
Critério do Alexandre: o que é independente primeiro, para esvaziar a fila; dependência de outro item sempre antes do dependente.

| # | Entrega | Depende de | Por quê nesta posição |
|---|---|---|---|
| 1 | **M1 Dividir/Juntar** | nada | Núcleo puro, risco baixo, reaproveita CRC32/SHA-256 |
| 2 | **M5 Busca por frase** (núcleo e chips) | só da busca atual | Tradutor puro e testável; a ação "salvar" fica à parte |
| 3 | **M2 Editor hexadecimal** | nada | Núcleo puro; UI própria; precisa de decisão de salvamento (D-F060-1) |
| 4 | **M5-T4 salvar busca** | **F042** (que depende da F023) | Só depois da F042 |
| 5 | **M3 SFTP** | decisão de biblioteca (T0); cópia na fila depende da **F036** | Provedor e segurança antes; integração com a fila depois da F036 |
| 6 | **M4 Script** | decisão de linguagem (T0); ações em lote dependem da **F036** | Maior superfície de segurança; por último |

Dependências externas a destravar antes dos itens 4 a 6: **F036** (T7/T8 aguardam D-F036-1) e **F042**.

## 3. Abordagem por módulo
- **M1:** `SplitEngine` (Files.Shared): lê o original em blocos (buffer fixo, sem carregar o arquivo), grava partes `nome.001`, `nome.002`... e `nome.manifest.json` (versão 1: nome lógico, tamanho total, tamanho da parte, lista de partes com nome, tamanho, CRC32; SHA-256 do total opcional). `CombineEngine` valida (ordem, ausentes, duplicadas, tamanhos, CRC32 de cada parte) **antes** de montar; monta em `.tmp` na pasta de destino e promove com renomeio só após o CRC32 do total bater. Original nunca é tocado. Opções descartadas: formato de 7-Zip/HJSplit (acopla a terceiros; Q5 decidido por recomendação).
- **M5:** `NaturalSearchParser` (Files.Shared), PT e EN, vocabulário em tabela (extensão, grupos "fotos/planilhas/pdf", datas relativas, tamanho, local). Saída: `SearchIntent` (lista de filtros + trechos não entendidos). A tela mostra chips; ao confirmar, aplica os filtros na busca existente. "Salvo/alterado" = modificação; "criado" = criação; "semana passada" = últimos 7 dias (Q8). Flag experimental em `GeneralSettingsService`, desligada.
- **M2:** `HexFileBuffer` (Files.Shared): leitura por blocos com cache LRU limitado, mapa de edições (offset → byte) e pilha de desfazer; só **substitui** bytes na v1 (sem inserir/remover, tamanho do arquivo nunca muda). Salvar (D-F060-1, recomendação): conferir identidade (tamanho e data de modificação) → gravar só os blocos alterados no próprio arquivo, com diário de recuperação ao lado → apagar o diário. Sem espaço extra necessário. UI: visualizador virtualizado só com as linhas visíveis (T3 depende da T1 e T2).
- **M3:** biblioteca candidata SSH.NET (ADR-007, T0 decide e checa licença/versão). Provedor SFTP separado do FTP; host key confiada só após confirmação e guardada por usuário; credenciais com proteção do Windows (DPAPI do usuário, INFERRED, T0 confirma o que o app já usa para FTP).
- **M4:** linguagem decidida por protótipo (ADR-008: Lua/MoonSharp ou JavaScript/Jint). API restrita (`fs.list/filter`, `queue.copy/move/rename`, `log`); simular obrigatório na primeira execução.

## 4. Mapa requisito → código
| Req | Onde |
|---|---|
| M1-AC-1..6, FR-M1 | `Files.Shared/Helpers/SplitEngine.cs`, `CombineEngine.cs`, `SplitManifest.cs`; ações em `Actions/FileSystem/SplitFileAction.cs` e `CombineFilesAction.cs`; diálogo novo |
| M5-AC-1..5, FR-M5 | `Files.Shared/Helpers/NaturalSearchParser.cs`; ligação em `FolderSearch.cs`/índice (T0 de M5 define o ponto) |
| M5-AC-6 | integração com F042 (bloqueado até a F042) |
| M2-AC-1..7, FR-M2 | `Files.Shared/Helpers/HexFileBuffer.cs`; página nova no app |
| M3, M4 | definidos nas T0 respectivas |

## 5. Arquivos a mexer e a não mexer
- Mexer: arquivos novos acima; `Strings/en-US/Resources.resw`; menu (`ContentPageContextFlyoutFactory.cs`); `GeneralSettingsService`/`IGeneralSettingsService` só para a flag do M5.
- **Não mexer:** `FilesystemOperations`/copiar/mover existentes (F036 é dona), `FtpStorage*` (M3 cria provedor novo), qualquer arquivo de `src/` fora do necessário a cada tarefa; nenhum pacote NuGet novo sem o gate (§10.7 da SPEC original) escrito na T0 do módulo.

## 6. Riscos
- Perda de dados: M1 (destino sobrescrito), M2 (edição in-place). Mitigação: temp + promoção, diário, conferência de identidade, testes com arquivos de teste em `%TEMP%\agy\<GUID>\`.
- Memória: M1/M2 com arquivos de 4 GB. Mitigação: buffers fixos; medição obrigatória com `dumpsys`-equivalente (contador de processo) antes de declarar.
- Segurança: M3 (host key, credencial), M4 (script). Mitigação: ADR e revisão do Claude antes do merge.

## 7. Estratégia de teste por AC
Unitário no `Files.Shared` para cada AC puro (M1-AC-1,3,4,5,6; M5-AC-1,3,4,5; M2-AC-2,4,5,6,7). AC de tela (diálogos, chips, grade hexa, SFTP real) só **depois das 23:59** e sem roubar foco, por UI Automation; sem foco = NOT TESTED. Cada teste cita o AC no nome.
