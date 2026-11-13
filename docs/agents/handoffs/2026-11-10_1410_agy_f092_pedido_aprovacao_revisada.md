# Handoff AGY -> Claude: Spec F092 Revisada com Todas as Correções Aplicadas

**Data:** 10/11/2026 14:10  
**De:** Antigravity (AGY)  
**Para:** Claude (arquiteta/revisora) e Alexandre  
**Branch:** `feature/f092-native-sidebar-parity`  
**Base:** `main` (commit `2e401a04f`)  

---

## 1. Resumo das Correções Aplicadas (conforme seu assignment)

1. **Separação de branches (§18.1):**
   - Criada branch `feature/decisions-20261110` saindo da `main`.
   - Cherry-pick do commit de decisões (`8664d871f`) e detalhamento dos mecanismos subjacentes para D-017 (opção 3 de janelas da T9 da F012 configurável), D-F036-1 (pausa Nível 1 entre arquivos estável, pausa Nível 2 CopyFile2 avançada configurável) e D-F036-3 (lote no mesmo volume permitido concorrentemente). Specs F012 e F036 atualizadas em conformidade.
   - Branch antiga `feature/f091-native-sidebar-parity` excluída.
2. **Spec renomeada para F092:**
   - Pasta renomeada para `docs/specs/F092-native-sidebar-parity/` e branch criada como `feature/f092-native-sidebar-parity`.
3. **Remoção de tecnologia dos requisitos funcionais:**
   - FR-001 e FR-003 reescritos em termos puramente comportamentais (sem `shell:MyComputerFolder` nem `shell:ControlPanelFolder`).
4. **Reconhecimento da arquitetura existente no Plan:**
   - Documentado que a seção de Rede (`SectionType.Network`) já existe com `ShowNetworkSection` (padrão `true`) e que `NetworkService.GetShortcutsAsync` já enumera atalhos de rede, reaproveitando esse pipeline em vez de duplicar em `StorageDevicesService`.
5. **Acréscimo da Tarefa T0:**
   - Adicionada `T0` de investigação e medição com `f092_sidebar_bench.ps1` e diagnóstico por evidência/asserção de `hd-roteador` e do servidor de mídia.
6. **Acréscimo da Tarefa para AC-5:**
   - Adicionada `T5` para expor e persistir os toggles de visibilidade de "Este Computador" e "Painel de Controle" em *Settings > Folders*.
7. **Previsão de prova de tela não-intrusiva:**
   - Prova de tela por automação silenciosa (UI Automation sem foco, agendada após as 23:59) detalhada na `T6`.
8. **Marcação de `[NEEDS CLARIFICATION]`:**
   - `AC-1`: Aguardando resposta do Alexandre se Bibliotecas devem aparecer ativadas também para usuários existentes ou apenas em instalação nova.
   - `AC-3`: Aguardando resposta do Alexandre sobre a natureza de `hd-roteador` e `alxchs@gmail.com (lenovo)` (atalhos, drive de roteador, servidor de mídia) e se deseja abrir conteúdo ou ver atalho.

---

## 2. Ação Solicitada à Claude
Revisar a spec F092 (`spec.md`, `plan.md` e `tasks.md`) e a branch de decisões `feature/decisions-20261110`. Nenhum código das tarefas T1 a T6 foi iniciado, aguardando aprovação formal.
