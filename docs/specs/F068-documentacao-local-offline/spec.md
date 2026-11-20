# F068 - Documentação Local Offline com Fallback Web

## 1. Visão Geral
Atender à solicitação do usuário Alexandre:
> "https://github.com/alxchs/KyraFiles
> Preciso de uma documentação local no app equivalente ao conteudo desse site e sempre usar o local primeiro, se não tiver o arquivo, ai acessa o site."

O aplicativo deve incluir em seu pacote/diretório de instalação uma documentação HTML completa, moderna e offline, cobrindo o conteúdo de Getting Started / Overview, Recursos (Abas, Painel Duplo, Modo Colunas, Command Palette, Tags, Git, etc.) e os diferenciais exclusivos do KyraFiles.
Toda ação de abertura de documentação no app deve verificar a presença do arquivo local primeiro: se existir, abre localmente sem rede; se o arquivo não for encontrado, realiza o fallback transparente para o site oficial.

## 2. Requisitos Detalhados

### 2.1 Conteúdo da Documentação Local
- Criar a estrutura de documentação em `src/KyraFiles.App/Docs/`:
  - `index.html`: Portal principal da documentação com navegação fluida, tema claro/escuro compatível com Windows Fluent Design, sumário lateral e busca rápida.
  - `getting-started/overview.html`: Conteúdo equivalente exato ao site `https://github.com/alxchs/KyraFiles`, detalhando a missão, recursos de multitarefa, tags, arquitetura e atalhos.
  - `features/`: Guias detalhados das principais funcionalidades:
    - `tabs.html`: Abas, tear-off, split para novo painel e atalhos.
    - `dual-pane.html`: Painel duplo (dual pane), disposição vertical/horizontal e cópia entre painéis.
    - `layout.html`: Modos de exibição (Detalhes, Colunas, Grade, Lista, Cards), ordenação e agrupamento.
    - `command-palette.html`: Command Palette (Ctrl+Shift+P) e lista das principais ações.
    - `git.html`: Integração com repositórios Git, colunas de autor/hash e status na barra de status.
    - `tags.html`: Criação, atribuição e filtragem por tags coloridas em NTFS.
    - `files-custom.html`: Recursos exclusivos do fork KyraFiles (Modo Colunas OneCommander/Mac, redimensionamento independente de colunas, substituição nativa do Explorer, busca externa F3 Agent Ransack/Everything, densidade compacta, diagnóstico de memória em repouso).

### 2.2 Mecanismo de Abertura com Fallback
- Criar `KyraFiles.App.Helpers.DocumentationHelper`:
  - Método `OpenDocumentationAsync(string? subPath = null)`:
    1. Resolve o caminho do arquivo local candidato (`Docs/index.html` ou `Docs/getting-started/overview.html`) em `AppContext.BaseDirectory\Docs`, `Package.Current.InstalledLocation\Docs` e `%LOCALAPPDATA%\Programs\KyraFiles\Docs`.
    2. Se o arquivo existir no disco local, abre imediatamente no navegador padrão via `StorageFile` / `ProcessStartInfo` (offline, sem dependência de internet).
    3. Se o arquivo não existir ou falhar a abertura local, dispara `Launcher.LaunchUriAsync` para a URL remota equivalente (`https://github.com/alxchs/KyraFiles`).

### 2.3 Integração no App
- `AboutViewModel.DoOpenDocumentation()`: Atualizado para invocar `DocumentationHelper.OpenDocumentationAsync("getting-started/overview.html")`.
- `SystemTrayIcon.cs`: Atualizado para invocar `DocumentationHelper.OpenDocumentationAsync("getting-started/overview.html")`.
- `ClassicMenuBar.xaml`: Inclusão do item de menu "Documentação (Manual do Usuário)" sob o menu `Ajuda` (Alt+H) apontando para a documentação local.
- `OpenDocumentationAction.cs`: Criada ação registrada no catálogo de comandos do app.
- `KyraFiles.App.csproj`: Adicionar `<Content Include="Docs\**\*"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>`.

## 3. Critérios de Aceite
- [AC-1] O arquivo `Docs/index.html` e `Docs/getting-started/overview.html` estão incluídos e são copiados para o diretório de instalação do app.
- [AC-2] Ao clicar em "Documentação" na tela Sobre (Settings > About) ou no SystemTrayIcon, o navegador abre o arquivo local `overview.html` diretamente de `%LOCALAPPDATA%\Programs\KyraFiles\Docs\`.
- [AC-3] Caso os arquivos locais sejam temporariamente renomeados/ausentes, o clique dispara com sucesso a navegação web para `https://github.com/alxchs/KyraFiles`.
- [AC-4] Testes unitários para `DocumentationHelper` validando a resolução local e a lógica de fallback.
- [AC-5] Compilação Release validada via `mkfile r` com 0 erros e binário atualizado em `%LOCALAPPDATA%\Programs\KyraFiles`.

