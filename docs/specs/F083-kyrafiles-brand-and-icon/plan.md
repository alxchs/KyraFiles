# F083 — Identidade KyraFiles: Marca, Ícones do Windows e Imagem no About — PLAN

## 1. Arquitetura e Componentes Impactados

### A. Recursos de Ícone e Mídia
1. **Origem da Arte**: `D:\DriveD\Downloads\KyraFiles.png` (resolução 1254x1254).
2. **Artefatos Derivados a Gerar**:
   - `src/KyraFiles.App/Assets/AppTiles/Dev/KyraLogo.png`: versão em alta resolução para exibição em XAML (`AboutPage`).
   - `src/KyraFiles.App/Assets/AppTiles/Release/KyraLogo.png` e `src/KyraFiles.App/Assets/AppTiles/Preview/KyraLogo.png`.
   - `Logo.ico` (geração de multi-resolução: 16x16, 24x24, 32x32, 48x48, 64x64, 128x128, 256x256) substituindo os arquivos `Logo.ico` em:
     - `src/KyraFiles.App/Assets/AppTiles/Dev/Logo.ico`
     - `src/KyraFiles.App/Assets/AppTiles/Preview/Logo.ico`
     - `src/KyraFiles.App/Assets/AppTiles/Release/Logo.ico`
   - Atualização dos tiles principais de manifest (`Square44x44Logo`, `Square150x150Logo`, `Small71x71Logo`, etc.) para refletir a nova identidade e extinguir as pastinhas rosa.

### B. Tela Sobre (`AboutPage.xaml` e `AboutViewModel.cs`)
1. Substituir o `FontIcon Glyph="&#xE946;"` em `AboutPage.xaml` por um controle de imagem com cantos arredondados contendo a mascote Kyra.
2. Garantir que `AppName` retorne `"KyraFiles"`.

### C. Strings e Recursos Localizados (`Resources.resw`)
1. Atualizar em `en-US`, `pt-BR`, `es-ES`, `de-DE` todas as strings onde o nome do aplicativo é mencionado diretamente para o usuário:
   - Diálogo de falha inesperada (`UnhandledExceptionDialogBody`)
   - Diálogo de elevação como administrador (`RunningAsAdmin`)
   - Diálogo de inicialização git (`GitInitFailed`)
   - Diálogo/notificação de execução em segundo plano (`AppIsStillRunningNotificationBody`, `AppIsStillRunningNotificationTitle`)
   - Título do aplicativo e diálogos de atualização
   - Descrições de motor de busca
   - Diálogos de integração com o sistema (definir como gerenciador padrão, inicialização com o Windows)
2. `Package.appxmanifest`:
   - Atualizar `<DisplayName>` e `<ShortName>` para `KyraFiles`.

---

## 2. Estratégia de Verificação e Rollout

1. **Geração dos Ícones**: Script utilitário em PowerShell utilizando `System.Drawing` para recortar/escalar com máxima qualidade (interpolação bicúbica / high quality bicubic) e compor o `.ico` nativo de 256 cores/ARGB.
2. **Compilação**: `mkfile d KyraFiles.slnx` para validar compilação XAML e assets.
3. **Testes Unitários**: `dotnet test` garantindo 0 regressões.
4. **Gate F021**: `pwsh tools\perf\verify_no_index_service.ps1`.
5. **Relatório**: `RELATORIO.md`.

