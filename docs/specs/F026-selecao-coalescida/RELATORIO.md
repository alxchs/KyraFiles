# Relatório de Conclusão e Aceite: SPEC-0022 (F026 — Navegação por teclado fluida: seleção coalescida)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 5 pts  
**Evidências:** `docs/agents/evidence/F026/` (`relatorio.md`, `frames/`, `hold_key2.csv`)

---

## 1. Escopo e Implementação
Eliminação de engasgos e lentidão na navegação contínua por setas do teclado:
- Coalescência de efeitos colaterais caros da seleção (re-measure de toolbar e cálculo de tamanho/contagem na barra de status) com timer de 60–80 ms durante rajadas de teclas (< 150 ms).
- Atualização imediata do item destacado, foco e rolagem da lista de arquivos.
- Método `FlushSelectionToToolbar()` chamado antes de ações do usuário (Enter, Delete, F2) garantindo que nenhum comando atue sobre seleção defasada.
- Otimização de `CacheLength` nos layouts para reduzir consumo de memória e retenção desnecessária de contêineres de linha.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | Mediana de linhas alcançadas sobe de 52 para 93 (+79%) e CPU de UI cai | `CONFIRMED` | Medições em `docs/agents/evidence/F026/relatorio.md` |
| **AC-2** | Toolbar e barra de status atualizam imediatamente após a parada | `CONFIRMED` | Timer de 60 ms pós-rajada |
| **AC-3** | Teclas de ação (Delete/Enter) acionam Flush imediato | `CONFIRMED` | `FlushSelectionToToolbar()` |
| **AC-4** | Ctrl+A, Shift+setas e clique operam com normalidade | `CONFIRMED` | Verificado em `BaseLayoutPage.cs` |
