# Handoff: F090 — Auditoria, Correções de Bugs e Testes Lógicos

- **Data/Hora:** 2026-11-11 22:06 BRT
- **Autor:** AGY (Antigravity)
- **Destinatário:** Claude / Alexandre
- **Branch:** `feature/f090-escala`

---

## 1. O que foi encontrado e corrigido na revisão de código

1. **Bug crítico de escala quadrática (`UiScaleService.cs`) — CONFIRMED no código:**
   - Antes, `ApplyLayoutCompensation` definia simultaneamente `rootElement.Scale = Vector3(s, s, 1)` **e** `rootElement.TransformMatrix = Matrix4x4.CreateScale(s, s, 1)`. Em WinUI 3 Composition, `TransformMatrix` e `Scale` se multiplicam (`s * s`), o que aplicaria 156,25% em 125%, 225% em 150% e 64% em 80%, cortando a interface apesar da compensação de largura/altura por `1 / s`.
   - Correção: fixado `CenterPoint = Vector3.Zero`, mantido apenas `rootElement.Scale = new Vector3((float)scaleFactor, (float)scaleFactor, 1.0f)` e `rootElement.TransformMatrix = Matrix4x4.Identity`.
   - Também corrigido rastreamento de janelas reutilizadas (ex.: cache de `WindowEx` em `FilePropertiesHelpers`, onde `Content` é trocado por um novo `Frame`), usando `TrackedWindowEntry`, `e.Size` no `SizeChanged`, limpeza de handlers no `Closed` e fallback no `Loaded`.

2. **Bug de região de arraste da barra de título (`DragZoneHelper.cs`) — CONFIRMED no código:**
   - `Caption` e `RightBorder` usavam `size.Width` (que já vem em pixels físicos de `AppWindow.Size`) dentro de `GetScaledRect`, multiplicando por `uiScaleMultiplier`: em 80%, os 20% direitos da barra de título perderiam `Caption` (não arrastáveis); em 150% + DPI alto, `RightBorder` saía da janela.
   - `GetScaledRect` também misturava `TransformToVisual(null)` (já escalado pela `Scale` da raiz) com `ActualSize` (local não escalado).
   - Correção: funções puras separadas em `UiScaleMath.cs` (`ScaleLocalRectToPhysical`, `ScaleRootElementRectToPhysical`, `ComputeTitleBarRegions`), mantendo `CaptionWidth = windowPixelWidth` e separando coordenadas de janela vs locais.

3. **Janelas secundárias sem escala (`FR-2` / `AC-4` / `plan.md`) — CONFIRMED no código:**
   - Adicionada chamada `Ioc.Default.GetService<IUiScaleService>()?.ApplyScale(...)` em:
     - `src/KyraFiles.App/Views/Settings/SettingsDialog.cs` (janela modal de Configurações F082)
     - `src/KyraFiles.App/Views/Settings/ToolbarCustomizationDialog.cs`
     - `src/KyraFiles.App/Utils/Storage/Helpers/FilePropertiesHelpers.cs`
     - `src/KyraFiles.App/Views/HexEditor/HexEditorWindow.cs`

4. **Coordenadas do menu de contexto (`FastContextFlyout.cs`) — CONFIRMED no código:**
   - Corrigida tolerância vertical `target.ActualHeight * scale * uiScale` e trocado `anchor.TransformToVisual(content)` por `anchor.TransformToVisual(null)` para incluir o fator de escala da raiz na predição de abertura para cima/baixo.

5. **Normalização e sincronização (`AppearanceSettingsService.cs`, `AppearanceViewModel.cs`):**
   - `AppUiScalePercent` normalizado via `UiScaleMath.NormalizeScalePercent` (faixa 80..150, padrão 100 se `<= 0`).
   - `AppearanceViewModel` usa `UiScaleMath.SupportedScalePercents` e sincroniza `SelectedAppUiScaleIndex` se `AppUiScalePercent` mudar.

---

## 2. Evidência executada nesta sessão

- **Busca de toolchain na máquina (`HELOIZE-PC`):**
  - Comando: `Test-Path` em `C:\Program Files (x86)\Windows Kits`, `C:\Program Files\Microsoft Visual Studio`, `C:\Program Files (x86)\Microsoft Visual Studio`, `C:\BuildTools`, `D:\` -> todos `False`.
  - `dotnet --info`: `.NET SDK 10.0.403` instalado. Projetos `KyraFiles.Shared`, `KyraFiles.App.Search.Index` e `KyraFiles.App.IndexService` restauram do cache local, mas `KyraFiles.App` (WinUI/MSIX) não compila sem Windows SDK.
- **Testes lógicos da F090 (`tests/test-ui-scale-math.ps1`):**
  - Comando: `powershell -NoProfile -File tests/test-ui-scale-math.ps1`
  - Resultado: **19/19 PASS** (`NormalizeScalePercent`, `ComputeCompensatedSize`, `ScaleLocalRectToPhysical`, `ScaleRootElementRectToPhysical`, `ComputeTitleBarRegions` em 80%, 100%, 125%, 150% + DPI).
- **Guarda de entrada (`tools/perf/Test-NoRawInput.ps1`):**
  - Comando: `powershell -NoProfile -File tools/perf/Test-NoRawInput.ps1`
  - Resultado: exit 0 (`SUCESSO`).
- **Line endings CRLF:** verificados e normalizados nos arquivos criados.

---

## 3. Decisão pendente e bloqueios

- **Registrada em `DECISIONS.md` (`D-F090-1`, PENDING DECISION):** escopo final de T4 para popups transitórios (`MenuFlyout`/`ToolTip`/`ContentDialog` em `PopupRoot`) e atalhos `Ctrl++`/`Ctrl+-`.
- **Estado de compilação / UI:** build completo WinUI, `verify_no_index_service.ps1`, capturas em 80%/100%/125%/150% e teste real de arraste/popups permanecem `NOT TESTED` até rodar na máquina com Windows SDK.
- **Estado do Git:** alterações atuais estão **não commitadas no working tree** da branch `feature/f090-escala` (na sandbox do agente, o `git` falha por permissão ao ler `C:/Users/heloi/.dev-method/git-hooks.gitconfig`).

---

## 4. Próximo passo recomendado

1. Commit local das alterações atuais em `feature/f090-escala` (fora da sandbox ou com acesso aos hooks globais).
2. Na máquina com Visual Studio + Windows SDK:
   - Compilar `KyraFiles.App` (`Release | x64`, `0 Error(s), 0 Warning(s)`).
   - Validar visualmente 80%, 100%, 125% e 150% na `MainWindow` e no `SettingsDialog` (capturas em `docs/agents/evidence/f090/`).
   - Testar arraste da barra de título (inclusive canto direito em 80%) e menus de contexto.
   - Rodar `pwsh tools\perf\verify_no_index_service.ps1` e comparar navegação em pasta grande com `main`.
   - Fechar `D-F090-1` e integrar na `main` via squash/rebase + ff-only.
