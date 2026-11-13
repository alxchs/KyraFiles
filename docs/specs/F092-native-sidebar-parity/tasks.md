# F092 — Paridade da Barra Lateral com o Explorador Nativo — TASKS

Status: EM APROVAÇÃO (AGY, 10/11/2026; clarifications resolvidas pelo Alexandre). Branch de trabalho: `feature/f092-native-sidebar-parity`.

Cada tarefa é pequena (um commit coeso), cita o que atende e termina em algo verificável.

| # | Tarefa | Atende | Pronto quando | Estado |
|---|---|---|---|---|
| **T0** | Investigação e medição de baseline: medir tempo de abertura atual com `f092_sidebar_bench.ps1` e investigar com evidência técnica por que `hd-roteador` e servidor de mídia não aparecem e se o KyraFiles navega em itens virtuais | NFR-001, AC-3 | Baseline de abertura medido e registrado; diagnóstico técnico documentado em `T0-diagnostico.md` com asserções (CONFIRMED/INFERRED) | concluído |
| **T1** | Ativar Bibliotecas (`SectionType.Library`) por padrão para todos os usuários em `GeneralSettingsService.cs`, atualizar toggles de visibilidade e testes unitários | US3, AC-1, FR-004 | `GeneralSettingsService.ShowLibrarySection` default é `true` para todos os usuários; teste unitário confirmando o valor; AC-1 verificado | concluído |
| **T2** | Implementar o nó navegável "Este Computador (This PC)" na barra lateral (`SidebarViewModel.cs`, `SectionType.ThisPC`, caminhos e ícones) | US1, AC-2, FR-001 | Item "Este Computador" aparece na barra lateral, abre `shell:MyComputerFolder` ao clicar; teste unitário aprovado | concluído |
| **T3** | Implementar o nó do "Painel de Controle" na barra lateral (`SectionType.ControlPanel`, `Constants.cs`, navegação para pasta shell do Control Panel) | US4, AC-4, FR-003 | Item "Painel de Controle" presente e funcional na barra lateral com ícone adequado; teste de navegação aprovado | concluído |
| **T4a** | Listar itens delegados de "Este Computador" (servidores de mídia DLNA e dispositivos portáteis) sob "Este Computador"/unidades via enumeração Shell em segundo plano (thread STA, sem travar UI) | US2, AC-3, FR-002, NFR-001 | Dispositivos e servidores de mídia (ex: `hd-roteador`, `alxchs@gmail.com`) listados sob Este Computador/unidades sem atrasar a inicialização | concluído |
| **T4b** | Abrir e navegar o conteúdo dos itens virtuais de mídia, testando arrastar, copiar, pré-visualizar e propriedades, com fallback claro ("Abrir no Explorador do Windows") para operações não suportadas | US2, AC-3, FR-002 | Navegação funcional nas subpastas virtuais ou aviso informativo oferecendo abertura no Explorador nativo | concluído |
| **T4c** | Ligar a coleção `NetworkService.Shortcuts` (`FOLDERID_NetHood` / `shell:NetHood`) como itens navegáveis da seção "Rede" na barra lateral | US2, AC-3, FR-002 | Atalhos de rede do usuário exibidos na seção Rede da barra lateral | concluído |
| **T5** | Implementar opções de configuração para exibir/ocultar "Este Computador" e "Painel de Controle" em *Settings > Folders* | US1, US4, AC-5, FR-005 | Toggles `ShowThisPCSection` e `ShowControlPanelSection` funcionais na interface de configurações e persistidos | concluído |
| **T6** | Validação de performance, prova de tela por UI Automation (AC-1 a 4, sem roubo de foco, pós-23:59), compilação Release e confecção de `RELATORIO.md` | AC-1, AC-2, AC-3, AC-4, AC-5, NFR-001, NFR-002 | `verify_no_index_service.ps1` aprovado com CPU < 5%; medição comparativa pós-implementação; capturas comprobatórias e relatório formal | concluído |

---

## Estratégia de Execução e Regras Mandatórias

1. **Autonomia e Branch Segregada:**
   - Trabalho realizado exclusivamente na branch `feature/f092-native-sidebar-parity` criada a partir da `main` sincronizada.
   - Proibido qualquer commit direto na `main`.
2. **Preservação do Foco (Regra Zero):**
   - Zero consoles efêmeros ou janelas interativas que disputem ou roubem o foco de digitação do Alexandre.
   - Provas de tela executadas de forma isolada e silenciosa por automação sem foco.
3. **Entrega e Integração:**
   - Integração na `main` exclusivamente via squash/rebase + ff-only com relatório `RELATORIO.md`.
