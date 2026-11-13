# F092 — Paridade da Barra Lateral com o Explorador Nativo
## T0 — Diagnóstico Técnico de Baseline e Investigação do Shell

**Data:** 10/11/2026 19:00  
**Autor:** Antigravity (AGY)  
**Status:** CONCLUÍDO (Submetido para avaliação da Claude)  
**Branch:** `feature/f092-native-sidebar-parity`  

---

### 1. Resumo Executivo

A investigação técnica da Tarefa T0 foi conduzida silenciosamente (sem abertura de janelas visíveis, sem consoles efêmeros e com preservação estrita do foco de digitação do usuário, em conformidade com a Regra Zero).

O diagnóstico identificou de forma exata e com comprovação técnica (estados `CONFIRMED` e comandos reproduzíveis) a causa raiz da ausência de cada item marcado com seta pelo Alexandre no Explorador nativo (`media_1794303153426_bed82879.png`):
1. **`hd-roteador` e `alxchs@gmail.com (lenovo)`** são pastas delegadas (*DelegateFolders*) do Shell sob "Este Computador" (*This PC* / `ssfDRIVES` / `shell:MyComputerFolder`). O KyraFiles não os listava porque sua seção "Drives" (`StorageDevicesService.cs`) utiliza exclusivamente `DriveInfo.GetDrives()`, que só enxerga letras de volume montadas (`C:\`, `D:\`, `R:\`).
2. **"Este Computador" (*This PC*) e "Painel de Controle"** não existiam como nós de navegação no enum `SectionType` nem em `SidebarViewModel.cs`.
3. **"Bibliotecas" (*Libraries*)** já tem implementação completa no KyraFiles (`SectionType.Library`), mas seu padrão de configuração em `GeneralSettingsService.cs` estava como `false` (desligado).
4. A navegação no conteúdo de `hd-roteador` e servidores de mídia UPnP foi testada via Shell COM e `SHCreateItemFromParsingName`: **o Shell do Windows consegue enumerar seus arquivos e pastas com sucesso** (`CONFIRMED`).

---

### 2. Evidências Técnicas Levantadas

#### Evidência 1: Enumeração do Namespace 17 (`ssfDRIVES` / "Este Computador")
- **Comando executado:** Inspeção via `Shell.Application.NameSpace(17).Items()` (`tools/perf/f092_inspect.ps1`).
- **Estado:** `CONFIRMED`
- **Resultado:**
  ```text
  Name                                   Type          IsFileSystem  IsFolder  SubItemsCount  Parsing Path
  ----                                   ----          ------------  --------  -------------  ------------
  hd-roteador                            Media Server  False         True      4              ::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\uuid:4d696e69-444c-164e-9d41-000000008fbe
  alxchs@gmail.com (lenovo)              Media Server  False         True      4              ::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\uuid:469c9335-af03-41f6-a2a9-995e411d8031
  heloizedittrich@gmail.com (heloize-pc) Media Server  False         True      0              ::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\uuid:dbf6fa24-ac83-4533-a571-4f53567e1c9f
  Windows-SSD (C:)                       Local Disk    True          True      40             C:\
  Data (D:)                              Local Disk    True          True      5              D:\
  Local Disk (R:)                        Local Disk    True          True      16             R:\
  ```
- **Conclusão:** No Windows Explorer nativo, `hd-roteador` e `alxchs@gmail.com (lenovo)` aparecem exatamente como filhos de "This PC".

#### Evidência 2: Registro do Windows (DelegateFolders de MyComputer)
- **Caminho:** `HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\DelegateFolders`
- **Estado:** `CONFIRMED`
- **Chaves encontradas:**
  - `{289AF617-1CC3-42A6-926C-E6A863F0E3BA}` = DLNA Media Servers Data Source
  - `{35786D3C-B075-49b9-88DD-029876E11C01}` = Portable Devices
  - `{9113A02D-00A3-46B9-BC5F-9C04DADDD5D7}` = Enhanced Storage Data Source
  - `{b155bdf8-02f0-451e-9a26-ae317cfd7779}` = nethood delegate folder

#### Evidência 3: Por que o KyraFiles omitia esses itens
- **Localização no código:** `src/KyraFiles.App/Services/Storage/StorageDevicesService.cs` linha 21:
  ```csharp
  var drives = await Task.Run(DriveInfo.GetDrives).ConfigureAwait(false);
  ```
- **Estado:** `CONFIRMED`
- **Análise:** A API .NET `DriveInfo.GetDrives()` retorna unicamente volumes Win32 montados com letra de unidade (C:, D:, R:). Dispositivos delegados, compartilhamentos sem letra e servidores DLNA/UPnP não possuem letra e são completamente ignorados por essa chamada.
- Além disso, em `SidebarViewModel.cs`:
  - `SectionType.Drives` renderiza apenas `drivesViewModel.Drives` (populado por `StorageDevicesService.cs`).
  - `SectionType.Network` renderiza apenas `NetworkService.Computers`. A coleção `NetworkService.Shortcuts` (que lê `FOLDERID_NetHood`) não era conectada à barra lateral.

#### Evidência 4: Capacidade de Navegar no Conteúdo de `hd-roteador`
- **Comando executado:** Inspeção profunda de subitens via `f092_inspect_subitems.ps1` e chamada nativa a `SHCreateItemFromParsingName` (`tools/perf/f092_test_shcreate.ps1`).
- **Estado:** `CONFIRMED`
- **Resultado:**
  - `SHCreateItemFromParsingName` para `::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\uuid:4d696e69-444c-164e-9d41-000000008fbe` retorna `hr = 0` (`SUCCESS`).
  - `GetFolder` retorna 4 subpastas:
    - `Browse Folders` (`path\64`): contém `$RECYCLE.BIN`, `15Victoria`, `Assistir`, etc.
    - `Music` (`path\1`)
    - `Pictures` (`path\3`)
    - `Video` (`path\2`)
  - O Shell do Windows trata perfeitamente o parsing name do servidor de mídia como uma pasta virtual navegável.
  - `ShellStorageFolder` do KyraFiles (`src/KyraFiles.App/Utils/Storage/StorageItems/ShellStorageFolder.cs`) já possui suporte a parsing names iniciados por `::{` (`IsShellPath`).
- **Risco / Caso de borda:** Se um dispositivo virtual estiver inacessível na rede ou recusar enumeração por `IShellFolder`, a aplicação deve exibir aviso informativo com botão/opção "Abrir no Explorador do Windows" (atendendo à decisão do Alexandre).

#### Reverificação da Claude (10/11/2026)
- **Estado:** `CONFIRMED`
- `Shell.Application NameSpace(17)` listou `hd-roteador`, `alxchs@gmail.com (lenovo)` e `heloizedittrich@gmail.com (heloize-pc)` como "Media Server" (`IsFileSystem=False`), com o parsing name `::{20D04FE0-...}\uuid:...`.
- `hd-roteador` abriu `Browse Folders`, `Music`, `Pictures` e `Video`.
- A listagem levou 700 ms.
- Scripts de reprodução e prova devidamente versionados no repositório:
  - `tools/perf/f092_inspect.ps1`
  - `tools/perf/f092_inspect_subitems.ps1`
  - `tools/perf/f092_test_shcreate.ps1`
  - `tools/perf/f092_sidebar_bench.ps1`

---

### 3. Baseline de Desempenho Medido (NFR-001)

Execução de `tools/perf/f092_sidebar_bench.ps1` com 5 iterações:

| Componente Medido | Média | Observações |
|---|---|---|
| `DriveInfo.GetDrives()` (.NET padrão atual) | **1.58 ms** | Retorna apenas 3 itens (`C:\`, `D:\`, `R:\`) |
| `Shell.Application NameSpace(17)` (`ssfDRIVES`) | **519.63 ms** | Retorna todos os 6 itens (incluindo `hd-roteador` e `alxchs@gmail.com`) |
| **Startup UI** | `NOT TESTED` | Preservação mandatória de foco antes das 23:59 (Regra Zero) |

**Diretriz de Performance:**  
A consulta a dispositivos de rede e itens do Shell leva ~500 ms. Por isso, ela **DEVE** continuar sendo executada estritamente em segundo plano (background thread com `Task.Run` ou `STATask`), inserindo os itens na barra lateral assincronamente à medida que são descobertos, garantindo **impacto ZERO** no tempo de renderização inicial da janela do KyraFiles.

---

### 4. Conclusão e Próximos Passos (Plano para T1–T5)

1. **T1 (Bibliotecas):** Alterar o valor padrão de `GeneralSettingsService.ShowLibrarySection` para `true` (para novas instalações e migrações padrão, respeitando desligamento intencional).
2. **T2 (Este Computador):** Adicionar `SectionType.ThisPC` na barra lateral, renderizando item navegável para `shell:MyComputerFolder`.
3. **T3 (Painel de Controle):** Adicionar `SectionType.ControlPanel` apontando para `::{26EE0668-A00A-44D7-9371-BEB064C98683}` com ícone e navegação do sistema.
4. **T4a (Listagem de Rede e Dispositivos):** Enriquecer `NetworkService` (`StorageNetworkService.cs`) e a barra lateral para listar os itens de `FOLDERID_NetHood` e servidores de mídia descobertos em segundo plano.
5. **T4b (Navegação com Fallback):** Assegurar abertura do conteúdo no KyraFiles e implementar diálogo/notificação de fallback com "Abrir no Explorador do Windows" caso um item virtual do Shell não possa ser enumerado internamente.
6. **T5 (Configurações):** Adicionar toggles para Este Computador e Painel de Controle em *Settings > Folders*.
7. **T6 (Validação Final):** Gate de recursos (`verify_no_index_service.ps1`), prova de tela e relatório `RELATORIO.md`.

*Relatório de T0 concluído. Código em `src/` intocado.*
