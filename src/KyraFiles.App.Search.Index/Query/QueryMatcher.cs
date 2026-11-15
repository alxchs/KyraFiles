// Copyright (c) Files Community. Licensed under the MIT License.

namespace KyraFiles.App.Search.Index.Query;

/// <summary>
/// Avaliador de expressões de busca por nome com suporte a múltiplas palavras (AND implícito),
/// curingas (* e ?) e correspondência insensível a maiúsculas e acentos.
/// </summary>
public sealed class QueryMatcher
{
	private readonly TermPattern[] _terms;

	/// <summary>
	/// Indica se a consulta está vazia ou contém apenas espaços em branco.
	/// </summary>
	public bool IsEmpty => _terms.Length == 0;

	/// <summary>
	/// Termos normalizados que compõem a consulta.
	/// </summary>
	public IReadOnlyList<string> Terms { get; }

	public QueryMatcher(string? query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			_terms = [];
			Terms = [];
			return;
		}

		var rawTokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var termList = new List<TermPattern>(rawTokens.Length);
		var termStrings = new List<string>(rawTokens.Length);

		foreach (var token in rawTokens)
		{
			var normalized = NameNormalizer.Normalize(token);
			if (normalized.Length == 0)
				continue;

			bool hasWildcard = normalized.Contains('*') || normalized.Contains('?');
			byte[] utf8 = Encoding.UTF8.GetBytes(normalized);
			termList.Add(new TermPattern(normalized, utf8, hasWildcard));
			termStrings.Add(normalized);
		}

		_terms = [.. termList];
		Terms = termStrings;
	}

	/// <summary>
	/// Verifica se o nome bruto fornecido satisfaz a consulta (normaliza o candidato antes de testar).
	/// </summary>
	public bool Matches(string? candidateName)
	{
		if (string.IsNullOrEmpty(candidateName))
			return IsEmpty;

		if (IsEmpty)
			return true;

		var normalized = NameNormalizer.Normalize(candidateName);
		return MatchesNormalized(normalized.AsSpan());
	}

	/// <summary>
	/// Verifica se o nome já normalizado satisfaz a consulta (zero alocações para busca rápida sobre o índice).
	/// </summary>
	public bool MatchesNormalized(ReadOnlySpan<char> normalizedCandidate)
	{
		if (IsEmpty)
			return true;

		for (int i = 0; i < _terms.Length; i++)
		{
			ref readonly var term = ref _terms[i];
			if (term.HasWildcard)
			{
				if (!IsWildcardMatch(normalizedCandidate, term.Pattern.AsSpan()))
					return false;
			}
			else
			{
				if (!normalizedCandidate.Contains(term.Pattern.AsSpan(), StringComparison.Ordinal))
					return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Algoritmo iterativo sem alocações de casamento de padrões com curingas (* e ?).
	/// </summary>
	public static bool IsWildcardMatch(ReadOnlySpan<char> text, ReadOnlySpan<char> pattern)
	{
		int textIdx = 0;
		int patIdx = 0;
		int starIdx = -1;
		int matchIdx = 0;

		while (textIdx < text.Length)
		{
			if (patIdx < pattern.Length && (pattern[patIdx] == '?' || pattern[patIdx] == text[textIdx]))
			{
				textIdx++;
				patIdx++;
			}
			else if (patIdx < pattern.Length && pattern[patIdx] == '*')
			{
				starIdx = patIdx;
				matchIdx = textIdx;
				patIdx++;
			}
			else if (starIdx != -1)
			{
				patIdx = starIdx + 1;
				matchIdx++;
				textIdx = matchIdx;
			}
			else
			{
				return false;
			}
		}

		while (patIdx < pattern.Length && pattern[patIdx] == '*')
		{
			patIdx++;
		}

		return patIdx == pattern.Length;
	}

	/// <summary>
	/// Verifica se o nome já normalizado em bytes UTF-8 satisfaz a consulta (alta performance com SIMD).
	/// </summary>
	public bool MatchesNormalizedUtf8(ReadOnlySpan<byte> normalizedCandidateUtf8)
	{
		if (IsEmpty)
			return true;

		for (int i = 0; i < _terms.Length; i++)
		{
			ref readonly var term = ref _terms[i];
			if (term.HasWildcard)
			{
				if (!IsWildcardMatchUtf8(normalizedCandidateUtf8, term.Utf8Pattern.AsSpan()))
					return false;
			}
			else
			{
				if (normalizedCandidateUtf8.IndexOf(term.Utf8Pattern.AsSpan()) < 0)
					return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Algoritmo iterativo sem alocações de casamento de padrões com curingas (* e ?) sobre bytes UTF-8.
	/// </summary>
	public static bool IsWildcardMatchUtf8(ReadOnlySpan<byte> text, ReadOnlySpan<byte> pattern)
	{
		int textIdx = 0;
		int patIdx = 0;
		int starIdx = -1;
		int matchIdx = 0;

		while (textIdx < text.Length)
		{
			if (patIdx < pattern.Length && (pattern[patIdx] == (byte)'?' || pattern[patIdx] == text[textIdx]))
			{
				textIdx++;
				patIdx++;
			}
			else if (patIdx < pattern.Length && pattern[patIdx] == (byte)'*')
			{
				starIdx = patIdx;
				matchIdx = textIdx;
				patIdx++;
			}
			else if (starIdx != -1)
			{
				patIdx = starIdx + 1;
				matchIdx++;
				textIdx = matchIdx;
			}
			else
			{
				return false;
			}
		}

		while (patIdx < pattern.Length && pattern[patIdx] == (byte)'*')
		{
			patIdx++;
		}

		return patIdx == pattern.Length;
	}

	private readonly struct TermPattern(string pattern, byte[] utf8Pattern, bool hasWildcard)
	{
		public string Pattern { get; } = pattern;
		public byte[] Utf8Pattern { get; } = utf8Pattern;
		public bool HasWildcard { get; } = hasWildcard;
	}
}
