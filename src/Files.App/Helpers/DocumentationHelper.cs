// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;

namespace Files.App.Helpers
{
	/// <summary>
	/// Helper for opening application documentation with local-first priority and web fallback.
	/// </summary>
	public static class DocumentationHelper
	{
		public const string DefaultDocumentationUrl = "https://files.community/docs";

		/// <summary>
		/// Opens documentation, prioritizing local offline HTML files and falling back to the web if not found.
		/// </summary>
		/// <param name="subPath">Relative documentation subpath (e.g. "getting-started/overview.html" or null for root).</param>
		public static async Task OpenDocumentationAsync(string? subPath = null)
		{
			var localPath = ResolveLocalDocumentationFile(subPath);
			if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
			{
				try
				{
					var file = await StorageFile.GetFileFromPathAsync(localPath);
					if (await Launcher.LaunchFileAsync(file))
						return;
				}
				catch
				{
					// Fallback to launching via ProcessStartInfo
					try
					{
						var psi = new ProcessStartInfo
						{
							FileName = localPath,
							UseShellExecute = true,
						};
						Process.Start(psi);
						return;
					}
					catch
					{
						// Fallback to web below
					}
				}
			}

			// Web fallback
			var baseUrl = DefaultDocumentationUrl.TrimEnd('/');
			var webUrl = string.IsNullOrWhiteSpace(subPath)
				? baseUrl
				: $"{baseUrl}/{subPath.TrimStart('/').Replace(".html", string.Empty, StringComparison.OrdinalIgnoreCase)}";

			await Launcher.LaunchUriAsync(new Uri(webUrl)).AsTask();
		}

		/// <summary>
		/// Resolves the absolute local file path for documentation if present on disk.
		/// </summary>
		/// <param name="subPath">Relative documentation subpath.</param>
		/// <param name="customBaseDir">Optional base directory override (used in unit tests).</param>
		/// <returns>Absolute path if found; otherwise, null.</returns>
		public static string? ResolveLocalDocumentationFile(string? subPath = null, string? customBaseDir = null)
		{
			var candidates = new List<string>();

			if (!string.IsNullOrWhiteSpace(customBaseDir))
			{
				candidates.Add(Path.Combine(customBaseDir, "Docs"));
			}
			else
			{
				var baseDir = AppContext.BaseDirectory;
				if (!string.IsNullOrWhiteSpace(baseDir))
					candidates.Add(Path.Combine(baseDir, "Docs"));

				try
				{
					var pkgDir = Package.Current?.InstalledLocation?.Path;
					if (!string.IsNullOrWhiteSpace(pkgDir))
						candidates.Add(Path.Combine(pkgDir, "Docs"));
				}
				catch
				{
					// Package.Current may throw outside of packaged context
				}

				var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				if (!string.IsNullOrWhiteSpace(localApp))
					candidates.Add(Path.Combine(localApp, "Programs", "FilesCustom", "Docs"));
			}

			var targetName = string.IsNullOrWhiteSpace(subPath)
				? "index.html"
				: subPath.Replace('/', Path.DirectorySeparatorChar);

			foreach (var root in candidates)
			{
				var candidatePath = Path.Combine(root, targetName);
				if (File.Exists(candidatePath))
					return Path.GetFullPath(candidatePath);

				// Fallback to index.html in the same root if subPath is not found
				var indexPath = Path.Combine(root, "index.html");
				if (File.Exists(indexPath))
					return Path.GetFullPath(indexPath);
			}

			return null;
		}

		/// <summary>
		/// Resolves the target URI or path with indicator whether it resolved locally or to web fallback.
		/// </summary>
		public static (bool IsLocal, string Target) ResolveDocumentationTarget(string? subPath = null, string? customBaseDir = null)
		{
			var local = ResolveLocalDocumentationFile(subPath, customBaseDir);
			if (!string.IsNullOrEmpty(local) && File.Exists(local))
				return (true, local);

			var baseUrl = DefaultDocumentationUrl.TrimEnd('/');
			var cleanSub = string.IsNullOrWhiteSpace(subPath)
				? string.Empty
				: subPath.TrimStart('/').Replace(".html", string.Empty, StringComparison.OrdinalIgnoreCase);

			var web = string.IsNullOrEmpty(cleanSub)
				? baseUrl
				: $"{baseUrl}/{cleanSub}";

			return (false, web);
		}
	}
}
