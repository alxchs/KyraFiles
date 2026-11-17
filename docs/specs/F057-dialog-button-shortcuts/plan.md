# F057 — PLAN

Status: **APROVADO (Claude, 26/10/2026)**, a pedido do Alexandre ("precisa ser incluída no plan"). Alcance global.

## 1. Arquitetura atual (lida em 26/10/2026)
- CONFIRMED: `FilesystemOperationDialog.xaml` é um `ContentDialog` com `PrimaryButtonText`, `SecondaryButtonText`, `CloseButtonText` ligados ao ViewModel e um `CheckBox` `chkPermanentlyDelete`; nenhum `AccessKey` (`git grep AccessKey -- *.xaml` vazio).
- CONFIRMED: 17 arquivos `src/KyraFiles.App/Dialogs/*.xaml` com `<ContentDialog`, todos com `Style="{StaticResource DefaultContentDialogStyle}"` (estilo do WinUI, sem ponto próprio para um `Setter`).
- CONFIRMED: não há ponto único de exibição: `TryShowAsync` em `Helpers/UI/UIHelpers.cs:97` cobre parte; várias caixas chamam `base.ShowAsync()` ou `SetContentDialogRoot(this).ShowAsync()` direto; seis lugares usam `new ContentDialog` e `ShowAsync()` (`LibGit2Service` x2, `StorageNetworkService`, `LibraryManager` x2, `DetailsLayoutPage`).
- CONFIRMED: os textos dos botões vêm de `Strings.*.GetLocalizedResource()` (OK, Yes, Cancel, Delete...), reaproveitados em muitas caixas.

## 2. Abordagem
**Letra automática pelo texto exibido**, sem marcador no `.resw` (muda o plano anterior, que previa marcador por tradução; seria editar centenas de strings e quebraria o reúso do texto fora da caixa):
1. `DialogAccessKeys.Assign` (lógica pura, testada): primeira letra latina do primeiro termo de cada controle; quem perdeu a letra pega o início de palavra livre, depois qualquer letra livre. Ordem de prioridade: principal, secundário, fechar, depois opções marcáveis na ordem da árvore. Sem repetição por construção.
2. `DialogShortcuts.Attach(ContentDialog)` (idempotente): ao abrir, acha os botões do modelo (`PrimaryButton`, `SecondaryButton`, `CloseButton`) e as opções (`CheckBox`, `RadioButton`), troca o conteúdo por um `TextBlock` com a letra sublinhada (nome acessível continua o texto simples) e trata `PreviewKeyDown`: letra A a Z, sem Ctrl/Win, e **só se o foco não estiver em TextBox/PasswordBox/RichEditBox/ComboBox/AutoSuggestBox/NumberBox**; botão = `InvokePattern`; opção = alterna.
3. Ligação: `DialogShortcuts.Attach(this)` no construtor das 17 caixas e em `TryShowAsync`; os seis `new ContentDialog` diretos na T4.
Descartadas: `AccessKey` nativo (só mostra com Alt e exige achar botões do modelo); marcador no `.resw` (ver acima); letra fixa no código (quebra a tradução).

## 3. Mapa requisito → código
| Requisito | Onde |
|---|---|
| FR-001, FR-004 | `src/KyraFiles.App/Helpers/UI/DialogShortcuts.cs` |
| FR-002, FR-003, AC-5 | `src/KyraFiles.App/Helpers/UI/DialogAccessKeys.cs` + `tests/KyraFiles.App.Search.Index.Tests/DialogAccessKeysTests.cs` (arquivo ligado ao projeto de teste) |
| AC-7, AC-9 | verificação de foco e `IsEnabled` em `Dialog_PreviewKeyDown` |

## 4. Mexer e não mexer
- Mexer: os dois arquivos novos, o construtor das 17 caixas (uma linha), `UIHelpers.TryShowAsync`, o projeto de teste.
- Não mexer: lógica de exclusão, `DeleteItemAction`, atalhos globais, `.resw`.

## 5. Riscos
- Trocar o `Content` do botão do modelo apaga o `TemplateBinding`: tratado reaplicando em cada mudança de `*ButtonText`. **NOT TESTED na tela.**
- Letra digitada com foco num botão ou na lista pode competir com a busca por digitação da lista: aceito, a letra do atalho vence.
- Textos longos ou em japonês/chinês/árabe: sem atalho (nunca quebra).

## 6. Estratégia de teste
- Unidade (feito na T1): AC-5/FR-003 e bordas, com controle negativo.
- Interface (T3): **só depois das 23:59, com o Alexandre longe, sem roubar o foco nem trazer janela para frente** (regra de 26/10/2026). A trava `Send-GuardedInput.ps1` já proíbe `SetForegroundWindow`. Verificação por UI Automation (nome e texto dos botões, `InvokePattern`) não precisa de foco. Teclas só entram se o app de teste já estiver em primeiro plano por conta própria; **se não estiver, o AC fica `NOT TESTED`** em vez de forçar a janela.
