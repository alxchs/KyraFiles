// Copyright (c) Files Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Files.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.App.Search.Index.Tests
{
	[TestClass]
	public sealed class NaturalSearchPostFilterTests
	{
		private sealed record TestItem(string Name, long Size, DateTimeOffset Modified, DateTimeOffset Created);

		private static readonly DateTimeOffset ReferenceDate = new(2026, 10, 27, 12, 0, 0, TimeSpan.FromHours(-3));

		[TestMethod]
		public void M5_AC1_PostFilter_Extensao_E_Modificado_AplicaFiltros()
		{
			// M5-AC-1: "todos os arquivos ini salvos nos últimos 12 dias"
			var query = "todos os arquivos ini salvos nos últimos 12 dias";
			var intent = NaturalSearchParser.Parse(query, ReferenceDate);
			var criteria = NaturalSearchFilterCriteria.FromIntent(intent);

			var items = new List<TestItem>
			{
				new("settings.ini", 1024, ReferenceDate.AddDays(-5), ReferenceDate.AddDays(-30)), // Match: .ini, modified 5 days ago
				new("notes.txt", 1024, ReferenceDate.AddDays(-5), ReferenceDate.AddDays(-30)),     // Fail: not .ini
				new("old.ini", 1024, ReferenceDate.AddDays(-20), ReferenceDate.AddDays(-30)),     // Fail: modified 20 days ago (> 12)
				new("app.exe", 50000, ReferenceDate.AddDays(-1), ReferenceDate.AddDays(-10))      // Fail: not .ini
			};

			var filtered = NaturalSearchPostFilter.FilterItems(
				items,
				criteria,
				x => x.Name,
				x => x.Size,
				x => x.Modified,
				x => x.Created);

			Assert.AreEqual(1, filtered.Count);
			Assert.AreEqual("settings.ini", filtered[0].Name);
		}

		[TestMethod]
		public void M5_AC2_PostFilter_RemoverChipExtensao_RefiltraSemExtensao()
		{
			// M5-AC-2: Remover chip refiltra a lista na hora, sem nova varredura
			var query = "todos os arquivos ini salvos nos últimos 12 dias";
			var intent = NaturalSearchParser.Parse(query, ReferenceDate);
			var criteriaInitial = NaturalSearchFilterCriteria.FromIntent(intent);

			var items = new List<TestItem>
			{
				new("settings.ini", 1024, ReferenceDate.AddDays(-5), ReferenceDate.AddDays(-30)),
				new("notes.txt", 2048, ReferenceDate.AddDays(-3), ReferenceDate.AddDays(-30)),
				new("old.ini", 1024, ReferenceDate.AddDays(-20), ReferenceDate.AddDays(-30)),
				new("recent.log", 4096, ReferenceDate.AddDays(-1), ReferenceDate.AddDays(-10))
			};

			// Initial filter: only settings.ini passes (.ini and modified in last 12 days)
			var initialFiltered = NaturalSearchPostFilter.FilterItems(
				items,
				criteriaInitial,
				x => x.Name,
				x => x.Size,
				x => x.Modified,
				x => x.Created);

			Assert.AreEqual(1, initialFiltered.Count);
			Assert.AreEqual("settings.ini", initialFiltered[0].Name);

			// User removes Extension chip: FileExtensions becomes null
			var criteriaWithoutExtension = criteriaInitial with { FileExtensions = null };

			var refiltered = NaturalSearchPostFilter.FilterItems(
				items,
				criteriaWithoutExtension,
				x => x.Name,
				x => x.Size,
				x => x.Modified,
				x => x.Created);

			// Now settings.ini, notes.txt, and recent.log all pass because all 3 were modified in last 12 days
			Assert.AreEqual(3, refiltered.Count);
			CollectionAssert.AreEquivalent(
				new[] { "settings.ini", "notes.txt", "recent.log" },
				refiltered.Select(x => x.Name).ToArray());
		}

		[TestMethod]
		public void M5_AC2_PostFilter_EditarChipExtensao_UsaNovoValor()
		{
			// M5-AC-2: edito um chip -> a busca usa o valor editado
			var query = "todos os arquivos ini salvos nos últimos 12 dias";
			var intent = NaturalSearchParser.Parse(query, ReferenceDate);
			var criteriaInitial = NaturalSearchFilterCriteria.FromIntent(intent);

			var items = new List<TestItem>
			{
				new("settings.ini", 1024, ReferenceDate.AddDays(-5), ReferenceDate.AddDays(-30)),
				new("notes.txt", 2048, ReferenceDate.AddDays(-3), ReferenceDate.AddDays(-30)),
				new("old.txt", 1024, ReferenceDate.AddDays(-20), ReferenceDate.AddDays(-30))
			};

			// User edits chip from .ini to .txt
			var criteriaEdited = criteriaInitial with { FileExtensions = new[] { ".txt" } };

			var refiltered = NaturalSearchPostFilter.FilterItems(
				items,
				criteriaEdited,
				x => x.Name,
				x => x.Size,
				x => x.Modified,
				x => x.Created);

			Assert.AreEqual(1, refiltered.Count);
			Assert.AreEqual("notes.txt", refiltered[0].Name);
		}

		[TestMethod]
		public void M5_AC4_PostFilter_Tamanho_E_DataCriacao()
		{
			// M5-AC-4: "maiores que 100 MB" e "criados ontem"
			var query = "maiores que 100 MB criados ontem";
			var intent = NaturalSearchParser.Parse(query, ReferenceDate);
			var criteria = NaturalSearchFilterCriteria.FromIntent(intent);

			long mb100 = 100L * 1024 * 1024;

			DateTimeOffset yesterdayStart = ReferenceDate.Date.AddDays(-1);
			DateTimeOffset yesterdayMid = yesterdayStart.AddHours(14);
			DateTimeOffset twoDaysAgo = ReferenceDate.Date.AddDays(-2);

			var items = new List<TestItem>
			{
				new("large_recent.iso", mb100 + 1024, ReferenceDate, yesterdayMid),       // Match: > 100MB, created yesterday
				new("small_recent.iso", mb100 - 1024, ReferenceDate, yesterdayMid),       // Fail: < 100MB
				new("large_old.iso", mb100 + 5000, ReferenceDate, twoDaysAgo)             // Fail: created 2 days ago
			};

			var filtered = NaturalSearchPostFilter.FilterItems(
				items,
				criteria,
				x => x.Name,
				x => x.Size,
				x => x.Modified,
				x => x.Created);

			Assert.AreEqual(1, filtered.Count);
			Assert.AreEqual("large_recent.iso", filtered[0].Name);
		}

		[TestMethod]
		public void M5_AC3_PostFilter_SemFiltrosEstruturados_PassaTodosOsItens()
		{
			// M5-AC-3: Frase sem filtro reconhecível
			var query = "coisas bonitas";
			var intent = NaturalSearchParser.Parse(query, ReferenceDate);
			var criteria = NaturalSearchFilterCriteria.FromIntent(intent);

			var items = new List<TestItem>
			{
				new("file1.doc", 100, ReferenceDate, ReferenceDate),
				new("file2.png", 200, ReferenceDate, ReferenceDate),
				new("file3.zip", 300, ReferenceDate, ReferenceDate)
			};

			var filtered = NaturalSearchPostFilter.FilterItems(
				items,
				criteria,
				x => x.Name,
				x => x.Size,
				x => x.Modified,
				x => x.Created);

			Assert.AreEqual(3, filtered.Count);
		}
	}
}
