# Relatório de Conclusão e Aceite: SPEC-0030 (F034 — Lista sem caixa de seleção por padrão)

**Data de conclusão:** 22/11/2026  
**Versão:** v4.3.31.0  
**Status:** CONCLUÍDO (`completed`)  
**Pontos:** 2 pts  
**Evidências:** `docs/agents/evidence/F034/` (`relatorio.md`, `ac1_sem_checkbox_detalhes.png`, `ac2_sem_checkbox_grade.png`)

---

## 1. Escopo e Implementação
Eliminação do custo de instanciação e pintura de caixas de seleção na lista de arquivos:
- `x:Load="False"` nas caixas de seleção das linhas e cabeçalho em todos os layouts (Detalhes, Grade, Lista, Blocos, Cartões).
- Configuração "Mostrar caixas de seleção ao selecionar itens" desligada por padrão.
- Reativação dinâmica sem necessidade de reiniciar a aplicação quando o usuário optar por ligar a opção em Configurações > Pastas.
- Clique no ícone do arquivo atua normalmente na seleção da linha sem interferência de elemento invisível.

---

## 2. Matriz de Critérios de Aceite

| ID | Cenário | Estado | Evidência |
|---|---|---|---|
| **AC-1** | Detalhes sem caixas de seleção no cabeçalho ou linhas | `CONFIRMED` | `ac1_sem_checkbox_detalhes.png` |
| **AC-2** | Modos Grade e Cartões sem caixas sobrepostas no ícone | `CONFIRMED` | `ac2_sem_checkbox_grade.png` |
| **AC-3** | Clique no ícone seleciona a linha diretamente | `CONFIRMED` | Comportamento nativo WinUI limpo |
| **AC-4 / AC-5** | Ligar e desligar em Configurações reflete instantaneamente | `CONFIRMED` | Reatividade em tempo de execução via `FindName` |
| **AC-6** | Instalação padrão vem rigorosamente com caixas desligadas | `CONFIRMED` | Padrão em `FoldersSettingsService.cs` |
