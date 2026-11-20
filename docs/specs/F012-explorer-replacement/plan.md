# F012 — Substituição Transparente do Windows Explorer com Fallback Automático — PLAN

Status: **APROVADO COM REVISÃO** (Claude, 09/10/2026). **A seção 3 e o item "Arquivos a mexer" abaixo foram substituídos pela D-015:** não há trampolim C#; vale o launcher C++ do upstream (`src/KyraFiles.App.Launcher`), compilado por `tools/Build-Native.ps1` junto com as DLLs de diálogo, com dois ajustes no caminho "Files desinstalado" (sem `MessageBox`, sem `runas`, reversão do registro concluída antes de abrir o Explorer). Arquivos a mexer: `KyraFilesLauncher.cpp`, `AdvancedViewModel.cs`, `UnsetFilesAsDefault.reg`, `tools/Build-Native.ps1`, `tools/Create-SelfContained-Release.ps1`, `tools/installer/Program.cs`, `tests/test-launcher-fallback.ps1`. O `KyraFiles.slnx` continua sem os projetos C++ (o `dotnet build` não os compila).
Metodologia: Spec-Driven Development (`METODO.md` §6).
Documento arquitetural: define **como** implementar a `spec.md`, mapeando a arquitetura existente no código, reutilizando componentes existentes e definindo a estratégia de teste e riscos.

---

## 1. Arquitetura Atual Lida no Código (CONFIRMED)

Uma inspeção exaustiva do código-fonte revelou os componentes que **já existem** no projeto:

