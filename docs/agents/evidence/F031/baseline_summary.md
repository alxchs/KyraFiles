# F031 — Linha de Base e Resultados Pós-Otimização (AC-1, AC-2, AC-3, NFR-1)

Data: 2026-10-22
Runs: 10 repetições oficiais por cenário
Ambiente: KyraFiles (Windows 11 x64)

## 1. Comparativo de Desempenho (Linha de Base vs Pós-Otimização)

Valores em milissegundos (ms):

| Cenário | Métrica | Linha de Base (v4.3.2) | Pós-Otimização (F031) | Redução Obtida | Meta F031 (AC-2) | Status |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: |
| **Troca de Aba (SwitchTab)** | Mediana | **1.116,7 ms** | **456,1 ms** | **-59,1%** | Redução ≥ 30% | **SUPERADA** |
| | Média | 1.231,6 ms | 509,4 ms | -58,6% | - | - |
| | Mínimo | 345,1 ms | 282,3 ms | -18,2% | - | - |
| | Máximo | 2.833,5 ms | 1.099,0 ms | -61,2% | - | - |
| **Nova Aba (NewTab)** | Mediana | **526,4 ms** | **287,9 ms** | **-45,3%** | Redução ≥ 30% | **SUPERADA** |
| | Média | 551,6 ms | 403,5 ms | -26,8% | - | - |
| | Mínimo | 426,8 ms | 208,8 ms | -51,1% | - | - |
| | Máximo | 678,7 ms | 1.058,1 ms | - | - | - |

## 2. Diagnóstico das Causas e Solução Aplicada (T2 e T3)
1. **Foco sem atraso artificial:** Removido o `await Task.Delay(100);` em `MainPage.xaml.cs` e substituído por `DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, ...)` não bloqueante.
2. **Fast-path de título na troca de abas:** Em `NavigationHelpers.cs`, reaproveitamento imediato do `MainPageViewModel.SelectedTabItem.Header` em memória, evitando consultas e I/O de disco a cada alternância de aba.

## 3. Validação de Não-Regressão e Critérios de Aceite
- **AC-1:** Linha de base medida com script versionado `tools/perf/f031_tabs.ps1` e registrada em `baseline_tabs.csv`.
- **AC-2:** Redução da mediana da troca de abas em **59,1%** (meta: ≥ 30%) e de nova aba em **45,3%** (meta: ≥ 30%).
- **AC-3:** Preservação de rolagem e seleção confirmada visualmente ao alternar abas (`tab10_proof.png`).
- **NFR-1:** Memória privada com 10 abas abertas manteve estabilidade (crescimento de apenas 0,08%, muito abaixo do teto de 10%).

Arquivos de evidência:
- `docs/agents/evidence/F031/baseline_tabs.csv` (10 rodadas linha de base)
- `docs/agents/evidence/F031/optimized_tabs.csv` (10 rodadas pós-otimização)
- `docs/agents/evidence/F031/tab10_proof.png` (comprovação visual de abas)
