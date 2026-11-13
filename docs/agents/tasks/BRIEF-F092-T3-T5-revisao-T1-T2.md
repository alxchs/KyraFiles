# BRIEF F092 — revisão da T1/T2 e micro-módulos T3 e T5 (Claude → AGY)

Data: 2026-11-12. Autor: Claude (somente leitura de código; nenhum arquivo de `src/` foi alterado).
Base revisada: `feature/f092-native-sidebar-parity` @ `e69c7eb21` (T2).

## 1. Revisão da T1 e T2 (estado por item)

| Item | Estado | Evidência |
|---|---|---|
| T1: `ShowLibrarySection` default `true` | CONFIRMED (código) | `GeneralSettingsService.cs:184` `get => Get(true)` |
| T1: "preserva quem desligou de propósito" | INFERRED | Depende de `Get(default)` só devolver o default quando a chave não está gravada. **Provar** lendo `BaseObservableJsonSettings`/`JsonSettingsDatabase` (ver 2.1) |
| T2: nó `ThisPC` criado, ordem antes de Drives, ícone, toggle no VM, hide pelo menu de contexto, handler de mudança de setting | CONFIRMED (código) | `SidebarViewModel.cs` linhas ~83, 204, 598, 719, 996 |
| T2: clique abre `shell:MyComputerFolder` | INFERRED | Não há teste de navegação; só teste de texto (regex) |
| Build / prova de tela | NOT TESTED | Heloize-PC não tem Windows SDK |

### Defeitos a corrigir (cirúrgicos)
1. **`SidebarSettingsTests.AC1_..._ResolutionLogic_...` é tautológico.** Testa uma função local `ResolveSetting` escrita dentro do próprio teste; nunca chama o código real. Troque por teste que use a classe real de persistência (ou remova e deixe só a evidência de leitura do código). Teste que não pode falhar não conta como `CONFIRMED`.
2. **Testes por regex sobre o `.cs`** valem só como guarda de regressão do valor default, não como prova de comportamento. No relatório, rotule como `INFERRED`, não `CONFIRMED`.
3. **Pendência AC-1 "em atualização":** provar com o comando exato (abrir `settings/*.json` de um perfil antigo sem a chave e conferir que sobe `true`; e com `"ShowLibrarySection": false` e conferir que continua `false`). Registrar saída no handoff.

## 2. Pacotes pequenos para a AGY (ordem; um por vez, regra §22)

### [LIVRE] T3 — Painel de Controle na barra lateral
- Depende de: nada (T2 pronta). Branch: continuar `feature/f092-native-sidebar-parity`.
- Copie o molde da T2 (commit `e69c7eb21`), trocando `ThisPC` por `ControlPanel`:
  1. `INavigationControlItem.cs`: `SectionType.ControlPanel` (depois de `ThisPC`).
  2. `Constants.cs`: `ControlPanelPath = @"::{26EE0668-A00A-44D7-9371-BEB064C98683}"` (já citado no plan).
  3. `IGeneralSettingsService` + `GeneralSettingsService`: `ShowControlPanelSection`, `Get(true)`.
  4. `SidebarViewModel.cs`: `SectionOrder`, `AreSectionsHidden`, propriedade `ShowControlPanelSection`, `CreateSectionAsync` (case), `SyncSidebarItemsAsync` (case com coleção vazia), handler de `OnSettingChangedEvent`, e o `case` de "ocultar seção".
  5. `SidebarSectionIcons.cs`: ícone (reutilizar um existente; **não** criar arte nova).
  6. **Falha do Painel de Controle** (spec §5): se a navegação falhar, abrir `ms-settings:` como alternativa.
- Pronto = `grep` mostra todos os 6 pontos; teste do mesmo tipo da T2; build só na máquina antiga.
- NÃO inclui: toggles na tela de Configurações (é a T5); itens filhos de rede (T4).

### [LIVRE] T5 — toggles em Settings > Folders
- Depende de: T3 (precisa de `ShowControlPanelSection`).
- Arquivos: `FoldersPage.xaml`, o ViewModel da página, `Resources.resw` (en-US e pt-BR; **sem texto fixo na tela**).
- Pronto = dois toggles ("Este Computador" e "Painel de Controle") persistem e refletem na barra sem reiniciar.

### [LIVRE, depende de T3] T4a, T4b, T4c — como já estão em `tasks.md`.
Restrição inegociável para a T4a: enumeração em thread STA com timeout de 3 s, nunca na thread de UI (NFR-001/002).

## 3. O que NÃO fazer
- Não tocar em `GeneralSettingsService` fora das propriedades novas (conflito certo com merges).
- Não rodar teste de tela antes das 23:59 (§16). Sem foco, sem `SetForegroundWindow`.
- Não declarar `CONFIRMED` build/UI sem rodar: a Heloize-PC não compila.

## 4. F090 (branch `feature/f090-escala`, já em 1 commit sobre a main)
Só falta build e teste na máquina antiga. Passos para o Alexandre/AGY lá:
1. `git fetch && git switch feature/f090-escala`.
2. `mkfile d src\KyraFiles.App\KyraFiles.App.csproj` (esperado: `0 Error(s)`).
3. `pwsh tests\test-ui-scale-math.ps1` (lógico) e roteiro em `docs/specs/F090-escala-da-interface/roteiro-testes.md` (tela, pós-23:59).
4. `pwsh tools\perf\verify_no_index_service.ps1` (gate F021).
5. Só então: `git switch main; git merge --ff-only feature/f090-escala`, nova versão (REGRA VERSAO), relatório.

## 5. Observação sobre o checkout
Este checkout (`c:\desenv\utils\KyraFiles`) está com a AGY na branch da F092. A Claude não edita nele; só trabalha no worktree `..\KyraFiles-claude-briefs`.
