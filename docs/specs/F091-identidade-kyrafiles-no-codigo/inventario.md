# Inventário Completo de Resíduos e Marcas Legadas (Backlog)

**Data do Inventário:** 2026-11-16
**Origem:** Protocolo Anti-Reincidência (INSTRUCTIONS_OPERARY.md R1)
**Verificador:** 	ools/check-legacy-names.ps1 ampliado
**Total de Ocorrências Identificadas:** 2369

Este inventário é a linha de base exata e auditável do passivo de renomeação. O trabalho da purga só é considerado concluído quando 	ools/check-legacy-names.ps1 retornar exit 0.

## 1. Resumo por Categoria de Pacote

| Categoria do Inventário | Ocorrências | Pacote de Trabalho Associado (INSTRUCTIONS_OPERARY §Ordem dos pacotes) |
| :--- | :---: | :--- |
| **Documentacao** | **1377** | Pacote 7 (.md e docs/) |
| **Codigo e Textos** | **462** | Pacote 3 (Componentes nativos) e Pacote 5 (Textos/Strings) |
| **Ferramentas e Scripts** | **243** | Pacote 4 (tools/ e scripts auxiliares) |
| **UI e Recursos** | **161** | Pacote 5 (XAML, .resw, manifesto) |
| **Estrutura de Pastas e Arquivos** | **101** | Pacote 2 (Nomes de pasta e arquivo - git mv) |
| **Instalador** | **25** | Pacote 4 (installer/ e .iss) |

---

## 2. Detalhamento por Categoria

### 2.1 Documentacao (1377 ocorrências)

Top arquivos com maior concentração de resíduos:

| Arquivo / Caminho | Ocorrências |
| :--- | :---: |
| `docs/agents/evidence/F060/m5-t0.md` | 44 |
| `docs/agents/handoffs/2026-10-28_0735_agy_f061-t2-t3.md` | 37 |
| `docs/agents/handoffs/2026-10-27_0055_agy_f060-m1-t1.md` | 31 |
| `docs/specs/F012-explorer-replacement/spec.md` | 30 |
| `docs/agents/evidence/F060/m3-t0.md` | 28 |
| `docs/specs/F012-explorer-replacement/plan.md` | 25 |
| `docs/agents/handoffs/2026-10-27_1030_agy_f060-m5-t2.md` | 24 |
| `docs/agents/handoffs/2026-10-27_0825_agy_f060-m2-t2.md` | 22 |
| `docs/agents/handoffs/2026-10-27_0850_agy_f060-m1-t4.md` | 21 |
| `docs/agents/handoffs/2026-10-27_0701_agy_f060-m1-t2.md` | 21 |
| `docs/agents/handoffs/2026-10-27_0758_agy_f060-m2-t1.md` | 20 |
| `docs/specs/F021-bloqueio-indexservice/spec.md` | 20 |
| `docs/agents/handoffs/2026-10-26_1257_agy_crashes.md` | 19 |
| `docs/agents/handoffs/2026-10-28_0710_agy_f061-t0-t1.md` | 18 |
| `docs/agents/handoffs/2026-10-27_1130_agy_f060-m2-t4.md` | 18 |

### 2.2 Codigo e Textos (462 ocorrências)

Top arquivos com maior concentração de resíduos:

| Arquivo / Caminho | Ocorrências |
| :--- | :---: |
| `STATUS.md` | 39 |
| `INSTRUCTIONS_OPERARY.md` | 22 |
| `DECISIONS.md` | 22 |
| `MASTER_SPEC.md` | 19 |
| `src/KyraFiles.App/ViewModels/Settings/AdvancedViewModel.cs` | 10 |
| `AGENTS.md` | 7 |
| `src/KyraFiles.App/App.xaml.cs` | 6 |
| `src/KyraFiles.App/Program.cs` | 6 |
| `src/KyraFiles.App/KyraFiles.App.csproj` | 5 |
| `.github/workflows/ci.yml` | 5 |
| `.gitignore` | 5 |
| `src/KyraFiles.App.Launcher/KyraFilesLauncher.cpp` | 4 |
| `src/KyraFiles.App/Docs/en-US/getting-started/overview.html` | 4 |
| `src/KyraFiles.App/Docs/features/layout.html` | 4 |
| `src/KyraFiles.App/Docs/getting-started/overview.html` | 4 |

### 2.3 Ferramentas e Scripts (243 ocorrências)

Top arquivos com maior concentração de resíduos:

