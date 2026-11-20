; Inno Setup Script: KyraFiles Setup
; Atende F017: T3, T4, T5 (Instalação modular, isolada, atualizações limpas e reversão de Win+E)

#define MyAppName "KyraFiles"
#define MyAppVersion "4.3.25"
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
SetupMutex=KyraFilesSetupSingleInstanceMutex

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
type
  TFlashWInfo = record
    cbSize: Cardinal;
    hwnd: HWND;
    dwFlags: Cardinal;
    uCount: Cardinal;
    dwTimeout: Cardinal;
  end;

const
  SW_RESTORE = 9;
  FLASHW_ALL = 3;
  FLASHW_TIMERNOFG = 12;
  ASFW_ANY = $FFFFFFFF;

function ShowWindowNative(hWnd: HWND; nCmdShow: Integer): Boolean;
  external 'ShowWindow@user32.dll stdcall';
function SetForegroundWindowNative(hWnd: HWND): Boolean;
  external 'SetForegroundWindow@user32.dll stdcall';
function GetForegroundWindowNative(): HWND;
  external 'GetForegroundWindow@user32.dll stdcall';
function IsIconicNative(hWnd: HWND): Boolean;
  external 'IsIconic@user32.dll stdcall';
function FlashWindowExNative(var pfwi: TFlashWInfo): Boolean;
  external 'FlashWindowEx@user32.dll stdcall';
function AllowSetForegroundWindowNative(dwProcessId: Cardinal): Boolean;
  external 'AllowSetForegroundWindow@user32.dll stdcall';
function GetTickCountNative(): DWORD;
  external 'GetTickCount@kernel32.dll stdcall';

var
  RegisterOk: Boolean;
  PageEnteredTick: DWORD;

procedure BringWindowToFrontOrFlash(TargetHwnd: HWND);
var
  FlashInfo: TFlashWInfo;
begin
  if TargetHwnd = 0 then
    Exit;

  if IsIconicNative(TargetHwnd) then
    ShowWindowNative(TargetHwnd, SW_RESTORE);

  SetForegroundWindowNative(TargetHwnd);

  // FR-004: se o Windows recusou trazer a janela para frente, piscar barra de tarefas
  if GetForegroundWindowNative() <> TargetHwnd then
  begin
    FlashInfo.cbSize := SizeOf(FlashInfo);
    FlashInfo.hwnd := TargetHwnd;
    FlashInfo.dwFlags := FLASHW_ALL or FLASHW_TIMERNOFG;
    FlashInfo.uCount := 5;
    FlashInfo.dwTimeout := 0;
    FlashWindowExNative(FlashInfo);
  end;
end;

// FR-003 (AC-5): trava de 1 s contra confirmacao/cancelamento acidental por digitacao
procedure ArmInputGuard();
begin
  PageEnteredTick := GetTickCountNative();
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  // FR-003: ignora confirmacao nos primeiros 1.000 ms apos exibicao da pagina
  if (CurPageID = wpReady) or (CurPageID = wpFinished) then
  begin
    if (GetTickCountNative() - PageEnteredTick) < 1000 then
      Result := False;
  end;
end;

procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
begin
  // FR-003: ignora cancelamento acidental no primeiro segundo da pagina
  if (GetTickCountNative() - PageEnteredTick) < 1000 then
  begin
    Cancel := False;
    Confirm := False;
  end;
end;

// Procedimentos de verificação pré-instalação
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
  ExistingHwnd: HWND;
begin
  // AC-8: se ja existe instancia do instalador aberta, traz ela para frente e encerra a segunda
  if CheckForMutexes('KyraFilesSetupSingleInstanceMutex') then
  begin
    ExistingHwnd := FindWindowByClassName('TWizardForm');
    if ExistingHwnd <> 0 then
      BringWindowToFrontOrFlash(ExistingHwnd);
    Result := False;
    Exit;
  end;

  // Fecha instâncias anteriores de forma cooperativa
  // Ordem importa: o Watchdog religa o HotkeyService e segura o KyraFiles.exe; encerra-lo primeiro e esperar a saida evita o exit 5 do RestartManager
  ShellExec('open', 'powershell.exe', '-NoProfile -Command "Get-Process KyraFiles.Watchdog -ErrorAction SilentlyContinue | Stop-Process -Force; Get-Process KyraFiles.Watchdog -ErrorAction SilentlyContinue | Wait-Process -Timeout 10 -ErrorAction SilentlyContinue; try { $ev = [System.Threading.EventWaitHandle]::OpenExisting(''Global\KyraFiles_ShutdownHotkeyService''); [void]$ev.Set(); Start-Sleep -Milliseconds 400 } catch {}; Get-Process KyraFiles.HotkeyService -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep -Milliseconds 500; Get-Process KyraFiles, KyraFiles.App.Server, KyraFiles.App.IndexService -ErrorAction SilentlyContinue | Stop-Process -Force"', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
  Result := True;
end;

procedure InitializeWizard();
begin
  // FR-001 / FR-003 / FR-004: traz o assistente para frente e aplica trava de 1 s
  BringWindowToFrontOrFlash(WizardForm.Handle);
  ArmInputGuard();
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  BringWindowToFrontOrFlash(WizardForm.Handle);
  if (CurPageID = wpReady) or (CurPageID = wpFinished) then
    ArmInputGuard();
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
      BringWindowToFrontOrFlash(WizardForm.Handle);
      ErrLogPath := ExpandConstant('{tmp}\appx_register_error.log');
      if FileExists(ErrLogPath) and LoadStringFromFile(ErrLogPath, ErrMsg) then
        MsgBox('O registro do pacote do KyraFiles falhou (codigo ' + IntToStr(ErrorCode) + '):' + #13#10#13#10 + String(ErrMsg) + #13#10 + 'Feche o KyraFiles e rode o instalador de novo.', mbError, MB_OK)
      else
        MsgBox('O registro do pacote do KyraFiles falhou (codigo ' + IntToStr(ErrorCode) + '). Feche o KyraFiles e rode o instalador de novo.', mbError, MB_OK);
    end
    else
    begin
      RegisterOk := True;
      AllowSetForegroundWindowNative(ASFW_ANY);
    end;
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
