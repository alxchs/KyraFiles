// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Files.App.Services.Settings
{
	/// <summary>
	/// Non-destructive settings migration service that imports configurations, layouts, and tags
	/// from legacy installations (Files Custom, Files Dev, Files Community) to KyraFiles.
	/// </summary>
	public sealed class UserSettingsMigrationService : IUserSettingsMigrationService
	{
		private const string SettingsFolderName = "settings";
		private const string UserSettingsFileName = "user_settings.json";

		/// <summary>
		/// Default singleton instance for direct lifecycle calls.
		/// </summary>
		public static UserSettingsMigrationService Instance { get; } = new();

		/// <inheritdoc/>
		public bool EnsureMigrated(string targetLocalFolderPath)
		{
			if (string.IsNullOrWhiteSpace(targetLocalFolderPath))
				return false;

			var targetSettingsDir = Path.Combine(targetLocalFolderPath, SettingsFolderName);
			var targetSettingsFile = Path.Combine(targetSettingsDir, UserSettingsFileName);

			// If target already has settings, no migration needed
			if (File.Exists(targetSettingsFile))
				return false;

			var candidateDirectories = DiscoverLegacySettingsDirectories(targetSettingsDir);
			foreach (var candidateDir in candidateDirectories)
			{
				var candidateFile = Path.Combine(candidateDir, UserSettingsFileName);
				if (File.Exists(candidateFile))
				{
					var migratedCount = MigrateDirectory(candidateDir, targetSettingsDir);
					if (migratedCount > 0)
						return true;
				}
			}

			return false;
		}

		/// <inheritdoc/>
		public int MigrateDirectory(string sourceSettingsDirectory, string targetSettingsDirectory)
		{
			if (string.IsNullOrWhiteSpace(sourceSettingsDirectory) ||
				string.IsNullOrWhiteSpace(targetSettingsDirectory) ||
				!Directory.Exists(sourceSettingsDirectory))
			{
				return 0;
			}

			// Don't copy to itself
			if (string.Equals(Path.GetFullPath(sourceSettingsDirectory), Path.GetFullPath(targetSettingsDirectory), StringComparison.OrdinalIgnoreCase))
				return 0;

			Directory.CreateDirectory(targetSettingsDirectory);

			var count = 0;
			var files = Directory.GetFiles(sourceSettingsDirectory, "*.*", SearchOption.TopDirectoryOnly);
			foreach (var file in files)
			{
				var fileName = Path.GetFileName(file);
				var targetFile = Path.Combine(targetSettingsDirectory, fileName);

				// Non-destructive: do not overwrite existing files in destination
				if (!File.Exists(targetFile))
				{
					try
					{
						File.Copy(file, targetFile, overwrite: false);
						count++;
					}
					catch
					{
						// Skip files that cannot be copied (e.g. locks)
					}
				}
			}

			return count;
		}

		/// <summary>
		/// Discovers plausible legacy directories containing previous user configuration.
		/// </summary>
		public static IEnumerable<string> DiscoverLegacySettingsDirectories(string currentTargetSettingsDir)
		{
			var list = new List<string>();

			try
			{
				var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				if (!string.IsNullOrWhiteSpace(localAppData))
				{
					// 1. Unpackaged legacy folders
					list.Add(Path.Combine(localAppData, "FilesCustom", SettingsFolderName));
					list.Add(Path.Combine(localAppData, "Files", SettingsFolderName));

					// 2. Packaged UWP/WinUI app folders under %LOCALAPPDATA%\Packages
					var packagesDir = Path.Combine(localAppData, "Packages");
					if (Directory.Exists(packagesDir))
					{
						var packageDirs = Directory.GetDirectories(packagesDir, "*Files*");
						foreach (var pkg in packageDirs)
						{
							var pkgName = Path.GetFileName(pkg);
							if (pkgName.StartsWith("FilesDev_", StringComparison.OrdinalIgnoreCase) ||
								pkgName.StartsWith("49306TheFilesOrganization.Files_", StringComparison.OrdinalIgnoreCase) ||
								pkgName.StartsWith("FilesCustom_", StringComparison.OrdinalIgnoreCase))
							{
								list.Add(Path.Combine(pkg, "LocalState", SettingsFolderName));
							}
						}
					}
				}
			}
			catch
			{
			}

			return list
				.Where(dir => !string.Equals(Path.GetFullPath(dir), Path.GetFullPath(currentTargetSettingsDir), StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase);
		}
	}
}
