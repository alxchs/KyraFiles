# F019 — PLAN

## Arquitetura lida no código (12/10/2026)
- CONFIRMED: `Helpers/UI/ShapeGeometryHelper.cs` monta `CompositionPath` (Win2D `CanvasPathBuilder`) e `ApplyClip(FrameworkElement, style)` aplica `CompositionGeometricClip` no visual. `UIElement.Clip` não aceita `PathGeometry` (WinUI 3).
- CONFIRMED: `Views/MainPage.xaml.cs` tem `ApplyShapeClip` (janela `ShellWindowContainer` e `FileAreaBorder`), ligado em `Page_Loaded` e na troca de `ControlShapeStyle`.
- CONFIRMED: aba = `TabContainer` (Grid) em `Styles/TabBarStyles.xaml` (~linha 138), `CornerRadius="{ThemeResource App.Theme.Tab.CornerRadius}"`; nenhum recorte geométrico nas abas.
- CONFIRMED: o layout `AppX\` registrado só é atualizado por `.\Open-KyraFiles.ps1 -Configuration Debug -NoLaunch`; `mkfile d` não o atualiza. Testar visual **só depois** desse deploy.

## Abordagem
1. `ShapeGeometryHelper`: novo parâmetro `float scale = 1f` em `ApplyClip` e `BuildPath`; cada construtor recebe `float s` e multiplica as profundidades fixas (16, 10, 12, 14, 18) por `s`. Escala 1 = tamanho de aba.
2. Nova propriedade anexada `Helpers/UI/ShapeClip.cs` (`ShapeClip.Scale`, double; 0 = desligado): no `Loaded` assina `SizeChanged` do elemento e `PropertyChanged` do `IAppearanceSettingsService` (filtra `ControlShapeStyle`, reexecuta no `DispatcherQueue`); no `Unloaded` cancela as assinaturas (tabela fraca por elemento).
3. Uso em XAML: `ShellWindowContainer` `helpers:ShapeClip.Scale="1.75"`, `FileAreaBorder` `1.25`, `TabContainer` `1`. `TabBarStyles.xaml` precisa de `xmlns:helpers="using:KyraFiles.App.Helpers"`.
4. Remover o código-behind do `MainPage` (`ApplyShapeClip`, `ShapeTarget_SizeChanged`, o `case ControlShapeStyle`).
5. Se algo importante ficar cortado (AC-4), reduzir a escala só daquele elemento, não da forma.

Não mexer: `AppResourcesService` (CornerRadius), F015, F017, F018.

## Testes por AC
Capturas por `tools/perf/f019_shapes_capture.ps1 -Shapes '0,1,2,3,4,5,6,7,8,9'` (já commitado; só a janela do KyraFiles, em primeiro plano) depois do deploy. AC-1/5: trocar a forma com o app aberto (UI Automation no combo de Aparência ou editar `user_settings.json` com o app aberto) e capturar. AC-4: olhar cada captura nos cantos. AC-6: redimensionar via `SetWindowPos` e capturar.

## Revisão 12/10/2026 (feedback) — perfis por tamanho
Substitui a abordagem "uma escala por elemento" quando o elemento é grande:
- `BuildPath(style, w, h, scale)` escolhe o perfil: **lateral completa** se `h < 80` (aba) ou **só cantos** caso contrário. Detalhes em pixels fixos (AC-7): `toothBase = 16*scale`, `toothHeight = 10*scale`, `waveLength = 24*scale`, `cornerDepth = 16*scale`; número de dentes/ondas = `floor(h / toothBase)`, nunca constante.
- Perfil só-cantos: cada canto recebe o motivo da forma (côncavo, onda curta, chanfro, dentes pequenos) dentro de um quadrado `cornerDepth x cornerDepth`; o resto da borda é reta. Esquerda e direita seguem a descrição de cada forma em `ControlShapeStyleKind` (ex.: TicketWave = esquerda côncava, direita com ondas curtas nos cantos; SawtoothTriangle = esquerda côncava, direita com 2–3 dentes pequenos em cada canto).
- `ShapeClip.Scale`: janela 1.75 (~28 px), área de arquivos 1.25 (~20 px), aba 1.
- Teste: capturas dos 4 cantos da janela por forma, com Blue + Thin Acrylic.
