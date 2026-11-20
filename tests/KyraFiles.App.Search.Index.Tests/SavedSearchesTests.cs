// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.Shared.SavedSearches;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class SavedSearchesTests
	{
		private string _tempDir = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_tempDir = Path.Combine(
				Path.GetTempPath(),
				"agy",
				Guid.NewGuid().ToString("N"),
				"desenv",
				"utils",
				"KyraFiles",
				"KyraFiles",
				"F042Tests");
			Directory.CreateDirectory(_tempDir);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (Directory.Exists(_tempDir))
				Directory.Delete(_tempDir, recursive: true);
		}

		[TestMethod]
		public void AC1_AC4_AC5_AC7_Store_AddEditReorderRemoveAndPersist()
		{
			var jsonPath = Path.Combine(_tempDir, "saved-searches.json");
			var scopeDir = Path.Combine(_tempDir, "docs");
			Directory.CreateDirectory(scopeDir);
			var sampleFile = Path.Combine(scopeDir, "contrato.pdf");
			File.WriteAllText(sampleFile, "pdf");

			var store = new SavedSearchStore(jsonPath);
			var a = store.Add(new SavedSearchDefinition
			{
				Name = "PDFs da semana",
				Query = "*.pdf",
				ScopePath = scopeDir,
				RelativeDateKind = SavedSearchRelativeDateKind.LastNDays,
				RelativeDaysCount = 7,
			});
			var b = store.Add(new SavedSearchDefinition
			{
				Name = "PDFs da semana", // nome repetido deve ganhar sufixo automatico
				Query = "*.docx",
				ScopePath = scopeDir,
			});

			Assert.AreEqual("PDFs da semana (2)", b.Name);

			// Reordenar (FR-003)
			Assert.IsTrue(store.Reorder(1, 0));
			Assert.AreEqual(b.Id, store.GetAll()[0].Id);

			// Editar (AC-4)
			a.Name = "PDFs editados";
			a.RelativeDaysCount = 14;
			Assert.IsTrue(store.Update(a));

			// Recarregar mantendo ordem (AC-7)
			var reloaded = new SavedSearchStore(jsonPath);
			reloaded.Load();
			Assert.IsNull(reloaded.LastLoadError);
			var items = reloaded.GetAll();
			Assert.AreEqual(2, items.Count);
			Assert.AreEqual(b.Id, items[0].Id);
			Assert.AreEqual("PDFs editados", items[1].Name);
			Assert.AreEqual(14, items[1].RelativeDaysCount);

			// Remover busca salva NAO apaga arquivos do disco (AC-5)
			Assert.IsTrue(reloaded.Remove(a.Id));
			Assert.AreEqual(1, reloaded.GetAll().Count);
			Assert.IsTrue(File.Exists(sampleFile));
		}

		[TestMethod]
		public void AC2_Evaluator_RelativeDateAdvancesWithReferenceClock()
		{
			var def = new SavedSearchDefinition
			{
				Name = "PDFs 7 dias",
				Query = "",
				Extensions = ["pdf"],
				ScopePath = _tempDir,
				RelativeDateKind = SavedSearchRelativeDateKind.LastNDays,
				RelativeDaysCount = 7,
			};

			var day1 = new DateTimeOffset(2026, 11, 18, 12, 0, 0, TimeSpan.FromHours(-3));
			var day2 = day1.AddDays(1);

			var eval1 = SavedSearchQueryEvaluator.Evaluate(def, day1);
			var eval2 = SavedSearchQueryEvaluator.Evaluate(def, day2);

			Assert.IsFalse(eval1.IsScopeMissing);
			Assert.AreEqual("*", eval1.EffectiveQuery);
			Assert.IsNotNull(eval1.Intent?.DateModifiedRange);
			Assert.IsNotNull(eval2.Intent?.DateModifiedRange);
			Assert.AreEqual(TimeSpan.FromDays(1), eval2.Intent!.DateModifiedRange!.From!.Value - eval1.Intent!.DateModifiedRange!.From!.Value);
			CollectionAssert.Contains(eval1.Intent.FileExtensions.ToList(), ".pdf");
		}

		[TestMethod]
		public void AC3_Evaluator_MissingScopeFolder_ReturnsClearWarningWithoutError()
		{
			var missingDir = Path.Combine(_tempDir, "nao-existe-mais");
			var def = new SavedSearchDefinition
			{
				Name = "Pasta removida",
				Query = "*.log",
				ScopePath = missingDir,
			};

			var eval = SavedSearchQueryEvaluator.Evaluate(def, DateTimeOffset.Now);

			Assert.IsTrue(eval.IsScopeMissing);
			Assert.IsFalse(string.IsNullOrWhiteSpace(eval.ScopeWarning));
			Assert.IsNull(eval.Intent);
		}

		[TestMethod]
		public void Store_CorruptedJson_RecoversGracefully()
		{
			var jsonPath = Path.Combine(_tempDir, "bad-saved.json");
			File.WriteAllText(jsonPath, "[[[ invalido");

			var store = new SavedSearchStore(jsonPath);
			store.Load();

			Assert.AreEqual(0, store.GetAll().Count);
			Assert.IsNotNull(store.LastLoadError);
		}
	}
}
