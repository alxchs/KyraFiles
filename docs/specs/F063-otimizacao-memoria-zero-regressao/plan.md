# F063 — Otimização de Consumo de Memória sem Degradação de Latência (P0) — PLAN

---

## 1. Arquitetura Atual e Diagnóstico

### Diagnóstico no Código
1. `src/KyraFiles.App.CsWin32/NativeMethods.txt` já inclui a declaração da API Win32:
   `K32EmptyWorkingSet` (linha 407). O CsWin32 gera a assinatura:
   `Windows.Win32.PInvoke.K32EmptyWorkingSet(Windows.Win32.Foundation.HANDLE hProcess)`.
2. Em `src/KyraFiles.App/App.xaml.cs`:
   - O método `TryEmptyWorkingSetWhenIdle` aceita `Windows.Win32.Foundation.HANDLE processHandle`, porém o método local `AggressiveGC` simplesmente ignora o handle e não chama `K32EmptyWorkingSet`!
   - Além disso, `TryEmptyWorkingSetWhenIdle` só é chamado nos métodos de finalização e fechamento para bandeja (`App.xaml.cs:234` e `417`), nunca durante o ciclo de vida normal com janela minimizada ou ociosa.
3. No Windows 10/11, a chamada de `K32EmptyWorkingSet` remove as páginas não essenciais do Working Set do processo e as move para a *Standby List* do gerenciador de memória do sistema operacional (na própria RAM). Como as páginas permanecem na RAM enquanto não houver pressão extrema de memória global no SO, o retorno à tela é atendido por **soft page faults** na RAM física, sem I/O de disco, ocorrendo em microssegundos (latência praticamente zero).

---

## 2. Abordagem Escolhida

Criar um serviço desacoplado de gestão de memória ou estender o ciclo de vida da janela:
1. **Helper / Serviço `MemoryTrimHelper`**:
   - `TrimWorkingSet(bool aggressiveGc = false)`: Executa em thread separada (`ThreadPool` / `Task.Run` com prioridade baixa).
   - Se houver operações de arquivo em andamento (`FileOperationsHelpers`), adia o trim.
   - Invoca `GC.Collect(2, GCCollectionMode.Optimized)` seguido de `PInvoke.K32EmptyWorkingSet(processHandle)`.
2. **Gatilhos de Acionamento**:
   - **Minimização de Janela:** `MainWindow` intercepta a mudança de estado da janela (`AppWindow.Changed` onde `DidPresenterChange` ou `Presenter.State == OverlappedPresenterState.Minimized`). Dispara com debounce de 2 segundos.
   - **Ociosidade Prolongada em Background:** Timer leve que verifica se a janela não tem foco há mais de 60 segundos.
   - **Restauração:** Não faz nada no caminho crítico de restauração! A UI restaura imediatamente via pipeline nativo do WinUI 3.
3. **Salvaguarda de Equilíbrio e Performance (Zero Lentidão)**:
   - Medição comparativa de latência antes e depois da implementação usando script em PowerShell (`tools/perf/measure_window_restore_latency.ps1`).
   - Se a restauração apresentar qualquer atraso > 50 ms, a compactação de LOH é desativada e mantido apenas o soft trim.

---

## 3. Arquivos a Modificar e a Não Modificar

### Modificar:
- `src/KyraFiles.App/Helpers/MemoryTrimHelper.cs` (novo helper especializado e seguro)
- `src/KyraFiles.App/App.xaml.cs` (corrigir `TryEmptyWorkingSetWhenIdle` para chamar `K32EmptyWorkingSet` e conectar o hook de idle)
- `src/KyraFiles.App/Views/MainWindow.xaml.cs` (adicionar escuta a evento de minimização para acionar o trim suave)
- `tools/perf/measure_memory_and_latency.ps1` (script de teste de baseline e verificação de latência de restauração)

### NÃO Modificar:
- `src/KyraFiles.App.Server` (permanece isolado)
- `src/KyraFiles.App.IndexService` (permanece rigorosamente desarmado conforme Gate F021)
- Lógica interna de virtualização de itens das pastas (`FolderViewModel`, `BaseLayoutPage`)

---

## 4. Estratégia de Teste e Roteiro

1. **Roteiro de Teste 1 (Baseline vs Trim):**
   - Abrir o KyraFiles instalado (v4.3.8).
   - Medir Working Set em uso normal (abrir pastas, navegar).
   - Minimizar a janela por 5 segundos.
   - Medir Working Set após minimização.
2. **Roteiro de Teste 2 (Latência de Restauração):**
   - Automatizar a minimização e restauração da janela via UI Automation ou Win32 `ShowWindow(SW_RESTORE)`.
   - Medir em milissegundos o tempo decorrido entre o comando de restauração e a janela estar visível e responsiva.
   - Meta mandatória: Tempo de restauração < 50 ms.
3. **Roteiro de Teste 3 (Ecosystem Memory Card F062):**
   - Abrir Configurações > Sobre > Consumo de Memória e validar os números atualizados.
