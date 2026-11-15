// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;

namespace KyraFiles.Shared.Helpers
{
	/// <summary>
	/// Helper for resolving and launching the modern PowerShell (PowerShell 7+ / pwsh.exe) with fallback to Windows PowerShell.
	/// </summary>
	public static class PowerShellHelpers
	{
		private static string? _cachedExecutable;
		private static readonly object _lock = new();

		/// <summary>
		/// Gets the preferred PowerShell executable path or binary name.
		/// Prefers modern PowerShell (<c>pwsh.exe</c>) if installed, falling back to <c>powershell.exe</c>.
		/// </summary>
		public static string PreferredExecutable
		{
			get
			{
				if (_cachedExecutable is not null)
					return _cachedExecutable;

				lock (_lock)
				{
					return _cachedExecutable ??= ResolvePreferredPowerShell();
				}
			}
		}

		/// <summary>
		/// Resets the cached executable path (primarily for unit tests).
		/// </summary>
		public static void ResetCache()
		{
			lock (_lock)
			{
				_cachedExecutable = null;
			}
		}

		private static string ResolvePreferredPowerShell()
		{
			try
			{
				// 1. Check well-known installation paths for PowerShell 7+
				var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				if (!string.IsNullOrEmpty(programFiles))
				{
					var pwsh7 = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");
					if (File.Exists(pwsh7))
						return pwsh7;

					var pwshPreview = Path.Combine(programFiles, "PowerShell", "7-preview", "pwsh.exe");
					if (File.Exists(pwshPreview))
						return pwshPreview;
				}

				var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				if (!string.IsNullOrEmpty(localAppData))
				{
					var storePwsh = Path.Combine(localAppData, "Microsoft", "WindowsApps", "pwsh.exe");
					if (File.Exists(storePwsh))
						return storePwsh;
				}

				var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
				if (!string.IsNullOrEmpty(programFilesX86))
				{
					var pwsh7X86 = Path.Combine(programFilesX86, "PowerShell", "7", "pwsh.exe");
					if (File.Exists(pwsh7X86))
						return pwsh7X86;
				}

				// 2. Check PATH environment variable for pwsh.exe
				var pathEnv = Environment.GetEnvironmentVariable("PATH");
				if (!string.IsNullOrEmpty(pathEnv))
				{
					foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
					{
						try
						{
							var candidate = Path.Combine(dir, "pwsh.exe");
							if (File.Exists(candidate))
								return candidate;
						}
						catch
						{
							// Ignore invalid paths in PATH
						}
					}
				}
			}
			catch
			{
				// Fallback on any unexpected error
			}

			// Fallback to legacy Windows PowerShell
			return "powershell.exe";
		}
	}
}
