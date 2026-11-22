# Relatório de Conclusão e Aceite: SPEC-0004 (F005 — Preview e Details com acesso imediato)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 2 pts  
**Evidências:** `docs/agents/evidence/f005/` (`initial.png`, `current_state.png`, `infopane_opened.png`, `infopane_closed.png`, `preview_tab_selected.png`, `details_tab_selected.png`, `test_alt_p.png`, `test_chord_alt_p.png`)

---

## 1. Escopo e Implementação
Implementação do chaveamento e acesso imediato aos painéis de pré-visualização e detalhes:
- Atalhos `Alt+P` (Preview) e `Alt+Shift+P` (Details) para acionamento direto do teclado.
- Função pura de chaveamento em `Helpers/` tratando abertura, fechamento e alternância de abas do painel lateral.
- `TogglePreviewPaneAction` e `ToggleDetailsPaneAction` atualizadas com `IsAccessibleGlobally => true`.
- Atalhos devidamente expostos nos tooltips das abas em `InfoPane.xaml`.
- Restauração correta de foco para a lista de arquivos ao fechar o painel por atalho.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1 a AC-6** | Alternância lógica e direta entre painéis fechado, preview e detalhes | `CONFIRMED` | 6 casos cobertos e testados |
| **AC-7 / AC-8** | Acionamento por atalhos Alt+P e Alt+Shift+P | `CONFIRMED` | `test_alt_p.png` e `test_chord_alt_p.png` |
| **AC-9** | Fechamento por atalho devolve foco à lista de arquivos | `CONFIRMED` | Comprovado em `infopane_closed.png` |
| **AC-10 / AC-11** | Tooltips exibem os respectivos atalhos | `CONFIRMED` | Textos em `InfoPane.xaml` |
