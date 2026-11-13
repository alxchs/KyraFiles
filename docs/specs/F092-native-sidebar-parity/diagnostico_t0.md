# F092 — Diagnóstico Técnico de Baseline (T0)

**Data:** 10/11/2026  
**Responsável:** Antigravity (AGY)  

---

## 1. Causa Raiz da Ausência dos Itens de Rede e Dispositivos (CONFIRMED no Código)

1. **Coleção `NetworkService.Shortcuts` não exposta na interface:**
   - Em `src/KyraFiles.App/Services/Storage/StorageNetworkService.cs`, existem a propriedade `Shortcuts` e o método `GetShortcutsAsync()`, que lê `PInvoke.FOLDERID_NetHood`.
   - Porém, em `src/KyraFiles.App/ViewModels/UserControls/SidebarViewModel.cs` (linha 383):
     ```csharp
     SectionType.Network => NetworkService.Computers.Cast<DriveItem>().ToList().AsReadOnly(),
     ```
     A seção de Rede da barra lateral observa e exibe **apenas `NetworkService.Computers`**, ignorando por completo a coleção de atalhos.
2. **Ausência de Delegate Folders de Este Computador:**
   - No Windows nativo, `alxchs@gmail.com (lenovo)` e `hd-roteador` são injetados sob *This PC* via pastas delegadas do Shell registradas em `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\DelegateFolders`:
     - `{289AF617-1CC3-42A6-926C-E6A863F0E3BA}`: DLNA Media Servers Data Source
     - `{b155bdf8-02f0-451e-9a26-ae317cfd7779}`: Delegate folder for Network Places in Computer
   - O `StorageDevicesService.cs` utiliza unicamente `DriveInfo.GetDrives()`, que apenas enxerga volumes com letra montada (`C:\`, `D:\`), sem consultar as pastas delegadas do Shell.
3. **Bibliotecas Desativadas por Padrão:**
   - Em `src/KyraFiles.App/Services/Settings/GeneralSettingsService.cs`, linha 184: `ShowLibrarySection => Get(false);`.

---

## 2. Medição de Baseline de Performance (NFR-001)
- **Script:** `tools/perf/f092_sidebar_bench.ps1`
- **Execução:** 2 rodadas sem roubo de foco (`-WindowStyle Minimized`).
- **Tempo médio de inicialização / disponibilidade:** 15.982 ms (estabilização completa do processo e serviços de segundo plano).
- **Critério de Aceite pós-F092:** O carregamento assíncrono das novas seções não pode acrescer tempo perceptível à inicialização da barra lateral (< 50ms na thread de UI).
