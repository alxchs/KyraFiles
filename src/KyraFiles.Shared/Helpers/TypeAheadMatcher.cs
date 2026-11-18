// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace KyraFiles.Shared.Helpers
{
	/// <summary>
	/// Diacritic-insensitive, Explorer-style type-ahead prefix matcher for file and folder lists.
	/// </summary>
	public static class TypeAheadMatcher
	{
		private static readonly CompareInfo InvariantCompare = CultureInfo.InvariantCulture.CompareInfo;
		private const CompareOptions PrefixOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

		/// <summary>
		/// Returns true if the typed character should contribute to the type-ahead jump query.
		/// Rejects control characters, surrogates, and leading whitespace (preserving Spacebar Quick Look).
		/// </summary>
		public static bool IsAcceptedCharacter(char character, string currentQuery)
		{
			if (char.IsControl(character) || char.IsSurrogate(character))
				return false;

			if (char.IsWhiteSpace(character) && string.IsNullOrEmpty(currentQuery))
				return false;

			return true;
		}

		/// <summary>
		/// Appends a character to the jump query, keeping single-character cycling when the same letter is repeated.
		/// </summary>
		public static string AppendCharacter(string currentQuery, char character)
		{
			var normalized = char.ToLowerInvariant(character).ToString();
			if (currentQuery.Length == 1 && string.Equals(currentQuery, normalized, StringComparison.Ordinal))
				return currentQuery;

			return currentQuery + normalized;
		}

		/// <summary>
		/// Finds the next item whose name starts with <paramref name="query"/> (case- and diacritic-insensitive).
		/// When a single character is repeated, cycles to the next matching item after <paramref name="currentlySelected"/>
		/// with wrap-around.
		/// </summary>
		public static T? FindMatch<T>(
			IReadOnlyList<T> items,
			Func<T, string?> nameSelector,
			T? currentlySelected,
			string query) where T : class
		{
			if (items.Count == 0 || string.IsNullOrEmpty(query))
				return null;

			int startIndex = 0;
			if (currentlySelected is not null)
			{
				for (int i = 0; i < items.Count; i++)
				{
					if (ReferenceEquals(items[i], currentlySelected))
					{
						startIndex = query.Length == 1 ? i + 1 : i;
						break;
					}
				}
			}

			for (int offset = 0; offset < items.Count; offset++)
			{
				int idx = (startIndex + offset) % items.Count;
				var candidate = items[idx];
				var name = nameSelector(candidate);
				if (!string.IsNullOrEmpty(name) && InvariantCompare.IsPrefix(name, query, PrefixOptions))
					return candidate;
			}

			return null;
		}
	}
}
