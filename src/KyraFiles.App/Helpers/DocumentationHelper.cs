// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Storage;
using Windows.System;

namespace KyraFiles.App.Helpers
{
	/// <summary>
	/// Helper for opening application documentation with local-first priority and web fallback.
	/// Supports multilingual offline documentation for pt-BR, en-US, es-ES, and de-DE.
	/// </summary>
	public static class DocumentationHelper
	{
		// There is no hosted documentation site; the web fallback is the project repository
		public const string DefaultDocumentationUrl = "https://github.com/alxchs/KyraFiles";

		/// <summary>
		/// Supported language codes.
		/// </summary>
		public static readonly string[] SupportedLanguages = { "pt-BR", "en-US", "es-ES", "de-DE" };

		/// <summary>
		/// Normalizes any language or culture code into one of the supported documentation language codes:
		/// pt-BR, en-US, es-ES, or de-DE.
		/// </summary>
		public static string NormalizeCultureCode(string? cultureName)
		{
			if (string.IsNullOrWhiteSpace(cultureName))
				return "en-US";

			var clean = cultureName.Trim().ToLowerInvariant().Replace('_', '-');
			if (clean.StartsWith("pt"))
				return "pt-BR";
			if (clean.StartsWith("es"))
				return "es-ES";
			if (clean.StartsWith("de"))
				return "de-DE";

			return "en-US";
		}

		/// <summary>
		/// Detects the currently active UI language code in the application.
		/// </summary>
		public static string GetActiveLanguageCode()
		{
			try
			{
				var primary = ApplicationLanguages.PrimaryLanguageOverride;
				if (!string.IsNullOrWhiteSpace(primary))
					return NormalizeCultureCode(primary);
			}
			catch
			{
			}

			try
			{
				var uiCulture = CultureInfo.CurrentUICulture?.Name;
				if (!string.IsNullOrWhiteSpace(uiCulture))
					return NormalizeCultureCode(uiCulture);
			}
			catch
			{
			}

			try
			{
				var culture = CultureInfo.CurrentCulture?.Name;
				if (!string.IsNullOrWhiteSpace(culture))
					return NormalizeCultureCode(culture);
			}
			catch
			{
			}

			return "en-US";
		}

		/// <summary>
		/// Opens documentation, prioritizing local offline HTML files and falling back to the web if not found.
		/// </summary>
		/// <param name="subPath">Relative documentation subpath (e.g. "getting-started/overview.html" or null for root).</param>
		/// <param name="cultureCode">Optional culture code override (e.g. "pt-BR", "en-US", "es-ES", "de-DE").</param>
		public static async Task OpenDocumentationAsync(string? subPath = null, string? cultureCode = null)
		{
			var localPath = ResolveLocalDocumentationFile(subPath, null, cultureCode);
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
			await Launcher.LaunchUriAsync(new Uri(DefaultDocumentationUrl)).AsTask();
		}

		/// <summary>
		/// Resolves the absolute local file path for documentation if present on disk.
		/// </summary>
		/// <param name="subPath">Relative documentation subpath.</param>
		/// <param name="customBaseDir">Optional base directory override (used in unit tests).</param>
		/// <param name="cultureCode">Optional culture code (defaults to active UI culture if null).</param>
		/// <returns>Absolute path if found; otherwise, null.</returns>
		public static string? ResolveLocalDocumentationFile(string? subPath = null, string? customBaseDir = null, string? cultureCode = null)
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
				{
					candidates.Add(Path.Combine(localApp, "Programs", "KyraFiles", "Docs"));
					candidates.Add(Path.Combine(localApp, "Programs", "KyraFiles", "Docs"));
				}
			}

			var lang = NormalizeCultureCode(cultureCode ?? GetActiveLanguageCode());
			var targetName = string.IsNullOrWhiteSpace(subPath)
				? "index.html"
				: subPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

			var hasExplicitLang = false;
			foreach (var supported in SupportedLanguages)
			{
				if (targetName.StartsWith(supported + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
				    targetName.Equals(supported, StringComparison.OrdinalIgnoreCase))
				{
					hasExplicitLang = true;
					break;
				}
			}

			foreach (var root in candidates)
			{
				if (!Directory.Exists(root))
					continue;

				if (hasExplicitLang)
				{
					var explicitPath = Path.Combine(root, targetName);
					if (File.Exists(explicitPath))
						return Path.GetFullPath(explicitPath);

					if (!explicitPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && File.Exists(explicitPath + ".html"))
						return Path.GetFullPath(explicitPath + ".html");
				}
				else
				{
					// 1. Check in active language folder
					var langPath = Path.Combine(root, lang, targetName);
					if (File.Exists(langPath))
						return Path.GetFullPath(langPath);

					if (!langPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && File.Exists(langPath + ".html"))
						return Path.GetFullPath(langPath + ".html");

					// 2. Check directly in root (legacy / unlocalized)
					var candidatePath = Path.Combine(root, targetName);
					if (File.Exists(candidatePath))
						return Path.GetFullPath(candidatePath);

					if (!candidatePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && File.Exists(candidatePath + ".html"))
						return Path.GetFullPath(candidatePath + ".html");

					// 3. Fallback to pt-BR if active wasn't pt-BR
					if (!string.Equals(lang, "pt-BR", StringComparison.OrdinalIgnoreCase))
					{
						var ptPath = Path.Combine(root, "pt-BR", targetName);
						if (File.Exists(ptPath))
							return Path.GetFullPath(ptPath);
						if (!ptPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && File.Exists(ptPath + ".html"))
							return Path.GetFullPath(ptPath + ".html");
					}

					// 4. Fallback to en-US if active wasn't en-US
					if (!string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase))
					{
						var enPath = Path.Combine(root, "en-US", targetName);
						if (File.Exists(enPath))
							return Path.GetFullPath(enPath);
						if (!enPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && File.Exists(enPath + ".html"))
							return Path.GetFullPath(enPath + ".html");
					}

					// 5. Check index.html in active language folder
					var langIndex = Path.Combine(root, lang, "index.html");
					if (File.Exists(langIndex))
						return Path.GetFullPath(langIndex);

					// 6. Check index.html in root
					var rootIndex = Path.Combine(root, "index.html");
					if (File.Exists(rootIndex))
						return Path.GetFullPath(rootIndex);
				}
			}

			return null;
		}

		/// <summary>
		/// Resolves the target URI or path with indicator whether it resolved locally or to web fallback.
		/// </summary>
		public static (bool IsLocal, string Target) ResolveDocumentationTarget(string? subPath = null, string? customBaseDir = null, string? cultureCode = null)
		{
			var local = ResolveLocalDocumentationFile(subPath, customBaseDir, cultureCode);
			if (!string.IsNullOrEmpty(local) && File.Exists(local))
				return (true, local);

			return (false, DefaultDocumentationUrl);
		}
	}
}
