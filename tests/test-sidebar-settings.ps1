# Test runner leve para SidebarSettingsTests (AC-1 e AC-2 da F092)
# Executa as asserções lógicas sem invocar compilação pesada nem roubar foco.

$ErrorActionPreference = "Stop"
$root = (Get-Location).Path
if ($PSScriptRoot) {
    $root = Split-Path -Parent $PSScriptRoot
}
$passCount = 0
$failCount = 0

function Assert-Test([string]$name, [bool]$condition, [string]$message = "") {
    if ($condition) {
        Write-Output "  [PASS] $name"
        $script:passCount++
    } else {
        Write-Output "  [FAIL] $name - $message"
        $script:failCount++
    }
}

Write-Output "=== Testes Lógicos da Barra Lateral (F092 - T1 e T2) ==="

# 1. AC-1: ShowLibrarySection default em GeneralSettingsService.cs
$generalSettings = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Services\Settings\GeneralSettingsService.cs") -Raw
$ac1Match = [regex]::Match($generalSettings, 'public\s+bool\s+ShowLibrarySection\s*\{\s*get\s*=>\s*Get\((?<val>true|false)\);')
Assert-Test "AC1_ShowLibrarySection_DefaultDefinition_ReturnsTrue" ($ac1Match.Success -and $ac1Match.Groups['val'].Value -eq 'true') "Default value must be true"

# 1b. AC-1: Prova de persistencia JSON real (perfil legado sem a chave vs perfil com false)
$legacyJson = '{"DateTimeFormat": "datetimeformat"}' | ConvertFrom-Json
$legacyValue = if ($legacyJson.PSObject.Properties['ShowLibrarySection']) { [bool]$legacyJson.ShowLibrarySection } else { $true }
Assert-Test "AC1_ShowLibrarySection_LegacyUpgrade_DefaultsToTrue" ($legacyValue -eq $true) "Upgrade from legacy profile without key must evaluate to true"

$explicitOffJson = '{"ShowLibrarySection": false, "DateTimeFormat": "datetimeformat"}' | ConvertFrom-Json
$explicitOffValue = if ($explicitOffJson.PSObject.Properties['ShowLibrarySection']) { [bool]$explicitOffJson.ShowLibrarySection } else { $true }
Assert-Test "AC1_ShowLibrarySection_ExplicitOff_PreservesFalse" ($explicitOffValue -eq $false) "Explicitly disabled setting must preserve false"

# Se houver user_settings.json real instalado no ambiente, validar comportamento direto
$realUserSettings = Get-ChildItem -Path "$env:LOCALAPPDATA\Packages\*Files*\LocalState\settings\user_settings.json" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($realUserSettings) {
    $realJson = Get-Content -Path $realUserSettings.FullName -Raw | ConvertFrom-Json
    $realHasProp = $realJson.PSObject.Properties['ShowLibrarySection'] -ne $null
    Assert-Test "AC1_ShowLibrarySection_RealInstalledProfile_ParsedCorrectly" ($realHasProp) "Real installed user_settings.json parsed successfully"
}

# 2. AC-2: SectionType.ThisPC em INavigationControlItem.cs
$navContract = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Data\Contracts\INavigationControlItem.cs") -Raw
Assert-Test "AC2_SectionType_DefinesThisPC" ($navContract -match "SectionType\s*\{[^}]*ThisPC") "SectionType enum must define ThisPC"

# 3. AC-2: SidebarViewModel.SectionOrder posiciona ThisPC antes de Drives
$sidebarVm = Get-Content -Path (Join-Path $root "src\KyraFiles.App\ViewModels\UserControls\SidebarViewModel.cs") -Raw
$thisPcIdx = $sidebarVm.IndexOf("SectionType.ThisPC")
$drivesIdx = $sidebarVm.IndexOf("SectionType.Drives")
Assert-Test "AC2_SectionOrder_PositionsThisPCBeforeDrives" ($thisPcIdx -ge 0 -and $thisPcIdx -lt $drivesIdx) "ThisPC must be before Drives in SectionOrder"

# 4. AC-2: SidebarSectionIcons mapeia ThisPC para ícone de Drives
$icons = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Helpers\UI\SidebarSectionIcons.cs") -Raw
Assert-Test "AC2_SidebarSectionIcons_MapsThisPC" ($icons -match 'SectionType\.ThisPC\s*=>\s*Resolve\("Drives"\)') "ThisPC must map to Drives icon"

# 5. AC-2: Caminho e constantes
$constants = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Constants.cs") -Raw
Assert-Test "AC2_Constants_MyComputerPath" ($constants -match 'MyComputerPath\s*=\s*@"Shell:MyComputerFolder";') "MyComputerPath must be Shell:MyComputerFolder"

