# F054 — Prévia técnica — PLAN

Status: APROVADO com ajustes (Claude, 25/11/2026; ver "Ajustes da revisão"). Spec: `spec.md` (aprovada pelo Alexandre em 24/10/2026).

## 1. Arquitetura atual (lida no código em 25/11/2026)

- **CONFIRMED** (`src/KyraFiles.App/ViewModels/UserControls/Previews/CodePreviewViewModel.cs`): usa `ColorCode` (`ILanguage`) com dicionário estático `extensions` (`FrozenDictionary<string, ILanguage>`) cobrindo `.cs`, `.ps1`, `.xml`, `.xaml`, `.js`, `.ts`, `.cpp`, `.java`, `.php`, etc. Não inclui `.json`, `.yaml`/`.yml`, `.toml`, `.csv`, `.log` nem numeração de linha ou árvore recolhível.
- **CONFIRMED** (`src/KyraFiles.App/UserControls/FilePreviews/CodePreview.xaml.cs`): `RenderDocument()` limpa `codeView.Blocks`, usa `CssPreviewRenderer.Render` para `.css`/`.scss` (evitando travamento do `ColorCode`) e `RichTextBlockFormatter` para o restante.
- **CONFIRMED** (`src/KyraFiles.App/ViewModels/UserControls/Previews/TextPreviewViewModel.cs`): `TryLoadAsTextAsync(ListedItem)` lê até `Constants.PreviewPane.TryLoadAsTextSizeLimit`, detecta binário (`\0\0\0\0`) e trunca em `Constants.PreviewPane.TextCharacterLimit`. Não possui modo `tail -f` nem leitura parcial em streaming para arquivos grandes (200 MB).
- **CONFIRMED** (`src/KyraFiles.App/UserControls/FilePreviews/TextPreview.xaml.cs` e `InfoPaneViewModel.cs`): exibem o conteúdo no painel de prévia sem suporte a tabela virtualizada para CSV ou árvore para JSON/XML/YAML.

## 2. Abordagem

1. **T0 — Medição inicial (ponto em aberto da spec):** medir tempo de abertura e memória atuais de `CodePreview` e `TextPreview` com arquivos de 1 MB (`.cs`, `.json`, `.csv`, `.log`) e 200 MB para definir o baseline de NFR-001 (≤ 300 ms).
2. **Realce de sintaxe + numeração de linha (US1, AC-1, FR-003):** estender `CodePreviewViewModel` (adicionar `.json`, `.yml`/`.yaml`, `.toml`, `.ini`, `.sql`, `.sh`) e `CodePreview.xaml`/`.cs` com coluna de números de linha sincronizada e suporte a temas claro/escuro (`ActualThemeChanged`).
3. **Árvore recolhível JSON/XML/YAML (US2, AC-2, AC-3):** criar `StructuredTreePreviewViewModel` e `StructuredTreePreview` (`TreeView` virtualizado) com alternância Texto ↔ Árvore. Para JSON inválido, exibir texto com destaque da linha/coluna do erro (`JsonException.LineNumber` / `BytePositionInLine`).
4. **Tabela virtualizada CSV (US3, AC-4):** criar `CsvPreviewViewModel` e `CsvPreview` com leitura sob demanda por janela de linhas (sem carregar 100 mil linhas em memória de uma vez).
5. **Acompanhamento de log (`tail -f`) e limite de arquivo grande (US4, AC-5, AC-6, AC-7, FR-001, FR-002):** abrir `FileStream` com `FileShare.ReadWrite | FileShare.Delete` (não bloqueia escrita externa); ler apenas prefixo configurável em arquivos > limite (ex.: 200 MB) com aviso; usar `FileSystemWatcher` + leitura incremental de offset quando "Acompanhar" estiver ligado; cancelar seleção intermediária via `CancellationToken` (AC-7).

## 3. Mapa requisito → código

| Requisito | Onde |
|---|---|
| US1, AC-1, FR-003 | `src/KyraFiles.App/ViewModels/UserControls/Previews/CodePreviewViewModel.cs`, `src/KyraFiles.App/UserControls/FilePreviews/CodePreview.xaml(.cs)` |
| US2, AC-2, AC-3 | Novo `StructuredTreePreviewViewModel.cs` e `StructuredTreePreview.xaml(.cs)`, `InfoPaneViewModel.cs` |
| US3, AC-4 | Novo `CsvPreviewViewModel.cs` e `CsvPreview.xaml(.cs)` |
| US4, AC-5, AC-6, FR-001, FR-002 | `src/KyraFiles.App/ViewModels/UserControls/Previews/TextPreviewViewModel.cs`, `src/KyraFiles.App/Constants.cs` |
| AC-7, NFR-001 | `InfoPaneViewModel.cs` (cancelamento de prévia anterior) + medição em T0/T5 |

**A NÃO mexer:** motor de busca (`KyraFiles.App.Search.Index`), operações de disco (`FilesystemOperations`) e previews de mídia/PDF/imagem.

## 4. Riscos

- `RichTextBlockFormatter` do `ColorCode` pode travar a UI em arquivos > 500 KB ou linhas muito longas: mitigar truncando por limite configurável (FR-002) e formatando fora da UI thread quando aplicável.
- Bloqueio de arquivo de log em uso por outro processo: usar exclusivamente `FileShare.ReadWrite | FileShare.Delete` (FR-001).

## 5. Estratégia de teste

| AC | Como se prova |
|---|---|
| AC-1 | Abrir `.cs`, `.ps1`, `.json` na prévia: realce e números de linha visíveis |
| AC-2 / AC-3 | JSON de 1.000 chaves expande/recolhe; JSON inválido mostra linha/coluna |
| AC-4 | CSV de 100 mil linhas rola fluido com memória estável |
| AC-5 | Log recebendo append com "Acompanhar" ligado atualiza em < 1 s e não trava escrita externa |
| AC-6 / AC-7 | Arquivo de 200 MB abre prefixo + aviso em ≤ 300 ms; troca rápida cancela intermediários |

## 6. Ajustes da revisão (Claude, 25/11/2026)

- A formatação do `ColorCode` roda fora da thread de UI e respeita o limite de FR-002; arquivo acima do limite nunca chega inteiro ao `RichTextBlockFormatter`.
- Árvore (T2) e tabela CSV (T3) também obedecem ao limite de FR-002 e ao cancelamento de AC-7: trocar de seleção cancela o parse em andamento.
- Leitura de amostra para detectar codificação usa `TextEncodingHelper.DetectEncoding` (já existe, F095); não criar outro detector.
- T0 registra também o pico de RAM (não só tempo) para os arquivos de 1 MB e 200 MB.
