# F031 — Plano

Status: **APROVADO** (Claude, 21/10/2026). Plano de investigação; a abordagem de código sai das medições (portão: nenhum código antes da T2).

## Arquitetura lida (CONFIRMED por leitura)
- `UserControls/TabBar/TabBar.xaml`: `TabView` com `TabItemsSource`; `SelectedIndex` ligado a `App.AppModel.TabStripSelectedIndex`.
- `TabBarItem` guarda um `Frame` por aba (`ContentFrame`); a aba nova navega um `ShellPanesPage` (`NavigationHelpers.AddNewTabByParamAsync`).
- A página inicial (Home) monta widgets (Acesso rápido, unidades, recentes); uma pasta monta a página de layout e lista os itens.

## Abordagem
1. **Medir (T1):** script `tools/perf/f031_tabs.ps1` mede de fora, por UI Automation, o tempo entre a tecla e o conteúdo pronto (nome do item focado/lista com itens), 10 repetições, Files e Explorer no mesmo lote, só com o computador ocioso e com as travas de primeiro plano (lições 16 e 17).
2. **Trace (T2):** `dotnet-trace` na troca e na aba nova, para listar o custo gerenciado; separar nativo por A/B.
3. **Hipóteses a testar uma por vez:** widgets da Home montados de forma síncrona na aba nova; recriação da página de layout na troca; `UpdateLayout` síncrono da barra de status; recarga da barra de ferramentas a cada troca.

## Arquivos
A definir pelas medições; o plano é atualizado (com data) antes do código.

## Testes
Os mesmos scripts antes e depois (AC-1, AC-2); conferência visual para AC-3.
