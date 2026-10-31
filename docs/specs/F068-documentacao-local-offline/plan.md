# Plano de Implementação - F068

## Arquitetura Técnica

1. **Estrutura de Arquivos e Assets (`src/Files.App/Docs/`)**:
   - `Docs/index.html`: Página principal com navegação lateral, design Fluent e links para todas as seções.
   - `Docs/getting-started/overview.html`: Equivalente fiel à página oficial `https://files.community/docs/getting-started/overview`, contendo introdução, multitarefa, tags, personalização e atalhos rápidos.
   - `Docs/features/tabs.html`, `dual-pane.html`, `layout.html`, `command-palette.html`, `git.html`, `tags.html`.
   - `Docs/features/files-custom.html`: Seção dedicada aos diferenciais exclusivos desenvolvidos para o Files Custom.
   - Folha de estilo embutida (CSS) limpa, responsiva, com variáveis CSS adaptáveis ao modo claro/escuro do sistema operacional (`prefers-color-scheme`), dispensando qualquer requisição externa (100% offline).

2. **Configuração do Projeto (`Files.App.csproj`)**:
   - Configurar os arquivos HTML como `<Content Include="Docs\**\*"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>`.
   - Garantir que tanto builds de Debug quanto a Release distribuída e instalada contenham a pasta `Docs` ao lado de `Files.exe`.

3. **Helper Centralizado (`DocumentationHelper.cs`)**:
   - Desenvolver `Files.App.Helpers.DocumentationHelper`.
   - Função de resolução `ResolveDocumentationTarget(string? subPath)`:
     - Retorna `(bool IsLocal, string TargetUriOrPath)`.
     - Testa a existência física nos diretórios locais conhecidos.
     - Se encontrado, devolve o caminho local (`file:///...` ou caminho do sistema).
     - Se ausente, devolve a URL web correspondente em `https://files.community/docs/`.
   - Método `OpenDocumentationAsync(string? subPath = null)`:
     - Executa a resolução e abre via `Launcher.LaunchUriAsync` ou `Process.Start`.

4. **Pontos de Invocação**:
   - `AboutViewModel.DoOpenDocumentation()`
   - `SystemTrayIcon.Documentation_Click()`
   - `OpenDocumentationAction.ExecuteAsync()`
   - Item no menu `ClassicMenuBar.xaml` sob `Ajuda`.

5. **Testes de Unidade e Validação**:
   - Teste em `tests/Files.App.Search.Index.Tests/DocumentationHelperTests.cs` cobrindo cenários com arquivo presente e ausente (fallback web).
   - Execução de suite de testes.
   - Build Release via `mkfile r` com 0 erros.
   - Atualização em `%LOCALAPPDATA%\Programs\FilesCustom` e validação F021.

