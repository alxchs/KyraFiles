# F047 — Limpar pastas pesadas de build — PLAN

Status: APROVADO (Claude, 24/10/2026). Spec aprovada pelo Alexandre em 24/10/2026. **Execução bloqueada** até o Claude liberar (`docs/agents/ORDEM-RIGIDA-AGY.md`).

## 1. Arquitetura atual (lida no código em 24/10/2026)
- **CONFIRMED** há um padrão de ação com diálogo próprio no projeto (`Dialogs/BulkRenameDialog.xaml(.cs)`, `ViewModels/Dialogs/BulkRenameDialogViewModel.cs`, abertura em `Services/App/AppDialogService.cs`).
- **INFERRED** a exclusão para a Lixeira passa por `FilesystemHelpers`/`IStorageTrashBinService`; T0 localiza o ponto exato e se há opção de "excluir definitivo".
- **INFERRED** o app não tem hoje nenhum conceito de "tipo de projeto"; esta é a primeira peça (F045 vai reaproveitar a detecção).

## 2. Abordagem
1. `BuildFolderRules`: tabela de regras `nome da pasta` → `arquivo marcador` (ex.: `node_modules`→`package.json`; `bin`/`obj`→`*.csproj|*.sln|*.dpr`; `.venv`→`pyvenv.cfg`; `target`→`Cargo.toml|pom.xml`; `.gradle`/`build`→`build.gradle*`; `.dart_tool`/`build`→`pubspec.yaml`). Padrão embutido + arquivo de configuração do usuário (FR-003).
2. `BuildFolderScanner`: varre a pasta escolhida com pilha própria, **sem entrar** numa pasta que já casou (NFR-001), ignora links simbólicos/junctions, e só oferece o item quando o marcador existe no **pai** (AC-2).
3. Segurança git (FR-002): se a pasta está dentro de um repositório, consulta os arquivos rastreados da pasta; se houver qualquer arquivo rastreado, o item não é oferecido.
4. Diálogo `BuildFolderCleanerDialog` com lista, tamanho (calculado em segundo plano, cancelável), total a liberar e botão "Mover para a Lixeira". Exclusão pela rotina existente, uma pasta por vez; falha por "em uso" pula o item e o lista (AC-4).
Opções descartadas: (a) apagar direto por `Directory.Delete` sem Lixeira como padrão: contraria a decisão da spec; (b) varrer o disco inteiro: fora de escopo.

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001 | `BuildFolderRules` + `BuildFolderScanner` (novos, em `src/KyraFiles.App/Services/` ou `Utils/`; T0 confirma) |
| FR-002; AC-5 | verificação de arquivos rastreados no scanner |
| FR-003 | arquivo de configuração das regras |
| NFR-001 | pilha própria com poda; teste de tempo em T5 |
| AC-1 a AC-6 | diálogo + ação no menu de contexto da pasta/fundo |

**Arquivos a mexer:** novos arquivos de scanner/regras/diálogo/viewmodel; registro do diálogo em `AppDialogService`; item de menu em `ContentPageContextFlyoutFactory.cs` (para pasta e para o fundo); `Strings/en-US/Resources.resw`. **A NÃO mexer:** `FilesystemOperations` (só chamar), F033, `Directory.Build.props`.

## 4. Riscos
- **Apagar o que não é build** (o risco principal): por isso marcador obrigatório + conferência git + Lixeira por padrão + confirmação com o total.
- `node_modules` com milhões de arquivos deixa a Lixeira lenta: medir; oferecer exclusão definitiva só como opção explícita.
- Nomes iguais em contextos diferentes (`build` de projeto Flutter e de projeto Gradle): a regra exige o marcador certo.

## 5. Estratégia de teste (por AC, com asserção)
Árvore de teste criada pelo próprio script em `%TEMP%\agy\<GUID>\...` com 3 projetos (.NET, Node, Flutter), um `bin` sem marcador e um repositório git com um arquivo rastreado dentro de uma pasta de build. Testes unitários do scanner (AC-1, AC-2, AC-5, AC-6) e do tempo (NFR-001, 200 projetos); UI com UIA + helper revisado (AC-3, AC-4); controle negativo: o `bin` sem marcador **não** pode aparecer. `resultado.csv` em `docs/agents/evidence/F047/`.