1. **Interface de Configurações (`CONFIRMED`):**
   - [`src/KyraFiles.App/ViewModels/Settings/AdvancedViewModel.cs`](file:///c:/desenv/utils/KyraFiles/src/KyraFiles.App/ViewModels/Settings/AdvancedViewModel.cs):
     - `IsSetAsDefaultFileManager`: propriedade observável (linhas 278–283).
     - `DetectIsSetAsDefaultFileManager()`: verifica se `Folder\shell\open\command` aponta para `KyraFiles.App.Launcher.exe` (linhas 259–265).
     - `SetAsDefaultExplorerCommand`: comando assíncrono que copia arquivos para `%LocalAppData%\Files` e aplica os registros (linhas 50–101).
   - [`src/KyraFiles.App/Views/Settings/AdvancedPage.xaml`](file:///c:/desenv/utils/KyraFiles/src/KyraFiles.App/Views/Settings/AdvancedPage.xaml):
     - Toggle switch para "Definir o KyraFiles como gerenciador padrão".

2. **Arquivos de Registro Preexistentes (`CONFIRMED`):**
   - [`src/KyraFiles.App/Assets/KyraFilesOpenDialog/SetFilesAsDefault.reg`](file:///c:/desenv/utils/KyraFiles/src/KyraFiles.App/Assets/KyraFilesOpenDialog/SetFilesAsDefault.reg):
     - Associa `HKEY_CURRENT_USER\SOFTWARE\Classes\Folder\shell\open\command` a `"%LOCALAPPDATA%\Files\KyraFiles.App.Launcher.exe" "%1"`.
     - Associa `HKEY_CURRENT_USER\SOFTWARE\Classes\Folder\shell\explore\command` a `"%LOCALAPPDATA%\Files\KyraFiles.App.Launcher.exe" "%1"`.
     - Associa `CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}` (atalho `Win + E`).
   - [`src/KyraFiles.App/Assets/KyraFilesOpenDialog/UnsetFilesAsDefault.reg`](file:///c:/desenv/utils/KyraFiles/src/KyraFiles.App/Assets/KyraFilesOpenDialog/UnsetFilesAsDefault.reg):
     - Restaura os valores para o `Explorer.exe` nativo com `DelegateExecute="{11dbb47c-a525-400b-9e80-a54615a090c0}"`.

3. **Código Histórico do Launcher C++ (`CONFIRMED`):**
   - [`src/KyraFiles.App.Launcher/KyraFilesLauncher.cpp`](file:///c:/desenv/utils/KyraFiles/src/KyraFiles.App.Launcher/KyraFilesLauncher.cpp):
     - Projeto C++ do upstream (`KyraFiles.App.Launcher.vcxproj`).
     - **Problema encontrado no build**: Esse projeto foi removido da `KyraFiles.slnx` no início do fork porque dependia do compilador MSBuild C++ completo. Por consequência, `KyraFiles.App.Launcher.exe` **não existe** no disco em `Assets/KyraFilesOpenDialog/`.
     - **Problema de UX no código original C++**: Na linha 79 de `KyraFilesLauncher.cpp`, ao detectar que o KyraFiles não existe, ele exibia um `MessageBox` intrusivo e tentava chamar `runas reg.exe` (UAC) para desinstalar, quebrando a experiência de fallback transparente.

---

## 2. O que NÃO Fazer (Evitar Reinvenção)

- **NÃO criar nova tela ou controles de UI**: A página *Configurações > Avançado* (`AdvancedPage.xaml`) e a ViewModel (`AdvancedViewModel.cs`) já possuem o toggle, os textos e os comandos.
- **NÃO alterar o protocolo de ativação do KyraFiles**: O app já se registra no Windows como pacote MSIX com protocolo de protocolo `kyrafiles-dev:` e alias `kyrafiles.exe`.
- **NÃO reescrever do zero os scripts de registro**: Os arquivos `.reg` existentes já cobrem as chaves corretas de `Folder\shell` e `Win + E`.

---

## 3. A Solução Proposta: Trampolim Resiliente com Fallback Seguro

Para cumprir a `spec.md` (especialmente o requisito inegociável de fallback automático sem falhas):

1. **Substituir o binário ausente por um Launcher Trampolim Autocontido (`KyraFiles.App.Launcher.exe`)**:
   - Um executável nativo ultrarrápido (sub-10ms), compilado pelo compilador já validado da máquina (`csc.exe` otimizado em x64, sem dependências externas de runtime).
   - **Fluxo de Execução do Trampolim**:
     1. Recebe a linha de comando (`%1`, `/select, "arquivo"`, etc.).
     2. Verifica em menos de 5ms se o KyraFiles está acessível (seja via alias `kyrafiles.exe` / `KyraFiles.exe`, via protocolo `kyrafiles-dev:` ou caminho de instalação).
     3. **Se o KyraFiles existir e responder**:
        - Despacha a chamada para o KyraFiles com os parâmetros correspondentes.
     4. **Se o KyraFiles NÃO existir, estiver inacessível, for apagado ou em Modo de Segurança**:
        - **Fallback transparente imediato**: invoca silenciosamente `%SystemRoot%\explorer.exe` passando os mesmos argumentos.
        - **Zero mensagens de erro**, zero janelas de diálogo, zero interrupções.
2. **Atualizar `AdvancedViewModel.cs`**:
   - Garantir que `KyraFiles.App.Launcher.exe` compilado esteja sempre presente no diretório `%LocalAppData%\Files` e em `Assets/KyraFilesOpenDialog`.
   - Ajustar `SetAsDefaultExplorerAsync` para aplicar o registro sem quebras.
3. **Integração no Instalador Autocontido (`tools/installer/Program.cs`)**:
   - Oferecer checkbox opcional *"Definir KyraFiles como explorador padrão"* na tela de instalação.
   - Na desinstalação, garantir execução limpa de `UnsetFilesAsDefault.reg`.

---

## 4. Mapa Requisito $\rightarrow$ Código

| Requisito | Cenários | Onde atuar | O que muda |
|---|---|---|---|
| **FR-001 / FR-002** (Interceptação de pastas e Win+E) | AC-1, AC-2, AC-4 | `SetFilesAsDefault.reg` / `AdvancedViewModel.cs` | Garantir gravação correta no HKCU sem necessidade de privilégios de Admin. |
| **FR-003** (Passagem de argumentos e `/select`) | AC-2, AC-3 | `tools/launcher/Program.cs` $\rightarrow$ `KyraFiles.App.Launcher.exe` | Trampolim traduz argumentos para URI `kyrafiles-dev:` ou chamada direta. |
| **FR-004 / NFR-001** (Fallback Automático Incondicional) | AC-3, AC-6, AC-7 | `tools/launcher/Program.cs` $\rightarrow$ `KyraFiles.App.Launcher.exe` | Se Files falhar ou não for achado, roda `%SystemRoot%\explorer.exe` silenciosamente. |
| **FR-005** (Reaproveitamento de instâncias) | AC-5 | Protocolo `kyrafiles-dev:` nativo do app | App já possui suporte a protocolo e abas via WinRT. |
| **FR-006** (Toggle reversível em Settings) | AC-8 | `AdvancedViewModel.cs` | Mantém o toggle existente funcionando sem erros. |
| **FR-007** (Instalador / Desinstalador) | AC-9 | `tools/installer/Program.cs` | Executa registro na instalação e reversão na desinstalação. |

---

## 5. Arquivos a Mexer e Arquivos a NÃO Mexer

### Arquivos a Mexer:
- `tools/launcher/Program.cs` (novo: código-fonte leve do launcher/trampolim em C# com compilação direta via `csc.exe` para `Assets/KyraFilesOpenDialog/KyraFiles.App.Launcher.exe`).
- `src/KyraFiles.App/ViewModels/Settings/AdvancedViewModel.cs` (ajustes pontuais para garantir cópia do launcher e detecção sem falha).
- `tools/Create-SelfContained-Release.ps1` (garantir compilação do launcher junto com o pacote).
- `tools/installer/Program.cs` (garantir reversão limpa na desinstalação).

### Arquivos a NÃO Mexer:
- Nenhum arquivo de layout (`DetailsLayoutPage`, `GridLayoutPage`, `BaseShellPage`).
- Nenhum arquivo de negócio de arquivo (`ListedItem`, `ShellViewModel`).
- `KyraFiles.slnx` (não reintroduzir projetos C++ `.vcxproj` desnecessários que quebram o build).

---

## 6. Riscos e Mitigações

1. **Risco:** O usuário deleta a pasta do KyraFiles sem desinstalar pelo Windows.
   - **Mitigação:** O launcher fica salvo em `%LocalAppData%\Files\KyraFiles.App.Launcher.exe`. Ao ser acionado, se notar que o app principal sumiu, ele executa o `explorer.exe` nativo e restaura o registro em segundo plano.
2. **Risco:** `Win + E` ser bloqueado por políticas do Windows 11.
   - **Mitigação:** A chave `CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}` no HKCU é a técnica padrão da indústria (usada pelo Total Commander e OneCommander).
3. **Risco:** Atraso na abertura de pastas.
   - **Mitigação:** O trampolim não carrega bibliotecas pesadas; tempo de execução medido abaixo de 10ms.

---

## 7. Estratégia de Teste

- **T-01:** Teste automatizado do trampolim: simular presença e ausência do KyraFiles e verificar se o processo correto é iniciado (`explorer.exe` no fallback).
- **T-02:** Ativação em *Configurações > Avançado* e teste com `Win + E`.
- **T-03:** Abertura de pasta com espaços e caminhos especiais.
- **T-04:** Simulação de exclusão do KyraFiles e prova de abertura automática do Windows Explorer.

## 9. Adendo de 09/10/2026: redirecionamento de janelas do Explorer (FR-009 a FR-011, D-017)

Status: **APROVADO** (Claude, 09/10/2026).

- **Arquitetura atual (CONFIRMED lendo o código):** `Utils/Shell/ShellWindowsAutomation.cs` já fala com a `ShellWindows` por chamadas diretas na tabela de métodos COM (compatível com AOT, sem `dynamic`); `MainWindow.InitializeFromCmdLineArgsAsync` já abre uma pasta vinda de fora (com `-select`) na janela existente; a instância "estacionada" em segundo plano espera o semáforo `Program.Pool`; `Win32Helper.OpenFolderInExistingShellWindow` é onde o KyraFiles abre o Explorer de propósito.
- **Abordagem:** classe nova `Utils/Shell/ExplorerWindowRedirector.cs`. `SetWinEventHook(EVENT_OBJECT_SHOW)` fora de contexto, na thread de UI; janela `CabinetWClass` nova fica transparente (`WS_EX_LAYERED`, alfa 0); em segundo plano, procura a janela na `ShellWindows` (pelo `IShellBrowser::GetWindow`) até ter a pasta atual, lê o nome de análise (`SHGetNameFromIDList`) e a seleção (`IFolderView2::GetSelection`); se for caminho de disco/rede ou "Este Computador", "Início", Lixeira ou Rede, fecha a janela (`WM_CLOSE`) e chama `MainWindow.OpenFromExplorerAsync`; senão, devolve a opacidade. Só a instância principal liga o gancho (mutex nomeado); a de diálogo (`App.OutputPath`) nunca. Só age com o gerenciador padrão ligado (mesma detecção do `AdvancedViewModel`).
- **Opções descartadas:** chaves de registro (não pegam, D-017); `Image File Execution Options` (exige administrador, risco de travar o shell); DLL dentro do Explorer (invasivo, FR-011); janela fora da tela em vez de transparente (o Explorer demorou mais para montar a pasta no protótipo).
- **Arquivos a mexer:** `ExplorerWindowRedirector.cs` (novo), `ShellWindowsAutomation.cs` (ler janela, pasta e seleção), `MainWindow.xaml.cs` (método de entrada), `App.xaml.cs` (ligar/desligar), `Win32Helper.Storage.cs` (avisar que o próprio Files abriu o Explorer), `NativeMethods.txt` do CsWin32 se faltar API.
- **Riscos:** janela fica invisível se o KyraFiles cair no meio (tempo máximo de 8 s e restauração em `finally`); Explorer muda a ordem de montagem entre versões (testar Win + E, `explorer.exe`, `/select`, Painel de Controle).
- **Teste por AC:** AC-1, AC-4, AC-3 (`explorer /select`), AC-11 (Painel de Controle), AC-12 (Files abrindo o Explorer) por script com medição de quanto tempo a janela fica visível (`tools/perf/f012_explorer_redirect.ps1`).