| Arquivo / Caminho | Ocorrências |
| :--- | :---: |
| `tools/installer/Program.cs` | 23 |
| `tools/Create-SelfContained-Release.ps1` | 13 |
| `tools/perf/f017_perf_audit.ps1` | 12 |
| `tools/perf/_desativados/f017_win_e_test.ps1.desativado` | 11 |
| `tools/perf/verify_no_index_service.ps1` | 10 |
| `tools/perf/_desativados/f017_installer_test.ps1.desativado` | 10 |
| `tools/perf/verify_win_e_health.ps1` | 9 |
| `tools/Deploy-Release.ps1` | 6 |
| `tools/perf/README.md` | 6 |
| `tools/perf/f017_test_persistence.ps1` | 5 |
| `tools/perf/f031_tabs.ps1` | 5 |
| `tools/update_resw_kyrafiles.ps1` | 5 |
| `tools/perf/test_f035_activation.ps1` | 5 |
| `tools/perf/f015_latency.ps1` | 5 |
| `tools/perf/run_crash_a_test.ps1` | 4 |

### 2.4 UI e Recursos (161 ocorrências)

Top arquivos com maior concentração de resíduos:

| Arquivo / Caminho | Ocorrências |
| :--- | :---: |
| `src/KyraFiles.App/App.xaml` | 6 |
| `src/KyraFiles.App.Controls/BreadcrumbBar/BreadcrumbBar.xaml` | 3 |
| `src/KyraFiles.App/UserControls/NavigationToolbar.xaml` | 3 |
| `src/KyraFiles.App/Views/Settings/AppearancePage.xaml` | 3 |
| `src/KyraFiles.App/Dialogs/CloneRepoDialog.xaml` | 2 |
| `src/KyraFiles.App/UserControls/StatusBar.xaml` | 1 |
| `src/KyraFiles.App/UserControls/StatusCenter/StatusCenter.xaml` | 1 |
| `src/KyraFiles.App/UserControls/TabBar/TabBar.xaml` | 1 |
| `src/KyraFiles.App/UserControls/Toolbar.xaml` | 1 |
| `src/KyraFiles.App/UserControls/Widgets/DrivesWidget.xaml` | 1 |
| `src/KyraFiles.App/UserControls/Widgets/NetworkLocationsWidget.xaml` | 1 |
| `src/KyraFiles.App/UserControls/Widgets/QuickAccessWidget.xaml` | 1 |
| `src/KyraFiles.App/UserControls/Widgets/RecentFilesWidget.xaml` | 1 |
| `src/KyraFiles.App/Views/HexEditor/HexEditorPage.xaml` | 1 |
| `src/KyraFiles.App/Views/HomePage.xaml` | 1 |

### 2.5 Estrutura de Pastas e Arquivos (101 ocorrências)

Top arquivos com maior concentração de resíduos:

| Arquivo / Caminho | Ocorrências |
| :--- | :---: |
| `docs/agents/evidence/f002/f002_filesuxlab_open.png` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/Transfer/BaseTransferItemAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/Transfer/BaseCopyMoveToFolderAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/SplitFileAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/RestoreRecycleBinAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/RestoreAllRecycleBinAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/RenameAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/PasteItemToSelectionAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/PasteItemAsShortcutAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/PasteItemAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/OpenItemAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/OpenFileLocationAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/FormatDriveFromSidebarAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/FormatDriveFromHomeAction.cs` | 1 |
| `src/KyraFiles.App/Actions/FileSystem/FormatDriveAction.cs` | 1 |

### 2.6 Instalador (25 ocorrências)

Top arquivos com maior concentração de resíduos:

| Arquivo / Caminho | Ocorrências |
| :--- | :---: |
| `installer/KyraFilesSetup.iss` | 11 |
| `installer/Update-KyraFiles.ps1` | 9 |
| `installer/package_manifest.json` | 5 |

---

## 3. Estratégia de Execução dos Próximos Pacotes

Seguindo estritamente a sequência de pacotes coesos (INSTRUCTIONS_OPERARY.md §Ordem dos pacotes):
1. **Pacote 1 (Concluído neste commit):** Verificador ampliado + allowlist autorizada + inventário completo.
2. **Pacote 2:** Renomeação de pastas e arquivos no git (git mv) para os 101 caminhos identificados.
3. **Pacote 3:** Componentes nativos C++ (OpenDialog, SaveDialog, Launcher: .idl, .h, .rgs, .vcxproj) e workflows .github/.
4. **Pacote 4:** installer/ e 	ools/ (incluindo Program.cs, scripts de deploy e benchmarks).
5. **Pacote 5:** Textos de UI, runtime, .resw, comentários de cabeçalho e tela Sobre.
6. **Pacote 6:** Limpeza de código de migração e fallbacks legados.
7. **Pacote 7:** Documentação viva (docs/, .md).
8. **Pacote 8:** Validação final e auditoria independente pelo Claude.
