# Handoff AGY -> Claude: Clarifications Resolvidas e T0 Concluída na Spec F092

**Data:** 10/11/2026 18:40  
**De:** Antigravity (AGY)  
**Para:** Claude (arquiteta/revisora) e Alexandre  
**Branch:** `feature/f092-native-sidebar-parity`  
**Base:** `main` (commit `2e401a04f`)  

---

## 1. Respostas do Alexandre às Clarifications
1. **Bibliotecas (AC-1):** Decidido: *"1 para todos"* — ativadas de fábrica tanto para novas instalações quanto atualizações de usuários com configurações existentes.
2. **Locais de Rede e Dispositivos em Este Computador (AC-3):** Decidido: *"2 utilizar o conteudo como se estivesse no meu computador, assim como faço hoje."* — navegação completa e nativa pelo conteúdo de pastas e arquivos de `hd-roteador` e do dispositivo `alxchs@gmail.com (lenovo)`.

---

## 2. Tarefa T0 de Investigação e Baseline Concluída
- **Script criado:** `tools/perf/f092_sidebar_bench.ps1` (medição de baseline sem roubo de foco).
- **Diagnóstico Técnico de Causa Raiz ([`diagnostico_t0.md`](file:///c:/desenv/utils/KyraFiles/docs/specs/F092-native-sidebar-parity/diagnostico_t0.md)):**
  1. `StorageNetworkService.cs` já implementa `GetShortcutsAsync()`, mas `SidebarViewModel.cs` (linha 383) liga a seção de Rede unicamente a `NetworkService.Computers`. A coleção `Shortcuts` nunca é exibida na barra lateral.
  2. No Windows nativo, `alxchs@gmail.com (lenovo)` e `hd-roteador` são pastas delegadas do Shell registradas em `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\DelegateFolders` (`{289AF617-1CC3-42A6-926C-E6A863F0E3BA}` e `{b155bdf8-02f0-451e-9a26-ae317cfd7779}`).
  3. `StorageDevicesService.cs` usava exclusivamente `DriveInfo.GetDrives()`, ignorando pastas delegadas e atalhos de rede.

---

## 3. Estado da Spec e Solicitação
- `docs/specs/F092-native-sidebar-parity/spec.md`: APROVADA pelo Alexandre em 10/11/2026.
- `docs/specs/F092-native-sidebar-parity/plan.md`: Alinhado com a arquitetura existente.
- `docs/specs/F092-native-sidebar-parity/tasks.md`: T0 marcada como `concluído`; T1 a T6 aguardando aprovação da Claude para início de código.
- Solicito a aprovação formal da Claude no `spec.md` para iniciar as tarefas T1 a T6.
