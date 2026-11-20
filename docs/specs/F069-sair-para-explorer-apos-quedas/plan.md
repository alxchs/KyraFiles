# F069 — Sair para o Explorer após quedas seguidas — PLAN

Status: **APROVADO PARA EXECUÇÃO**  
Metodologia: Spec-Driven Development (`METODO.md` §6)

---

## 1. Arquitetura e Componentes

### 1.1 `CrashGuardService` (`KyraFiles.Shared/Helpers/CrashGuardService.cs` e `KyraFiles.App/Helpers/Application/CrashGuardService.cs`)
Responsável pelo monitoramento do ciclo de vida, contagem de quedas com janela deslizante de 60 segundos e orquestração de fallback:
- **Arquivo de Estado:** `%LOCALAPPDATA%\Files\crash_guard.json`.
- **Arquivo de Log:** `%LOCALAPPDATA%\Files\crash_guard.log`.
- **Janela de Detecção:** 60 segundos.
- **Limiar de Quedas:** 2 eventos no intervalo.
- **Critério de Estabilidade:** 10 segundos de execução contínua com janela ativa sem exceções fatais.

### 1.2 Formato do Estado (`CrashGuardState`)
```json
{
  "RecentCrashes": [
    "2026-10-31T19:30:10.1234567Z",
    "2026-10-31T19:30:45.9876543Z"
  ],
  "FallbackTriggered": true,
  "PromptOnNextManualLaunch": true,
  "LastCrashReason": "Process terminated abnormally in startup within 3s",
  "LastExitCode": -1073741819
}
```

### 1.3 Fluxo de Execução
1. **Startup (`Program.cs`):**
   - Ao iniciar, `CrashGuardService.RecordStart()` registra o início da sessão.
   - Registra manipuladores de `AppDomain.CurrentDomain.UnhandledException` e `TaskScheduler.UnobservedTaskException`.
   - Verifica `CrashGuardService.ShouldFallbackToExplorer()`. Se for uma chamada de abertura de pasta com fallback ativado, invoca `explorer.exe` no caminho recebido e encerra silenciosamente o processo do KyraFiles sem travar o usuário.
2. **Pós-Ativação Estável (`App.xaml.cs` / `MainWindow.xaml.cs`):**
   - Dispara um timer/delay de 10 segundos. Se o app permanecer vivo, chama `CrashGuardService.RecordStableState()`, que limpa a contagem de quedas.
   - Se `PromptOnNextManualLaunch` estiver ativo, exibe um diálogo não intrusivo informando que o Explorer foi usado anteriormente e que o KyraFiles foi restaurado com sucesso.
3. **Lançador C++ (`src/KyraFiles.App.Launcher/KyraFilesLauncher.cpp`):**
   - Lê o arquivo `crash_guard.json` ao ser acionado. Se `FallbackTriggered` estiver marcado como verdadeiro e houver 2 quedas recentes em 60s, delega diretamente para `RunFileExplorer(openDirectory)`.

---

## 2. Estratégia de Testes

Criar `tests/KyraFiles.App.Search.Index.Tests/CrashGuardServiceTests.cs`:
- `Test_SingleCrash_DoesNotTriggerFallback`: 1 queda isolada não deve acionar fallback.
- `Test_TwoCrashesWithin60Seconds_TriggersFallback`: 2 quedas em 30s devem acionar `ShouldFallback = true`.
- `Test_CrashesOlderThan60Seconds_ArePruned`: quedas de mais de 60s atrás são descartadas.
- `Test_StableExecution_ClearsCrashHistory`: marcar execução estável limpa as quedas anteriores.
- `Test_StatePersistence_Roundtrip`: persistência no arquivo JSON e recuperação íntegra.

---

## 3. Desempenho e Recursos
- Zero impacto no tempo de inicialização (leitura e gravação de um JSON minúsculo < 1 KB ou assíncrono).
- Nenhuma thread em background permanente; apenas um timer one-shot de 10 segundos para validação de estabilidade.
- Contenção de recursos conforme F021 preservada.

