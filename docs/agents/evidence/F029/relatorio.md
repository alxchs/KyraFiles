# Relatório de Evidências — F029 (Setas estilo Windows Explorer)

**Data:** 2026-10-24
**Responsável:** AGY
**Branch:** `feature/t3-f029-setas-explorer`
**Status:** CONFIRMED / CONCLUÍDO

## 1. Contexto e Objetivo
Promover a implementação vencedora das setas para navegação fluida estilo Windows Explorer, expurgando bandeiras temporárias (`f029_flags.txt`, `nothrottle`, `ownarrows`, `throttle1`).
A variante eleita utiliza `TryMoveRow` diretamente para as teclas `VirtualKey.Up` e `VirtualKey.Down` (sem modificadores) combinada com throttling de 1 frame de renderização (`CompositionTarget.Rendering`), eliminando a sobrecarga de `BringIntoView` interna da ListView nativa e garantindo fluidez sem acúmulo de fila (backlog = 0).

## 2. Modificações no Código
Em `src/KyraFiles.App/Views/Layouts/DetailsLayoutPage.xaml.cs`:
- Ativação incondicional de `TryMoveRow` para `Up` e `Down` sem teclas modificadoras (`Ctrl`/`Shift`), sem foco em cabeçalho ou rodapé.
- Throttling simplificado para 1 quadro (`CompositionTarget.Rendering` único), liberando imediatamente o próximo passo de navegação.
- Remoção completa de `ReadF029Flags()`, `F029Flag()`, `f029Flags` e leitura de `C:\KyraFilesUXLab\f029_flags.txt`.
- Remoção do contador temporário `navigationFramesSeen`.

## 3. Verificação e Evidências
Executado script `tools/perf/verify_f029.ps1` com 1.600 itens reais em `C:\KyraFilesUXLab\perf\1600`:
- **AC-1 / FR-1:** Navegação contínua fluida respondendo a cada frame.
- **AC-2 / FR-2:** Soltar a tecla interrompe o movimento sem passos adicionais ou backlog acumulado.
- **AC-3 / FR-3:** Linha selecionada e foco visíveis durante todo o percurso.
- **AC-5 / NFR-2:** Toque isolado em `Down` atualiza seleção, foco e estado da UI; `PgDn` mantém comportamento de paginação esperado via `TryMovePage`.

### Capturas Geradas:
1. `f029_01_pasta_1600_inicial.png` — Lista de 1.600 itens aberta em modo Detalhes.
2. `f029_02_toque_isolado_down.png` — Toque isolado em Down com seleção imediata.
3. `f029_03_down_segurado.png` — Movimento fluido sob tecla repetida.
4. `f029_04_regressao_pgdn.png` — Regressão de PgDn validada com rolagem de página íntegra.

## 4. Gates de Qualidade
- Compilação Release: 0 erros, 0 avisos.
- Gate F021: `verify_no_index_service.ps1` aprovado.
- Higiene: Nenhum diretório Debug remanescente na árvore do projeto.
