# F022 — Correção do Crash de Startup e Estabilização do Instalador — TASKS

Metodologia: Spec-Driven Development (`METODO.md` §6).

- [x] **T1 — Registro de Serviços no DI (`AppLifecycleHelper.cs`):**
  - Adicionar `using KyraFiles.App.ViewModels.Help;` no topo.
  - Registrar `.AddSingleton<IHelpService, HelpService>()` na seção de serviços.
  - Registrar `.AddSingleton<HelpViewModel>()` na seção de ViewModels.

- [x] **T2 — Atualização do Instalador Inno Setup (`installer/KyraFilesSetup.iss`):**
  - Ajustar o comando de registro no `ssPostInstall` para desinstalar instâncias prévias antes de registrar o `{app}\AppxManifest.xml`.
  - Configurar atalhos em `[Icons]` e ação de execução em `[Run]` para invocar o alias registrado `{localappdata}\Microsoft\WindowsApps\kyrafiles.exe` com o ícone de `{app}\KyraFiles.exe`.

- [x] **T3 — Build e Geração do Instalador:**
  - Compilar `KyraFiles.App` em Release x64 via `mkfile r src\KyraFiles.App\KyraFiles.App.csproj`.
  - Atualizar o layout AppX via `Open-KyraFiles.ps1 -NoLaunch`.
  - Compilar o instalador `KyraFilesSetup-4.2.37.exe` via `ISCC.exe`.

- [x] **T4 — Teste Físico e Comprovação Visual:**
  - Instalar o executável do setup gerado.
  - Abrir o KyraFiles pelo atalho e confirmar que a janela aparece na tela e permanece viva por mais de 10 segundos sem fechar.
  - Verificar `debug.log` para confirmar ausência de exceções.
  - Rodar o portão obrigatório de verificação de ausência do `IndexService` (`tools/verify_no_index_service.ps1`).
