// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;

namespace Files.App.Helpers
{
	/// <summary>
	/// Files Custom (F057): chooses the accelerator letter of each button/option of a dialog from its own (already translated) text.
	/// Pure logic (no UI types) so it is unit tested by linking this file into the test project.
	/// </summary>
	public static class DialogAccessKeys
	{
		/// <summary>
		/// Returns, for each text, the index of the character used as accelerator, or -1 when it has none.
		/// Order of <paramref name="texts"/> is the priority order; no letter is used twice in the same dialog.
		/// </summary>
		public static int[] Assign(IReadOnlyList<string?> texts)
		{
			var result = new int[texts.Count];
			var used = new HashSet<char>();

			for (int i = 0; i < result.Length; i++)
				result[i] = -1;

			// Pass 1: the first letter of the first word, so the usual letter (D for Delete, C for Cancel) wins over a fallback
			for (int i = 0; i < texts.Count; i++)
			{
				var text = texts[i];
				if (string.IsNullOrEmpty(text))
					continue;

				int idx = FirstLetterIndex(text);
				if (idx >= 0 && used.Add(BaseLetter(text[idx])))
					result[i] = idx;
			}

			// Pass 2: texts that lost their first letter take the first free word start, then the first free letter
			for (int i = 0; i < texts.Count; i++)
			{
				var text = texts[i];
				if (result[i] >= 0 || string.IsNullOrEmpty(text))
					continue;

				int chosen = -1;
				for (int pass = 0; pass < 2 && chosen < 0; pass++)
				{
					for (int j = 0; j < text.Length; j++)
					{
						if (!IsLatinLetter(text[j]))
							continue;
						if (pass == 0 && j > 0 && IsLatinLetter(text[j - 1]))
							continue; // pass 0 only looks at word starts

						if (used.Add(BaseLetter(text[j])))
						{
							chosen = j;
							break;
						}
					}
				}

				result[i] = chosen;
			}

			return result;
		}

		/// <summary>Upper-case A-Z letter for a character with its diacritics removed; '\0' when it is not a Latin letter.</summary>
		public static char BaseLetter(char c)
		{
			var decomposed = c.ToString().Normalize(System.Text.NormalizationForm.FormD);
			foreach (var d in decomposed)
			{
				if (CharUnicodeInfo.GetUnicodeCategory(d) == UnicodeCategory.NonSpacingMark)
					continue;

				var upper = char.ToUpperInvariant(d);
				return upper is >= 'A' and <= 'Z' ? upper : '\0';
			}

			return '\0';
		}

		public static bool IsLatinLetter(char c) => BaseLetter(c) != '\0';

		private static int FirstLetterIndex(string text)
		{
			for (int i = 0; i < text.Length; i++)
				if (IsLatinLetter(text[i]))
					return i;

			return -1;
		}
	}
}
