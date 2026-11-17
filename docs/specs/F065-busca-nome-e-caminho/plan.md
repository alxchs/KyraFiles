# F065 — Busca: Nome e Caminho nas colunas — PLAN

Status: APROVADO (Alexandre, 30/10/2026: "aprovado e siga sem pausas com todas as pendencias").

## Arquitetura atual (lida no código em 30/10/2026)
- `EverythingSearchProvider.SearchAsync` (`src/KyraFiles.App/Services/Search/EverythingSearchProvider.cs:223-236`) cria `ListedItem` com `ItemPath`, `ItemNameRaw`, tamanho, datas e tipo, **sem `Opacity`**. — CONFIRMED (leitura). `IndexedSearchProvider.cs:203,228` e `NativeFilesSearchProvider.cs:119` definem `Opacity = isHidden ? Constants.UI.DimItemOpacity : 1`. — CONFIRMED (leitura).
- `ListedItem.Opacity` é `double` sem inicializador (`Data/Items/ListedItem.cs:139`), logo 0. — CONFIRMED (leitura).
- O nome na lista de detalhes usa `Opacity="{x:Bind Opacity}"` (`Views/Layouts/DetailsLayoutPage.xaml:1028`). — CONFIRMED (leitura). Que isso produza o nome em branco da captura: INFERRED (não reproduzido; ver T1).
- Coluna Caminho: cabeçalho `PathHeader` (xaml ~745-752) e célula `ItemPathTextBlock` (xaml ~1193-1203); exibição por `PathColumn.Show()/Hide()` em `DetailsLayoutPage.xaml.cs:490-493`, dentro de `FilesystemViewModel_PageTypeUpdated`, chamado (a) com `PageTypeUpdated` do `ShellViewModel` — que, nos pontos lidos (linhas ~2256-2278), não define `IsTypeSearchResults` — e (b) uma vez ao navegar (`xaml.cs:263-269`), lendo `InstanceViewModel.IsPageTypeSearchResults`, definido em `BaseLayoutPage.cs:604`. Por que o Caminho não apareceu na captura: **desconhecido** (preferência `ShowPathColumn`, largura zero ou evento tardio; ver T2).

## Abordagem
1. Reproduzir e provar a causa do Nome em branco antes de mudar código (T1). Hipótese: definir `Opacity` no provedor Everything, igual aos demais.
2. Descobrir por que o Caminho não aparece (T2) e corrigir a causa comum, não o sintoma. Opção descartada: forçar `PathColumn.Show()` em todo resultado sem entender, pois mascara o evento tardio.
3. Considerar inicializar `Opacity = 1` por padrão em `ListedItem` só se T1 mostrar que outros caminhos de criação também esquecem; é mudança de modelo usada em todo o app e exige regressão ampla, então fica como opção B, decidida em T1 e registrada em `DECISIONS.md`.

## Mapa spec → código
| Requisito | Onde é atendido |
|---|---|
| FR-001 | `EverythingSearchProvider.cs` (e, se T1 mandar, `ListedItem.cs`) |
| FR-002, FR-003 | `DetailsLayoutPage.xaml.cs` / `ShellViewModel.cs` conforme T2 |
| NFR-001 | script `tools/perf/` de busca, antes/depois |

## Arquivos
- Mexer: `Services/Search/EverythingSearchProvider.cs`; `Views/Layouts/DetailsLayoutPage.xaml.cs` e/ou `ViewModels/ShellViewModel.cs` (só o ponto da causa); testes do provedor.
- **Não mexer:** `ListedItem.cs` (salvo opção B decidida), `IndexedSearchProvider`, `NativeFilesSearchProvider`, `EverythingSearchProvider` além do `Opacity`, motor de busca natural (F060 M5), instalador, F063.

## Riscos e regressões a vigiar
- Opacidade 1 em item oculto (deve ficar esmaecido).
- Busca em lote (`OnBatchReceived`): itens parciais também precisam do `Opacity`.
- Desempenho: o conserto não pode adicionar trabalho por item.

## Estratégia de teste
| Cenário | Tipo | Como se prova |
|---|---|---|
| AC-1..3 | unitário do provedor + funcional no app | teste: todo resultado com `Opacity > 0`; captura da lista por motor (só 09:00 de 31/10, sem roubar foco) |
| AC-4, AC-5 | funcional no app | captura da busca a partir de pasta já aberta; UI Automation para ler os cabeçalhos |
| AC-6 | unitário | item oculto sai com `DimItemOpacity` |
| AC-7 | funcional | voltar à pasta e ver que o Caminho some |
| NFR-001 | medição | `tools/perf/` antes e depois, mesmos parâmetros, 10 rodadas |
