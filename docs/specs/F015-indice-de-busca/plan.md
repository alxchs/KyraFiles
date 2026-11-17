# F015 — Índice de busca próprio — PLAN

Status: APROVADO (Claude, 11/10/2026), condicionado às duas verificações T1 e T2: se o resultado delas contrariar uma premissa marcada `INFERRED` abaixo, o plan volta para revisão antes de T3.

## Arquitetura atual (lida no código em 11/10/2026)

- `CONFIRMED` A busca da lista de arquivos entra por `ShellViewModel.SearchAsync(FolderSearch)` (`src/KyraFiles.App/ViewModels/ShellViewModel.cs`, ~linha 3479). Ela pede o motor a `ISearchProviderFactory.GetCurrentProvider()`; se não for `native`, chama `provider.SearchAsync(request, results, token)` e só atualiza a tela **no fim**. Os resultados parciais (`SearchTick`) só existem para o `FolderSearch` nativo.
- `CONFIRMED` Os motores estão em `src/KyraFiles.App/Services/Search/` (`NativeFilesSearchProvider`, `AgentRansackSearchProvider`, `EverythingSearchProvider`, `SearchProviderFactory`), registrados como singleton em `Helpers/Application/AppLifecycleHelper.cs` (linhas 374-377). O contrato é `Data/Contracts/ISearchProvider.cs` (`SearchRequest`: `Query`, `Folder`, `MaxItemCount`, `SearchSubfolders`, `SearchContent`).
- `CONFIRMED` Agent Ransack e Everything abrem um processo por busca, esperam ele sair, leem um CSV e chamam `Directory.Exists` para cada resultado; criam `ListedItem` só com caminho e nome (sem tamanho e sem data). O Everything exige `es.exe`, ausente nesta máquina; sem ele, cai no nativo sem aviso, enquanto `IsAvailable` diz `true` se o processo `Everything` está aberto.
- `CONFIRMED` Dois outros pontos usam `FolderSearch` direto, sem passar pela fábrica: as sugestões do Omnibar (`ViewModels/UserControls/NavigationToolbarViewModel.cs`, ~linha 1217, `MaxItemCount = 10`, espera 200 ms) e o Atualizar de uma página de resultados (`Views/Shells/BaseShellPage.cs`, ~linha 599).
- `CONFIRMED` `FolderSearch` (`Utils/Storage/Search/FolderSearch.cs`) trata `Folder == "Home"` buscando em todos os `drivesViewModel.Drives` que não são de rede, bibliotecas via `App.LibraryManager`, e consultas de etiqueta (`IsTagQuery`). Usa o índice do Windows e cai em varredura Win32.
- `CONFIRMED` A escolha de motor é `SearchEnginePreference` (`Data/Enums/SearchEngineKind.cs`: 0 Native, 1 AgentRansack, 2 Everything), lida em `Services/Settings/FoldersSettingsService.cs` e exibida por `ViewModels/Settings/FoldersViewModel.cs`.
- `CONFIRMED` O pacote (`src/KyraFiles.App/Package.appxmanifest`) já declara `runFullTrust`, `broadFileSystemAccess` e **`allowElevation`**, e já leva um executável à parte, `KyraFiles.App.Server` (servidor WinRT fora do processo, `WinExe`, `net10.0-windows`), modelo para empacotar mais um executável.
- `CONFIRMED` Testes C#: o repositório usa MSTest 4.4.1 (`Directory.Packages.props`). Não há projeto de teste unitário para a lógica do app; `tests/` tem testes de interação e scripts PowerShell.
- `CONFIRMED` Ambiente de referência (11/10/2026, `Get-Volume`, `Get-CimInstance Win32_MountPoint`/`Win32_Volume`, `Get-Disk`): C: 930 GB NVMe, D: 806 GB e R: 125 GB no disco SATA, S: 466 GB no disco USB, todos NTFS; volume do Google Drive montado em `C:\Google\Drive\disk` (relatado como FAT32, 930 GB); dois volumes de sistema sem letra; nenhum `subst`, nenhuma unidade de rede.

## Abordagem

### Visão geral

