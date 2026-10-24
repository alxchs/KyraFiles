; Inno Setup Script: Files Custom Setup
; Atende F017: T3, T4, T5 (Instalação modular, isolada, atualizações limpas e reversão de Win+E)

#define MyAppName "Files Custom"
#define MyAppVersion "4.3.6"
#define MyAppPublisher "Files Custom"
#define MyAppExeName "Files.exe"
#define MyHotkeyExeName "Files.HotkeyService.exe"
#define MyWatchdogExeName "Files.Watchdog.exe"
#define SourceAppX "..\src\Files.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\AppX"
#define SourceBin "..\bin\Release"

[Setup]
AppId={{D37F21F6-45BB-4CA7-B221-507EA334F912}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\FilesCustom
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts\Setup
OutputBaseFilename=FilesCustomSetup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=yes
CloseApplicationsFilter=Files.exe,Files.Watchdog.exe,Files.HotkeyService.exe,Files.App.Server.exe,Files.App.IndexService.exe
RestartApplications=no
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startupwatchdog"; Description: "Iniciar interceptador ultraleve do Win+E com o Windows"; GroupDescription: "Integração do Sistema:"
Name: "replacewine"; Description: "Redirecionar abertura de pastas do Explorer para o Files Custom"; GroupDescription: "Integração do Sistema:"; Flags: unchecked

[Files]
; Layout completo do pacote AppX (necessário para o .NET 10 host e WinUI 3 funcionarem sem quebra de hostpolicy.dll)
Source: "{#SourceAppX}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.locked.*,*.pdb"

; Binários auxiliares do subsistema F017 e busca Everything
Source: "{#SourceBin}\Files.HotkeyService.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBin}\Files.Watchdog.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\alxch\bin\es.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "package_manifest.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{localappdata}\Microsoft\WindowsApps\files-dev.exe"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{autodesktop}\{#MyAppName}"; Filename: "{localappdata}\Microsoft\WindowsApps\files-dev.exe"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0

[Registry]
; Inicialização do Watchdog com o Windows (padrão marcado na instalação)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "FilesCustomWatchdog"; ValueData: """{app}\{#MyWatchdogExeName}"""; Tasks: startupwatchdog; Flags: uninsdeletevalue

; Associação opcional de abertura de pastas via alias MSIX registrado (files-dev.exe)
Root: HKCU; Subkey: "Software\Classes\Folder\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{localappdata}\Microsoft\WindowsApps\files-dev.exe"" ""%1"""; Tasks: replacewine; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Folder\shell\open\command"; ValueType: string; ValueName: "DelegateExecute"; ValueData: ""; Tasks: replacewine; Flags: uninsdeletevalue

[Run]
; Inicia o Watchdog silenciosamente no término da instalação se solicitado
Filename: "{app}\{#MyWatchdogExeName}"; Description: "Iniciar interceptador de atalho"; Flags: nowait runhidden; Tasks: startupwatchdog
Filename: "{localappdata}\Microsoft\WindowsApps\files-dev.exe"; Description: "Abrir o {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Garante remoção de 100% da pasta isolada na desinstalação (FR-007, AC-7)
Type: filesandordirs; Name: "{app}"

[Code]
// Procedimentos de verificação pré-instalação
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  // Fecha instâncias anteriores de forma cooperativa
  ShellExec('open', 'powershell.exe', '-NoProfile -Command "try { $ev = [System.Threading.EventWaitHandle]::OpenExisting(''Global\FilesCustom_ShutdownHotkeyService''); [void]$ev.Set(); Start-Sleep -Milliseconds 400 } catch {}; Get-Process Files.Watchdog, Files.HotkeyService, Files, Files.App.Server, Files.App.IndexService -ErrorAction SilentlyContinue | Stop-Process -Force"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Remove registro prévio se existir de outro caminho e registra a nova identidade de pacote AppX com o Windows
    ShellExec('open', 'powershell.exe', '-NoProfile -NonInteractive -WindowStyle Hidden -Command "$p = Get-AppxPackage FilesDev -ErrorAction SilentlyContinue; if ($p) { Remove-AppxPackage $p.PackageFullName -ErrorAction SilentlyContinue }; Add-AppxPackage -Register ''' + ExpandConstant('{app}') + '\AppxManifest.xml'' -ForceApplicationShutdown -ForceUpdateFromAnyVersion"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Encerra cooperativamente o Watchdog e HotkeyService e remove chaves de pasta/Run/CLSID para devolver o Win+E ao Explorer
    ShellExec('open', 'powershell.exe', '-NoProfile -Command "try { $ev = [System.Threading.EventWaitHandle]::OpenExisting(''Global\FilesCustom_ShutdownHotkeyService''); [void]$ev.Set(); Start-Sleep -Milliseconds 400 } catch {}; Get-Process Files.Watchdog, Files.HotkeyService, Files, Files.App.Server, Files.App.IndexService -ErrorAction SilentlyContinue | Stop-Process -Force"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'FilesCustomWatchdog');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Classes\Folder\shell\open');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Classes\Folder\shell\explore');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Classes\CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}');

    // Remove o registro do pacote AppX
    ShellExec('open', 'powershell.exe', '-NoProfile -NonInteractive -WindowStyle Hidden -Command "Get-AppxPackage FilesDev -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  end;
end;
