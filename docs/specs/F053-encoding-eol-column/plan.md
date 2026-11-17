# F053 — Codificação e Fim de Linha — PLAN

Status: APROVADO. Responde "como", sem alterar o "o quê" da `spec.md`.

## 1. Arquitetura Atual
- `ExplorerPropertyService.cs` gerencia as colunas dinâmicas do Windows Explorer e catálogo de propriedades.
- `ListedItem.SyncDynamicColumns` consulta `IExplorerPropertyService.GetFormattedProperty(filePath, canonicalName)`.
- Ações contextuais de sistema de arquivos são registradas via `[GeneratedRichCommand]` e inseridas em `ContentPageContextFlyoutFactory.cs`.
- `FileExtensionHelpers` já possui utilitários para detecção de arquivos de texto e código.

## 2. Abordagem Técnica
1. **Helper de Análise de Texto (`TextEncodingHelper.cs`):**
   - Método `GetFileInfo(string filePath)`:
     - Lê os primeiros 4 KB do arquivo com `FileShare.ReadWrite | FileShare.Delete` (NFR-001).
     - Detecção de BOM: UTF-8 BOM (`EF BB BF`), UTF-16 LE (`FF FE`), UTF-16 BE (`FE FF`), UTF-32.
     - Validação de UTF-8 sem BOM e detecção de caracteres binários nulos (`\0`). Se binário, `IsText = false`.
     - Detecção de fim de linha na amostra:
       - Presença de `\r\n` vs `\n` isolado vs `\r` isolado.
       - Se houver `\r\n` e `\n` isolados no mesmo arquivo: retorna "Misto" (AC-2).
     - Cache em memória indexado por `(FilePath, LastWriteTime)` para que a listagem de 10.000 arquivos permaneça ultrarrápida e sem reprocessamento (AC-6).
2. **Conversão em Lote Atômica e Reversível (`BatchTextConverter.cs`):**
   - Métodos: `ConvertToCrlfAsync(IEnumerable<string> files)` e `ConvertToLfAsync(IEnumerable<string> files)`.
   - Para cada arquivo:
     - Verifica se é texto (`IsText`). Se não for (ex.: imagem/binário), pula e reporta no resultado (AC-7).
     - Se o arquivo estiver bloqueado/em uso por outro processo: captura `IOException`, pula o arquivo e continua os demais (AC-4).
     - Grava os dados convertidos em arquivo temporário no `%TEMP%` e substitui atomicamente (`File.Replace` ou `File.Move` com overwrite) (FR-002).
     - Armazena backup em memória/temporário permitindo reversão byte a byte via desfazer (`Ctrl+Z`) (AC-5).
3. **Exposição em Colunas (`ExplorerPropertyService.cs`):**
   - Novas propriedades dinâmicas:
     - `KyraFiles.Text.Encoding` ("Codificação")
     - `KyraFiles.Text.LineEnding` ("Fim de linha")
   - Retorna vazio para binários (AC-1) e valor formatado para arquivos de texto.
4. **Comandos de Contexto:**
   - `ConvertToCrlfAction` ("Converter para CRLF")
   - `ConvertToLfAction` ("Converter para LF")
   - Exibidos no menu de contexto de arquivos selecionados.

## 3. Riscos e Mitigações
- **Arquivos enormes (> 1 GB):** Ler apenas os primeiros 4 KB em streaming. A listagem nunca carrega o arquivo inteiro.
- **Falha de escrita no meio do lote:** Cada arquivo é processado atomicamente via arquivo temporário independente.