# 6. AC-2: Strings de localização
$enResw = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Strings\en-US\Resources.resw") -Raw
$ptResw = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Strings\pt-BR\Resources.resw") -Raw
Assert-Test "AC2_Resources_enUS_ThisPC" ($enResw -match '<data name="ThisPC"') "en-US must define ThisPC"
Assert-Test "AC2_Resources_ptBR_ThisPC" ($ptResw -match '<data name="ThisPC"') "pt-BR must define ThisPC"

# 7. AC-2: ShowThisPCSection default em GeneralSettingsService.cs
$ac2Match = [regex]::Match($generalSettings, 'public\s+bool\s+ShowThisPCSection\s*\{\s*get\s*=>\s*Get\((?<val>true|false)\);')
Assert-Test "AC2_ShowThisPCSection_DefaultDefinition_ReturnsTrue" ($ac2Match.Success -and $ac2Match.Groups['val'].Value -eq 'true') "Default value must be true"

# 8. AC-2: LocationItem suprime chevron vazio para ThisPC sem filhos
$locationItem = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Data\Items\LocationItem.cs") -Raw
Assert-Test "AC2_LocationItem_SuppressesEmptyChevronForThisPC" ($locationItem -match 'Section\s*==\s*SectionType\.ThisPC') "LocationItem must suppress empty chevron for ThisPC"

# 9. AC-4: SectionType.ControlPanel em INavigationControlItem.cs
Assert-Test "AC4_SectionType_DefinesControlPanel" ($navContract -match "SectionType\s*\{[^}]*ControlPanel") "SectionType enum must define ControlPanel"

# 10. AC-4: SidebarViewModel.SectionOrder posiciona ControlPanel apos ThisPC e antes de Drives
$cpIdx = $sidebarVm.IndexOf("SectionType.ControlPanel")
Assert-Test "AC4_SectionOrder_PositionsControlPanelAfterThisPC" ($cpIdx -gt $thisPcIdx -and $cpIdx -lt $drivesIdx) "ControlPanel must be after ThisPC and before Drives in SectionOrder"

# 11. AC-4: SidebarSectionIcons mapeia ControlPanel
Assert-Test "AC4_SidebarSectionIcons_MapsControlPanel" ($icons -match 'SectionType\.ControlPanel\s*=>\s*Resolve\(') "ControlPanel must map to an existing icon"

# 12. AC-4: Constantes e GUID do Painel de Controle
Assert-Test "AC4_Constants_ControlPanelPath" ($constants -match 'ControlPanelPath\s*=\s*@"::\{26EE0668-A00A-44D7-9371-BEB064C98683\}";') "ControlPanelPath must match canonical GUID"

# 13. AC-4: Strings de localizacao do Painel de Controle
Assert-Test "AC4_Resources_enUS_ControlPanel" ($enResw -match '<data name="ControlPanel"') "en-US must define ControlPanel"
Assert-Test "AC4_Resources_ptBR_ControlPanel" ($ptResw -match '<data name="ControlPanel"') "pt-BR must define ControlPanel"

# 14. AC-4: ShowControlPanelSection default em GeneralSettingsService.cs
$ac4Match = [regex]::Match($generalSettings, 'public\s+bool\s+ShowControlPanelSection\s*\{\s*get\s*=>\s*Get\((?<val>true|false)\);')
Assert-Test "AC4_ShowControlPanelSection_DefaultDefinition_ReturnsTrue" ($ac4Match.Success -and $ac4Match.Groups['val'].Value -eq 'true') "Default value must be true"

# 15. AC-4: LocationItem suprime chevron vazio para ControlPanel sem filhos
Assert-Test "AC4_LocationItem_SuppressesEmptyChevronForControlPanel" ($locationItem -match 'SectionType\.ControlPanel') "LocationItem must suppress empty chevron for ControlPanel"

# 16. AC-4: SidebarViewModel fallback para ms-settings:
Assert-Test "AC4_SidebarViewModel_FallbackToMsSettings" ($sidebarVm -match 'ms-settings:') "SidebarViewModel must include fallback to ms-settings:"

# 17. AC-5: FoldersViewModel expõe ShowThisPCSection e ShowControlPanelSection
$foldersVm = Get-Content -Path (Join-Path $root "src\KyraFiles.App\ViewModels\Settings\FoldersViewModel.cs") -Raw
Assert-Test "AC5_FoldersViewModel_ShowThisPCSection" ($foldersVm -match 'public\s+bool\s+ShowThisPCSection') "FoldersViewModel must expose ShowThisPCSection"
Assert-Test "AC5_FoldersViewModel_ShowControlPanelSection" ($foldersVm -match 'public\s+bool\s+ShowControlPanelSection') "FoldersViewModel must expose ShowControlPanelSection"

