# F090 - Plano (RASCUNHO, Claude, 04/11/2026)

Estados: CONFIRMED = li no código agora. INFERRED = conhecimento da plataforma, não testado aqui.

## Estado atual
- CONFIRMED: existe `AppDensity` (`AppDensityKind`: Normal, Compacto, Ultra compacto) em `IAppearanceSettingsService.cs:152`, aplicado em `LayoutSizeKindHelper.cs` (alturas) e `SetAppThemeDensity`. Card dele em `AppearancePage.xaml:125`.
- CONFIRMED: não existe nenhuma escala global (`grep UiScale|DisplayScale|ScaleFactor` sem resultado).
- CONFIRMED: a raiz do app é `rootFrame`, atribuída em `MainWindow.xaml.cs:318` (`Instance.Content = rootFrame`).
- CONFIRMED: `DragZoneHelper.cs:30` usa `xamlRoot.RasterizationScale`; qualquer transformação na raiz precisa conferir essa região de arraste da barra de título.
- CONFIRMED: o pacote do Community Toolkit presente em `Directory.Packages.props` não inclui `LayoutTransformer` pelo nome; não encontrei esse controle em uso (`grep` sem resultado).

## Correção às premissas da AGY
A AGY propôs `UIElement.Scale` ou `ScaleTransform` na raiz. **INFERRED, a confirmar no T0:**
- `ScaleTransform` e `UIElement.Scale` não alteram o layout: o conteúdo cresce visualmente mas o espaço medido continua o mesmo, então cortes, faixas vazias e barras de rolagem aparecem (conflito direto com AC-1 e AC-7).
- Menus, dicas e diálogos nascem em camadas separadas (popups) e não herdam a transformação da raiz (conflito com AC-4).
- A zona de arraste da barra de título é calculada em pixels reais e não acompanha a transformação (AC-5).

## Opções
| Opção | Prós | Contras |
|---|---|---|
| A. Transformação com layout compensado (contêiner que mede o filho com tamanho dividido pela escala) | uma peça só, escala tudo na raiz | popups ficam fora; arraste e hit-test precisam de prova |
| B. Escalar métricas por recurso (tamanhos de fonte, alturas, larguras e ícones lidos de `ThemeResource`) | popups e diálogos acompanham; nítido | toca muitos estilos; ícones fixos em XAML não escalam; maior regressão |
| C. Híbrida: A na janela principal + recursos escalados só para popups e diálogos | cobre AC-4 | mais complexa |

Recomendação provisória: **A como primeiro passo, e C se o T0 mostrar que AC-4 é inaceitável sem ela.** A decisão sai do T0, não deste rascunho.

## Dados e UI
`AppearanceSettingsService`: nova propriedade `AppUiScalePercent` (padrão 100, valores 80, 90, 100, 110, 125, 150). Contrato em `IAppearanceSettingsService.cs`. `AppearancePage.xaml`: novo `SettingsCard` ao lado de "Densidade", com `ComboBox`. `AppearanceViewModel.cs`: opções e comando. Strings em `Resources.resw` (en-US e pt-BR). Serviço de aplicação: um novo `IUiScaleService` que mantém o valor, aplica à raiz de cada janela aberta e escuta mudanças.

## Mapa requisito -> código
FR-1, FR-2, FR-3: `AppearanceSettingsService`, `AppearancePage.xaml`, `AppearanceViewModel.cs`, novo serviço, `MainWindow.xaml.cs`.
AC-4, AC-5, AC-7: decididos pelo T0.
NFR-2: conferir `UseLayoutRounding` e `RasterizationScale`.

## Não mexer
`LayoutSizeKindHelper` (densidade) e o motor de arraste dos itens, salvo prova de defeito; `TextBlockStyles.xaml`.

## Riscos
Alto: AC-4 e AC-5 podem não ser atingíveis só com a opção A. Médio: janelas de Configurações (F082, janela modal própria) têm raiz própria e precisam receber a mesma escala. Médio: a F089 (fontes) e a F090 mexem em recursos de tema; fazer F089 primeiro.

## Estratégia de teste por AC
AC-1, AC-2, AC-6, AC-7: capturas em 80%, 100% e 150% (app aberto, sem roubar foco, depois das 23:59). AC-3: reabrir. AC-5: clique e arraste manual em 125% com o Alexandre, ou UI Automation sem foco. AC-8: teste com a escala do Windows em 150%. NFR-1: `verify_no_index_service.ps1` e navegação em pasta grande contra a colab.
