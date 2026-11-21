// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KyraFiles.Shared.CodeSearch
{
	/// <summary>
	/// Matcher for .gitignore patterns supporting comments, wildcards, directory rules, and negation.
	/// </summary>
	public sealed class GitIgnoreMatcher
	{
		private readonly List<GitIgnoreRule> _rules = new();

		private static readonly string[] DefaultIgnorePatterns = new[]
		{
			".git/",
			".vs/",
			".vscode/",
			".idea/",
			"bin/",
			"obj/",
			"node_modules/",
			"*.suo",
			"*.user"
		};

		public GitIgnoreMatcher(bool includeDefaults = true)
		{
			if (includeDefaults)
			{
				foreach (var pattern in DefaultIgnorePatterns)
				{
					AddPattern(pattern);
				}
			}
		}

		public static GitIgnoreMatcher LoadFromDirectory(string rootDirectory, bool includeDefaults = true)
		{
			var matcher = new GitIgnoreMatcher(includeDefaults);
			if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
				return matcher;

			var gitIgnorePath = Path.Combine(rootDirectory, ".gitignore");
			if (File.Exists(gitIgnorePath))
			{
				try
				{
					using var stream = new FileStream(gitIgnorePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
					using var reader = new StreamReader(stream);
					string? line;
					while ((line = reader.ReadLine()) is not null)
					{
						matcher.AddPattern(line);
					}
				}
				catch
				{
					// Best-effort reading; if inaccessible, default rules remain active
				}
			}

			return matcher;
		}

		public static GitIgnoreMatcher FromLines(IEnumerable<string> lines, bool includeDefaults = true)
		{
			var matcher = new GitIgnoreMatcher(includeDefaults);
			if (lines is not null)
			{
				foreach (var line in lines)
				{
					matcher.AddPattern(line);
				}
			}
			return matcher;
		}

		public void AddPattern(string pattern)
		{
			if (string.IsNullOrWhiteSpace(pattern))
				return;

			var trimmed = pattern.Trim();
			if (trimmed.StartsWith("#", StringComparison.Ordinal))
				return; // Comment

			bool isNegated = false;
			if (trimmed.StartsWith("!", StringComparison.Ordinal))
			{
				isNegated = true;
				trimmed = trimmed.Substring(1).Trim();
			}

			if (string.IsNullOrEmpty(trimmed))
				return;

			bool isDirectoryOnly = false;
			if (trimmed.EndsWith("/", StringComparison.Ordinal) || trimmed.EndsWith("\\", StringComparison.Ordinal))
			{
				isDirectoryOnly = true;
				trimmed = trimmed.TrimEnd('/', '\\');
			}

			var (regex, insideDirRegex) = BuildRegexes(trimmed, isDirectoryOnly);
			_rules.Add(new GitIgnoreRule(trimmed, regex, insideDirRegex, isNegated, isDirectoryOnly));
		}

		public bool IsIgnored(string relativePath, bool isDirectory)
		{
			if (string.IsNullOrWhiteSpace(relativePath))
				return false;

			var normalized = relativePath.Replace('\\', '/').Trim('/');
			bool ignored = false;

			foreach (var rule in _rules)
			{
				if (rule.Matches(normalized, isDirectory))
				{
					ignored = !rule.IsNegated;
				}
			}

			return ignored;
		}

		private static (Regex DirectRegex, Regex? InsideDirRegex) BuildRegexes(string glob, bool isDirectoryOnly)
		{
			var normalized = glob.Replace('\\', '/').Trim('/');
			bool matchAnywhere = !normalized.Contains('/');

			var sbDirect = new System.Text.StringBuilder();
			if (matchAnywhere)
			{
				sbDirect.Append(@"(?:^|.*/)");
			}
			else
			{
				sbDirect.Append('^');
			}

			AppendGlobToRegex(sbDirect, normalized);

			Regex? insideDirRegex = null;
			if (isDirectoryOnly)
			{
				var sbInside = new System.Text.StringBuilder(sbDirect.ToString());
				sbInside.Append(@"/.*$");
				insideDirRegex = new Regex(sbInside.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

				sbDirect.Append(@"(?:/.*)?$");
			}
			else
			{
				sbDirect.Append(@"(?:/.*)?$");
			}

			var directRegex = new Regex(sbDirect.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
			return (directRegex, insideDirRegex);
		}

		private static void AppendGlobToRegex(System.Text.StringBuilder sb, string glob)
		{
			for (int i = 0; i < glob.Length; i++)
			{
				char c = glob[i];
				if (c == '*')
				{
					if (i + 1 < glob.Length && glob[i + 1] == '*')
					{
						sb.Append(".*");
						i++;
						if (i + 1 < glob.Length && glob[i + 1] == '/')
						{
							i++; // Skip following slash in **/
						}
					}
					else
					{
						sb.Append("[^/]*");
					}
				}
				else if (c == '?')
				{
					sb.Append("[^/]");
				}
				else if (".+()^$[]{}\\|".IndexOf(c) >= 0)
				{
					sb.Append('\\').Append(c);
				}
				else
				{
					sb.Append(c);
				}
			}
		}

		private sealed class GitIgnoreRule
		{
			public string RawPattern { get; }
			public Regex Regex { get; }
			public Regex? InsideDirRegex { get; }
			public bool IsNegated { get; }
			public bool IsDirectoryOnly { get; }

			public GitIgnoreRule(string rawPattern, Regex regex, Regex? insideDirRegex, bool isNegated, bool isDirectoryOnly)
			{
				RawPattern = rawPattern;
				Regex = regex;
				InsideDirRegex = insideDirRegex;
				IsNegated = isNegated;
				IsDirectoryOnly = isDirectoryOnly;
			}

			public bool Matches(string normalizedPath, bool isDirectory)
			{
				if (IsDirectoryOnly && !isDirectory)
				{
					return InsideDirRegex is not null && InsideDirRegex.IsMatch(normalizedPath);
				}

				return Regex.IsMatch(normalizedPath);
			}
		}
	}
}
