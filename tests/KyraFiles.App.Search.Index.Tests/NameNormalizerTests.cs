// Copyright (c) Files Community. Licensed under the MIT License.

using System.Text;
using KyraFiles.App.Search.Index.Query;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class NameNormalizerTests
{
	[TestMethod]
	public void Normalize_Acao_AllCasesAndAccents_BecomeAcao()
	{
		Assert.AreEqual("acao", NameNormalizer.Normalize("Ação"));
		Assert.AreEqual("acao", NameNormalizer.Normalize("AÇÃO"));
		Assert.AreEqual("acao", NameNormalizer.Normalize("ação"));
		Assert.AreEqual("acao", NameNormalizer.Normalize("acao"));
		Assert.AreEqual("acao", NameNormalizer.Normalize("ACAO"));
	}

	[TestMethod]
	public void Normalize_NfcVsNfd_AreEquivalent()
	{
		// Forma NFC: 'ç' e 'ã' como caracteres pré-compostos únicos
		string nfc = "A\u00E7\u00E3o"; // "Ação"

		// Forma NFD: 'c' + cedilha combinante (\u0327), 'a' + til combinante (\u0303)
		string nfd = "Ac\u0327a\u0303o";

		Assert.AreNotEqual(nfc, nfd, "Precondition: NFC and NFD strings have different code points");
		Assert.AreEqual("acao", NameNormalizer.Normalize(nfc));
		Assert.AreEqual("acao", NameNormalizer.Normalize(nfd));
		Assert.AreEqual(NameNormalizer.Normalize(nfc), NameNormalizer.Normalize(nfd));
	}

	[TestMethod]
	public void Normalize_VariousAccents_RemovesDiacritics()
	{
		string input = "áéíóú àèìòù âêîôû äëïöü ãõ ñ";
		string expected = "aeiou aeiou aeiou aeiou ao n";

		Assert.AreEqual(expected, NameNormalizer.Normalize(input));
	}

	[TestMethod]
	public void Normalize_EmojiAndPunctuation_PreservedWithLowercase()
	{
		Assert.AreEqual("📁 pasta 2026!", NameNormalizer.Normalize("📁 Pasta 2026!"));
		Assert.AreEqual("rocket 🚀 launch", NameNormalizer.Normalize("ROCKET 🚀 LAUNCH"));
	}

	[TestMethod]
	public void Normalize_EmptyOrNull_ReturnsEmpty()
	{
		Assert.AreEqual(string.Empty, NameNormalizer.Normalize(string.Empty));
		Assert.AreEqual(string.Empty, NameNormalizer.Normalize(null!));
	}

	[TestMethod]
	public void NormalizeToUtf8_ReturnsNormalizedBytes()
	{
		byte[] bytes = NameNormalizer.NormalizeToUtf8("AÇÃO");
		string decoded = Encoding.UTF8.GetString(bytes);

		Assert.AreEqual("acao", decoded);
	}
}
