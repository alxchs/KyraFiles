# F031 — Plano

Status: **APROVADO E ATUALIZADO** (AGY, 22/10/2026 - T2 concluída, hipóteses identificadas para T3).

## Linha de Base Medida (T1 / AC-1)
- **Troca de Aba (SwitchTab):** Mediana de **1.116,7 ms** (min: 345,1 ms, max: 2.833,5 ms).
- **Nova Aba (NewTab):** Mediana de **526,4 ms** (min: 426,8 ms, max: 678,7 ms).
- Evidências salvas em `docs/agents/evidence/F031/baseline_summary.md` e `baseline_tabs.csv`.

## Diagnóstico e Causas-Raiz Identificadas (T2)
1. **Delay Artificial de 100 ms em `MultitaskingControl_CurrentInstanceChanged`:**
   - Em `src/KyraFiles.App/Views/MainPage.xaml.cs` (linha 219), existe `await Task.Delay(100);` forçado no fluxo principal antes de invocar `FocusActivePane()`.
2. **I/O de Disco Redundante na Troca de Aba:**
   - A chamada `await NavigationHelpers.UpdateInstancePropertiesAsync(navArgs)` recalcula via `GetSelectedTabInfoAsync(path)` no disco o título da janela a cada troca de aba, ignorando que o `tabItem.Header` já possui o nome da pasta em cache.
3. **Ciclo Síncrono de Reavaliação de Painéis:**
   - `LoadPaneChanged()` e notificações do `SidebarAdaptiveViewModel` forçam medições síncronas de bounds da janela e recalibram o InfoPane mesmo quando o estado não mudou.
4. **Criação de Nova Aba com Carga Síncrona:**
   - `AddNewTabByParamAsync` dispara navegação imediata do `ContentFrame` na thread de UI e chama `UpdateTabInfoAsync` bloqueando a exibição do novo item.

## Abordagem de Otimização (T3)
- **Otimização 1 (FR-1):** Eliminar o `Task.Delay(100)` de `MainPage.xaml.cs`, trocando por `DispatcherQueue.TryEnqueue` para o foco sem atraso forçado.
- **Otimização 2 (FR-1):** Otimizar `UpdateInstancePropertiesAsync` para reutilizar o `tabItem.Header` já em memória quando disponível, evitando varredura de disco desnecessária.
- **Otimização 3 (FR-2):** Otimizar `AddNewTabByParamAsync` para inicializar a aba com título provisório instantâneo e renderização assíncrona desacoplada da thread de layout da barra de abas.
- **Validação A/B:** Medir novamente com `tools/perf/f031_tabs.ps1` comprovando a redução da latência (meta: redução >= 30%).

## Arquivos Afetados
- `src/KyraFiles.App/Views/MainPage.xaml.cs`
- `src/KyraFiles.App/Helpers/Navigation/NavigationHelpers.cs`
- `src/KyraFiles.App/UserControls/TabBar/BaseTabBar.cs`
