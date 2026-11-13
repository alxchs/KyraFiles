@echo off
title Preparacao de Ambiente de Compilacao - KyraFiles
echo ======================================================================
echo  Instalacao das Ferramentas de Compilacao do KyraFiles (Heloize-PC)
echo ======================================================================
echo.

echo [1/3] Verificando / Instalando Windows SDK 10.0.26100...
winget install --id Microsoft.WindowsSDK.10.0.26100 --accept-source-agreements --accept-package-agreements --silent
echo.

echo [2/3] Verificando / Instalando Visual Studio 2022 Build Tools (C++ e ATL)...
winget install --id Microsoft.VisualStudio.2022.BuildTools --accept-source-agreements --accept-package-agreements --silent --override "--passive --wait --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 --add Microsoft.VisualStudio.Component.VC.ATL --add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools"
echo.

echo [3/3] Verificando / Instalando Inno Setup 6 (Gerador de Instalador)...
winget install --id JRSoftware.InnoSetup --accept-source-agreements --accept-package-agreements --silent
echo.

echo ======================================================================
echo  Instalacao finalizada! Execute: pwsh tools\setup_compiler_environment.ps1
echo ======================================================================
pause
