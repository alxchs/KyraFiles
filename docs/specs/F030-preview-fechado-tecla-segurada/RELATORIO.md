# Relatório de Conclusão e Aceite: SPEC-0026 (F030 — Preview fechado na tecla segurada)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 3 pts  
**Evidências:** `docs/agents/evidence/F030/` (`relatorio.md`, `f030_preview_during_hold.png`, `f030_preview_pane_open.png`, `f030_preview_released.png`)

---

## 1. Escopo e Implementação
Implementação da suspensão do carregamento de miniaturas e pré-visualizações durante o disparo repetido de teclas de navegação (Down, Up, PgDn, PgUp) com o painel lateral aberto:
- `AppModel.IsNavigationKeyHeld`: flag reativa detectando repetição de teclas.
- `InfoPaneViewModel`: desarma e cancela operações de leitura em andamento enquanto a tecla estiver pressionada.
- Preservação da geometria do painel lateral (sem re-layout ou flickers da lista de arquivos).

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | Painel aberto, tecla Down segurada por 3s: painel vazio durante a tecla; após soltura restaura o item final | `CONFIRMED` | `f030_preview_during_hold.png` e `f030_preview_released.png` |
| **AC-2** | Toque único em Down atualiza preview normalmente | `CONFIRMED` | Comportamento padrão preservado |
| **AC-3** | Down segurado 5s com painel aberto: fluidez e vazão sem retenção por previews intermediários | `CONFIRMED` | `docs/agents/evidence/F030/relatorio.md` |