Um **motor de índice** único, numa biblioteca sem interface (`KyraFiles.App.Search.Index`), roda dentro de um **processo próprio** (`KyraFiles.App.IndexService.exe`), que funciona de dois jeitos:

| | Modo completo | Modo básico |
|---|---|---|
| Como roda | serviço do Windows, conta SYSTEM, instalado uma vez com UAC (método decidido em T1) | processo do usuário, sem privilégio, iniciado pelo KyraFiles |
| Indexação inicial NTFS | lê a MFT bruta do volume em blocos sequenciais grandes | varre pastas com `GetFileInformationByHandleEx(FileIdBothDirectoryInfo)`, em paralelo por disco físico |
| Mudanças com o KyraFiles aberto | lê o diário USN (`FSCTL_READ_USN_JOURNAL`) | `ReadDirectoryChangesExW` recursivo por volume; estouro do buffer → reconferir só a subárvore afetada |
| Mudanças com o KyraFiles fechado | retoma do último USN salvo; diário reiniciado ou truncado → reindexa o volume e diz por quê | ao abrir, reconfere pastas cuja data de modificação mudou (a data de uma pasta muda quando entra, sai ou é renomeado um item dela) |
| Volumes | NTFS | todos os locais não cobertos pelo completo (FAT32, exFAT, ReFS, virtuais) |
| Permissão (FR-025) | filtra cada resultado com a identidade do usuário que perguntou (personificação do cliente do pipe), com cache por pasta | natural: o processo só enxerga o que o usuário enxerga |

O índice fica **no processo do motor, não no KyraFiles** (NFR-003). o KyraFiles conversa com ele por **named pipe** com protocolo versionado: consulta, resultados em lotes, cancelamento, estado e comandos de configuração. No modo completo, o pipe é do serviço; no básico, um pipe por sessão do usuário. Volumes NTFS podem estar no serviço e os demais no processo básico ao mesmo tempo: o KyraFiles consulta os dois e junta os resultados.

### Identidade de volume e caminhos (FR-010 a FR-016)

- **Chave de tudo:** `(GUID do volume, número de referência do arquivo)`. Dois caminhos que chegam ao mesmo arquivo dão a mesma chave, o que torna a duplicata impossível por construção (FR-014).
- **Tabela de volumes** (`FindFirstVolumeW` + `GetVolumePathNamesForVolumeNameW` + `GetDriveTypeW`):
  - cada GUID guarda a lista de caminhos de montagem;
  - a raiz de exibição é a primeira letra ou, sem letra, a pasta de montagem mais curta (FR-013);
  - `DRIVE_REMOTE` fica fora (FR-017);
  - volume sem letra e sem montagem fica fora (FR-002).
- **Letras `subst`** (`QueryDosDeviceW`, destino `\??\...`): viram apelidos, nunca volumes (FR-011).
- **Escopo da busca:**
  - a pasta atual é resolvida com `GetFinalPathNameByHandleW(VOLUME_NAME_GUID | FILE_NAME_NORMALIZED)`, que atravessa de uma vez `subst`, junção, link simbólico e ponto de montagem e devolve volume e caminho real;
  - a consulta roda no lugar real;
  - os resultados voltam com o prefixo real trocado pelo prefixo por onde o usuário navegou (FR-015).
  - "Este computador" e "Home" usam todos os volumes indexados, com suas raízes de exibição.
- **Links e montagens:**
  - a indexação nunca desce por ponto de reanálise (FR-012): no básico, pasta com `FILE_ATTRIBUTE_REPARSE_POINT` é registrada e não aberta; no completo, cada volume só contém o que é dele;
  - assim, ciclos não existem (AC-9).
- **Link físico** (FR-016):
  - no completo, o "local original" é o **primeiro atributo `$FILE_NAME` não-DOS** do registro da MFT. Os demais nomes são descartados.
  - no básico, um `FileId` repetido no mesmo volume é link físico; para esses arquivos (raros), `FindFirstFileNameW` diz o primeiro nome.
  - Se as duas ordens coincidem (`INFERRED`, verificado em T13 comparando os dois modos no mesmo volume), os modos dão o mesmo resultado.
  - Nomes 8.3 (namespace DOS) nunca entram.

