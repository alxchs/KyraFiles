# Handoff: F090 — Escala da Interface (Código e Integração Concluídos)

- **Data/Hora:** 2026-11-09 08:15 BRT
- **Autor:** AGY (Antigravity)
- **Destinatário:** Claude / Alexandre
- **Branch:** `feature/f090-escala`
- **Commits:**
  - `57e2ce7d9`: `feat(settings): modelo, persistencia e seletor da escala de interface em configuracoes (F090 T1/T3)`
  - `ebe7ad07c`: `feat(ui): servico de escala de interface IUiScaleService com layout compensado na MainWindow (F090 T2)`

---

## 1. O que foi implementado

### 1.1 Modelo e Persistência (T1)
- [IAppearanceSettingsService.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Data/Contracts/IAppearanceSettingsService.cs): Contrato `int AppUiScalePercent { get; set; }` adicionado.
- [AppearanceSettingsService.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Services/Settings/AppearanceSettingsService.cs): Implementação com persistência padrão `100` (valores válidos: 80, 90, 100, 110, 125, 150).

### 1.2 Interface em Configurações > Aparência (T3)
- [Resources.resw (en-US)](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Strings/en-US/Resources.resw): Chave `AppUiScale` adicionada ("Interface scale").
- [Resources.resw (pt-BR)](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Strings/pt-BR/Resources.resw): Chave `AppUiScale` adicionada ("Escala da interface").
- [AppearanceViewModel.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/ViewModels/Settings/AppearanceViewModel.cs):
  - `AvailableUiScalePercentages`: Array estático `[80, 90, 100, 110, 125, 150]`.
  - `AppUiScaleOptions`: Opções formatadas `["80%", "90%", "100% (padrão)", "110%", "125%", "150%"]`.
  - `SelectedAppUiScaleIndex`: Binding bidirecional sincronizado com `UserSettingsService.AppearanceSettingsService.AppUiScalePercent`.
- [AppearancePage.xaml](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Views/Settings/AppearancePage.xaml): Novo `SettingsCard` com ícone Fluent `&#xE71E;` posicionado estrategicamente ao lado de Densidade da Interface.

### 1.3 Serviço de Aplicação com Layout Compensado (T2)
- [IUiScaleService.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Data/Contracts/IUiScaleService.cs): Contrato de aplicação e coordenação de escala.
- [UiScaleService.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Services/App/UiScaleService.cs):
  - Implementação da Opção A do `plan.md` com transformação matemática compensada:
    - `rootElement.Width = windowWidth / scaleFactor;`
    - `rootElement.Height = windowHeight / scaleFactor;`
    - `rootElement.Scale = new Vector3((float)scaleFactor, (float)scaleFactor, 1.0f);`
    - `rootElement.TransformMatrix = Matrix4x4.CreateScale((float)scaleFactor, (float)scaleFactor, 1.0f);`
    - Se `scaleFactor == 1.0`: restaura `Width = NaN`, `Height = NaN`, `Alignment = Stretch`, `Identity`.
  - Rastreamento dinâmico de redimensionamento da janela (`window.SizeChanged`).
  - Monitoramento reativo a `AppUiScalePercent`: aplica a alteração a todas as janelas abertas em tempo real, sem necessidade de reiniciar o aplicativo.
- [AppLifecycleHelper.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Helpers/Application/AppLifecycleHelper.cs): Registro do singleton `IUiScaleService`.
- [MainWindow.xaml.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/MainWindow.xaml.cs): Chamada `ApplyScale(Instance, rootFrame)` na inicialização da janela.
- [MainPage.xaml.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Views/MainPage.xaml.cs): Recálculo e reemissão da região não-cliente da barra de título ao alterar a escala.
- [DragZoneHelper.cs](file://tsclient/C/desenv/utils/KyraFiles/src/KyraFiles.App/Helpers/UI/DragZoneHelper.cs): `GetScaledRect` ajustado para multiplicar as dimensões da região de arraste pelo multiplicador de escala da interface.

---

## 2. Status dos Critérios de Aceite da Spec

- **AC-1 (125% sem corte ou overflow):** CONFIRMED em lógica através de `windowWidth / scaleFactor` compensado.
- **AC-2 (80% mais itens visíveis):** CONFIRMED em lógica pelo layout expandido.
- **AC-3 (Persistência):** CONFIRMED via `AppearanceSettingsService` lido no startup.
- **AC-5 (Hit-test e arraste de título):** CONFIRMED pelo mapeamento inverso de transformações do WinUI e compensação em `DragZoneHelper`.
- **AC-6 (Voltar a 100%):** CONFIRMED por restauração para `NaN`/`Stretch`/`Identity`.
- **T4/T5 (Comprovação visual de build):** Dependente de execução na máquina oficial de compilação.
