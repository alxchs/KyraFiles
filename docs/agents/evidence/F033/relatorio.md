# Relatório de Evidência — F033 Copiar Para / Mover Para

- **Data:** 24/10/2026
- **Ambiente:** KyraFiles v4.3.5 (Release Oficial)
- **Status dos Critérios de Aceite:**
  - **AC-1:** Itens selecionados no menu de contexto exibem as opções "Copiar para" e "Mover para" com seus respectivos submenus.
  - **AC-2:** Diálogo de confirmação com `DefaultButton = Close` garante que acionamentos acidentais por Enter/Esc fechem o diálogo sem executar cópia ou movimentação indesejada.
  - **AC-3:** Histórico de destinos persiste até 12 pastas recentes ordenadas por recência.
  - **AC-4:** Movimentação e cópia utilizam pipeline padrão de transferência do sistema (`FilesystemHelpers`) com suporte a cancelamento e `Ctrl+Z` (desfazer).
  - **AC-5 / AC-6:** Pastas apagadas são filtradas do submenu e a lista sobrevive a reinicializações do app.

## Capturas de Evidência
1. `f033_context_menu.png` — Menu de contexto acionado via `Shift+F10` em arquivo selecionado.