### Estrutura do índice e consulta (FR-020 a FR-024, NFR-001, NFR-003)

- **Armazenamento** por volume, em arrays contíguos, sem um objeto por arquivo:
  - por entrada: pai (índice), referência do arquivo, tamanho, datas de criação e de modificação, atributos;
  - nome original em UTF-8;
  - nome **normalizado**: minúsculas, sem acento (decomposição + remoção de marcas combinantes), também em bytes.
  - A estimativa é de ~90 bytes por item, dentro da meta de 100 MB por milhão (`INFERRED`, medido em T5).
- **Consulta:**
  - varredura paralela do buffer normalizado com busca vetorizada (`MemoryExtensions.IndexOf` sobre `byte`);
  - várias palavras = todas presentes; `*` e `?` = curingas em segmentos.
  - O escopo é um conjunto de bits das pastas descendentes da pasta de escopo, calculado na hora e com cache.
  - O caminho é montado subindo pelos pais, só para os itens enviados.
- **Envio** em lotes: o primeiro lote assim que houver 200 itens ou 30 ms, depois a cada 100 ms.
  - Teto padrão de 100 mil resultados, com sinal de "limitado" para a tela.
  - Cancelamento verificado entre blocos.

### Integração no KyraFiles

- **Motor da busca:** novo `IndexedSearchProvider` (`id = "index"`) vira o padrão para busca por nome.
  - O contrato `ISearchProvider` ganha entrega progressiva (callback de lote).
  - `ShellViewModel.SearchAsync` passa a atualizar a tela por lote para qualquer motor.
  - Os `ListedItem` já saem com tamanho, datas e atributos do índice (FR-024).
- **Sugestões do Omnibar e Atualizar** passam pela fábrica, deixando de chamar `FolderSearch` direto.
- **Continuam no `FolderSearch`:**
  - escopo em unidade de rede (FR-017);
  - consulta por etiqueta;
  - volume ainda sem índice.
  - Em todos os casos a tela diz por quê (FR-041), nunca em silêncio.
- **Configurações:** a página de busca troca a escolha de motor da F011 por:
  - o estado do índice por volume (FR-042);
  - exclusões e reindexação (FR-003);
  - instalar e remover o modo completo (FR-041).
  - Agent Ransack e Everything saem da busca por nome. O código deles fica; a remoção é tarefa futura (spec §6).

### Opções descartadas

- **Índice dentro do processo do KyraFiles:** a RAM cresceria com o índice (NFR-003), e o modo completo precisaria de privilégio no próprio Files.
- **Índice de pesquisa do Windows (Windows Search):** não cobre todas as pastas, não acha tudo por padrão e é a lentidão que já existe hoje.
- **`FSCTL_ENUM_USN_DATA` em vez da MFT bruta no modo completo:** dá nomes e pais, mas não tamanho nem datas. Buscá-los arquivo por arquivo seria lento e violaria FR-024. Fica como plano B se a leitura da MFT se mostrar inviável em T2.
- **Banco de dados (SQLite etc.) para o índice:** consulta por substring sem índice próprio é varredura de qualquer jeito, e com mais sobrecarga que arrays em memória.

## Mapa spec → código

| Requisito | Onde é atendido |
|---|---|
| FR-001, FR-002, FR-011, FR-013, FR-017 | `KyraFiles.App.Search.Index/Volumes/VolumeTable` |
| FR-003, FR-042 | protocolo `Status`/`Configure` + página de configurações |
| FR-004 | indexadores nunca abrem arquivo; atributos `RECALL_ON_*` respeitados |
| FR-010, FR-014 | chave `(volume, referência)` em `Index/VolumeIndex` |
| FR-012 | `Basic/DirectoryScanner` (não desce reparse) e `Full/MftReader` |
| FR-015 | `Volumes/ScopeResolver` |
| FR-016 | `Full/MftReader` (primeiro `$FILE_NAME`) e `Basic/HardLinkResolver` |
| FR-020, FR-021 | `Query/NameNormalizer`, `Query/QueryMatcher` |
| FR-022, FR-023 | `Query/QueryEngine` + `IndexedSearchProvider` + `ShellViewModel.SearchAsync` |
| FR-024 | `IndexedSearchProvider` monta `ListedItem` completo |
| FR-025 | `Service/AccessFilter` (completo); natural no básico |
| FR-026 | filtro de ocultos e de sistema aplicado no KyraFiles com a preferência atual |
| FR-030, FR-031 | `Full/UsnFollower`; `Basic/ChangeWatcher` |
| FR-032 | `Basic/Reconciler` |
| FR-033 | `Volumes/VolumeArrival` + snapshot com número de série e ID do diário |
| FR-040, FR-041, FR-043 | `KyraFiles.App.IndexService` (modos), `IndexedSearchProvider` (fallback com aviso) |
| NFR-001 a NFR-005 | scripts `tools/perf/f015_*.ps1` |
| NFR-006 | snapshots do completo em `%ProgramData%\KyraFiles\Index` com ACL SYSTEM + Administradores; do básico na `LocalCache` do pacote |
| NFR-007 | `SearchProviderFactory` + configurações |

