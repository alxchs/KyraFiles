# F071 — Integridade e Arquivos Baixados — PLAN

Status: RASCUNHO (AGY, 31/10/2026). Só começa com a `spec.md` APROVADA. Responde "como", sem alterar o "o quê".

## Arquitetura atual (lida no código; estado de cada afirmação)

- `src/Files.Shared/Helpers/ChecksumHelpers.cs` (CONFIRMED): Implementa métodos estáticos assíncronos para geração de checksums (`CreateCRC32`, `CreateMD5`, `CreateSHA1`, `CreateSHA256`, `CreateSHA384`, `CreateSHA512`) recebendo `Stream` e `CancellationToken`. Utiliza as APIs de streaming do runtime .NET (`HashDataAsync`).
- `src/Files.App/ViewModels/Properties/HashesViewModel.cs` (CONFIRMED): Gerencia a aba de hashes da tela de propriedades de um único arquivo (`ListedItem`). Dispara o cálculo no dispatcher e possui comparação básica de texto via `OnHashInputTextChanged()`.
- Detecção e Remoção de Zona da Web (CONFIRMED):
  - Detecção: `FileProperties.cs` utiliza `Win32Helper.ReadStringFromFile($"{itemPath}:Zone.Identifier") is not null`.
  - Remoção: `GeneralPage.xaml.cs` executa `PInvoke.DeleteFileFromApp($"{itemPath}:Zone.Identifier")` isoladamente em um único arquivo ao clicar no botão Desbloquear.
  - Não existe atualmente comando ou serviço de lote para remoção de `Zone.Identifier` em múltiplos arquivos selecionados (CONFIRMED).

## Abordagem

1. **Backend Primeiro (Núcleo Sem Tela e Testado)**
   - Criar módulo desacoplado no `Files.App.Storage` / `Files.Shared`:
     - `IFileIntegrityService` com métodos:
       - `CalculateHashAsync(string filePath, HashAlgorithmName algorithm, IProgress<FileHashProgress>? progress, CancellationToken ct)`:
         Utiliza `IncrementalHash` e `FileStream` aberto em modo sequencial (`FileOptions.Asynchronous | FileOptions.SequentialScan`) com buffer fixo de 128 KB, processando blocos incrementais para garantir pico de memória desprezível (< 10 MB de RAM ocupada pelo buffer) mesmo para arquivos de 50+ GB.
       - `CompareHash(string computedHash, string inputCandidate)`: normalização com remoção de prefixos (`SHA256:`, `0x`), espaços, quebras de linha e comparação insensível a maiúsculas.
     - `IZoneIdentifierService` com métodos:
       - `HasZoneIdentifier(string filePath)`: checagem leve de existência do fluxo de dados alternativo `:Zone.Identifier`.
       - `UnblockFilesAsync(IEnumerable<string> filePaths, IProgress<int>? progress, CancellationToken ct)`:
         Processamento em lote não bloqueante que remove o fluxo via `DeleteFile` Win32 e retorna `BatchUnblockResult` com o número de arquivos desbloqueados, inalterados e falhas.
   - Suíte de testes unitários rica cobrindo cálculos de hash, cancelamento de stream, arquivos vazios, correspondência de strings formatadas e criação/remoção de streams NTFS simulados.

2. **Integração de Comandos e Notificações (Tela por Último)**
   - Criar comando `UnblockDownloadedFilesAction` derivado de `IAction` em `src/Files.App/Actions/FileSystem/`.
   - Adicionar o comando ao menu de contexto de múltiplos itens (`ContentPageContextFlyoutFactory.cs`) e barra de comandos, exibido quando arquivos regulares estiverem selecionados.
   - Ao término do desbloqueio em lote, exibir notificação de sucesso através do Status Center / InfoBar com o sumário quantitativo (ex.: *"3 arquivos baixados foram desbloqueados"*).
   - Requisitos de tela permanecem em `NOT TESTED` durante a entrega do backend.

## Mapa spec → código
| Requisito | Onde é atendido |
|---|---|
| FR-001, FR-002 | `FileIntegrityService.cs` com `IncrementalHash` e streaming assíncrono |
| FR-003 | `FileIntegrityService.CompareHash` e `HashesViewModel.cs` |
| FR-004 | `ZoneIdentifierService.HasZoneIdentifier` via checagem Win32 de ADS |
| FR-005 | `ZoneIdentifierService.UnblockFilesAsync` e `UnblockDownloadedFilesAction.cs` |
| FR-006 | Notificação de Status Center em `StatusCenterHelper.cs` |
| NFR-001 | Streaming em chunks fixos de 128 KB com `IncrementalHash` |
| NFR-002 | `Task.Run` com I/O assíncrono em background thread |
| NFR-003 | Cores de tema padrão do WinUI (azul/neutro), sem vermelho |

## Arquivos
- **Mexer:**
  - `src/Files.Shared/Helpers/ChecksumHelpers.cs`: extensão ou refatoração para suportar progresso em streaming com `IncrementalHash`.
  - `src/Files.App/Services/`: inclusão de `IZoneIdentifierService` e `IFileIntegrityService`.
  - `src/Files.App/Actions/FileSystem/UnblockDownloadedFilesAction.cs`: ação de contexto.
  - `src/Files.App/Strings/en-US/Resources.resw` e `pt-BR/Resources.resw`: novas chaves de localização.
  - `tests/Files.App.Search.Index.Tests/`: testes unitários do motor de hash e remoção de Zone.Identifier.
- **Não mexer:**
  - Código de propriedades clássico que já funciona para arquivo individual.
  - Indexador de busca e serviços do sistema.

## Riscos e regressões a vigiar
- **Arquivos travados por antivírus ou outro processo:** A exclusão do fluxo `:Zone.Identifier` pode retornar `ERROR_ACCESS_DENIED` se um antivírus estiver inspecionando o arquivo no momento. Mitigação: registrar como item com erro e continuar os demais do lote.
- **Sistemas de arquivos FAT32/exFAT:** Sistemas não-NTFS não suportam Alternate Data Streams. Mitigação: `HasZoneIdentifier` retorna false imediatamente.

## Estratégia de teste
| Cenário | Tipo | Como se prova |
|---|---|---|
| AC-1 | Teste unitário de memória | Arquivo temporário de 2 GB gerado em chunks; hash SHA-256 medido com `GC.GetTotalMemory` antes/depois mantendo delta < 50 MB |
| AC-2 | Teste unitário | `CancellationTokenSource.Cancel()` disparado após 100 ms; `OperationCanceledException` capturada em < 500 ms |
| AC-3 | Teste unitário | Comparações com hashes em maiúsculo, minúsculo, com espaços e prefixos retornam true |
| AC-4 | Teste unitário | Arquivo bloqueado com `FileShare.None` gera tratamento amigável de erro |
| AC-5, AC-6, AC-7 | Teste de integração | Criação de 5 arquivos temporários (3 com stream `:Zone.Identifier`); execução de `UnblockFilesAsync` valida 3 desbloqueados e 2 intactos |