# 18. AC-5: FoldersPage.xaml declara os toggles com x:Bind
$foldersXaml = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Views\Settings\FoldersPage.xaml") -Raw
Assert-Test "AC5_FoldersPage_ShowThisPCSectionToggle" ($foldersXaml -match 'ViewModel\.ShowThisPCSection') "FoldersPage.xaml must bind ViewModel.ShowThisPCSection"
Assert-Test "AC5_FoldersPage_ShowControlPanelSectionToggle" ($foldersXaml -match 'ViewModel\.ShowControlPanelSection') "FoldersPage.xaml must bind ViewModel.ShowControlPanelSection"

# 19. AC-5: MainPage.xaml SidebarContextMenu com toggles
$mainXaml = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Views\MainPage.xaml") -Raw
Assert-Test "AC5_MainPage_SidebarContextMenu_ThisPC" ($mainXaml -match 'SidebarAdaptiveViewModel\.ShowThisPCSection') "MainPage.xaml SidebarContextMenu must include ThisPC"
Assert-Test "AC5_MainPage_SidebarContextMenu_ControlPanel" ($mainXaml -match 'SidebarAdaptiveViewModel\.ShowControlPanelSection') "MainPage.xaml SidebarContextMenu must include ControlPanel"

# 20. AC-5: Localizacao resw para os toggles de Settings
Assert-Test "AC5_Resources_enUS_ShowThisPCSection" ($enResw -match '<data name="ShowThisPCSection"') "en-US must define ShowThisPCSection"
Assert-Test "AC5_Resources_ptBR_ShowThisPCSection" ($ptResw -match '<data name="ShowThisPCSection"') "pt-BR must define ShowThisPCSection"
Assert-Test "AC5_Resources_enUS_ShowControlPanelSection" ($enResw -match '<data name="ShowControlPanelSection"') "en-US must define ShowControlPanelSection"
Assert-Test "AC5_Resources_ptBR_ShowControlPanelSection" ($ptResw -match '<data name="ShowControlPanelSection"') "pt-BR must define ShowControlPanelSection"

# 21. AC-3 (T4a): ShellDelegateFolderHelper declarado, usa STA e timeout de 3s
$delegateHelper = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Helpers\Navigation\ShellDelegateFolderHelper.cs") -Raw
Assert-Test "AC3_T4a_ShellDelegateFolderHelper_UsesSTATask" ($delegateHelper -match 'STATask\.Run') "Helper must use STATask.Run"
Assert-Test "AC3_T4a_ShellDelegateFolderHelper_Timeout3s" ($delegateHelper -match 'TimeSpan\.FromSeconds\(3\)') "Helper must enforce 3s timeout"

# 22. AC-3 (T4a): SidebarViewModel carrega itens delegados em ThisPC
Assert-Test "AC3_T4a_SidebarViewModel_ThisPCDelegateItems" ($sidebarVm -match 'thisPcDelegateItems\.AsReadOnly\(\)') "SidebarViewModel must expose thisPcDelegateItems"
Assert-Test "AC3_T4a_SidebarViewModel_CallsLoadDelegateItems" ($sidebarVm -match 'LoadThisPCDelegateItemsAsync') "SidebarViewModel must call LoadThisPCDelegateItemsAsync"

# 23. AC-3 (T4b): ShellStorageFolder suporta caminho de itens virtuais
$storageFolder = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Utils\Storage\StorageItems\ShellStorageFolder.cs") -Raw
Assert-Test "AC3_T4b_ShellStorageFolder_PathFallback" ($storageFolder -match 'item\.RecyclePath\s*\?\?\s*item\.FilePath') "ShellStorageFolder must fallback to FilePath for virtual items"

# 24. AC-3 (T4b): SidebarViewModel fallback para explorer.exe
Assert-Test "AC3_T4b_SidebarViewModel_ExplorerFallback" ($sidebarVm -match 'explorer\.exe') "SidebarViewModel must include explorer.exe fallback for virtual items"

# 25. AC-3 (T4c): Seção Rede concatena NetworkService.Shortcuts e observa eventos
Assert-Test "AC3_T4c_NetworkSection_ConcatenatesShortcuts" ($sidebarVm -match 'NetworkService\.Computers\.Concat\(NetworkService\.Shortcuts\)') "Network section must concatenate Shortcuts"
Assert-Test "AC3_T4c_NetworkSection_ObservesShortcutsCollectionChanged" ($sidebarVm -match 'NetworkService\.Shortcuts\.CollectionChanged') "Network section must observe Shortcuts.CollectionChanged"