## Arquivos

**Mexer:**
- Novos:
  - `src/KyraFiles.App.Search.Index/` (biblioteca `net10.0-windows`, sem WinUI);
  - `src/KyraFiles.App.IndexService/` (executável dos dois modos);
  - `tests/KyraFiles.App.Search.Index.Tests/` (MSTest);
  - `tools/perf/f015_*.ps1`.
- Existentes:
  - `Files.slnx`;
  - `Package.appxmanifest`, para empacotar o novo exe e o serviço, conforme T1;
  - `KyraFiles.App.csproj`;
  - `Data/Contracts/ISearchProvider.cs`;
  - `Services/Search/SearchProviderFactory.cs`;
  - novo `Services/Search/IndexedSearchProvider.cs`;
  - `Helpers/Application/AppLifecycleHelper.cs`, para o registro;
  - `ViewModels/ShellViewModel.cs`, só `SearchAsync`;
  - `ViewModels/UserControls/NavigationToolbarViewModel.cs`, só as sugestões;
  - `Views/Shells/BaseShellPage.cs`, só `Refresh_Click`;
  - `ViewModels/Settings/FoldersViewModel.cs` e a página XAML correspondente, ou uma página nova de busca;
  - `Strings/*/Resources.resw`;
  - textos de ajuda da F014 sobre busca.

**Não mexer:**
- `FolderSearch.cs`, salvo o mínimo para ser chamado como reserva;
- a ação F3 da F013;
- `AgentRansackSearchProvider.cs` e `EverythingSearchProvider.cs`, que só deixam de ser usados para busca por nome;
- o redirecionador da F012 e os diálogos de arquivo;
- o restante do `ShellViewModel`.

## Riscos e regressões a vigiar

- **Leitura da MFT bruta:**
  - correções de sequência de atualização (fixups);
  - listas de atributos em registros fragmentados;
  - registros de base vs. extensão;
  - MFT fragmentada.
  - Mitigação: comparar, no mesmo volume, o resultado do completo com o do básico (T13).
- **Serviço SYSTEM é superfície de ataque:**
  - o pipe valida tudo o que recebe;
  - nenhum caminho vindo do cliente é aberto como SYSTEM sem personificação;
  - ACL do pipe só para usuários autenticados locais;
  - sem comando que leia ou altere arquivo.
- **Versão do serviço diferente da do KyraFiles** depois de atualizar: aperto de mão de versão; se não bate, o KyraFiles cai no básico e oferece atualizar o serviço (UAC).
- **Antivírus** reclamando de serviço que abre `\\.\C:`: assinatura do pacote e nome claro do serviço.
- **Google Drive:** o volume virtual pode responder devagar ou baixar dados ao ser listado (`INFERRED` que não baixa; medido em T2). Se baixar, ele sai do índice por padrão, com opção de incluir.
- **Regressão na busca atual:** etiquetas, bibliotecas e rede continuam no `FolderSearch` e precisam ser testadas de novo.
- **Memória do índice no serviço sempre ligado:** medida em T17 contra o NFR-003.

## Estratégia de teste

