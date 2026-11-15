// Copyright (c) Files Community. Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace KyraFiles.App.Search.Index.Query;

/// <summary>
/// Provedor de normalização de strings para busca rápida insensível a maiúsculas/minúsculas e acentuação (diacríticos).
/// </summary>
public static class NameNormalizer
{
	/// <summary>
	/// Normaliza o texto removendo marcas diacríticas (acentos) e convertendo para minúsculas invariantes.
	/// Suporta formas canônicas compostas (NFC) e decompostas (NFD) de forma equivalente.
	/// </summary>
	/// <param name="text">Texto de entrada.</param>
	/// <returns>Texto em minúsculas e sem acentos.</returns>
	public static string Normalize(string text)
	{
		if (string.IsNullOrEmpty(text))
			return string.Empty;

		// Decompõe caracteres em FormD (ex: 'á' -> 'a' + '\u0301')
		var decomposed = text.Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder(decomposed.Length);

		for (int i = 0; i < decomposed.Length; i++)
		{
			char c = decomposed[i];
			var category = CharUnicodeInfo.GetUnicodeCategory(c);

			if (category != UnicodeCategory.NonSpacingMark)
			{
				sb.Append(char.ToLowerInvariant(c));
			}
		}

		// Recompõe para FormC
		return sb.ToString().Normalize(NormalizationForm.FormC);
	}

	/// <summary>
	/// Normaliza o texto diretamente para bytes UTF-8 (útil para arrays em memória do índice).
	/// </summary>
	public static byte[] NormalizeToUtf8(string text)
	{
		var normalized = Normalize(text);
		return Encoding.UTF8.GetBytes(normalized);
	}
}
