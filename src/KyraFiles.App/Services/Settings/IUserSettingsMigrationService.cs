// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Generic;

namespace KyraFiles.App.Services.Settings
{
	/// <summary>
	/// Represents a service responsible for non-destructive settings migration between application identities.
	/// </summary>
	public interface IUserSettingsMigrationService
	{
		/// <summary>
		/// Checks whether migration is needed and safely imports legacy settings into the target local data folder.
		/// </summary>
		/// <param name="targetLocalFolderPath">The root local folder of the current application.</param>
		/// <returns>True if settings were successfully migrated; false if settings already existed or no legacy sources were found.</returns>
		bool EnsureMigrated(string targetLocalFolderPath);

		/// <summary>
		/// Migrates settings files from a specific legacy directory into the target settings directory.
		/// </summary>
		/// <param name="sourceSettingsDirectory">Directory containing legacy settings files.</param>
		/// <param name="targetSettingsDirectory">Target directory where settings should be placed.</param>
		/// <returns>Count of migrated files.</returns>
		int MigrateDirectory(string sourceSettingsDirectory, string targetSettingsDirectory);
	}
}
