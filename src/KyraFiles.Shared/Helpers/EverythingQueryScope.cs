// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace KyraFiles.Shared.Helpers
{
	/// <summary>
	/// Splits an Everything query into search terms and an optional folder scope written after the mask (F094).
	/// Without a scope the search covers the whole Everything index.
	/// </summary>
	public readonly record struct EverythingQueryScope(string Terms, string? ScopePath, bool HasNativePathSyntax)
	{
		private static readonly string[] NativePathOperators = ["path:", "parent:", "infolder:", "nosubfolders:", "folder:"];

		public static EverythingQueryScope Parse(string? query, Func<string, bool> directoryExists)
		{
			ArgumentNullException.ThrowIfNull(directoryExists);

			var text = query?.Trim() ?? string.Empty;
			if (text.Length == 0)
				return new(string.Empty, null, false);

			foreach (var op in NativePathOperators)
			{
				if (text.Contains(op, StringComparison.OrdinalIgnoreCase))
					return new(text, null, true);
			}

			var tokens = Tokenize(text);
			for (int i = 0; i < tokens.Count; i++)
			{
				if (!LooksLikeAbsolutePath(tokens[i].Value))
					continue;

				var candidate = NormalizePath(text[tokens[i].Start..].Trim());
				if (candidate.Length == 0 || !SafeExists(directoryExists, candidate))
					return new(text, null, false);

				var terms = text[..tokens[i].Start].Trim();
				return new(terms, candidate, false);
			}

			return new(text, null, false);
		}

		/// <summary>Builds a query whose scope survives <see cref="Parse"/>; an empty scope means the whole index.</summary>
		public static string Compose(string? terms, string? scopePath)
		{
			var t = terms?.Trim() ?? string.Empty;
			var s = scopePath?.Trim() ?? string.Empty;
			if (s.Length == 0 || string.Equals(s, "Home", StringComparison.OrdinalIgnoreCase))
				return t;

			var quoted = $"\"{s}\"";
			return t.Length == 0 ? quoted : $"{t} {quoted}";
		}

		private static bool SafeExists(Func<string, bool> directoryExists, string path)
		{
			try
			{
				return directoryExists(path);
			}
			catch
			{
				return false;
			}
		}

		private static bool LooksLikeAbsolutePath(string token)
		{
			if (token.StartsWith(@"\\", StringComparison.Ordinal))
				return token.Length > 2;

			return token.Length >= 3
				&& char.IsAsciiLetter(token[0])
				&& token[1] == ':'
				&& (token[2] == '\\' || token[2] == '/');
		}

		private static string NormalizePath(string raw)
		{
			var path = raw.Replace("\"", string.Empty, StringComparison.Ordinal).Trim().Replace('/', '\\');
			var isUnc = path.StartsWith(@"\\", StringComparison.Ordinal);
			var trimmed = path.TrimEnd('\\');

			// Keep "C:\" as a drive root instead of collapsing it to "C:".
			if (trimmed.Length == 2 && trimmed[1] == ':')
				return trimmed + "\\";

			return isUnc && trimmed.Length <= 2 ? string.Empty : trimmed;
		}

		private static List<(int Start, string Value)> Tokenize(string text)
		{
			var tokens = new List<(int Start, string Value)>();
			var current = new StringBuilder();
			int start = -1;
			bool inQuotes = false;

			for (int i = 0; i < text.Length; i++)
			{
				var c = text[i];
				if (c == '"')
				{
					inQuotes = !inQuotes;
					if (start < 0)
						start = i;
					continue;
				}

				if (char.IsWhiteSpace(c) && !inQuotes)
				{
					if (start >= 0)
					{
						tokens.Add((start, current.ToString()));
						current.Clear();
						start = -1;
					}
					continue;
				}

				if (start < 0)
					start = i;
				current.Append(c);
			}

			if (start >= 0)
				tokens.Add((start, current.ToString()));

			return tokens;
		}
	}
}
