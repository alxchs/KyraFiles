# F056 — Copiar caminho em vários formatos — PLAN

Status: APROVADO (Claude, 24/10/2026). Spec aprovada pelo Alexandre em 24/10/2026. **Execução bloqueada** até o Claude liberar (ver `docs/agents/ORDEM-RIGIDA-AGY.md`, S5 revisada).

## 1. Arquitetura atual (lida no código em 24/10/2026)
- **CONFIRMED** `src/KyraFiles.App/Actions/FileSystem/CopyPathAction.cs`: copia o caminho da **pasta atual** (`WorkingDirectory`), troca `\` por `/` só em caminho FTP, grava com `DataPackage.SetText` + `Clipboard.Flush()` dentro de `SafetyExtensions.IgnoreExceptions`.
- **CONFIRMED** `CopyItemPathAction.cs` e `CopyItemPathWithQuotesAction.cs` (mesma pasta): copiam o caminho dos **itens selecionados**. Item de menu em `src/KyraFiles.App/Data/Factories/ContentPageContextFlyoutFactory.cs:~540`, visível se `GeneralSettingsService.ShowCopyPath` e há seleção, e fora da Lixeira.
- **CONFIRMED** padrão de submenu a imitar: `CreateCopyMoveToFolderItem` (`ContentPageContextFlyoutFactory.cs:72`) e `BaseCopyMoveToFolderAction` (F033), que recebe o destino como `parameter`.
- **INFERRED** o projeto de testes unitários existe em `tests/`; T0 confirma qual e como roda.

## 2. Abordagem
Um formatador **puro e testável** (`PathFormatter`) que recebe `caminho`, `formato`, `raiz do repositório` e `pasta atual` e devolve texto, sem tocar a área de transferência. Uma ação `CopyPathAsAction` (recebe o formato como `parameter`) aplica o formatador a cada item e grava o resultado, um por linha. O menu ganha o submenu "Copiar caminho como…" construído como o submenu da F033, sem mexer nos itens "Copiar caminho" que já existem (FR-002: reaproveitar, não duplicar).
Opções descartadas: (a) reescrever as ações atuais: arrisca regressão sem ganho; (b) um item de menu por formato: polui o menu.

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001 (formatos) | novo `PathFormatter` (+ enum `PathFormat`) em `src/KyraFiles.App/Helpers/` (T0 confirma a pasta) |
| FR-002 | `CopyPathAsAction` chama o mesmo gravador de área de transferência das ações atuais |
| AC-6 (relativo ao repositório) | descoberta da raiz git subindo pastas até achar `.git`; T0 verifica se já existe helper de git para reaproveitar |
| AC-7 (vários itens) | `CopyPathAsAction` itera a seleção na ordem exibida |
| AC-8 (UNC e WSL) | regra explícita em `PathFormatter` (UNC não tem unidade: devolve `//servidor/pasta` ou erro claro, nunca um caminho errado calado) |
| US2 (formatos visíveis) | opções em `GeneralSettingsService` + página de configurações |
| NFR-001 | o submenu é montado sem I/O (a raiz do repositório só é buscada ao executar) |

**Arquivos a mexer:** os acima mais `Strings/en-US/Resources.resw` (textos; só en-US) e o registro dos comandos. **A NÃO mexer:** `CopyPathAction`, `CopyItemPathAction`, `CopyItemPathWithQuotesAction`, a F033, `Directory.Build.props`.

## 4. Riscos
- Escape errado em JSON/C# com caracteres especiais: cobrir com testes unitários dirigidos pela tabela da spec.
- Caminho longo (`\\?\`): tratar o prefixo e testar.
- Clipboard falha silenciosa: manter o mesmo padrão de `IgnoreExceptions`, mas **registrar** a falha.

## 5. Estratégia de teste (por AC, com asserção)
Testes unitários do `PathFormatter` cobrem AC-1 a AC-6 e AC-8 com os exemplos **exatos** da spec (entrada → saída esperada). AC-7 e a presença do submenu (AC de UI) com UI Automation e o helper `Send-GuardedInput.ps1`, em instância própria. Controle negativo: um formato com saída propositalmente errada tem de reprovar. Saída em `docs/agents/evidence/F056/resultado.csv`.
