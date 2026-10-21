# F028 — Plano

Status: **APROVADO** (Claude, 20/10/2026).

## Arquitetura lida (CONFIRMED por leitura)
- `Services/Settings/DevToolsSettingsService.cs`: `IDEPath`/`IDEName` usam `Get(SoftwareHelpers.IsVSCodeInstalled() ? ... : ...)`; o argumento padrão é avaliado em toda leitura.
- `Helpers/Environment/SoftwareHelpers.cs`: `IsVSCodeInstalled()` enumera `Uninstall` em HKCU e HKLM abrindo cada subchave.
- `Actions/Content/PreviewPopup/LaunchPreviewPopupAction.cs`: a cada `SelectedItems` muda chama `SwitchPopupPreviewAsync` → `GetProviderAsync()` → `DetectAvailability` de QuickLook (pipe), Seer e PowerToys (registro).
- `Views/Layouts/DetailsLayoutPage.xaml.cs` `FileList_PreviewKeyDown`: ramos `PageDown`/`PageUp` (F024) chamam `SetSelectedItem` + `ScrollIntoView` + `FocusSelectedItems`.

## Abordagem
1. Cache do resultado de `IsVSCodeInstalled()` (campo estático preguiçoso).
2. `SwitchPopupPreviewAsync` com debounce: contador de versão + `Task.Delay(150)`; só a última mudança executa.
3. Remover os ramos `PageDown`/`PageUp` do handler (nativo assume).
Descartado: cache com TTL do provedor (mantém detecção por mudança de seleção).

## Arquivos
`SoftwareHelpers.cs`, `LaunchPreviewPopupAction.cs`, `DetailsLayoutPage.xaml.cs`. **Não mexer:** `Toolbar.xaml.cs`, `BaseLayoutPage.cs`.

## Riscos
Instalar o VS Code com o app aberto só aparece após reiniciar; preview acompanha com 150 ms de atraso.

## Testes
AC-1 pelos scripts F025 e trace Speedscope; AC-2 a AC-4 manual/UI Automation.

## Histórico (20/10/2026, ampliação)
- **FR-5:** `DetailsLayoutPage.FileList_PreviewKeyDown`: em repetição (`KeyStatus.WasKeyDown`) de Up/Down/PgUp/PgDn, descarta enquanto `navigationStepPending`; o flag cai no 2º `CompositionTarget.Rendering` depois do passo.
- **FR-6:** `DetailsLayoutPage.xaml`: `CacheLength` removido (padrão 4, igual ao upstream). O 1.0 da F026 deixava o mouse esvaziar a tela.
- `BaseLayoutPage`: `SelectionEffectsBurstMs` 350 e `SelectionEffectsSettleMs` 350. Com 120 ms, o intervalo entre passos (agora esperando o quadro) passava do tempo de espera e o toolbar voltava a ser reconstruído a cada passo (15 linhas em 4 s).
- `LaunchPreviewPopupAction`: no caminho de troca de seleção, o provedor detectado fica guardado 5 s (o trace mostrou a detecção ainda rodando, 302 ms).
- **2ª ampliação (FR-7):** `KeyStatus.WasKeyDown` **não funciona** no WinUI 3 desktop (provado: o Windows marca 29 de 30 teclas sintéticas como repetição, `RepeatProbe`, mas a espera de quadro e o adiamento não entravam). Repetição agora = 2º `KeyDown` da mesma tecla sem `KeyUp` (handler de `KeyUp` com `handledEventsToo`). `BaseLayoutPage.ExtendNavigationDeferral()` mantém o adiamento de rolagem já existente (`NotifyScrollStateChanged`); o timer de 200 ms não encerra o adiamento enquanto `IsNavigationKeyHeld` (estado real da tecla, `GetKeyStateForCurrentThread`), porque uma página pode bloquear o thread de UI por mais de 200 ms e o timer disparava entre páginas (instrumentação temporária: 169 cargas iniciadas e 0 adiadas antes; 292 adiadas depois; removida).
- **3ª revisão (21/10/2026, FR-3 revisada, FR-8):** o cálculo de `e59007015` somava `página × RowHeight` ao deslocamento e escolhia `atual + página`, o que deixa o destino na primeira linha do viewport, coberta pelo cabeçalho fixo (`HeaderGrid`). Novo cálculo, por geometria real lida na hora: topo da área útil = base do `HeaderGrid` no viewport (0 se ele saiu da tela); posição de uma linha = posição real de um contêiner realizado (`ItemsStackPanel.FirstVisibleIndex`) + diferença de índice × altura real da linha. Daí saem a primeira e a última linha inteiramente visíveis, o destino (FR-3 revisada) e o deslocamento mínimo que deixa o destino inteiro na área útil. Lista agrupada (`CollectionViewSource.IsSourceGrouped`) não é tratada (nativo). Ordem de exibição tirada de `FileList.Items`, não de `FilesAndFolders`. Só `DetailsLayoutPage.xaml.cs`.
