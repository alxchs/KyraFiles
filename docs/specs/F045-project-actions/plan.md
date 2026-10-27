# F045 — Ações por tipo de projeto e comandos próprios — PLAN

Status: APROVADO (Claude, 24/10/2026). Spec aprovada pelo Alexandre em 24/10/2026 (ponto em aberto decidido: integrar com `mkfile`). **Execução bloqueada** até o Claude liberar (`docs/agents/ORDEM-RIGIDA-AGY.md`).

## 1. Arquitetura atual (lida no código em 24/10/2026)
- **CONFIRMED** não há conceito de "tipo de projeto" nem de comandos do usuário. Há ferramentas de desenvolvimento só para IDE: `IDevToolsSettingsService` com `IDEPath` e `IDEName` (`Data/Contracts/IDevToolsSettingsService.cs`), a página `Views/Settings/DevToolsPage.xaml` + `ViewModels/Settings/DevToolsViewModel.cs`, e as ações `OpenInIDEAction`/`OpenRepoInIDEAction`.
- **CONFIRMED** padrão de ação sensível ao contexto: `GitInitAction` observa `IContentPageContext.IsGitRepository` e dispara `PropertyChanged` quando muda (`Actions/Git/GitInitAction.cs:23,42`). `Utils/Git/GitHelpers.cs` existe.
- **CONFIRMED** padrão para executar programa: `Win32Helper.RunPowershellCommandAsync(comando, PowerShellExecutionOptions)` devolve só `bool` (`Helpers/Win32/Win32Helper.Storage.cs:268`), com `ToPowerShellStringLiteral` para escapar texto. **Não captura a saída** nem tem cancelamento.
- **CONFIRMED** (METODO §12 do Alexandre) o `mkfile` só está no PATH do **PowerShell**, não do Bash; reconhece `.dpr/.dpk`, `.sln/.csproj` e `pubspec.yaml`.
- **INFERRED** o app não tem hoje um ponto para uma barra de ações por pasta; T0 define onde encaixar (abaixo da barra de navegação).

## 2. Abordagem
1. **`ProjectDetector`:** dado o caminho da pasta, devolve o tipo e as ações por **marcadores** (`.sln`, `.csproj`, `.dpr`, `pubspec.yaml`, `package.json`, `Cargo.toml`...). Resultado em cache por pasta, calculado **fora da thread de UI** e só ao entrar na pasta (NFR-001: não atrasa a abertura).
2. **Ações embutidas** por tipo, como **modelos fixos** (`mkfile d`, `mkfile r`, "Abrir solução", "Rodar testes"); o `mkfile` é chamado pelo PowerShell (FR-003: nada executa sem clique do usuário).
3. **Comandos do usuário** em um arquivo de configuração (`project-actions.json`, versionável e exportável): nome, tipos em que aparece, comando com marcadores `{caminho}`, `{seleção}`, `{raiz do repositório}`, `{nome}`, `{extensão}`, e a opção "um item por vez / todos juntos" para `{seleção}`.
4. **Execução segura (AC-7):** cada marcador é **escapado como argumento** (lista de argumentos ou `ToPowerShellStringLiteral`), nunca concatenado cru; teste com espaços, aspas, acentos e `$`.
5. **Novo executor com saída:** runner próprio (`Process` com saída redirecionada, limite de linhas, cancelamento e código de saída), reaproveitando o escape existente; mostra o resultado e as últimas linhas em caso de falha (AC-5); roda em segundo plano com "Cancelar" (AC-6).
6. **Barra de ações** (controle novo) visível só quando há tipo reconhecido (AC-1, AC-2) e os comandos também na paleta de comandos e no menu.
Opções descartadas: (a) encaixar em `IDevToolsSettingsService` (mistura IDE com comandos); (b) execução automática ao entrar na pasta (viola FR-003 e é perigoso).

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001 (marcadores) | expansão de marcadores no executor |
| FR-002 (config editável e exportável) | `project-actions.json` + página em `DevToolsPage` (ver T5) |
| FR-003 (nada automático) | só executa por clique/atalho; teste de controle |
| NFR-001 | cache + detecção assíncrona; medição em T6 |
| AC-1, AC-2 | `ProjectDetector` + barra de ações |
| AC-3, AC-4, AC-7 | executor com escape por argumento |
| AC-5, AC-6 | executor com saída, código de saída e cancelamento |

**Arquivos a mexer:** novos `ProjectDetector`, executor com saída, serviço de comandos do usuário, barra de ações; ponto de encaixe na janela (T0 decide); `DevToolsPage`/`DevToolsViewModel` (edição, em tarefa própria); `Strings/en-US`. **A NÃO mexer:** `OpenInIDEAction`, `Win32Helper.RunPowershellCommandAsync` (só reaproveitar o escape; o executor novo é à parte), `Directory.Build.props`.

## 4. Riscos
- **Injeção de comando** pelos marcadores (nome de arquivo com `;` ou `$()`): por isso escape por argumento e teste adversarial (AC-7).
- **Comando que precisa de administrador:** avisar, nunca elevar sozinho.
- **Saída enorme:** limite de linhas e de bytes; descartar o excedente.
- **Várias detecções ao mudar de pasta rápido:** cancelar a anterior.
- **`mkfile` ausente do PATH do processo do app** (PATH do PowerShell vs Bash): chamar pelo PowerShell e testar com o PATH real do usuário.

## 5. Estratégia de teste (por AC, com asserção)
Testes unitários do detector (AC-1, AC-2) com árvore de teste em `%TEMP%\agy\<GUID>\...` e do escape dos marcadores com cadeia adversarial (AC-7: `a b`, `ç`, `x'y`, `$(calc)`, `;`), conferindo que o argumento chega **idêntico** a um programa de teste que o imprime. Executor: comando que falha (AC-5), demorado e cancelado (AC-6). Interface (barra aparece e some; comando `code "{caminho}"` simulado por um executável de teste, não o VS Code real) por UI Automation com o helper revisado ou Sandbox. Controle negativo: pasta sem marcador **não** mostra a barra; nada executa ao apenas entrar na pasta. `resultado.csv` em `docs/agents/evidence/F045/`.
