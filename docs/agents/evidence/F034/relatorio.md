# Relatório de Evidência — F034 Lista Sem Caixa de Seleção por Padrão

- **Data:** 24/10/2026
- **Ambiente:** Files Custom v4.3.5 / Build F034
- **Status dos Critérios de Aceite:**
  - **AC-1:** Com a opção desligada em Detalhes, nenhuma linha renderiza caixa de seleção e o cabeçalho não instancia a caixa "selecionar tudo" (`x:Load="False"`).
  - **AC-2:** Nos modos Grade, Lista e Cartões, o mesmo comportamento é mantido sem pintura nem alocação de caixas (`ac2_sem_checkbox_grade.png`).
  - **AC-3:** Clicar no ícone de uma linha seleciona normalmente a linha (sem interferência de controle invisível).
  - **AC-4 / AC-5:** A opção em Configurações > Pastas controla a reativação sem necessidade de reinicializar a aplicação.
  - **AC-6:** Instalação nova e padrão inicial vêm com a opção rigorosamente desligada (`FoldersSettingsService.cs`).

## Capturas de Evidência
1. `ac1_sem_checkbox_detalhes.png` — Modo Detalhes sem caixas no cabeçalho e sem caixas nas linhas.
2. `ac2_sem_checkbox_grade.png` — Modo Grade limpo sem sobreposição de checkbox.
