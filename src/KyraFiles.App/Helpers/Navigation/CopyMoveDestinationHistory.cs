// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace KyraFiles.App.Helpers
{
	/// <summary>
	/// F033: the last folders chosen as "Copy to"/"Move to" destination, most recent first, shared by both actions
	/// and kept between sessions.
	/// </summary>
	public static class CopyMoveDestinationHistory
	{
		public const int MaxCount = 12;

		private static IGeneralSettingsService GeneralSettingsService
			=> Ioc.Default.GetRequiredService<IUserSettingsService>().GeneralSettingsService;

		/// <summary>
		/// The remembered folders that still exist. Network folders are listed without a check, because probing an
		/// offline share would stall the context menu.
		/// </summary>
		public static IReadOnlyList<string> GetAvailable()
		{
			return (GeneralSettingsService.RecentCopyMoveDestinations ?? [])
				.Where(path => IsNetworkPath(path) || SafetyExtensions.IgnoreExceptions(() => Directory.Exists(path)))
				.ToList();
		}

		/// <summary>
		/// Puts the folder on top, without repeating it, keeping at most <see cref="MaxCount"/> entries.
		/// </summary>
		public static void Add(string folder)
		{
			var list = (GeneralSettingsService.RecentCopyMoveDestinations ?? [])
				.Where(path => !string.Equals(path, folder, StringComparison.OrdinalIgnoreCase))
				.Prepend(folder)
				.Take(MaxCount)
				.ToList();

			GeneralSettingsService.RecentCopyMoveDestinations = list;
		}

		private static bool IsNetworkPath(string path)
		{
			if (path.StartsWith(@"\\", StringComparison.Ordinal))
				return true;

			var root = Path.GetPathRoot(path);
			return !string.IsNullOrEmpty(root)
				&& SafetyExtensions.IgnoreExceptions(() => new DriveInfo(root).DriveType == System.IO.DriveType.Network);
		}
	}
}
