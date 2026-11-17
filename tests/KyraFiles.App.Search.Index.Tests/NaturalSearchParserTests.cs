// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class NaturalSearchParserTests
	{
		private static readonly DateTimeOffset FixedReferenceDate = new(2026, 10, 27, 12, 0, 0, TimeSpan.FromHours(-3));
		private static readonly DateTimeOffset YearRolloverDate = new(2026, 1, 5, 12, 0, 0, TimeSpan.FromHours(-3));
		private static readonly DateTimeOffset MonthRolloverDate = new(2026, 3, 1, 12, 0, 0, TimeSpan.FromHours(-3));

		[TestMethod]
		public void M5_AC1_TodosOsArquivosIniSalvosNosUltimos12Dias_PT()
		{
			var query = "todos os arquivos ini salvos nos últimos 12 dias";
			var intent = NaturalSearchParser.Parse(query, FixedReferenceDate);

			Assert.IsTrue(intent.HasStructuredFilters, "Deve reconhecer filtros estruturados.");
			Assert.IsTrue(intent.IsFullyUnderstood, "A consulta deve ser totalmente compreendida.");
			CollectionAssert.AreEqual(new[] { ".ini" }, intent.FileExtensions.ToArray());
			Assert.IsNotNull(intent.DateModifiedRange, "Filtro de modificação deve existir.");
			Assert.AreEqual(FixedReferenceDate.AddDays(-12), intent.DateModifiedRange.From);
			Assert.AreEqual(FixedReferenceDate, intent.DateModifiedRange.To);
			Assert.IsNull(intent.DateCreatedRange, "Não deve haver filtro de criação.");
			Assert.IsNull(intent.SizeRange, "Não deve haver filtro de tamanho.");
			Assert.IsNull(intent.SearchText, "Não deve sobrar texto de pesquisa.");
			Assert.AreEqual(0, intent.UnrecognizedTokens.Count);
		}

		[TestMethod]
		public void M5_AC5_AllIniFilesSavedInTheLast12Days_EN()
		{
			var query = "all ini files saved in the last 12 days";
			var intent = NaturalSearchParser.Parse(query, FixedReferenceDate);

			Assert.IsTrue(intent.HasStructuredFilters);
			Assert.IsTrue(intent.IsFullyUnderstood);
			CollectionAssert.AreEqual(new[] { ".ini" }, intent.FileExtensions.ToArray());
			Assert.IsNotNull(intent.DateModifiedRange);
			Assert.AreEqual(FixedReferenceDate.AddDays(-12), intent.DateModifiedRange.From);
			Assert.AreEqual(FixedReferenceDate, intent.DateModifiedRange.To);
			Assert.IsNull(intent.DateCreatedRange);
			Assert.IsNull(intent.SizeRange);
			Assert.IsNull(intent.SearchText);
			Assert.AreEqual(0, intent.UnrecognizedTokens.Count);
		}

		[TestMethod]
		public void M5_AC3_FrasesSemFiltroReconhecivel_NaoEntendeu()
		{
			string[] unrecognizedQueries =
			{
				"coisas bonitas",
				"something strange",
				"abracadabra xyz123"
			};

			foreach (var query in unrecognizedQueries)
			{
				var intent = NaturalSearchParser.Parse(query, FixedReferenceDate);

				Assert.IsFalse(intent.HasStructuredFilters, $"Não deve criar filtros para '{query}'.");
				Assert.IsFalse(intent.IsFullyUnderstood);
				Assert.AreEqual(0, intent.FileExtensions.Count);
				Assert.IsNull(intent.DateModifiedRange);
				Assert.IsNull(intent.DateCreatedRange);
				Assert.IsNull(intent.SizeRange);
				Assert.IsNull(intent.SearchText);
				Assert.IsTrue(intent.UnrecognizedTokens.Count > 0, $"Deve reportar tokens não compreendidos para '{query}'.");
			}
		}

		[TestMethod]
		public void M5_AC3_AmbiguasRecentes_NaoInventaFiltro()
		{
			string[] ambiguousQueries =
			{
				"arquivos recentes",
				"recent files",
				"recentes",
				"recent"
			};

			foreach (var query in ambiguousQueries)
			{
				var intent = NaturalSearchParser.Parse(query, FixedReferenceDate);

				Assert.IsFalse(intent.HasStructuredFilters, $"Não deve inventar filtro para termo ambíguo '{query}'.");
				Assert.IsNull(intent.DateModifiedRange, $"Não deve inventar DateModifiedRange para '{query}'.");
				Assert.IsNull(intent.DateCreatedRange);
				Assert.IsNull(intent.SizeRange);
				Assert.IsTrue(intent.UnrecognizedTokens.Count > 0);
			}
		}

		[TestMethod]
		public void M5_AC3_AmbiguasRecentes_ComFiltroExtensao_PreservaExtensaoEApontaNaoEntendido()
		{
			var intentPt = NaturalSearchParser.Parse("fotos recentes", FixedReferenceDate);
			Assert.IsTrue(intentPt.HasStructuredFilters);
			Assert.IsTrue(intentPt.FileExtensions.Contains(".jpg"));
			Assert.IsNull(intentPt.DateModifiedRange, "Não deve inventar data para 'recentes'.");
			Assert.IsTrue(intentPt.UnrecognizedTokens.Contains("recentes"));
			Assert.IsFalse(intentPt.IsFullyUnderstood);

			var intentEn = NaturalSearchParser.Parse("recent photos", FixedReferenceDate);
			Assert.IsTrue(intentEn.HasStructuredFilters);
			Assert.IsTrue(intentEn.FileExtensions.Contains(".jpg"));
			Assert.IsNull(intentEn.DateModifiedRange, "Não deve inventar data para 'recent'.");
			Assert.IsTrue(intentEn.UnrecognizedTokens.Contains("recent"));
			Assert.IsFalse(intentEn.IsFullyUnderstood);
		}

		[TestMethod]
		public void M5_AC4_MaioresQue100MB_TamanhoCorreto()
		{
			long expectedBytes = 100L * 1024L * 1024L;

			var intentPt = NaturalSearchParser.Parse("maiores que 100 MB", FixedReferenceDate);
			Assert.IsTrue(intentPt.HasStructuredFilters);
			Assert.IsNotNull(intentPt.SizeRange);
			Assert.AreEqual(expectedBytes, intentPt.SizeRange.MinBytes);
			Assert.IsNull(intentPt.SizeRange.MaxBytes);

			var intentEn = NaturalSearchParser.Parse("larger than 100 MB", FixedReferenceDate);
			Assert.IsTrue(intentEn.HasStructuredFilters);
			Assert.IsNotNull(intentEn.SizeRange);
			Assert.AreEqual(expectedBytes, intentEn.SizeRange.MinBytes);
			Assert.IsNull(intentEn.SizeRange.MaxBytes);
		}

		[TestMethod]
		public void M5_AC4_FotosDeOntem_ExtensaoEDataCorretas()
		{
			var yesterdayStart = new DateTimeOffset(FixedReferenceDate.Year, FixedReferenceDate.Month, FixedReferenceDate.Day, 0, 0, 0, FixedReferenceDate.Offset).AddDays(-1);
			var yesterdayEnd = yesterdayStart.AddDays(1).AddTicks(-1);

			var intentPt = NaturalSearchParser.Parse("fotos de ontem", FixedReferenceDate);
			Assert.IsTrue(intentPt.HasStructuredFilters);
			Assert.IsTrue(intentPt.FileExtensions.Contains(".jpg"));
			Assert.IsTrue(intentPt.FileExtensions.Contains(".png"));
			Assert.IsNotNull(intentPt.DateModifiedRange);
			Assert.AreEqual(yesterdayStart, intentPt.DateModifiedRange.From);
			Assert.AreEqual(yesterdayEnd, intentPt.DateModifiedRange.To);

			var intentEn = NaturalSearchParser.Parse("photos from yesterday", FixedReferenceDate);
			Assert.IsTrue(intentEn.HasStructuredFilters);
			Assert.IsTrue(intentEn.FileExtensions.Contains(".jpg"));
			Assert.IsNotNull(intentEn.DateModifiedRange);
			Assert.AreEqual(yesterdayStart, intentEn.DateModifiedRange.From);
			Assert.AreEqual(yesterdayEnd, intentEn.DateModifiedRange.To);
		}

		[TestMethod]
		public void M5_AC4_AC5_Combinado_FotosDeOntemMaioresQue100MB_PT_EN()
		{
			long expectedBytes = 100L * 1024L * 1024L;
			var yesterdayStart = new DateTimeOffset(FixedReferenceDate.Year, FixedReferenceDate.Month, FixedReferenceDate.Day, 0, 0, 0, FixedReferenceDate.Offset).AddDays(-1);
			var yesterdayEnd = yesterdayStart.AddDays(1).AddTicks(-1);

			var intentPt = NaturalSearchParser.Parse("fotos de ontem maiores que 100 MB", FixedReferenceDate);
			Assert.IsTrue(intentPt.HasStructuredFilters);
			Assert.IsTrue(intentPt.FileExtensions.Contains(".jpg"));
			Assert.IsNotNull(intentPt.DateModifiedRange);
			Assert.AreEqual(yesterdayStart, intentPt.DateModifiedRange.From);
			Assert.AreEqual(yesterdayEnd, intentPt.DateModifiedRange.To);
			Assert.IsNotNull(intentPt.SizeRange);
			Assert.AreEqual(expectedBytes, intentPt.SizeRange.MinBytes);

			var intentEn = NaturalSearchParser.Parse("photos from yesterday larger than 100 MB", FixedReferenceDate);
			Assert.IsTrue(intentEn.HasStructuredFilters);
			Assert.IsTrue(intentEn.FileExtensions.Contains(".jpg"));
			Assert.IsNotNull(intentEn.DateModifiedRange);
			Assert.AreEqual(yesterdayStart, intentEn.DateModifiedRange.From);
			Assert.AreEqual(yesterdayEnd, intentEn.DateModifiedRange.To);
			Assert.IsNotNull(intentEn.SizeRange);
			Assert.AreEqual(expectedBytes, intentEn.SizeRange.MinBytes);
		}

		[TestMethod]
		public void M5_AC1_AC5_ExtensaoComESemPonto_PT_EN()
		{
			string[] extensionQueries =
			{
				"arquivos ini",
				"arquivos .ini",
				"extensão ini",
				"extensão .ini",
				"extension ini",
				"extension .ini",
				"ini files",
				".ini files",
				"todos os arquivos ini",
				"todos os .ini",
				"all ini files",
				"all .ini files"
			};

			foreach (var q in extensionQueries)
			{
				var intent = NaturalSearchParser.Parse(q, FixedReferenceDate);
				Assert.IsTrue(intent.HasStructuredFilters, $"Deve reconhecer extensão em '{q}'.");
				CollectionAssert.AreEqual(new[] { ".ini" }, intent.FileExtensions.ToArray(), $"Divergência na extensão de '{q}'.");
			}
		}

		[TestMethod]
		public void M5_AC1_AC4_ViradaDeMesEDeAno_DatasFixas()
		{
			// Virada de ano: referência em 05/01/2026 -> últimos 10 dias deve cair em 26/12/2025
			var intentYearPt = NaturalSearchParser.Parse("arquivos salvos nos últimos 10 dias", YearRolloverDate);
			Assert.IsNotNull(intentYearPt.DateModifiedRange);
			Assert.AreEqual(2025, intentYearPt.DateModifiedRange.From!.Value.Year);
			Assert.AreEqual(12, intentYearPt.DateModifiedRange.From.Value.Month);
			Assert.AreEqual(26, intentYearPt.DateModifiedRange.From.Value.Day);
			Assert.AreEqual(YearRolloverDate, intentYearPt.DateModifiedRange.To);

			var intentYearEn = NaturalSearchParser.Parse("files saved in the last 10 days", YearRolloverDate);
			Assert.IsNotNull(intentYearEn.DateModifiedRange);
			Assert.AreEqual(2025, intentYearEn.DateModifiedRange.From!.Value.Year);
			Assert.AreEqual(12, intentYearEn.DateModifiedRange.From.Value.Month);
			Assert.AreEqual(26, intentYearEn.DateModifiedRange.From.Value.Day);

			// Virada de mês: referência em 01/03/2026 -> ontem deve cair em 28/02/2026
			var intentMonthPt = NaturalSearchParser.Parse("arquivos de ontem", MonthRolloverDate);
			Assert.IsNotNull(intentMonthPt.DateModifiedRange);
			Assert.AreEqual(2026, intentMonthPt.DateModifiedRange.From!.Value.Year);
			Assert.AreEqual(2, intentMonthPt.DateModifiedRange.From.Value.Month);
			Assert.AreEqual(28, intentMonthPt.DateModifiedRange.From.Value.Day);

			var intentMonthEn = NaturalSearchParser.Parse("files from yesterday", MonthRolloverDate);
			Assert.IsNotNull(intentMonthEn.DateModifiedRange);
			Assert.AreEqual(2026, intentMonthEn.DateModifiedRange.From!.Value.Year);
			Assert.AreEqual(2, intentMonthEn.DateModifiedRange.From.Value.Month);
			Assert.AreEqual(28, intentMonthEn.DateModifiedRange.From.Value.Day);
		}

		[TestMethod]
		public void M5_AC1_AC4_Qualificadores_CriadoVsModificado()
		{
			// Criado
			var intentCriadoPt = NaturalSearchParser.Parse("arquivos criados hoje", FixedReferenceDate);
			Assert.IsNotNull(intentCriadoPt.DateCreatedRange, "Deve preencher DateCreatedRange.");
			Assert.IsNull(intentCriadoPt.DateModifiedRange, "Não deve preencher DateModifiedRange quando for explicitamente criado.");

			var intentCriadoEn = NaturalSearchParser.Parse("files created today", FixedReferenceDate);
			Assert.IsNotNull(intentCriadoEn.DateCreatedRange);
			Assert.IsNull(intentCriadoEn.DateModifiedRange);

			// Modificado
			var intentModificadoPt = NaturalSearchParser.Parse("arquivos modificados hoje", FixedReferenceDate);
			Assert.IsNotNull(intentModificadoPt.DateModifiedRange, "Deve preencher DateModifiedRange.");
			Assert.IsNull(intentModificadoPt.DateCreatedRange);

			var intentModificadoEn = NaturalSearchParser.Parse("files modified today", FixedReferenceDate);
			Assert.IsNotNull(intentModificadoEn.DateModifiedRange);
			Assert.IsNull(intentModificadoEn.DateCreatedRange);

			// Semana passada = últimos 7 dias (Q8)
			var intentSemanaPt = NaturalSearchParser.Parse("arquivos salvos semana passada", FixedReferenceDate);
			Assert.IsNotNull(intentSemanaPt.DateModifiedRange);
			Assert.AreEqual(FixedReferenceDate.AddDays(-7), intentSemanaPt.DateModifiedRange.From);
			Assert.AreEqual(FixedReferenceDate, intentSemanaPt.DateModifiedRange.To);

			var intentSemanaEn = NaturalSearchParser.Parse("files saved last week", FixedReferenceDate);
			Assert.IsNotNull(intentSemanaEn.DateModifiedRange);
			Assert.AreEqual(FixedReferenceDate.AddDays(-7), intentSemanaEn.DateModifiedRange.From);
			Assert.AreEqual(FixedReferenceDate, intentSemanaEn.DateModifiedRange.To);
		}

		[TestMethod]
		public void M5_AC1_AC4_Grupos_Planilhas_Pdf_Videos_Audios_Tamanhos()
		{
			// Planilhas este mês
			var intentPlanilhas = NaturalSearchParser.Parse("planilhas salvas este mês", FixedReferenceDate);
			Assert.IsTrue(intentPlanilhas.FileExtensions.Contains(".xlsx"));
			Assert.IsTrue(intentPlanilhas.FileExtensions.Contains(".csv"));
			Assert.IsNotNull(intentPlanilhas.DateModifiedRange);

			// PDF criado no mês passado
			var intentPdf = NaturalSearchParser.Parse("documentos pdf criados no mês passado", FixedReferenceDate);
			CollectionAssert.AreEqual(new[] { ".pdf" }, intentPdf.FileExtensions.ToArray());
			Assert.IsNotNull(intentPdf.DateCreatedRange);

			// Vídeos > 1.5 GB
			var intentVideos = NaturalSearchParser.Parse("vídeos maiores que 1.5 GB", FixedReferenceDate);
			Assert.IsTrue(intentVideos.FileExtensions.Contains(".mp4"));
			Assert.IsNotNull(intentVideos.SizeRange);
			Assert.AreEqual((long)(1.5 * 1024 * 1024 * 1024), intentVideos.SizeRange.MinBytes);

			// Log < 500 KB
			var intentLog = NaturalSearchParser.Parse("arquivos log menores que 500 KB", FixedReferenceDate);
			CollectionAssert.AreEqual(new[] { ".log" }, intentLog.FileExtensions.ToArray());
			Assert.IsNotNull(intentLog.SizeRange);
			Assert.AreEqual(500L * 1024L, intentLog.SizeRange.MaxBytes);

			// Entre 10 MB e 50 MB
			var intentRange = NaturalSearchParser.Parse("arquivos entre 10 MB e 50 MB", FixedReferenceDate);
			Assert.IsNotNull(intentRange.SizeRange);
			Assert.AreEqual(10L * 1024L * 1024L, intentRange.SizeRange.MinBytes);
			Assert.AreEqual(50L * 1024L * 1024L, intentRange.SizeRange.MaxBytes);
		}

		[TestMethod]
		public void M5_AC1_TextoRestante_NomeArquivo()
		{
			var intentPt = NaturalSearchParser.Parse("relatório salvo ontem", FixedReferenceDate);
			Assert.AreEqual("relatório", intentPt.SearchText);
			Assert.IsNotNull(intentPt.DateModifiedRange);

			var intentEn = NaturalSearchParser.Parse("budget file saved yesterday", FixedReferenceDate);
			Assert.AreEqual("budget", intentEn.SearchText);
			Assert.IsNotNull(intentEn.DateModifiedRange);
		}

		private sealed record TestCase(
			string Phrase,
			DateTimeOffset ReferenceDate,
			string[] ExpectedExtensions,
			bool ExpectModified,
			bool ExpectCreated,
			bool ExpectSize,
			string? ExpectedSearchText,
			bool ExpectHasFilters);

		[TestMethod]
		public void M5_AC1_AC3_AC4_AC5_TabelaDePeloMenos40Frases()
		{
			var testTable = new TestCase[]
			{
				// 1 a 5: Variações PT de ini nos últimos 12 dias
				new("todos os arquivos ini salvos nos últimos 12 dias", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),
				new("todos os arquivos .ini salvos nos últimos 12 dias", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),
				new("arquivos ini salvos nos últimos 12 dias", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),
				new("arquivos .ini modificados nos últimos 12 dias", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),
				new("arquivos ini alterados nos últimos 12 dias", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),

				// 6 a 9: Variações EN de ini in the last 12 days
				new("all ini files saved in the last 12 days", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),
				new("all .ini files saved in the last 12 days", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),
				new("ini files modified in the past 12 days", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),
				new("ini files changed in the last 12 days", FixedReferenceDate, new[] { ".ini" }, true, false, false, null, true),

				// 10 a 12: Sem filtro reconhecível
				new("coisas bonitas", FixedReferenceDate, Array.Empty<string>(), false, false, false, null, false),
				new("something strange", FixedReferenceDate, Array.Empty<string>(), false, false, false, null, false),
				new("abracadabra xyz123", FixedReferenceDate, Array.Empty<string>(), false, false, false, null, false),

				// 13 a 16: Ambíguas (recentes sem número)
				new("arquivos recentes", FixedReferenceDate, Array.Empty<string>(), false, false, false, null, false),
				new("recent files", FixedReferenceDate, Array.Empty<string>(), false, false, false, null, false),
				new("recentes", FixedReferenceDate, Array.Empty<string>(), false, false, false, null, false),
				new("recent", FixedReferenceDate, Array.Empty<string>(), false, false, false, null, false),

				// 17 a 18: Fotos recentes (tem extensão, mas data recente é ambígua)
				new("fotos recentes", FixedReferenceDate, new[] { ".jpg" }, false, false, false, null, true),
				new("recent photos", FixedReferenceDate, new[] { ".jpg" }, false, false, false, null, true),

				// 19 a 20: Fotos de ontem
				new("fotos de ontem", FixedReferenceDate, new[] { ".jpg" }, true, false, false, null, true),
				new("photos from yesterday", FixedReferenceDate, new[] { ".jpg" }, true, false, false, null, true),

				// 21 a 22: Maiores que 100 MB
				new("maiores que 100 MB", FixedReferenceDate, Array.Empty<string>(), false, false, true, null, true),
				new("larger than 100 MB", FixedReferenceDate, Array.Empty<string>(), false, false, true, null, true),

				// 23 a 24: Fotos de ontem maiores que 100 MB
				new("fotos de ontem maiores que 100 MB", FixedReferenceDate, new[] { ".jpg" }, true, false, true, null, true),
				new("photos from yesterday larger than 100 MB", FixedReferenceDate, new[] { ".jpg" }, true, false, true, null, true),

				// 25 a 28: Criados hoje vs Modificados hoje
				new("arquivos criados hoje", FixedReferenceDate, Array.Empty<string>(), false, true, false, null, true),
				new("files created today", FixedReferenceDate, Array.Empty<string>(), false, true, false, null, true),
				new("arquivos modificados hoje", FixedReferenceDate, Array.Empty<string>(), true, false, false, null, true),
				new("files modified today", FixedReferenceDate, Array.Empty<string>(), true, false, false, null, true),

				// 29 a 30: Semana passada (7 dias)
				new("arquivos salvos semana passada", FixedReferenceDate, Array.Empty<string>(), true, false, false, null, true),
				new("files saved last week", FixedReferenceDate, Array.Empty<string>(), true, false, false, null, true),

				// 31 a 32: Planilhas este mês
				new("planilhas salvas este mês", FixedReferenceDate, new[] { ".xlsx" }, true, false, false, null, true),
				new("spreadsheets saved this month", FixedReferenceDate, new[] { ".xlsx" }, true, false, false, null, true),

				// 33 a 34: Documentos PDF no mês passado
				new("documentos pdf criados no mês passado", FixedReferenceDate, new[] { ".pdf" }, false, true, false, null, true),
				new("pdf documents created last month", FixedReferenceDate, new[] { ".pdf" }, false, true, false, null, true),

				// 35 a 36: Log menores que 500 KB
				new("arquivos log menores que 500 KB", FixedReferenceDate, new[] { ".log" }, false, false, true, null, true),
				new("log files smaller than 500 KB", FixedReferenceDate, new[] { ".log" }, false, false, true, null, true),

				// 37 a 38: Vídeos maiores que 1.5 GB
				new("vídeos maiores que 1.5 GB", FixedReferenceDate, new[] { ".mp4" }, false, false, true, null, true),
				new("videos larger than 1.5 GB", FixedReferenceDate, new[] { ".mp4" }, false, false, true, null, true),

				// 39 a 40: Faixa de tamanho (entre 10 MB e 50 MB)
				new("arquivos entre 10 MB e 50 MB", FixedReferenceDate, Array.Empty<string>(), false, false, true, null, true),
				new("files between 10 MB and 50 MB", FixedReferenceDate, Array.Empty<string>(), false, false, true, null, true),

				// 41 a 42: Virada de ano (05/01/2026 -> 26/12/2025)
				new("arquivos salvos nos últimos 10 dias", YearRolloverDate, Array.Empty<string>(), true, false, false, null, true),
				new("files saved in the last 10 days", YearRolloverDate, Array.Empty<string>(), true, false, false, null, true),

				// 43 a 44: Virada de mês (01/03/2026 -> 28/02/2026)
				new("arquivos de ontem", MonthRolloverDate, Array.Empty<string>(), true, false, false, null, true),
				new("files from yesterday", MonthRolloverDate, Array.Empty<string>(), true, false, false, null, true),

				// 45 a 46: Texto restante do nome
				new("relatório salvo ontem", FixedReferenceDate, Array.Empty<string>(), true, false, false, "relatório", true),
				new("budget file saved yesterday", FixedReferenceDate, Array.Empty<string>(), true, false, false, "budget", true),

				// 47 a 48: Múltiplas extensões
				new("arquivos txt e log salvos nos últimos 3 dias", FixedReferenceDate, new[] { ".txt", ".log" }, true, false, false, null, true),
				new("txt and log files saved in the last 3 days", FixedReferenceDate, new[] { ".txt", ".log" }, true, false, false, null, true),

				// 49 a 50: Músicas criadas este ano
				new("músicas criadas este ano", FixedReferenceDate, new[] { ".mp3" }, false, true, false, null, true),
				new("music created this year", FixedReferenceDate, new[] { ".mp3" }, false, true, false, null, true)
			};

			Assert.IsTrue(testTable.Length >= 40, $"A tabela deve ter pelo menos 40 frases (atual: {testTable.Length}).");

			int index = 0;
			foreach (var tc in testTable)
			{
				index++;
				var intent = NaturalSearchParser.Parse(tc.Phrase, tc.ReferenceDate);

				Assert.AreEqual(tc.ExpectHasFilters, intent.HasStructuredFilters,
					$"[Caso {index}: '{tc.Phrase}'] Expectativa de filtros estruturados divergente.");

				if (tc.ExpectedExtensions.Length > 0)
				{
					foreach (var ext in tc.ExpectedExtensions)
					{
						Assert.IsTrue(intent.FileExtensions.Contains(ext),
							$"[Caso {index}: '{tc.Phrase}'] Extensão esperada '{ext}' não encontrada.");
					}
				}

				Assert.AreEqual(tc.ExpectModified, intent.DateModifiedRange is not null,
					$"[Caso {index}: '{tc.Phrase}'] Expectativa de DateModifiedRange divergente.");

				Assert.AreEqual(tc.ExpectCreated, intent.DateCreatedRange is not null,
					$"[Caso {index}: '{tc.Phrase}'] Expectativa de DateCreatedRange divergente.");

				Assert.AreEqual(tc.ExpectSize, intent.SizeRange is not null,
					$"[Caso {index}: '{tc.Phrase}'] Expectativa de SizeRange divergente.");

				if (tc.ExpectedSearchText is not null)
				{
					Assert.AreEqual(tc.ExpectedSearchText, intent.SearchText,
						$"[Caso {index}: '{tc.Phrase}'] Texto de pesquisa remanescente divergente.");
				}
			}
		}
	}
}
