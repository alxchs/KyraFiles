// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using KyraFiles.App.Utils.Storage.Operations;

namespace KyraFiles.App.Helpers
{
	/// <summary>
	/// Helper for executing build folder deletion and detecting file locks (AC-3, AC-4).
	/// </summary>
	public static class BuildFolderCleanerHelper
	{
		/// <summary>
		/// Deletes a build folder, either permanently or to the Recycle Bin.
		/// Detects files in use and returns whether the operation succeeded or was blocked (AC-4).
		/// </summary>
		public static async Task<(bool Succeeded, bool InUse)> DeleteBuildFolderAsync(string folderPath, bool permanently)
		{
			if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
				return (true, false);

			// Check beforehand if any file inside the directory has an exclusive lock
			if (IsDirectoryLocked(folderPath))
			{
				return (false, true);
			}

			if (permanently)
			{
				try
				{
					// Normalize attributes to allow deletion of read-only files
					foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
					{
						try
						{
							File.SetAttributes(file, FileAttributes.Normal);
						}
						catch
						{
						}
					}

					Directory.Delete(folderPath, recursive: true);
					bool stillExists = Directory.Exists(folderPath);
					return (!stillExists, stillExists);
				}
				catch (IOException)
				{
					return (false, true);
				}
				catch (UnauthorizedAccessException)
				{
					return (false, true);
				}
				catch
				{
					return (false, true);
				}
			}
			else
			{
				// Attempt Recycle Bin deletion via ShellFileOperations
				try
				{
					long hwnd = MainWindow.Instance?.WindowHandle.ToInt64() ?? 0;
					var (shellSuccess, _) = await FileOperationsHelpers.DeleteItemAsync([folderPath], permanently: false, hwnd, asAdmin: false, null);

					if (shellSuccess && !Directory.Exists(folderPath))
					{
						return (true, false);
					}
				}
				catch
				{
				}

				// Fallback if Shell deletion was not available (e.g. headless/test environment)
				if (Directory.Exists(folderPath))
				{
					if (IsDirectoryLocked(folderPath))
					{
						return (false, true);
					}

					try
					{
						Directory.Delete(folderPath, true);
						bool stillExists = Directory.Exists(folderPath);
						return (!stillExists, stillExists);
					}
					catch
					{
						return (false, true);
					}
				}

				return (true, false);
			}
		}

		/// <summary>
		/// Probes if the directory or any contained file is currently open with an exclusive lock.
		/// </summary>
		public static bool IsDirectoryLocked(string folderPath)
		{
			try
			{
				if (!Directory.Exists(folderPath))
					return false;

				foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
				{
					try
					{
						using var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
					}
					catch (IOException)
					{
						return true;
					}
					catch (UnauthorizedAccessException)
					{
						return true;
					}
				}
			}
			catch
			{
				return true;
			}

			return false;
		}
	}
}
