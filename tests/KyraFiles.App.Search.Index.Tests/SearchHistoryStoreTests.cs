// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using KyraFiles.Shared.SearchHistory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class SearchHistoryStoreTests
	{
		private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.FromHours(-3));

		private static SearchHistoryEntry E(string q, string scope = "", string? engine = "everything")
			=> new() { Query = q, ScopePath = scope, EngineId = engine, LastRunAt = Now };

		[TestMethod]
		public void AC9_TodasAsBuscasMaisRecentePrimeiro()
		{
			var store = new SearchHistoryStore();
			store.Record(E("A"));
			store.Record(E("B"));
			store.Record(E("C"));

			CollectionAssert.AreEqual(new[] { "C", "B", "A" }, store.GetAll().Select(x => x.Query).ToArray());
		}

		[TestMethod]
		public void AC10_RepetirSobeParaOTopoSemDuplicar()
		{
			var store = new SearchHistoryStore();
			store.Record(E("A"));
			store.Record(E("B"));
			store.Record(E("C"));
			store.Record(E("b"));

			CollectionAssert.AreEqual(new[] { "b", "C", "A" }, store.GetAll().Select(x => x.Query).ToArray());
		}

		[TestMethod]
		public void FR014_MesmoTextoComEscopoDiferente_SaoBuscasDistintas()
		{
			var store = new SearchHistoryStore();
			store.Record(E("*.cs", @"C:\a", "native"));
			store.Record(E("*.cs", @"C:\b", "native"));
			store.Record(E("*.cs", @"c:\a\", "native"));

			Assert.AreEqual(2, store.GetAll().Count);
			Assert.AreEqual(@"c:\a", store.GetAll()[0].ScopePath);
		}

		[TestMethod]
		public void AC17_GuardaAs20MaisRecentes()
		{
			var store = new SearchHistoryStore();
			for (int i = 1; i <= 25; i++)
				store.Record(E($"q{i}"));

			var all = store.GetAll();
			Assert.AreEqual(SearchHistoryStore.MaxEntries, all.Count);
			Assert.AreEqual("q25", all[0].Query);
			Assert.AreEqual("q6", all[^1].Query);
		}

		[TestMethod]
		public void BuscaVaziaNaoEntra()
		{
			var store = new SearchHistoryStore();
			Assert.IsFalse(store.Record(E("   ")));
			Assert.AreEqual(0, store.GetAll().Count);
		}

		[TestMethod]
		public void AC11_AC13_PersisteRemoveELimpa()
		{
			var dir = Path.Combine(Path.GetTempPath(), "claude", Guid.NewGuid().ToString("N"));
			var file = Path.Combine(dir, "search-history.json");
			try
			{
				var store = new SearchHistoryStore(file);
				store.Load();
				Assert.IsTrue(store.IsNew);
				store.Record(E("A", @"C:\x", "native"));
				store.Record(E("B"));
				store.Record(E("C"));

				var reopened = new SearchHistoryStore(file);
				reopened.Load();
				Assert.IsFalse(reopened.IsNew);
				CollectionAssert.AreEqual(new[] { "C", "B", "A" }, reopened.GetAll().Select(x => x.Query).ToArray());
				Assert.AreEqual(@"C:\x", reopened.GetAll()[2].ScopePath);
				Assert.AreEqual("native", reopened.GetAll()[2].EngineId);

				reopened.Remove(E("B"));
				var afterRemove = new SearchHistoryStore(file);
				afterRemove.Load();
				CollectionAssert.AreEqual(new[] { "C", "A" }, afterRemove.GetAll().Select(x => x.Query).ToArray());

				afterRemove.Clear();
				var afterClear = new SearchHistoryStore(file);
				afterClear.Load();
				Assert.AreEqual(0, afterClear.GetAll().Count);
				Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length, "Nenhum temporário pode sobrar.");
			}
			finally
			{
				if (Directory.Exists(dir))
					Directory.Delete(dir, true);
			}
		}

		[TestMethod]
		public void ImportaHistoricoAntigoUmaVezSemDuplicar()
		{
			var store = new SearchHistoryStore();
			store.ImportLegacy(["novo", "velho", "NOVO", " "], Now);

			CollectionAssert.AreEqual(new[] { "novo", "velho" }, store.GetAll().Select(x => x.Query).ToArray());
			Assert.IsNull(store.GetAll()[0].EngineId);
			Assert.AreEqual(string.Empty, store.GetAll()[0].ScopePath);
		}
	}
}