| Cenário | Tipo | Como se prova |
|---|---|---|
| AC-1, AC-2 | funcional no app + medição | `tools/perf/f015_search_latency.ps1`: digita por UI Automation, mede até a lista aparecer, 10 repetições, mediana e pior caso |
| AC-3, AC-4 | integração + funcional | cria `subst` em pasta de teste, busca, conta ocorrências por caminho e por chave |
| AC-5, AC-6, AC-9 | integração | junção (sem admin) e link simbólico em `%TEMP%\claude\desenv\utils\KyraFiles\f015\`; ciclo junção→ancestral |
| AC-7, AC-8 | integração (requer admin) | VHDX de teste montado em pasta e com letra, criado e removido pelo script com autorização do Alexandre; alternativa observável: o volume do Google Drive (montado só em pasta) |
| AC-10, AC-11, AC-12 | integração | script cria, renomeia, move e apaga e mede o tempo até a busca refletir; com o KyraFiles fechado e aberto de novo |
| AC-13 | manual com roteiro | desconecta e reconecta o disco USB S:, com captura |
| AC-14 | funcional no app | serviço parado ou desinstalado → estado e aviso na tela, com captura |
| AC-15, AC-21 | unitário + integração | normalizador e casador; link físico com `fsutil hardlink create` (sem admin) |
| AC-16 | integração (requer segundo usuário) | conta local de teste criada pelo Alexandre; busca como usuário comum não mostra o perfil dela |
| AC-17 | medição | bytes baixados pelo processo do Google Drive antes e depois de indexar (contadores de rede do processo) |
| AC-18, AC-19, AC-22 | funcional no app | Esc, troca de pasta, ordenar, menu de contexto, unidade de rede (compartilhamento local `\\localhost\...` mapeado) |
| AC-20 | medição comparativa | mesmo script, mesmas consultas, Files × Everything. O `es.exe` não está instalado: baixá-lo do site da voidtools para a pasta temporária do projeto só com autorização do Alexandre; sem isso, cronômetro de UI Automation na janela do Everything |
| NFR-001 a NFR-005 | medição | scripts `f015_*` versionados; resultados em `STATUS.md` e na nota de liberação |
| Regressão | funcional | etiquetas, bibliotecas, busca em rede, F3 da F013 e sugestões do Omnibar |

## Medições T2 (11/10/2026)

### T2b — Varredura básica sem admin (`f015_basic_scan.ps1`)
Medição empírica por volume físico usando `FileIdBothDirectoryInfo` (sem abrir arquivos, sem descer em pontos de reanálise):

| Volume | Arquivos | Pastas | Total Itens | Tempo | Velocidade | RAM Pico (Delta) |
|---|---|---|---|---|---|---|
| `C:\` (NVMe) | 2.018.532 | 442.963 | 2.461.495 | 123,00 s | ~20.012 itens/s | 41 MB (+17 MB) |
| `D:\` (SATA) | 167.230 | 5.405 | 172.635 | 16,63 s | ~10.380 itens/s | 30 MB (+6 MB) |
| `R:\` (SATA) | 139.046 | 20.089 | 159.135 | 55,85 s | ~2.849 itens/s | 30 MB (+7 MB) |
| `S:\` (USB)  | 4 | 8 | 12 | 0,10 s | — | 23 MB (+0 MB) |

- **Conclusão T2b:** Viabilidade do modo básico confirmada (CONFIRMED). Mesmo no volume C: com quase 2,5 milhões de itens, o processo consumiu pico de apenas 41 MB de RAM e terminou em ~2 minutos.

### T2c — Google Drive Probe (`f015_gdrive_probe.ps1`)
- Caminho: `C:\Google\Drive\disk`
- Listagem: 80 itens em 0,09 s.
- Processo: `GoogleDriveFS` (PID 26260).
- I/O Read antes: 5666 bytes; I/O Read depois: 5666 bytes. Delta = 0 bytes (0 MB).
- **Conclusão T2c:** Listagem segura confirmada (CONFIRMED). Não provoca download em segundo plano.

### T2a — MFT Bruta (`f015_mft_probe.ps1`)
- Script pronto em `tools/perf/f015_mft_probe.ps1`.
- Estado: Pendente de execução com elevação de Administrador (UAC do Alexandre).
