# F029 — Plano

Status: **APROVADO** (Claude, 21/10/2026).

## Arquitetura lida (CONFIRMED por leitura, 21/10/2026)
- `Views/Layouts/DetailsLayoutPage.xaml.cs`, `FileList_PreviewKeyDown`: ↓/↑ não são tratados pelo app (a lista nativa move seleção, foco e rolagem). A F028 acrescentou a espera de quadro (FR-5 da F028): uma repetição só é aceita depois do 2º `CompositionTarget.Rendering` após o passo anterior; as demais são descartadas (`navigationStepPending`).
- PgUp/PgDn já são do app (`TryMovePage`), com geometria lida dos contêineres e cabeçalho fixo (`HeaderGrid`, composição via `StickyHeaderBehavior`, sem custo no thread de UI).
- `ListView` com `ScrollViewer.BringIntoViewOnFocusChange=True`; contêiner com o estilo padrão (`ListViewItemPresenter`); modelo da linha com ~30 elementos, ~15 `Binding ElementName=PageRoot` e `x:Load` condicionais.
- Trace gerenciado de `Down` (`trace_down_f028`): 81% do thread de UI fora do código gerenciado (layout/desenho do WinUI **e** espera ociosa, o amostrador não separa). Trace nativo exige administrador (WPR): **fora de alcance**.

## Hipóteses (uma por vez, A/B com o mesmo script)
- **H1 — a espera de quadro descarta teclas.** O Explorer processa toda repetição. Testar: sem espera; espera de 1 quadro.
- **H2 — o passo nativo é caro** (rolagem por `BringIntoView` do foco, possivelmente com animação, e acerto de foco). Testar: ↓/↑ tratados pelo app como no PgDn: seleção, rolagem mínima instantânea (`ChangeView(..., disableAnimation: true)`) e foco no contêiner.
- **H3 — efeitos da seleção ainda pesam** (toolbar, barra de status, preview). Diagnóstico apenas: desligar por bandeira e medir; não vira entrega.

Bandeiras **temporárias** lidas uma vez de `C:\KyraFilesUXLab\f029_flags.txt` (um build, várias variantes, sem recompilar). Removidas antes do merge; o vencedor vira código fixo.

## Mapa requisito → código
- FR-1/FR-2/FR-3: `DetailsLayoutPage.xaml.cs` (`FileList_PreviewKeyDown`, novo `TryMoveRow`, geometria compartilhada com `TryMovePage`).
- NFR-2: sem mexer em `BaseLayoutPage` (efeitos), salvo se H3 provar o contrário (aí volta à spec).

## Não mexer
`StickyHeaderBehavior`, modelo da linha (XAML), `ShellViewModel`, outros layouts.

## Riscos
- ↓/↑ do app perdem algum comportamento nativo (Shift+↓ estendendo a partir da âncora, Ctrl+↓ movendo só o foco, grupo). Mitigação: o app só trata ↓/↑ **sem modificadores** e com lista não agrupada; o resto fica nativo.
- Sem espera de quadro, volta a fila acumulada (FR-2). Mitigação: medir `backlog_steps_after_release`.

## Testes
- Meta: Explorer medido com `f025_hold_key.ps1 -WindowClass CabinetWClass` (5 rodadas, `Down`).
- Cada variante: 5 rodadas `Down` + CPU; vencedora: folha de contato (`f028_contact_sheet.ps1 -Key Down`), PgDn de regressão (5 rodadas), toque isolado conferido na tela.
