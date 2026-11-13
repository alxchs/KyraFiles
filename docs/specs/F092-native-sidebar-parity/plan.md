# F092 — Paridade da Barra Lateral com o Explorador Nativo — PLAN

Status: RASCUNHO (AGY, 10/11/2026; revisado após feedback da Claude). Submetido para aprovação técnica da Claude e do Alexandre.

---

## 1. Arquitetura Atual e Diagnóstico Técnico (CONFIRMED no Código)

1. **Itens Delegados de "Este Computador" (`DelegateFolders` / Dispositivos / Servidores de Mídia)**:
   - O diagnóstico técnico de T0 comprovou que `hd-roteador` e `alxchs@gmail.com (lenovo)` aparecem no Explorador nativo como filhos de "Este Computador" (*This PC* / `NameSpace(17)` / `ssfDRIVES`), registrados em `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\DelegateFolders`.
   - **Arquitetura (T4a):** Não misturar com `NetworkService` nem poluir `StorageDevicesService.cs`. Criar enumeração dedicada em segundo plano (thread STA própria via `STATask`, com timeout de 3 segundos), consultando os itens de `NameSpace(17)` que não são volumes de disco (`!IsFileSystem`), exibindo-os sob "Este Computador"/unidades exatamente como o Explorador nativo.
   - **Atualização da lista:** Servidores de mídia entram e saem da rede dinamicamente. A lista é atualizada na inicialização da barra lateral e em segundo plano; quando um servidor estiver fora do ar ou inacessível, o KyraFiles exibirá aviso informativo claro e a opção "Abrir no Explorador do Windows".
   - **Atalhos de Rede (T4c):** A coleção existente `NetworkService.Shortcuts` (`FOLDERID_NetHood` / `shell:NetHood`), que hoje não era ligada à interface, será vinculada como subitens da seção "Rede" (`SectionType.Network`).

2. **Seção de Bibliotecas (`SectionType.Library`)**:
   - `src/KyraFiles.App/Data/Contracts/INavigationControlItem.cs`: já define `SectionType.Library`.
   - `src/KyraFiles.App/ViewModels/UserControls/SidebarViewModel.cs`: já possui implementação completa em `BuildSection` para `SectionType.Library`, delegando para `App.LibraryManager.UpdateLibrariesAsync`.
   - `src/KyraFiles.App/Services/Settings/GeneralSettingsService.cs`: linha 184 define `ShowLibrarySection` como `get => Get(false);`. O valor padrão `false` omite a seção.
   - **Abordagem (T1):** Atualizar o valor padrão de `ShowLibrarySection` para `true` para todos os usuários (novas instalações e migrações padrão), preservando a escolha de usuários que desligaram a opção explicitamente.

3. **Item "Este Computador (This PC)"**:
   - `src/KyraFiles.App/Constants.cs`: linha 218 já define `public const string MyComputerPath = @"Shell:MyComputerFolder";`.
   - `src/KyraFiles.App/Data/Contracts/INavigationControlItem.cs`: adicionar `SectionType.ThisPC` (ou instanciar como `LocationItem` dedicado de navegação raiz).
   - `src/KyraFiles.App/ViewModels/UserControls/SidebarViewModel.cs`: adicionar em `SectionOrder` (posicionado estrategicamente antes de `Drives`), renderizando `LocationItem` navegável apontando para `Constants.UserEnvironmentPaths.MyComputerPath`.

4. **Item "Painel de Controle (Control Panel)"**:
   - `src/KyraFiles.App/Constants.cs`: adicionar constante pública `ControlPanelPath = @"::{26EE0668-A00A-44D7-9371-BEB064C98683}";`.
   - `src/KyraFiles.App/Data/Contracts/INavigationControlItem.cs`: adicionar `SectionType.ControlPanel` ao enum.
   - `src/KyraFiles.App/ViewModels/UserControls/SidebarViewModel.cs`: criar seção/item apontando para o caminho do painel com ícone de sistema.
   - `src/KyraFiles.App/Services/Settings/GeneralSettingsService.cs`: adicionar propriedades `ShowThisPCSection` e `ShowControlPanelSection` com suporte na interface de configurações (`FoldersPage.xaml`).

---

## 2. Mapa Requisito → Código

