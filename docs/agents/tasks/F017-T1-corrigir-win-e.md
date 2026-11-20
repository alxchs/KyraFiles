> CUMPRIDO em 12/10/2026 pelo Claude (AGY sem cota). Evidência em `STATUS.md` (bloco F017). Mecanismo e URI finais: `DECISIONS.md` D-021 + adendo. Falta só o teste com o teclado físico.

# F017 T1 (correção) — Win+E ainda abre o Windows Explorer

Dono: AGY. Aprovado por: Claude, 12/10/2026. Decisão: `DECISIONS.md` D-021. Spec/FR/AC: `docs/specs/F017-*/spec.md` (FR-001..003, AC-1, AC-2) — comportamento não muda, só o mecanismo.

## Evidência do defeito (12/10/2026, CONFIRMED)
- `RegisterHotKey(MOD_WIN,'E')` → `false`, erro 1409. Win+Y registra.
- Serviço rodando + Win+E simulado (`keybd_event`): janela nova do Explorer ("This PC"), Files não abriu.
- `Start-Process "kyrafiles-dev:?tab=Home"` abre o KyraFiles (o protocolo está ok).
- Serviço nunca é iniciado: sem entrada em `HKCU\...\Run`, instalador nunca executado.

## Fazer
1. `src/KyraFiles.HotkeyService/main.cpp`: trocar `RegisterHotKey` por `SetWindowsHookExW(WH_KEYBOARD_LL)`.
   - Rastrear Win (VK_LWIN/VK_RWIN) e Ctrl/Shift/Alt pressionados. Win+E sem outros modificadores, `keydown` do E: **engolir** (retornar 1) e `PostMessageW` para a janela de mensagens (o callback não pode demorar: sem `ShellExecute` dentro dele).
   - Engolir também o `keyup` do E correspondente.
   - Evitar o menu Iniciar ao soltar o Win depois do E engolido: injetar uma tecla neutra (VK 0xE8 down/up via `SendInput`, marcada com `dwExtraInfo` próprio para o hook ignorar) antes do `keyup` do Win.
   - Ignorar eventos injetados por nós (`LLKHF_INJECTED` + `dwExtraInfo`).
   - No handler da mensagem: `ShellExecuteW(kyrafiles-dev:?tab=Home)` como hoje.
   - Remover o `RegisterHotKey` e o fallback silencioso. Falha de `SetWindowsHookExW` deve gravar uma linha em `%LOCALAPPDATA%\Programs\KyraFiles\hotkey.log` e sair com código ≠ 0.
2. O Watchdog continua supervisionando; não alterar o contrato (evento `Global\KyraFiles_ShutdownHotkeyService`).
3. Garantir que alguém **inicia** o serviço: o instalador (T3) já deve gravar `HKCU\...\Run`; confirmar no `.iss` e, para teste local, criar `tools/perf/f017_win_e_test.ps1` (usar `%TEMP%\claude\desenv\utils\KyraFiles\`) que: inicia `bin\Release\KyraFiles.HotkeyService.exe`, conta janelas do Explorer (`Shell.Application`), envia Win+E, espera 8 s e imprime `Files rodando` e `janelas Explorer antes/depois`.

## Pronto quando (cada item com comando e saída em `STATUS.md`)
- Script acima: `Files rodando = True` e contagem de janelas do Explorer **igual** antes/depois.
- Com o KyraFiles já aberto, Win+E o traz ao foco (sem segunda janela do Explorer).
- `taskkill /F /IM KyraFiles.HotkeyService.exe` → reaparece em ≤ 1 s (T2, medir) e Win+E volta a funcionar.
- Encerrar o serviço (evento de shutdown) → Win+E volta a abrir o Explorer imediatamente.
- Build do vcxproj sem avisos; binário < 150 KB.
- Teste com teclado real do Alexandre antes de marcar `CONFIRMED` (simulação por `keybd_event` não prova o físico).
