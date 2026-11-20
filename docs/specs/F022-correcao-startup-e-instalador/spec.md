# F022 — Correção do Crash de Startup e Estabilização do Instalador

## Visão Geral e Contexto do Incidente
Após a instalação do pacote `KyraFilesSetup-4.2.37.exe`, o executável `KyraFiles.exe` abria, nenhuma janela aparecia e, após aproximadamente 10 segundos, o processo encerrava sozinho.

A inspeção detalhada do Windows Event Viewer e do arquivo `debug.log` revelou duas causas-raiz determinantes:
1. **Injeção de Dependências Incompleta (`debug.log`):**
   `OpenHelpAction` e `MainPageViewModel` invocam `Ioc.Default.GetRequiredService<IHelpService>()` e `Ioc.Default.GetRequiredService<HelpViewModel>()`, mas nenhum dos dois estava registrado em `AppLifecycleHelper.ConfigureHost()`. Isso causava uma `InvalidOperationException: The requested service type was not registered.` não observada, derrubando o processo na thread finalizadora.
2. **Registro e Ativação MSIX no Instalador Inno Setup (`KyraFilesSetup.iss`):**
   Quando uma versão prévia de desenvolvimento do pacote `KyraFiles` já existe no Windows (ex: apontando para `C:\desenv\...`), o comando `Add-AppxPackage -Register` falha silenciosamente se o pacote antigo não for desinstalado antes. Além disso, executar `KyraFiles.exe` solto diretamente fora da ativação por pacote MSIX causa falha COM `REGDB_E_CLASSNOTREG` no `WindowsAppSDK DeploymentManager`. Os atalhos e a inicialização pós-instalação devem usar a ativação adequada de pacote via `kyrafiles.exe` ou `shell:AppsFolder\KyraFiles_sxng2gqmgahde!App`.

## Requisitos Funcionais (FR)
- **FR-001 (DI de Help):** `IHelpService` (implementado por `HelpService`) e `HelpViewModel` DEVEM ser registrados como Singletons em `AppLifecycleHelper.ConfigureHost()`.
- **FR-002 (Robustez de Registro no Inno Setup):** O script `KyraFilesSetup.iss` DEVE remover qualquer pacote `KyraFiles` pré-existente antes de registrar o novo manifesto do `{app}`, garantindo que o `InstallLocation` reflita `%LOCALAPPDATA%\Programs\KyraFiles`.
- **FR-003 (Atalhos e Ativação Confiáveis):** O instalador DEVE configurar a abertura e atalhos de forma compatível com a identidade de pacote WinUI 3 (via alias `%LOCALAPPDATA%\Microsoft\WindowsApps\kyrafiles.exe` ou `shell:AppsFolder\KyraFiles_sxng2gqmgahde!App` ou launcher nativo).
- **FR-004 (Verificação Visual de Janela):** É obrigatório verificar fisicamente a abertura da janela do KyraFiles na tela, garantindo que ela permaneça aberta, estável e funcional além dos 10 segundos.