| Requisito / AC | Arquivo a Modificar | Modificação |
|---|---|---|
| **T0** (Investigação e medição) | `tools/perf/f092_inspect*.ps1`, `f092_sidebar_bench.ps1` | Diagnóstico técnico com scripts versionados de evidência e bench |
| **AC-1 (T1)** (Bibliotecas ativadas) | `src/KyraFiles.App/Services/Settings/GeneralSettingsService.cs` | Atualizar default de `ShowLibrarySection` para `true` + teste unitário |
| **AC-2 (T2)** (Este Computador na barra) | `INavigationControlItem.cs`, `SidebarViewModel.cs`, `SidebarSectionIcons.cs` | Adicionar nó `ThisPC` com navegação para `MyComputerPath` |
| **AC-4 (T3)** (Painel de Controle) | `Constants.cs`, `INavigationControlItem.cs`, `SidebarViewModel.cs`, `SidebarSectionIcons.cs` | Adicionar constante, item e navegação |
| **AC-3 (T4a)** (Dispositivos sob Este Computador) | `SidebarViewModel.cs`, novo helper STA de Shell | Enumerar itens não-filesystem de `NameSpace(17)` sob Este Computador/unidades |
| **AC-3 (T4b)** (Navegação de mídias e fallback) | `ShellStorageFolder.cs`, `NavigationHelpers.cs` | Navegar subitens virtuais e fallback para o Explorador nativo caso indisponível |
| **AC-3 (T4c)** (Atalhos de Rede na barra) | `SidebarViewModel.cs`, `StorageNetworkService.cs` | Ligar `NetworkService.Shortcuts` (`FOLDERID_NetHood`) à seção Rede |
| **AC-5 (T5)** (Configurações da barra) | `GeneralSettingsService.cs`, `FoldersPage.xaml`, `Resources.resw` (en-US, pt-BR) | Toggles de visibilidade de Este Computador e Painel de Controle |
| **Validação (T6)** (Prova de tela e Gate) | `tools/perf/verify_no_index_service.ps1`, automação pós-23:59 | Medição comparativa com a main, capturas silenciosas e gate F021 |

---

## 3. Arquivos a NÃO Mexer
- `src/KyraFiles.App.Server/*` (IPC)
- `src/KyraFiles.App/Services/Git/*` (versionamento Git)
- `src/KyraFiles.App/Services/Storage/StorageDevicesService.cs` (mantém o escopo estrito de unidades locais de disco)

---

## 4. Riscos e Mitigações
- **Risco de atraso no startup por consultas de dispositivos/rede:**  
  *Mitigação:* A enumeração de delegate folders e servidores de mídia roda em background thread STA (`STATask`), com timeout de 3 segundos. Impacto zero na thread de UI.
- **Risco de operações em itens virtuais de servidor de mídia:**  
  *Mitigação:* Arquivos dentro de servidores de mídia UPnP/DLNA são streams virtuais e não arquivos convencionais do sistema de arquivos NTFS/FAT. Operações como arrastar/soltar, copiar para o disco, geração de pré-visualizações avançadas e leitura de propriedades do Win32 podem ter limitações inerentes do Shell Provider da Microsoft. A Tarefa T4b testará e documentará explicitamente cada uma dessas interações. Caso uma operação não seja suportada pelo ShellItem virtual, haverá diálogo claro orientando o usuário a abrir no Explorador nativo.
- **Risco de baseline de abertura não ter referência:**  
  *Mitigação:* A medição de abertura da UI da build da `main` será executada de forma automatizada e silenciosa após as 23:59 (respeito estrito à Regra Zero), registrando a referência oficial antes de qualquer teste comparativo de interface na T6.
- **Risco de roubo de foco durante testes/provas de tela:**  
  *Mitigação (Regra Zero):* Zero consoles efêmeros e proibição absoluta de ativação de janelas antes das 23:59. Automações de tela exclusivamente off-screen ou pós-23:59.

---

## 5. Estratégia de Testes e Provas por AC
- **AC-1:** Teste unitário confirmando valor de `ShowLibrarySection` e prova de tela mostrando a seção no app.
- **AC-2:** Teste de navegação do `LocationItem` apontando para `MyComputerPath` e prova de tela do clique abrindo a visão de unidades.
- **AC-3:** Teste de enumeração em `NetworkService` com asserção e prova de tela com a listagem de atalhos.
- **AC-4:** Teste de comando e navegação do atalho do Painel de Controle.
- **AC-5:** Teste de persistência dos toggles de configuração em `GeneralSettingsService`.
