// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KyraFiles.Shared.Helpers
{
	public enum PathFormat
	{
		Windows,
		Wsl,
		Json,
		CSharpLiteral,
		FileUrl,
		RelativeToRepo,
		RelativeToCurrentFolder,
	}

	public static class PathFormatter
	{
		public static string StripExtendedLengthPrefix(string path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
				return @"\\" + path.Substring(8);

			if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
				return path.Substring(4);

			return path;
		}

		public static string Format(string path, PathFormat format, string? repositoryRoot = null, string? currentFolder = null)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));

			var cleanPath = StripExtendedLengthPrefix(path);

			switch (format)
			{
				case PathFormat.Windows:
					return cleanPath;

				case PathFormat.Wsl:
					return FormatWsl(cleanPath);

				case PathFormat.Json:
					return FormatJson(cleanPath);

				case PathFormat.CSharpLiteral:
					return FormatCSharpLiteral(cleanPath);

				case PathFormat.FileUrl:
					return FormatFileUrl(cleanPath);

				case PathFormat.RelativeToRepo:
					return FormatRelativeToRepo(cleanPath, repositoryRoot);

				case PathFormat.RelativeToCurrentFolder:
					return FormatRelativeToCurrentFolder(cleanPath, currentFolder);

				default:
					throw new ArgumentOutOfRangeException(nameof(format), format, null);
			}
		}

		public static string FormatAll(IEnumerable<string> paths, PathFormat format, string? repositoryRoot = null, string? currentFolder = null)
		{
			if (paths is null)
				throw new ArgumentNullException(nameof(paths));

			return string.Join("\n", paths.Select(p => Format(p, format, repositoryRoot, currentFolder)));
		}

		private static string FormatWsl(string path)
		{
			// AC-8: UNC paths in WSL (e.g. \\srv\pasta\a.txt -> //srv/pasta/a.txt)
			if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
			{
				var uncBody = path.TrimStart('\\', '/').Replace('\\', '/');
				return "//" + uncBody;
			}

			// Drive letter: e.g. C:\desenv\x y\a.txt -> /mnt/c/desenv/x y/a.txt
			if (path.Length >= 2 && path[1] == ':' && char.IsLetter(path[0]))
			{
				var drive = char.ToLowerInvariant(path[0]);
				var remainder = path.Substring(2).Replace('\\', '/');
				if (remainder.Length > 0 && remainder[0] != '/')
					remainder = "/" + remainder;

				return $"/mnt/{drive}{remainder}";
			}

			// Fallback: replace backslashes with forward slashes
			return path.Replace('\\', '/');
		}

		private static string FormatJson(string path)
		{
			var sb = new StringBuilder(path.Length + 16);
			sb.Append('"');
			foreach (var ch in path)
			{
				switch (ch)
				{
					case '\\':
						sb.Append(@"\\");
						break;
					case '"':
						sb.Append(@"\""");
						break;
					case '\b':
						sb.Append(@"\b");
						break;
					case '\f':
						sb.Append(@"\f");
						break;
					case '\n':
						sb.Append(@"\n");
						break;
					case '\r':
						sb.Append(@"\r");
						break;
					case '\t':
						sb.Append(@"\t");
						break;
					default:
						sb.Append(ch);
						break;
				}
			}
			sb.Append('"');
			return sb.ToString();
		}

		private static string FormatCSharpLiteral(string path)
		{
			// Verbatim string literal: @"..." with " escaped as ""
			var escaped = path.Replace("\"", "\"\"");
			return $"@\"{escaped}\"";
		}

		private static string FormatFileUrl(string path)
		{
			if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsFile)
				return uri.AbsoluteUri;

			return new Uri(path).AbsoluteUri;
		}

		private static string FormatRelativeToRepo(string path, string? repositoryRoot)
		{
			if (string.IsNullOrWhiteSpace(repositoryRoot))
				throw new InvalidOperationException("Repository root was not provided or path is not inside a git repository.");

			var cleanRoot = StripExtendedLengthPrefix(repositoryRoot);
			var rel = Path.GetRelativePath(cleanRoot, path).Replace('\\', '/');

			if (Path.IsPathRooted(rel) || rel.StartsWith("../", StringComparison.Ordinal) || rel == "..")
				throw new InvalidOperationException($"Path '{path}' is outside the repository root '{repositoryRoot}'.");

			return rel;
		}

		private static string FormatRelativeToCurrentFolder(string path, string? currentFolder)
		{
			if (string.IsNullOrWhiteSpace(currentFolder))
				return Path.GetFileName(path);

			var cleanFolder = StripExtendedLengthPrefix(currentFolder);
			return Path.GetRelativePath(cleanFolder, path).Replace('\\', '/');
		}
	}
}
