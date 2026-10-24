# Relatório de Evidência — F030 Preview Fechado na Tecla Segurada

- **Data:** 24/10/2026
- **Ambiente:** Files Custom v4.3.5 (Release Oficial)
- **Status dos Critérios de Aceite:**
  - **AC-1:** Com o painel de preview/detalhes aberto, segurar tecla de navegação suspende o carregamento de miniaturas (`f030_preview_during_hold.png`), evitando travamento da lista.
  - **AC-2:** Ao soltar a tecla, o item selecionado naquele momento é aplicado de forma única e estável (`f030_preview_released.png`).
  - **AC-3:** Toque isolado e navegação única mantém carregamento normal sem alteração visual de layout.

## Capturas de Evidência
1. `f030_preview_pane_open.png` — Painel lateral ativado via atalho `Alt+P`.
2. `f030_preview_during_hold.png` — Estado durante tecla segurada (cargas intermediárias desarmadas).
3. `f030_preview_released.png` — Estado final restaurado após soltura da tecla.
