// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class TypeAheadMatcherTests
	{
		private sealed record DummyItem(string Name);

		[TestMethod]
		public void IsAcceptedCharacter_RejectsControlCharsAndLeadingSpace()
		{
			Assert.IsFalse(TypeAheadMatcher.IsAcceptedCharacter('\r', ""));
			Assert.IsFalse(TypeAheadMatcher.IsAcceptedCharacter('\n', ""));
			Assert.IsFalse(TypeAheadMatcher.IsAcceptedCharacter('\t', ""));
			Assert.IsFalse(TypeAheadMatcher.IsAcceptedCharacter('\b', "a"));
			Assert.IsFalse(TypeAheadMatcher.IsAcceptedCharacter('\x1B', "a"));
			Assert.IsFalse(TypeAheadMatcher.IsAcceptedCharacter('\x03', "")); // Ctrl+C
			Assert.IsFalse(TypeAheadMatcher.IsAcceptedCharacter(' ', ""));
			Assert.IsTrue(TypeAheadMatcher.IsAcceptedCharacter(' ', "my"));
			Assert.IsTrue(TypeAheadMatcher.IsAcceptedCharacter('a', ""));
			Assert.IsTrue(TypeAheadMatcher.IsAcceptedCharacter('9', ""));
			Assert.IsTrue(TypeAheadMatcher.IsAcceptedCharacter('.', ""));
		}

		[TestMethod]
		public void AppendCharacter_CyclesSingleRepeatedLetter_AndAppendsMultiChar()
		{
			Assert.AreEqual("a", TypeAheadMatcher.AppendCharacter("", 'A'));
			Assert.AreEqual("a", TypeAheadMatcher.AppendCharacter("a", 'a'));
			Assert.AreEqual("ar", TypeAheadMatcher.AppendCharacter("a", 'R'));
			Assert.AreEqual("arq", TypeAheadMatcher.AppendCharacter("ar", 'q'));
		}

		[TestMethod]
		public void FindMatch_MatchesDiacriticsInPortuguese()
		{
			var items = new[]
			{
				new DummyItem("Documentos"),
				new DummyItem("Área de Trabalho"),
				new DummyItem("Vídeos"),
				new DummyItem("Configurações"),
			};

			Assert.AreEqual("Área de Trabalho", TypeAheadMatcher.FindMatch(items, x => x.Name, null, "area")?.Name);
			Assert.AreEqual("Vídeos", TypeAheadMatcher.FindMatch(items, x => x.Name, null, "vid")?.Name);
			Assert.AreEqual("Configurações", TypeAheadMatcher.FindMatch(items, x => x.Name, null, "configuracoes")?.Name);
		}

		[TestMethod]
		public void FindMatch_CyclesAndWrapsAroundOnRepeatedSingleLetter()
		{
			var items = new[]
			{
				new DummyItem("alpha.txt"),
				new DummyItem("apricot.txt"),
				new DummyItem("beta.txt"),
				new DummyItem("avocado.txt"),
			};

			var m1 = TypeAheadMatcher.FindMatch(items, x => x.Name, null, "a");
			Assert.AreEqual("alpha.txt", m1?.Name);

			var m2 = TypeAheadMatcher.FindMatch(items, x => x.Name, m1, "a");
			Assert.AreEqual("apricot.txt", m2?.Name);

			var m3 = TypeAheadMatcher.FindMatch(items, x => x.Name, m2, "a");
			Assert.AreEqual("avocado.txt", m3?.Name);

			var m4 = TypeAheadMatcher.FindMatch(items, x => x.Name, m3, "a");
			Assert.AreEqual("alpha.txt", m4?.Name);
		}

		[TestMethod]
		public void FindMatch_KeepsCurrentItemWhenMultiCharPrefixStillMatches()
		{
			var items = new[]
			{
				new DummyItem("app.json"),
				new DummyItem("apricot.txt"),
			};

			var m1 = TypeAheadMatcher.FindMatch(items, x => x.Name, null, "a");
			Assert.AreEqual("app.json", m1?.Name);

			var m2 = TypeAheadMatcher.FindMatch(items, x => x.Name, m1, "ap");
			Assert.AreEqual("app.json", m2?.Name);

			var m3 = TypeAheadMatcher.FindMatch(items, x => x.Name, m2, "apr");
			Assert.AreEqual("apricot.txt", m3?.Name);
		}
	}
}