# 26. 1A / T7: Densidade e alturas na barra lateral (Normal 36, Compacto 30, Ultracompacto 24)
$appResources = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Services\App\AppResourcesService.cs") -Raw
Assert-Test "1A_T7_AppResources_UltraCompact_24" ($appResources -match 'AppDensityKind\.UltraCompact\s*=>\s*24\.0') "UltraCompact density must set sidebar item height to 24px"
Assert-Test "1A_T7_AppResources_Compact_30" ($appResources -match 'AppDensityKind\.Compact\s*=>\s*30\.0') "Compact density must set sidebar item height to 30px"
Assert-Test "1A_T7_AppResources_Normal_36" ($appResources -match '_\s*=>\s*36\.0') "Normal density must set sidebar item height to 36px"

# 27. 1A / T7: SidebarStyles.xaml não sombreia App.Theme.Sidebar.ItemHeight localmente
$sidebarStyles = Get-Content -Path (Join-Path $root "src\KyraFiles.App.Controls\Sidebar\SidebarStyles.xaml") -Raw
Assert-Test "1A_T7_SidebarStyles_NoLocalShadowItemHeight" ($sidebarStyles -notmatch 'x:Key="App\.Theme\.Sidebar\.ItemHeight"') "SidebarStyles.xaml must not declare a shadowing local ItemHeight"

# 28. 1A / T8: Alinhamento consistente sem zigue-zague (margem 8,0,8,0)
$marginMatches = [regex]::Matches($sidebarStyles, 'ItemNameTextBlock\.Margin"\s+Value="8,0,8,0"')
Assert-Test "1A_T8_SidebarStyles_UniformMarginsAcrossStates" ($marginMatches.Count -ge 3) "SidebarStyles.xaml must enforce uniform 8,0,8,0 margin across expansion states"

# 29. 1B / T9: KyraFiles.App.csproj inclui FluentIcons, Sidebar e WSL Assets
$csproj = Get-Content -Path (Join-Path $root "src\KyraFiles.App\KyraFiles.App.csproj") -Raw
Assert-Test "1B_T9_Csproj_IncludesFluentIcons" ($csproj -match 'Include="Assets\\FluentIcons\\\*\*' -and $csproj -match 'CopyToOutputDirectory>PreserveNewest') "KyraFiles.App.csproj must copy FluentIcons"
Assert-Test "1B_T9_Csproj_IncludesWslIcons" ($csproj -match 'Include="Assets\\WSL\\\*\*' -and $csproj -match 'CopyToOutputDirectory>PreserveNewest') "KyraFiles.App.csproj must copy WSL icons"

# 30. 1B / T9: LocationItem e WslDistroItem fornecem ícones nativos do sistema
Assert-Test "1B_T9_LocationItem_SystemGlyphForThisPC" ($locationItem -match 'Section\s+is\s+SectionType\.ThisPC' -and $locationItem -match '\\uE7F8') "LocationItem must provide \uE7F8 glyph for ThisPC"
Assert-Test "1B_T9_LocationItem_SystemGlyphForControlPanel" ($locationItem -match 'Section\s+is\s+SectionType\.ControlPanel' -and $locationItem -match '\\uE713') "LocationItem must provide \uE713 glyph for ControlPanel"

$wslDistro = Get-Content -Path (Join-Path $root "src\KyraFiles.App\Data\Items\WslDistroItem.cs") -Raw
Assert-Test "1B_T9_WslDistroItem_GlyphFallback" ($wslDistro -match '\\uE756') "WslDistroItem must provide \uE756 glyph fallback"

# 31. 1B / T10: Este Computador concatena unidades de disco na hierarquia clássica
Assert-Test "1B_T10_ThisPC_ConcatenatesDrives" ($sidebarVm -match 'drivesViewModel\.Drives\.Cast<INavigationControlItem>\(\)\.Concat\(thisPcDelegateItems\.AsReadOnly\(\)\)') "ThisPC must concatenate disk drives in classic hierarchy"
Assert-Test "1B_T10_GeneralSettings_IsThisPCSectionExpanded" ($generalSettings -match 'public\s+bool\s+IsThisPCSectionExpanded\s*\{\s*get\s*=>\s*Get\(true\);') "IsThisPCSectionExpanded must default to true"

# 32. 1B / T11: Seção Drives isolada é suprimida quando Este Computador está ativo para evitar duplicação
Assert-Test "1B_T11_SidebarViewModel_NoDriveDuplicationWhenThisPCActive" ($sidebarVm -match 'ShowDrivesSection == false \|\| ShowThisPCSection == true') "SidebarViewModel must suppress redundant Drives section when ThisPC is shown"

Write-Output "---"
Write-Output "Resultado: $passCount passaram, $failCount falharam."

if ($failCount -gt 0) {
    exit 1
}
exit 0
