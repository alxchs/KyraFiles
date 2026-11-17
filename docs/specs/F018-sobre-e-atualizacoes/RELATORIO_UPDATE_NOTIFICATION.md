# Relatório de Diagnóstico e Correção — Notificação de Atualização no App Desatualizado

**Data:** 14/11/2026  
**Responsável:** AGY  
**Branch:** `feature/f018-update-notification`  
**Escopo:** Item 3 da priorização ("Não tem aparecido a notificação no aplicativo desatualizado").

---

## 1. Causa Raiz Mapeada

Identificamos duas causas raízes complementares que impediam a notificação de atualizações no aplicativo desatualizado:

1. **Injeção de Dependência Desativada por Falta de Substituição de Placeholder de CI/CD (`AppLifecycleHelper.cs`):**
   - O método `AppLifecycleHelper.AppEnvironment` lia o placeholder `"cd_app_env_placeholder"` (que só era substituído em pipelines do GitHub Actions da KyraFiles Community).
   - Como o binário local/instalado não tinha o placeholder substituído, a função fazia fallback para `AppEnvironment.Dev`.
   - Na configuração de injeção de dependência (`ConfigureServices`), para `AppEnvironment.Dev`, o sistema registrava `DummyUpdateService`, um serviço "fantasma" cuja propriedade `IsUpdateAvailable` retornava sempre `false` estático e `CheckForUpdatesAsync()` não realizava qualquer chamada de rede.
   - Consequentemente, o serviço real `SideloadUpdateService` nunca era instanciado nem executado.

2. **Dicionário Incompatível e Exceção em Modo Sideload (`SideloadUpdateService.cs`):**
   - `_sideloadVersion` continha apenas chaves `"Files"` e `"FilesPreview"`, mapeadas para URLs da KyraFiles Community (`.appinstaller`).
   - O aplicativo com a identidade nova (`KyraFilesDev` ou `KyraFiles`) disparava `KeyNotFoundException` ao tentar buscar a URL do instalador.

---

## 2. Solução Implementada

1. **Ativação Padrão do Ambiente Sideload Oficial (`AppLifecycleHelper.cs`):**
   - Ajustado o fallback de `AppEnvironment` para `AppEnvironment.SideloadStable`.
   - Com isso, a injeção de dependência registra formalmente `SideloadUpdateService`, ativando a rotina de checagem em segundo plano no startup (`CheckAppUpdate`) e a cada ciclo periódico de 5 horas (`PeriodicallyCheckForUpdatesAsync`).

2. **Integração com Releases Oficiais (`SideloadUpdateService.cs`):**
   - Ao detectar atualização via `ReleaseCheckService.CheckAsync()`, a flag `IsUpdateAvailable = true` é despachada na UI thread, acionando o botão e notificação na Toolbar (`NavigationToolbar.xaml`).
   - O clique em "Atualizar" agora abre diretamente a página oficial do release para download do instalador atualizado `KyraFilesSetup-*.exe` via `Windows.System.Launcher.LaunchUriAsync()`.
   - Adicionada salvaguarda em `ApplyPackageUpdateAsync` prevenindo falhas de dicionário.
