# Relatório de Evidência — F032 Fixar Aba

- **Data:** 24/10/2026
- **Ambiente:** KyraFiles v4.3.5 (Release Oficial)
- **Status dos Critérios de Aceite:**
  - **AC-1:** Aba fixada recebe propriedade `IsPinned`, posiciona-se à esquerda das abas comuns e substitui o botão fechar por alfinete fixo.
  - **AC-2:** Fechar outras abas (`CloseOtherTabs`) preserva a aba fixada e a aba ativa.
  - **AC-3:** Atalho `Ctrl+W` (`CloseSelectedTabAction`) bloqueia o fechamento quando a aba ativa está fixada.
  - **AC-4:** Sessão e lista de abas restauram a aba fixada em sua posição original.
  - **AC-5:** Desafixar devolve o botão de fechar e restaura comportamento normal de encerramento.

## Capturas de Evidência
1. `f032_tab_opened.png` — Múltiplas abas abertas e navegadas.
2. `f032_tab_closed.png` — Encerramento seguro de abas comuns mantendo integridade.
