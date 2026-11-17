; Inno Setup Script: KyraFiles Setup
; Atende F017: T3, T4, T5 (Instalação modular, isolada, atualizações limpas e reversão de Win+E)

#define MyAppName "KyraFiles"
#define MyAppVersion "4.3.19"
#define MyAppPublisher "KyraFiles"
#define MyAppExeName "KyraFiles.exe"
#define MyHotkeyExeName "KyraFiles.HotkeyService.exe"
#define MyWatchdogExeName "KyraFiles.Watchdog.exe"
#define SourceAppX "..\src\KyraFiles.App\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64"
#define SourceBin "..\bin\Release"
#define SourceServer "..\src\KyraFiles.App.Server\bin\x64\Release\net10.0-windows10.0.26100.0\win-x64"
#define SourceIndex "..\src\KyraFiles.App.IndexService\bin\x64\Release\net10.0-windows10.0.26100.0"

[Setup]
AppId={{D37F21F6-45BB-4CA7-B221-507EA334F912}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\KyraFiles
DisableDirPage=yes
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts\Setup
OutputBaseFilename=KyraFilesSetup-{#MyAppVersion}
#ifdef FAST_COMPRESSION
Compression=lzma/fast
SolidCompression=no
#else
Compression=lzma2/ultra64
SolidCompression=yes
#endif
CloseApplications=yes
CloseApplicationsFilter=KyraFiles.exe,KyraFiles.Watchdog.exe,KyraFiles.HotkeyService.exe,KyraFiles.App.Server.exe,KyraFiles.App.IndexService.exe
RestartApplications=no
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startupwatchdog"; Description: "Iniciar interceptador ultraleve do Win+E com o Windows"; GroupDescription: "Integração do Sistema:"
Name: "replacewine"; Description: "Redirecionar abertura de pastas do Explorer para o KyraFiles"; GroupDescription: "Integração do Sistema:"; Flags: unchecked

[Files]
; Layout completo do pacote AppX (necessário para o .NET 10 host e WinUI 3 funcionarem sem quebra de hostpolicy.dll)
Source: "{#SourceAppX}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.locked.*,*.pdb"
Source: "{#SourceServer}\*"; DestDir: "{app}\KyraFiles.App.Server"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"
Source: "{#SourceIndex}\*"; DestDir: "{app}\KyraFiles.App.IndexService"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

; Binários auxiliares do subsistema F017 e busca Everything
Source: "{#SourceBin}\KyraFiles.HotkeyService.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBin}\KyraFiles.Watchdog.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceBin}\es.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "package_manifest.json"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{localappdata}\Microsoft\WindowsApps\kyrafiles.exe"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0
Name: "{autodesktop}\{#MyAppName}"; Filename: "{localappdata}\Microsoft\WindowsApps\kyrafiles.exe"; IconFilename: "{app}\{#MyAppExeName}"; IconIndex: 0

[Registry]
; Inicialização do Watchdog com o Windows (padrão marcado na instalação)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "KyraFilesWatchdog"; ValueData: """{app}\{#MyWatchdogExeName}"""; Tasks: startupwatchdog; Flags: uninsdeletevalue

; Associação opcional de abertura de pastas via alias MSIX registrado (kyrafiles.exe)
Root: HKCU; Subkey: "Software\Classes\Folder\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{localappdata}\Microsoft\WindowsApps\kyrafiles.exe"" ""%1"""; Tasks: replacewine; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Folder\shell\open\command"; ValueType: string; ValueName: "DelegateExecute"; ValueData: ""; Tasks: replacewine; Flags: uninsdeletevalue

[Run]
; Inicia o Watchdog silenciosamente no término da instalação se solicitado
Filename: "{app}\{#MyWatchdogExeName}"; Description: "Iniciar interceptador de atalho"; Flags: nowait runhidden; Tasks: startupwatchdog
Filename: "{localappdata}\Microsoft\WindowsApps\kyrafiles.exe"; Description: "Abrir o {#MyAppName}"; Flags: nowait postinstall skipifsilent

[InstallDelete]
; Purga vestigios de locks e arquivos de lock temporarios
Type: files; Name: "{app}\*.locked.*"

[UninstallDelete]
; Garante remoção de 100% da pasta isolada na desinstalação (FR-007, AC-7)
Type: filesandordirs; Name: "{app}"

[Code]
var
  RegisterOk: Boolean;

// Procedimentos de verificação pré-instalação
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  // Fecha instâncias anteriores de forma cooperativa
  // Ordem importa: o Watchdog religa o HotkeyService e segura o KyraFiles.exe; encerra-lo primeiro e esperar a saida evita o exit 5 do RestartManager
  ShellExec('open', 'powershell.exe', '-NoProfile -Command "Get-Process KyraFiles.Watchdog -ErrorAction SilentlyContinue | Stop-Process -Force; Get-Process KyraFiles.Watchdog -ErrorAction SilentlyContinue | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue; try { $ev = [System.Threading.EventWaitHandle]::OpenExisting(''Global\KyraFiles_ShutdownHotkeyService''); [void]$ev.Set(); Start-Sleep -Milliseconds 400 } catch {}; Get-Process KyraFiles.HotkeyService -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep -Milliseconds 500; Get-Process KyraFiles, KyraFiles.App.Server, KyraFiles.App.IndexService -ErrorAction SilentlyContinue | Stop-Process -Force"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
  ErrLogPath: String;
  ErrMsg: AnsiString;
begin
  if CurStep = ssPostInstall then
  begin
    // Remove qualquer registro prévio de KyraFiles preservando os dados da aplicação, pois o Windows bloqueia registro em Development Mode se o pacote já existir com a mesma versão
    // Falha do registro nao pode ser escondida: sai com 1 e grava detalhe em log para auditoria
    ShellExec('open', 'powershell.exe', '-NoProfile -NonInteractive -WindowStyle Hidden -Command "try { Get-AppxPackage -Name *KyraFiles* -ErrorAction SilentlyContinue | ForEach-Object { Remove-AppxPackage $_.PackageFullName -PreserveApplicationData -ErrorAction SilentlyContinue }; Add-AppxPackage -Register ''' + ExpandConstant('{app}') + '\AppxManifest.xml'' -ForceApplicationShutdown -ForceUpdateFromAnyVersion -ErrorAction Stop } catch { [System.IO.File]::WriteAllText(''' + ExpandConstant('{tmp}') + '\appx_register_error.log'', $_.Exception.ToString()); exit 1 }"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
    if ErrorCode <> 0 then
    begin
      ErrLogPath := ExpandConstant('{tmp}\appx_register_error.log');
      if FileExists(ErrLogPath) and LoadStringFromFile(ErrLogPath, ErrMsg) then
        MsgBox('O registro do pacote do KyraFiles falhou (codigo ' + IntToStr(ErrorCode) + '):' + #13#10#13#10 + String(ErrMsg) + #13#10 + 'Feche o KyraFiles e rode o instalador de novo.', mbError, MB_OK)
      else
        MsgBox('O registro do pacote do KyraFiles falhou (codigo ' + IntToStr(ErrorCode) + '). Feche o KyraFiles e rode o instalador de novo.', mbError, MB_OK);
    end
    else
      RegisterOk := True;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ErrorCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Encerra cooperativamente o Watchdog e HotkeyService e remove chaves de pasta/Run/CLSID para devolver o Win+E ao Explorer
    ShellExec('open', 'powershell.exe', '-NoProfile -Command "try { $ev = [System.Threading.EventWaitHandle]::OpenExisting(''Global\KyraFiles_ShutdownHotkeyService''); [void]$ev.Set(); Start-Sleep -Milliseconds 400 } catch {}; Get-Process KyraFiles.Watchdog, KyraFiles.HotkeyService, KyraFiles, KyraFiles.App.Server, KyraFiles.App.IndexService -ErrorAction SilentlyContinue | Stop-Process -Force"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'KyraFilesWatchdog');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Classes\Folder\shell\open');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Classes\Folder\shell\explore');
    RegDeleteKeyIncludingSubkeys(HKEY_CURRENT_USER, 'Software\Classes\CLSID\{52205fd8-5dfb-447d-801a-d0b52f2e83e1}');

    // Remove o registro do pacote AppX
    ShellExec('open', 'powershell.exe', '-NoProfile -NonInteractive -WindowStyle Hidden -Command "Get-AppxPackage -Name *KyraFiles* -ErrorAction SilentlyContinue | Remove-AppxPackage -ErrorAction SilentlyContinue"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  end;
end;

// Instalacao que abortou nao pode deixar o Win+E desligado: religa o Watchdog da versao anterior
procedure DeinitializeSetup();
var
  ErrorCode: Integer;
  WatchdogPath: String;
begin
  if not RegisterOk then
  begin
    WatchdogPath := ExpandConstant('{autopf}\KyraFiles\{#MyWatchdogExeName}');
    if FileExists(WatchdogPath) then
      Exec(WatchdogPath, '', '', SW_HIDE, ewNoWait, ErrorCode);
  end;
end;
