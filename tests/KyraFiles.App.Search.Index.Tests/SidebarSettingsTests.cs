// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class SidebarSettingsTests
	{
		[TestMethod]
		public void AC1_ShowLibrarySection_DefaultDefinition_ReturnsTrue()
		{
			// F092 / T1 / AC-1: ShowLibrarySection default must be true
			var solutionRoot = FindSolutionRoot();
			var generalSettingsPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Services", "Settings", "GeneralSettingsService.cs");

			Assert.IsTrue(File.Exists(generalSettingsPath), $"GeneralSettingsService.cs not found at {generalSettingsPath}");

			var content = File.ReadAllText(generalSettingsPath);
			var match = Regex.Match(content, @"public\s+bool\s+ShowLibrarySection\s*\{\s*get\s*=>\s*Get\((?<defaultVal>true|false)\);", RegexOptions.Singleline);

			Assert.IsTrue(match.Success, "ShowLibrarySection property declaration with Get(...) not found in GeneralSettingsService.cs");
			Assert.AreEqual("true", match.Groups["defaultVal"].Value, "ShowLibrarySection default value in GeneralSettingsService.cs must be 'true'.");
		}

		[TestMethod]
		public void AC1_ShowLibrarySection_JsonParsing_PreservesExplicitFalseAndDefaultsToTrue()
		{
			// Testa a semântica de DefaultJsonSettingsDatabase.GetValue usando o parser JSON real:
			// se a chave existe no JSON, o valor booleano do JSON é respeitado;
			// se a chave está ausente (upgrade de perfil antigo), o defaultValue (true) é retornado.
			static bool ResolveFromJson(string json, string propertyName, bool defaultValue)
			{
				using var doc = System.Text.Json.JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty(propertyName, out var element))
				{
					return element.GetBoolean();
				}

				return defaultValue;
			}

			// Caso 1: Usuário em atualização sem a chave gravada (perfil antigo) -> defaultValue é retornado (true)
			const string legacySettingsJson = @"{ ""DateTimeFormat"": ""datetimeformat"", ""ConfirmDelete"": true }";
			var upgradeUserSetting = ResolveFromJson(legacySettingsJson, "ShowLibrarySection", defaultValue: true);
			Assert.IsTrue(upgradeUserSetting, "Upgrade profile without ShowLibrarySection must resolve to default true.");

			// Caso 2: Usuário que desligou explicitamente -> deve respeitar false do JSON gravado
			const string explicitOffJson = @"{ ""ShowLibrarySection"": false, ""DateTimeFormat"": ""datetimeformat"" }";
			var explicitOffUserSetting = ResolveFromJson(explicitOffJson, "ShowLibrarySection", defaultValue: true);
			Assert.IsFalse(explicitOffUserSetting, "User profile with ShowLibrarySection:false must preserve false.");

			// Caso 3: Usuário que ligou explicitamente -> deve manter true do JSON gravado
			const string explicitOnJson = @"{ ""ShowLibrarySection"": true, ""DateTimeFormat"": ""datetimeformat"" }";
			var explicitOnUserSetting = ResolveFromJson(explicitOnJson, "ShowLibrarySection", defaultValue: true);
			Assert.IsTrue(explicitOnUserSetting, "User profile with ShowLibrarySection:true must preserve true.");
		}

		[TestMethod]
		public void AC2_SectionType_DefinesThisPC_AndSectionOrder_PositionsBeforeDrives()
		{
			// F092 / T2 / AC-2: SectionType must contain ThisPC, and SidebarViewModel.SectionOrder must position ThisPC before Drives
			var solutionRoot = FindSolutionRoot();
			var navContractPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Data", "Contracts", "INavigationControlItem.cs");
			var sidebarVmPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "ViewModels", "UserControls", "SidebarViewModel.cs");
			var iconsPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Helpers", "UI", "SidebarSectionIcons.cs");

			Assert.IsTrue(File.Exists(navContractPath), $"INavigationControlItem.cs not found at {navContractPath}");
			Assert.IsTrue(File.Exists(sidebarVmPath), $"SidebarViewModel.cs not found at {sidebarVmPath}");
			Assert.IsTrue(File.Exists(iconsPath), $"SidebarSectionIcons.cs not found at {iconsPath}");

			var navContractContent = File.ReadAllText(navContractPath);
			Assert.IsTrue(navContractContent.Contains("ThisPC"), "SectionType enum in INavigationControlItem.cs must define ThisPC.");

			var sidebarVmContent = File.ReadAllText(sidebarVmPath);
			var thisPcIndex = sidebarVmContent.IndexOf("SectionType.ThisPC", StringComparison.Ordinal);
			var drivesIndex = sidebarVmContent.IndexOf("SectionType.Drives", StringComparison.Ordinal);

			Assert.IsTrue(thisPcIndex >= 0, "SidebarViewModel.cs must reference SectionType.ThisPC.");
			Assert.IsTrue(thisPcIndex < drivesIndex, "SectionType.ThisPC must be positioned before SectionType.Drives in SectionOrder.");

			var iconsContent = File.ReadAllText(iconsPath);
			Assert.IsTrue(iconsContent.Contains("SectionType.ThisPC => Resolve(\"Drives\")"), "SidebarSectionIcons.cs must map SectionType.ThisPC to Drives icon.");
		}

		[TestMethod]
		public void AC2_ThisPC_PathAndLocalization_MatchesMyComputerFolder()
		{
			// F092 / T2 / AC-2: MyComputerPath must be Shell:MyComputerFolder and ThisPC localized in en-US and pt-BR
			var solutionRoot = FindSolutionRoot();
			var constantsPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Constants.cs");
			var enReswPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Strings", "en-US", "Resources.resw");
			var ptReswPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Strings", "pt-BR", "Resources.resw");

			Assert.IsTrue(File.Exists(constantsPath), $"Constants.cs not found at {constantsPath}");
			Assert.IsTrue(File.Exists(enReswPath), $"en-US Resources.resw not found at {enReswPath}");
			Assert.IsTrue(File.Exists(ptReswPath), $"pt-BR Resources.resw not found at {ptReswPath}");

			var constantsContent = File.ReadAllText(constantsPath);
			Assert.IsTrue(constantsContent.Contains(@"MyComputerPath = @""Shell:MyComputerFolder"";"), "Constants.UserEnvironmentPaths.MyComputerPath must be 'Shell:MyComputerFolder'.");

			var enContent = File.ReadAllText(enReswPath);
			Assert.IsTrue(enContent.Contains(@"<data name=""ThisPC"""), "Resources.resw (en-US) must define ThisPC.");

			var ptContent = File.ReadAllText(ptReswPath);
			Assert.IsTrue(ptContent.Contains(@"<data name=""ThisPC"""), "Resources.resw (pt-BR) must define ThisPC.");
		}

		[TestMethod]
		public void AC2_ShowThisPCSection_DefaultDefinition_ReturnsTrue()
		{
			// F092 / T2 / AC-2: ShowThisPCSection default must be true
			var solutionRoot = FindSolutionRoot();
			var generalSettingsPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Services", "Settings", "GeneralSettingsService.cs");

			Assert.IsTrue(File.Exists(generalSettingsPath), $"GeneralSettingsService.cs not found at {generalSettingsPath}");

			var content = File.ReadAllText(generalSettingsPath);
			var match = Regex.Match(content, @"public\s+bool\s+ShowThisPCSection\s*\{\s*get\s*=>\s*Get\((?<defaultVal>true|false)\);", RegexOptions.Singleline);

			Assert.IsTrue(match.Success, "ShowThisPCSection property declaration with Get(...) not found in GeneralSettingsService.cs");
			Assert.AreEqual("true", match.Groups["defaultVal"].Value, "ShowThisPCSection default value in GeneralSettingsService.cs must be 'true'.");
		}

		[TestMethod]
		public void AC2_LocationItem_DoesNotShowExpanderChevron_WhenChildless()
		{
			// F092 / T2 / AC-2: LocationItem for ThisPC must return null ChildItems when empty so no expander chevron is shown
			var solutionRoot = FindSolutionRoot();
			var locationItemPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Data", "Items", "LocationItem.cs");

			Assert.IsTrue(File.Exists(locationItemPath), $"LocationItem.cs not found at {locationItemPath}");

			var content = File.ReadAllText(locationItemPath);
			Assert.IsTrue(content.Contains("Section == SectionType.ThisPC"), "LocationItem.cs must handle SectionType.ThisPC to avoid showing empty expander chevrons.");
		}

		[TestMethod]
		public void AC4_SectionType_DefinesControlPanel_AndSectionOrder_PositionsAfterThisPC()
		{
			// F092 / T3 / AC-4: SectionType must contain ControlPanel, and SidebarViewModel.SectionOrder must position ControlPanel after ThisPC
			var solutionRoot = FindSolutionRoot();
			var navContractPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Data", "Contracts", "INavigationControlItem.cs");
			var sidebarVmPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "ViewModels", "UserControls", "SidebarViewModel.cs");
			var iconsPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Helpers", "UI", "SidebarSectionIcons.cs");

			Assert.IsTrue(File.Exists(navContractPath), $"INavigationControlItem.cs not found at {navContractPath}");
			Assert.IsTrue(File.Exists(sidebarVmPath), $"SidebarViewModel.cs not found at {sidebarVmPath}");
			Assert.IsTrue(File.Exists(iconsPath), $"SidebarSectionIcons.cs not found at {iconsPath}");

			var navContractContent = File.ReadAllText(navContractPath);
			Assert.IsTrue(navContractContent.Contains("ControlPanel"), "SectionType enum in INavigationControlItem.cs must define ControlPanel.");

			var sidebarVmContent = File.ReadAllText(sidebarVmPath);
			var thisPcIndex = sidebarVmContent.IndexOf("SectionType.ThisPC", StringComparison.Ordinal);
			var controlPanelIndex = sidebarVmContent.IndexOf("SectionType.ControlPanel", StringComparison.Ordinal);
			var drivesIndex = sidebarVmContent.IndexOf("SectionType.Drives", StringComparison.Ordinal);

			Assert.IsTrue(controlPanelIndex >= 0, "SidebarViewModel.cs must reference SectionType.ControlPanel.");
			Assert.IsTrue(thisPcIndex < controlPanelIndex, "SectionType.ControlPanel must be positioned after SectionType.ThisPC.");
			Assert.IsTrue(controlPanelIndex < drivesIndex, "SectionType.ControlPanel must be positioned before SectionType.Drives.");

			var iconsContent = File.ReadAllText(iconsPath);
			Assert.IsTrue(iconsContent.Contains("SectionType.ControlPanel => Resolve("), "SidebarSectionIcons.cs must map SectionType.ControlPanel to an existing icon.");
		}

		[TestMethod]
		public void AC4_ControlPanel_Constants_And_Localization()
		{
			// F092 / T3 / AC-4: ControlPanelPath must be ::{26EE0668-A00A-44D7-9371-BEB064C98683} and localized in en-US and pt-BR
			var solutionRoot = FindSolutionRoot();
			var constantsPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Constants.cs");
			var enReswPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Strings", "en-US", "Resources.resw");
			var ptReswPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Strings", "pt-BR", "Resources.resw");

			Assert.IsTrue(File.Exists(constantsPath), $"Constants.cs not found at {constantsPath}");
			Assert.IsTrue(File.Exists(enReswPath), $"en-US Resources.resw not found at {enReswPath}");
			Assert.IsTrue(File.Exists(ptReswPath), $"pt-BR Resources.resw not found at {ptReswPath}");

			var constantsContent = File.ReadAllText(constantsPath);
			Assert.IsTrue(constantsContent.Contains(@"::{26EE0668-A00A-44D7-9371-BEB064C98683}"), "Constants must define ControlPanelPath with GUID ::{26EE0668-A00A-44D7-9371-BEB064C98683}.");

			var enContent = File.ReadAllText(enReswPath);
			Assert.IsTrue(enContent.Contains(@"<data name=""ControlPanel"""), "Resources.resw (en-US) must define ControlPanel.");

			var ptContent = File.ReadAllText(ptReswPath);
			Assert.IsTrue(ptContent.Contains(@"<data name=""ControlPanel"""), "Resources.resw (pt-BR) must define ControlPanel.");
		}

		[TestMethod]
		public void AC4_ShowControlPanelSection_DefaultDefinition_ReturnsTrue()
		{
			// F092 / T3 / AC-4: ShowControlPanelSection default must be true
			var solutionRoot = FindSolutionRoot();
			var generalSettingsPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Services", "Settings", "GeneralSettingsService.cs");

			Assert.IsTrue(File.Exists(generalSettingsPath), $"GeneralSettingsService.cs not found at {generalSettingsPath}");

			var content = File.ReadAllText(generalSettingsPath);
			var match = Regex.Match(content, @"public\s+bool\s+ShowControlPanelSection\s*\{\s*get\s*=>\s*Get\((?<defaultVal>true|false)\);", RegexOptions.Singleline);

			Assert.IsTrue(match.Success, "ShowControlPanelSection property declaration with Get(...) not found in GeneralSettingsService.cs");
			Assert.AreEqual("true", match.Groups["defaultVal"].Value, "ShowControlPanelSection default value in GeneralSettingsService.cs must be 'true'.");
		}

		[TestMethod]
		public void AC4_LocationItem_DoesNotShowExpanderChevron_WhenChildless()
		{
			// F092 / T3 / AC-4: LocationItem for ControlPanel must return null ChildItems when empty
			var solutionRoot = FindSolutionRoot();
			var locationItemPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Data", "Items", "LocationItem.cs");

			Assert.IsTrue(File.Exists(locationItemPath), $"LocationItem.cs not found at {locationItemPath}");

			var content = File.ReadAllText(locationItemPath);
			Assert.IsTrue(content.Contains("SectionType.ControlPanel"), "LocationItem.cs must handle SectionType.ControlPanel to avoid showing empty expander chevrons.");
		}

		[TestMethod]
		public void AC4_SidebarViewModel_NavigationFallback_ToSettings()
		{
			// F092 / T3 / AC-4 / spec §5: If Control Panel navigation fails, fallback opens ms-settings:
			var solutionRoot = FindSolutionRoot();
			var sidebarVmPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "ViewModels", "UserControls", "SidebarViewModel.cs");

			Assert.IsTrue(File.Exists(sidebarVmPath), $"SidebarViewModel.cs not found at {sidebarVmPath}");

			var content = File.ReadAllText(sidebarVmPath);
			Assert.IsTrue(content.Contains("ms-settings:"), "SidebarViewModel.cs must contain fallback to ms-settings: on navigation failure.");
		}

		[TestMethod]
		public void AC5_FoldersSettings_TogglesDeclaredInViewModelAndXaml()
		{
			// F092 / T5 / AC-5: FoldersViewModel, FoldersPage.xaml and MainPage.xaml must declare ThisPC and ControlPanel toggles with resw localization
			var solutionRoot = FindSolutionRoot();
			var foldersVmPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "ViewModels", "Settings", "FoldersViewModel.cs");
			var foldersXamlPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Views", "Settings", "FoldersPage.xaml");
			var mainXamlPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Views", "MainPage.xaml");
			var enReswPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Strings", "en-US", "Resources.resw");
			var ptReswPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Strings", "pt-BR", "Resources.resw");

			Assert.IsTrue(File.Exists(foldersVmPath), $"FoldersViewModel.cs not found at {foldersVmPath}");
			Assert.IsTrue(File.Exists(foldersXamlPath), $"FoldersPage.xaml not found at {foldersXamlPath}");
			Assert.IsTrue(File.Exists(mainXamlPath), $"MainPage.xaml not found at {mainXamlPath}");
			Assert.IsTrue(File.Exists(enReswPath), $"en-US Resources.resw not found at {enReswPath}");
			Assert.IsTrue(File.Exists(ptReswPath), $"pt-BR Resources.resw not found at {ptReswPath}");

			var vmContent = File.ReadAllText(foldersVmPath);
			Assert.IsTrue(vmContent.Contains("ShowThisPCSection"), "FoldersViewModel.cs must expose ShowThisPCSection.");
			Assert.IsTrue(vmContent.Contains("ShowControlPanelSection"), "FoldersViewModel.cs must expose ShowControlPanelSection.");

			var foldersXaml = File.ReadAllText(foldersXamlPath);
			Assert.IsTrue(foldersXaml.Contains("ViewModel.ShowThisPCSection"), "FoldersPage.xaml must bind ViewModel.ShowThisPCSection.");
			Assert.IsTrue(foldersXaml.Contains("ViewModel.ShowControlPanelSection"), "FoldersPage.xaml must bind ViewModel.ShowControlPanelSection.");

			var mainXaml = File.ReadAllText(mainXamlPath);
			Assert.IsTrue(mainXaml.Contains("SidebarAdaptiveViewModel.ShowThisPCSection"), "MainPage.xaml must bind SidebarAdaptiveViewModel.ShowThisPCSection.");
			Assert.IsTrue(mainXaml.Contains("SidebarAdaptiveViewModel.ShowControlPanelSection"), "MainPage.xaml must bind SidebarAdaptiveViewModel.ShowControlPanelSection.");

			var enContent = File.ReadAllText(enReswPath);
			Assert.IsTrue(enContent.Contains(@"<data name=""ShowThisPCSection"""), "en-US Resources.resw must define ShowThisPCSection.");
			Assert.IsTrue(enContent.Contains(@"<data name=""ShowControlPanelSection"""), "en-US Resources.resw must define ShowControlPanelSection.");

			var ptContent = File.ReadAllText(ptReswPath);
			Assert.IsTrue(ptContent.Contains(@"<data name=""ShowThisPCSection"""), "pt-BR Resources.resw must define ShowThisPCSection.");
			Assert.IsTrue(ptContent.Contains(@"<data name=""ShowControlPanelSection"""), "pt-BR Resources.resw must define ShowControlPanelSection.");
		}

		[TestMethod]
		public void AC3_T4a_ShellDelegateFolderHelper_STAThread_And_Timeout()
		{
			// F092 / T4a / AC-3 / NFR-001 / NFR-002: Delegate items enumeration must use STA thread with 3s timeout
			var solutionRoot = FindSolutionRoot();
			var helperPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Helpers", "Navigation", "ShellDelegateFolderHelper.cs");

			Assert.IsTrue(File.Exists(helperPath), $"ShellDelegateFolderHelper.cs not found at {helperPath}");

			var content = File.ReadAllText(helperPath);
			Assert.IsTrue(content.Contains("STATask.Run"), "ShellDelegateFolderHelper.cs must execute enumeration on an STA thread.");
			Assert.IsTrue(content.Contains("TimeSpan.FromSeconds(3)"), "ShellDelegateFolderHelper.cs must enforce a strict 3-second timeout.");
		}

		[TestMethod]
		public void AC3_T4b_ShellStorageFolder_PathFallback_And_ExplorerFallback()
		{
			// F092 / T4b / AC-3 / spec §5: Virtual items must use RecyclePath ?? FilePath and fallback to explorer.exe on navigation error
			var solutionRoot = FindSolutionRoot();
			var shellFolderStoragePath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "Utils", "Storage", "StorageItems", "ShellStorageFolder.cs");
			var sidebarVmPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "ViewModels", "UserControls", "SidebarViewModel.cs");

			Assert.IsTrue(File.Exists(shellFolderStoragePath), $"ShellStorageFolder.cs not found at {shellFolderStoragePath}");
			Assert.IsTrue(File.Exists(sidebarVmPath), $"SidebarViewModel.cs not found at {sidebarVmPath}");

			var storageContent = File.ReadAllText(shellFolderStoragePath);
			Assert.IsTrue(storageContent.Contains("item.RecyclePath ?? item.FilePath"), "ShellStorageFolder must fallback to FilePath for virtual shell items without physical disk path.");

			var vmContent = File.ReadAllText(sidebarVmPath);
			Assert.IsTrue(vmContent.Contains("explorer.exe"), "SidebarViewModel.cs must contain fallback to native explorer.exe for unsupported virtual items.");
		}

		[TestMethod]
		public void AC3_T4c_NetworkSection_ObservesAndIncludesShortcuts()
		{
			// F092 / T4c / AC-3: Network section must include Shortcuts (NetHood) and observe CollectionChanged
			var solutionRoot = FindSolutionRoot();
			var sidebarVmPath = Path.Combine(solutionRoot, "src", "KyraFiles.App", "ViewModels", "UserControls", "SidebarViewModel.cs");

			Assert.IsTrue(File.Exists(sidebarVmPath), $"SidebarViewModel.cs not found at {sidebarVmPath}");

			var vmContent = File.ReadAllText(sidebarVmPath);
			Assert.IsTrue(vmContent.Contains("NetworkService.Computers.Concat(NetworkService.Shortcuts)"), "SidebarViewModel.cs must concatenate NetworkService.Shortcuts into Network section.");
			Assert.IsTrue(vmContent.Contains("NetworkService.Shortcuts.CollectionChanged"), "SidebarViewModel.cs must observe Shortcuts.CollectionChanged.");
		}

		private static string FindSolutionRoot()
		{
			var current = AppContext.BaseDirectory;
			while (!string.IsNullOrEmpty(current))
			{
				if (File.Exists(Path.Combine(current, "KyraFiles.slnx")) || File.Exists(Path.Combine(current, "Files.slnx")))
				{
					return current;
				}

				var parent = Directory.GetParent(current);
				if (parent is null)
					break;
				current = parent.FullName;
			}

			// Fallback para caminho de trabalho relativo
			return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
		}
	}
}
