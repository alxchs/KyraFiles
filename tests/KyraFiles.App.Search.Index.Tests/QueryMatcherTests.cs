// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.App.Search.Index.Query;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class QueryMatcherTests
{
	[TestMethod]
	public void AC15_BuscaAcao_MatchesExpectedVariants()
	{
		var matcher = new QueryMatcher("acao");

		Assert.IsTrue(matcher.Matches("Ação.docx"));
		Assert.IsTrue(matcher.Matches("AÇÃO.txt"));
		Assert.IsTrue(matcher.Matches("acao.md"));
		Assert.IsTrue(matcher.Matches("minha_nova_ação_final.pdf"));

		Assert.IsFalse(matcher.Matches("outro_documento.txt"));
		Assert.IsFalse(matcher.Matches("act.txt"));
	}

	[TestMethod]
	public void QueryWithAccents_MatchesUnaccentedCandidates()
	{
		var matcher = new QueryMatcher("Ação");

		Assert.IsTrue(matcher.Matches("acao.txt"));
		Assert.IsTrue(matcher.Matches("AÇÃO.TXT"));
		Assert.IsTrue(matcher.Matches("Ação.txt"));
	}

	[TestMethod]
	public void MultipleWords_ImplicitAnd_MatchesInAnyOrder()
	{
		var matcher = new QueryMatcher("relatorio 2026 pdf");

		Assert.IsTrue(matcher.Matches("relatorio_financeiro_2026.pdf"));
		Assert.IsTrue(matcher.Matches("2026_anual_pdf_relatorio.docx"));
		Assert.IsTrue(matcher.Matches("PDF_RELATÓRIO_2026.zip"));

		// Faltando um dos termos obrigatórios
		Assert.IsFalse(matcher.Matches("relatorio_2025.pdf"));
		Assert.IsFalse(matcher.Matches("financeiro_2026.pdf"));
		Assert.IsFalse(matcher.Matches("relatorio_2026.docx"));
	}

	[TestMethod]
	public void Wildcard_ExtensionPattern_MatchesCorrectly()
	{
		var matcher = new QueryMatcher("*.txt");

		Assert.IsTrue(matcher.Matches("arquivo.txt"));
		Assert.IsTrue(matcher.Matches("meu.arquivo.txt"));
		Assert.IsTrue(matcher.Matches(".txt"));

		Assert.IsFalse(matcher.Matches("arquivo.txt.bak"));
		Assert.IsFalse(matcher.Matches("arquivo.docx"));
	}

	[TestMethod]
	public void Wildcard_QuestionMark_MatchesSingleChar()
	{
		var matcher = new QueryMatcher("doc?.txt");

		Assert.IsTrue(matcher.Matches("doc1.txt"));
		Assert.IsTrue(matcher.Matches("docA.txt"));
		Assert.IsTrue(matcher.Matches("doc_.txt"));

		Assert.IsFalse(matcher.Matches("doc.txt"));
		Assert.IsFalse(matcher.Matches("doc12.txt"));
	}

	[TestMethod]
	public void CombinedTerms_WildcardAndSubstring()
	{
		var matcher = new QueryMatcher("*.pdf relatorio");

		Assert.IsTrue(matcher.Matches("meu_relatório_anual.pdf"));
		Assert.IsTrue(matcher.Matches("relatorio.pdf"));

		Assert.IsFalse(matcher.Matches("meu_relatorio.docx"));
		Assert.IsFalse(matcher.Matches("balanco_anual.pdf"));
	}

	[TestMethod]
	public void EmptyQuery_MatchesAnyCandidate()
	{
		var emptyMatcher = new QueryMatcher("");
		Assert.IsTrue(emptyMatcher.IsEmpty);
		Assert.IsTrue(emptyMatcher.Matches("qualquer_arquivo.txt"));

		var whitespaceMatcher = new QueryMatcher("    ");
		Assert.IsTrue(whitespaceMatcher.IsEmpty);
		Assert.IsTrue(whitespaceMatcher.Matches("qualquer_arquivo.txt"));
	}

	[TestMethod]
	public void MatchesNormalized_WithPreNormalizedSpan_MatchesFast()
	{
		var matcher = new QueryMatcher("relatorio");
		string preNormalized = "meu_relatorio_final.txt";

		Assert.IsTrue(matcher.MatchesNormalized(preNormalized.AsSpan()));
	}
}
