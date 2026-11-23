# PEDIDO DE AUDITORIA: AGY -> CLAUDE (Homologação de 15 SPECs e Fix de Startup v4.3.31)

**Data/Hora:** 2026-11-22 21:20  
**De:** Antigravity (AGY)  
**Para:** Claude (Arquiteta) e Alexandre  
**Branch:** `main` (commit mais recente: `c204899c7`)  
**Tag/Release em Produção:** `v4.3.31.0`  

---

### 1. Contexto e Motivação do Pedido

A pedido do Alexandre, submeto à sua rigorosa auditoria técnica de arquitetura e conformidade documental:
1. A correção definitiva do encerramento silencioso e ciclo de instâncias no startup do KyraFiles (liberado na versão oficial **v4.3.31**).
2. A homologação, fechamento de tarefas e documentação de aceite (`RELATORIO.md`) de **15 SPECs** que estavam com código entregue e integrado, mas permaneciam com estado `in_validation` (`aguardando testes`) no controle de produção v2.

---

### 2. O Que Foi Corrigido no Startup (v4.3.31 / Commit `465363f9e`)

- **Causa Raiz Identificada:** Em `src/KyraFiles.App/Program.cs`, o construtor estático `static Program()` herdava do upstream Files um semáforo nomeado `$"Files-{AppLifecycleHelper.AppEnvironment}-Instance"`. Em corridas, inicializações subsequentes ou colisões de chave, ele tentava redirecionar e executava `Environment.Exit(0)` **dentro do construtor estático antes mesmo de `Main()` ser chamado**. Além disso, o redirecionamento lia `INSTANCE_ACTIVE` sem verificar se o PID gravado correspondia a um processo real e vivo do sistema operacional. Se o processo anterior morresse abruptamente, novas instâncias tentavam falar com um PID inexistente e encerravam silenciosamente.
- **Solução Aplicada:**
  1. Remoção do bloco de `Environment.Exit(0)` do construtor estático. O controle de instância única foi unificado e delegado com exclusividade ao `TryBecomeMainInstance()` (Mutex de sessão F086, que trata nativamente `AbandonedMutexException`).
  2. Implementação do método `IsInstanceProcessAlive(int pid)`, validando a existência via `Process.GetProcessById`, se o processo não encerrou e se o nome do executável é `KyraFiles` / `Files`. Se estiver morto, a chave `INSTANCE_ACTIVE` é expurgada do `LocalSettings` e o processo assume como principal.
- **Validação:** Compilação Release x64 aprovada (0 erros), Gate F021 aprovado (`IndexService` inativo), instalador `KyraFilesSetup-4.3.31.exe` gerado, deploy local validado e publicado em produção no GitHub.

---

### 3. As 15 SPECs Homologadas (55 Pontos Totais)

Todas as 15 SPECs abaixo já possuíam código-fonte implementado e integrado na árvore da `main`. Foram confeccionados seus respectivos arquivos `RELATORIO.md`, fechadas as tarefas pendentes em `tasks.md` e registradas as evidências físicas:

| SPEC | Legado | Nome | Pts | Arquivos / Evidências Auditadas |
|---|---|---|--:|---|
| **SPEC-0003** | F004 | classic menu bar | 3 | `ClassicMenuBar.xaml`, `f004_*.png` em `evidence/f004/` |
| **SPEC-0004** | F005 | preview details toggle | 2 | `TogglePreviewPaneAction`, `evidence/f005/` (Alt+P/Alt+Shift+P) |
| **SPEC-0009** | F011 | search engine choice | 3 | `SearchProviderFactory.cs`, `spec.md`, `plan.md`, `tasks.md` |
| **SPEC-0012** | F014 | sistema help contextual | 5 | `HelpService.cs`, `HelpPane.xaml`, `evidence/f014/` |
| **SPEC-0014** | F017 | win e persistent launcher | 5 | `KyraFiles.HotkeyService.exe`, `KyraFiles.Watchdog.exe` |
| **SPEC-0017** | F020 | aba unica por endereco e posicao settings | 2 | `NavigationHelpers.cs`, 16/16 testes lógicos aprovados |
| **SPEC-0019** | F022 | correcao startup e instalador | 3 | `installer/KyraFilesSetup.iss`, DI de Help |
| **SPEC-0020** | F023 | hybrid fast search | 8 | `EverythingSearchProvider.cs`, `evidence/F023/` (estresse 15 ciclos) |
| **SPEC-0022** | F026 | selecao coalescida | 5 | `BaseLayoutPage.cs`, `evidence/F026/` (+79% vazão de teclas) |
| **SPEC-0025** | F029 | setas como explorer | 3 | `DetailsLayoutPage.xaml.cs`, `evidence/F029/` (throttling 1 frame) |
| **SPEC-0026** | F030 | preview fechado tecla segurada | 3 | `AppModel.IsNavigationKeyHeld`, `evidence/F030/` |
| **SPEC-0028** | F032 | fixar aba | 3 | `IsPinned`, `evidence/F032/` |
| **SPEC-0029** | F033 | copiar mover para | 3 | `FilesystemHelpers`, `evidence/F033/` (default "Não") |
| **SPEC-0030** | F034 | lista sem checkbox | 2 | `x:Load="False"`, `evidence/F034/` |
| **SPEC-0031** | F035 | second instance | 5 | `Program.cs`, `evidence/F035/`, unificado com F086 e v4.3.31 |

---

### 4. Estado da Produção Pós-Homologação

- **Pontos Concluídos:** 266 / 353 pontos (**75% concluído**).
- **SPECs Concluídas:** 63 SPECs.
- **SPECs Restantes em Validação (`in_validation`):** 9 SPECs (F060, F061, F062, F063, F065, F070, F086, F089, F090).
- **SPECs em Fila (`not_started`):** 5 SPECs (F039, F041, F046, F050, F054).
- **Governança:** `production-control.json` e `production-events.jsonl` atualizados e validados com `Validar-Producao.ps1` (status **OK**).

---

### 5. O Que Pedimos à Claude

1. Auditar os arquivos `RELATORIO.md` gerados em `docs/specs/` para as 15 SPECs e confirmar se os critérios de aceite e referências batem com a verdade do código e das evidências salvas.
2. Auditar a alteração do ciclo de vida em `src/KyraFiles.App/Program.cs` quanto à concorrência, robustez e ausência de efeitos colaterais.
3. Se concordar, emitir o parecer em `docs/agents/handoffs/` e autorizar o avanço sobre as 9 SPECs restantes de `in_validation` ou sobre a fila de novas specs.
