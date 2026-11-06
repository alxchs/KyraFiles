# F089 - Plano (RASCUNHO, Claude, 04/11/2026)

Estados: CONFIRMED = li no código agora. INFERRED = deduzido, precisa de prova.

## O que a AGY afirmou e o que conferi
| Afirmação da AGY | Estado | Evidência |
|---|---|---|
| `AppResourcesService.cs:95-104` grava `string` em `ContentControlThemeFontFamily` e `App.Theme.FileArea.FontFamily` | **CONFIRMED** | linhas 95 a 103: `Resources[...] = <string>` |
| O recurso é declarado como objeto `FontFamily` | **CONFIRMED** | `App.xaml:118`: `<FontFamily x:Key="App.Theme.FileArea.FontFamily">` |
| Por isso a conversão falha e volta à fonte padrão | **INFERRED** | não rodei o app; falta o T0 |
| `ContentControlThemeFontFamily` em `ThemeDictionaries` encobre a raiz | **INFERRED, duvidoso** | chave direta em `Application.Resources` costuma ter prioridade sobre dicionários mesclados. Só vale se o T0 provar |
| `App.Theme.BaseTextBlockStyle` impõe `FontFamily` e quebra a herança do `ListView` | **CONFIRMED (leitura)** | `TextBlockStyles.xaml:5`; itens do Details usam `App.Theme.BodyTextBlockStyle` (`DetailsLayoutPage.xaml:1471,1485,1546,1560`), que herda dele. O `FontFamily` está só no `ListView` (`:232`) e no `ListViewHeaderItem` (`:59`) |
| A barra lateral usa `ContentControlThemeFontFamily` | **CONFIRMED** | `KyraFiles.App.Controls/Sidebar/SidebarStyles.xaml:165` |

Conclusão: a sidebar lê o recurso diretamente, e mesmo assim não mudou. Isso aponta a causa 1 (tipo ou prioridade do recurso) como comum aos dois sintomas. A causa 3 só explica a lista de arquivos. **Regra de verificação 2: não tratar como dois bugs independentes antes do T0.**

Também CONFIRMED: `AppThemeResourcesHelper.cs:106-111` aplica as fontes na inicialização, com `if (... != StandardFont)`. A evidência da F009 (`docs/agents/evidence/f009/`) não prova nada: mostra a tela inicial na fonte padrão, sem fonte trocada. Não foi aberta a captura `f009_settings_fonts.png`.

## Abordagem
1. **T0, prova da causa (sem corrigir):** com o app aberto, gravar em log o tipo e o valor de `Application.Current.Resources["ContentControlThemeFontFamily"]` antes e depois da troca, e se o `ThemeResource` na sidebar enxerga o valor novo. Decide entre as causas 1 e 2.
2. **Correção A (provável):** gravar `new FontFamily(nome)` nos dois recursos; nome inválido cai no padrão com `try/catch`.
3. **Correção B (só se o T0 mostrar):** gravar também nos `ThemeDictionaries` ativos (Default, Light, HighContrast).
4. **Correção C (lista de arquivos):** nos três layouts, mapear localmente `ContentControlThemeFontFamily` para `{ThemeResource App.Theme.FileArea.FontFamily}` nos `Resources` da página, de modo que `App.Theme.BodyTextBlockStyle` produza a fonte da área. Alternativa descartada: trocar o `FontFamily` em cada `TextBlock` (centenas de pontos e regressão a cada nova coluna).
5. A troca em tempo real exige que `ThemeResource` seja reavaliado: conferir `ApplyResources()` e, se preciso, reaplicar o tema na raiz.

## Mapa requisito -> código
FR-1/FR-3: `AppResourcesService.cs`, `AppThemeResourcesHelper.cs`, `AppearanceViewModel.cs:320-360`.
FR-2: `DetailsLayoutPage.xaml`, `GridLayoutPage.xaml`, `ColumnLayoutPage.xaml`, `UserControls/DataGridHeader.xaml`.
NFR-2: `AppResourcesService.cs`.

## Não mexer
`TextBlockStyles.xaml` (impacta todo o app), fontes de ícones (`SymbolThemeFontFamily`), cabeçalhos de licença, namespaces.

## Riscos
Alto: `ThemeResource` pode exigir recriar o tema para atualizar em tempo real. Médio: layouts com `TextBlock` fora do `ListView` (painel de informações, `InfoPane.xaml`) usam a fonte da interface e devem continuar assim.

## Estratégia de teste por AC
| AC | Prova |
|---|---|
| AC-1, AC-2, AC-4 | captura de tela antes e depois; só depois das 23:59 ou com o Alexandre longe (§16), sem roubar foco |
| AC-3 | reabrir o app e capturar |
| AC-5 | teste unitário do método de aplicação com nome inválido |
| NFR-1 | `verify_no_index_service.ps1` e o script de navegação em pasta grande contra a `colab` |
