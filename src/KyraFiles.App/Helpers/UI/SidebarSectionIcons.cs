// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using KyraFiles.App.Data.Contracts;
using Windows.UI.ViewManagement;

namespace KyraFiles.App.Helpers
{
	public static class SidebarSectionIcons
	{
		private const string BasePath = "ms-appx:///Assets/FluentIcons/SidebarSections/";

		private static readonly AccessibilitySettings AccessibilitySettings = new();
		private static readonly UISettings UISettings = new();

		public static string? For(SectionType section) => section switch
		{
			SectionType.Home => Resolve("Home"),
			SectionType.Pinned => Resolve("Pinned"),
			SectionType.Library => Resolve("Libraries"),
			SectionType.ThisPC => Resolve("Drives"),
			SectionType.ControlPanel => Resolve("Drives"),
			SectionType.Drives => Resolve("Drives"),
			SectionType.CloudDrives => Resolve("CloudDrives"),
			SectionType.Network => Resolve("Network"),
			SectionType.WSL => Resolve("Wsl"),
			SectionType.FileTag => Resolve("Tags"),
			_ => null
		};

		private static string Resolve(string name)
		{
			if (!AccessibilitySettings.HighContrast)
				return $"{BasePath}{name}.png";

			var bg = UISettings.GetColorValue(UIColorType.Background);
			var suffix = bg.R + bg.G + bg.B < 384 ? "Black" : "White";
			return $"{BasePath}{name}-{suffix}.png";
		}
	}
}
