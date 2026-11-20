# F022 — Correção do Crash de Startup e Estabilização do Instalador — PLAN

Metodologia: Spec-Driven Development (`METODO.md` §6).

## 1. Análise Técnica e Arquitetural
- O `AppLifecycleHelper.cs` centraliza a montagem do container de injeção de dependências (`IServiceCollection`) em `ConfigureHost(AppModel appModel)`.
- Adicionar:
  ```csharp
  .AddSingleton<IHelpService, HelpService>()
  ```
  e
  ```csharp
  .AddSingleton<HelpViewModel>()
  ```
  com os respectivos namespaces (`using KyraFiles.App.Services.Help;` e `using KyraFiles.App.ViewModels.Help;`).
- No Inno Setup (`installer/KyraFilesSetup.iss`):
  - Em `CurStepChanged(ssPostInstall)`:
    Executar PowerShell que verifica se `Get-AppxPackage KyraFiles` existe; se sim, chama `Remove-AppxPackage`, e em seguida `Add-AppxPackage -Register '{app}\AppxManifest.xml' -ForceApplicationShutdown -ForceUpdateFromAnyVersion`.
  - Nos `[Icons]` e `[Run]`:
    Garantir que a ativação use o Application Execution Alias (`{localappdata}\Microsoft\WindowsApps\kyrafiles.exe`) ou acionamento via `explorer.exe shell:AppsFolder\KyraFiles_sxng2gqmgahde!App` ou launcher stub nativo, com ícone apropriado `{app}\KyraFiles.exe,0`.
- Compilar o projeto via `mkfile` e recompilar o setup via Inno Setup (`ISCC.exe`).
- Testar a instalação localmente, abrir a aplicação de verdade, verificar que a janela aparece na tela e que o `debug.log` não reporta exceções.
