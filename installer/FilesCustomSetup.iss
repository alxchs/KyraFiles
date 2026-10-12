; Inno Setup Script: Files Custom Setup
; Atende F017: T3, T4, T5 (Instalação modular, isolada, atualizações limpas e reversão de Win+E)

#define MyAppName "Files Custom"
#define MyAppVersion "4.2.37"
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
CloseApplicationsFilter=Files.exe,Files.Watchdog.exe,Files.HotkeyService.exe,Files.App.Server.exe
RestartApplications=no
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startupwatchdog"; Description: "Iniciar interceptador ultraleve do Win+E com o Windows"; GroupDescription: "Integração do Sistema:"; Flags: unchecked
Name: "replacewine"; Description: "Tornar o Files Custom o gerenciador de arquivos padrão (Win+E)"; GroupDescription: "Integração do Sistema:"

[Files]
; Binários principais na raiz da pasta do aplicativo
Source: "{#SourceAppX}\Files.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceAppX}\Files.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBin}\Files.HotkeyService.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBin}\Files.Watchdog.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceAppX}\AppxManifest.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceAppX}\resources.pri"; DestDir: "{app}"; Flags: ignoreversion

; Servidor COM fora do processo
Source: "{#SourceAppX}\Files.App.Server\*"; DestDir: "{app}\Files.App.Server"; Flags: ignoreversion recursesubdirs createallsubdirs

; Subpasta isolada de Dependências e bibliotecas do Runtime
Source: "{#SourceAppX}\*.dll"; DestDir: "{app}\Dependencies"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourceAppX}\*.winmd"; DestDir: "{app}\Dependencies"; Flags: ignoreversion skipifsourcedoesntexist
Source: "{#SourceAppX}\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Registry]
; Inicialização do Watchdog com o Windows (quando marcado na instalação)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "FilesCustomWatchdog"; ValueData: """{app}\{#MyWatchdogExeName}"""; Tasks: startupwatchdog; Flags: uninsdeletevalue

; Associação do Explorer / Pastas para o Files Custom
Root: HKCU; Subkey: "Software\Classes\Folder\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: replacewine; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Folder\shell\open\command"; ValueType: string; ValueName: "DelegateExecute"; ValueData: ""; Tasks: replacewine; Flags: uninsdeletevalue

[Run]
; Inicia o Watchdog silenciosamente no término da instalação se solicitado
Filename: "{app}\{#MyWatchdogExeName}"; Description: "Iniciar interceptador de atalho"; Flags: nowait postinstall runhidden skipifsilent; Tasks: startupwatchdog
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir o {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Finalização executada na seção Code para controle confiável de processos

[Code]
// Procedimentos de verificação pré-instalação
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  // Fecha instâncias anteriores de forma cooperativa
  ShellExec('open', 'powershell.exe', '-NoProfile -Command "Get-Process Files.Watchdog, Files.HotkeyService, Files -ErrorAction SilentlyContinue | Stop-Process -Force"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Encerra cooperativamente o Watchdog e HotkeyService antes de remover os arquivos
    ShellExec('open', 'powershell.exe', '-NoProfile -Command "Get-Process Files.Watchdog, Files.HotkeyService, Files -ErrorAction SilentlyContinue | Stop-Process -Force"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  end;
end;

