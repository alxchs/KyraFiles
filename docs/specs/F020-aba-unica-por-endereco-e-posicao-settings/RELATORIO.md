# Relatório de Conclusão e Aceite: SPEC-0017 (F020 — Aba única por endereço e Configurações na posição 0)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 2 pts  
**Evidências:** `tools/perf/test_f020_logic.ps1` (16/16 testes aprovados), nota de release `2026-10-15_0000_build-F020-aba-unica-configuracoes-posicao-0.md`

---

## 1. Escopo e Implementação
Implementação da regra de aba única e ancoragem fixa para configurações:
- `NormalizeNavigationPath` e `FindExistingTabIndex` em `NavigationHelpers.cs`: normalização case-insensitive e de barras.
- Reutilização automática de abas: tentar abrir endereço já aberto alterna o foco diretamente para a aba existente sem duplicar.
- Aba de Configurações sempre aberta ou movida para o índice 0 (extrema esquerda).
- Acionamento de Configurações pela barra lateral ou atalho não destrói a pasta em exibição na aba ativa.
- Proteção da ação de duplicar aba (`Ctrl+Shift+K`) e navegação na barra de endereços (Omnibar).

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | Abrir pasta já aberta transfere o foco para a aba existente | `CONFIRMED` | 16/16 testes lógicos aprovados |
| **AC-2** | Normalização estrita de caminho (caixa e barras) | `CONFIRMED` | `NavigationHelpers.NormalizeNavigationPath` |
| **AC-3** | Configurações sempre na posição 0 | `CONFIRMED` | `AppInstances.Move(existingIndex, 0)` |
| **AC-4** | Reabertura de Configurações foca índice 0 sem duplicar | `CONFIRMED` | `SidebarViewModel.cs` |
| **AC-5 / AC-6** | Barra de endereços e duplicação respeitam unicidade | `CONFIRMED` | `NavigationToolbarViewModel.cs` |
