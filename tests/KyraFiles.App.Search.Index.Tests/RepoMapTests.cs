// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.Shared.RepoMap;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class RepoMapTests
	{
		private string _testDir = null!;

		[TestInitialize]
		public void Setup()
		{
			_testDir = Path.Combine(Path.GetTempPath(), "agy", "f055_" + Guid.NewGuid().ToString("N"), "KyraFiles");
			Directory.CreateDirectory(_testDir);
		}

		[TestCleanup]
		public void Cleanup()
		{
			try
			{
				if (Directory.Exists(_testDir))
					Directory.Delete(_testDir, recursive: true);
			}
			catch
			{
				// Best effort cleanup
			}
		}

		[TestMethod]
		public async Task AnalyzeAsync_EmptyDirectory_ReturnsZeroTotals()
		{
			var analyzer = new RepoMapAnalyzer();
			var options = new RepoMapOptions { RootPath = _testDir, IncludeGitHotSpots = false };

			var result = await analyzer.AnalyzeAsync(options);

			Assert.AreEqual(0, result.TotalFiles);
			Assert.AreEqual(0, result.TotalLines);
			Assert.AreEqual(0, result.TotalBytes);
			Assert.AreEqual(0, result.Languages.Count);
		}

		[TestMethod]
		public async Task AnalyzeAsync_MultipleLanguages_TotalsMatchSumExactly_AC1()
		{
			// Create C# files
			File.WriteAllText(Path.Combine(_testDir, "Program.cs"), "line1\nline2\nline3\n");
			File.WriteAllText(Path.Combine(_testDir, "Helper.cs"), "line1\nline2\n");

			// Create XAML file
			File.WriteAllText(Path.Combine(_testDir, "View.xaml"), "<Window>\n  <Grid />\n</Window>\n");

			// Create Markdown file
			File.WriteAllText(Path.Combine(_testDir, "README.md"), "# Title\n\nDescription\n");

			var analyzer = new RepoMapAnalyzer();
			var options = new RepoMapOptions { RootPath = _testDir, IncludeGitHotSpots = false };

			var result = await analyzer.AnalyzeAsync(options);

			Assert.AreEqual(4, result.TotalFiles);
			long sumLines = result.Languages.Sum(l => l.TotalLines);
			long sumBytes = result.Languages.Sum(l => l.TotalBytes);
			int sumFiles = result.Languages.Sum(l => l.FileCount);

			Assert.AreEqual(sumFiles, result.TotalFiles, "Total files must match sum of languages");
			Assert.AreEqual(sumLines, result.TotalLines, "Total lines must match sum of languages");
			Assert.AreEqual(sumBytes, result.TotalBytes, "Total bytes must match sum of languages");

			var csLang = result.Languages.FirstOrDefault(l => l.LanguageName == "C#");
			Assert.IsNotNull(csLang);
			Assert.AreEqual(2, csLang.FileCount);
			Assert.AreEqual(5, csLang.TotalLines); // 3 + 2

			var xamlLang = result.Languages.FirstOrDefault(l => l.LanguageName == "XAML");
			Assert.IsNotNull(xamlLang);
			Assert.AreEqual(1, xamlLang.FileCount);
			Assert.AreEqual(3, xamlLang.TotalLines);

			var mdLang = result.Languages.FirstOrDefault(l => l.LanguageName == "Markdown");
			Assert.IsNotNull(mdLang);
			Assert.AreEqual(1, mdLang.FileCount);
			Assert.AreEqual(3, mdLang.TotalLines);
		}

		[TestMethod]
		public async Task AnalyzeAsync_FilesWithinLanguage_OrderedLargestToSmallest_AC2()
		{
			File.WriteAllText(Path.Combine(_testDir, "Small.cs"), "1\n2\n");
			File.WriteAllText(Path.Combine(_testDir, "Large.cs"), "1\n2\n3\n4\n5\n6\n7\n8\n9\n10\n");
			File.WriteAllText(Path.Combine(_testDir, "Medium.cs"), "1\n2\n3\n4\n5\n");

			var analyzer = new RepoMapAnalyzer();
			var options = new RepoMapOptions { RootPath = _testDir, IncludeGitHotSpots = false };

			var result = await analyzer.AnalyzeAsync(options);

			var csLang = result.Languages.First(l => l.LanguageName == "C#");
			Assert.AreEqual(3, csLang.Files.Count);
			Assert.AreEqual("Large.cs", Path.GetFileName(csLang.Files[0].FilePath));
			Assert.AreEqual("Medium.cs", Path.GetFileName(csLang.Files[1].FilePath));
			Assert.AreEqual("Small.cs", Path.GetFileName(csLang.Files[2].FilePath));
			Assert.IsTrue(csLang.Files[0].FileSizeBytes >= csLang.Files[1].FileSizeBytes);
			Assert.IsTrue(csLang.Files[1].FileSizeBytes >= csLang.Files[2].FileSizeBytes);
		}

		[TestMethod]
		public async Task AnalyzeAsync_GitIgnoreRespect_AC5()
		{
			// Create gitignore
			File.WriteAllText(Path.Combine(_testDir, ".gitignore"), "ignored/\n*.tmp\n");

			Directory.CreateDirectory(Path.Combine(_testDir, "ignored"));
			File.WriteAllText(Path.Combine(_testDir, "ignored", "Hidden.cs"), "1\n2\n3\n");

			Directory.CreateDirectory(Path.Combine(_testDir, "src"));
			File.WriteAllText(Path.Combine(_testDir, "src", "Visible.cs"), "1\n2\n3\n");
			File.WriteAllText(Path.Combine(_testDir, "src", "Temp.tmp"), "1\n2\n3\n");

			var analyzer = new RepoMapAnalyzer();

			// Test with RespectGitIgnore = true
			var optionsRespect = new RepoMapOptions { RootPath = _testDir, RespectGitIgnore = true, IncludeGitHotSpots = false };
			var resultRespect = await analyzer.AnalyzeAsync(optionsRespect);

			var csFilesRespect = resultRespect.AllFiles.Select(f => Path.GetFileName(f.FilePath)).ToList();
			Assert.IsTrue(csFilesRespect.Contains("Visible.cs"));
			Assert.IsFalse(csFilesRespect.Contains("Hidden.cs"));
			Assert.IsFalse(csFilesRespect.Contains("Temp.tmp"));

			// Test with RespectGitIgnore = false
			var optionsNoRespect = new RepoMapOptions { RootPath = _testDir, RespectGitIgnore = false, IncludeGitHotSpots = false };
			var resultNoRespect = await analyzer.AnalyzeAsync(optionsNoRespect);

			var csFilesNoRespect = resultNoRespect.AllFiles.Select(f => Path.GetFileName(f.FilePath)).ToList();
			Assert.IsTrue(csFilesNoRespect.Contains("Visible.cs"));
			Assert.IsTrue(csFilesNoRespect.Contains("Hidden.cs"));
			Assert.IsTrue(csFilesNoRespect.Contains("Temp.tmp"));
		}

		[TestMethod]
		public async Task AnalyzeAsync_OutsideGitRepo_ExplainsGitRequirement_AC4()
		{
			File.WriteAllText(Path.Combine(_testDir, "Test.cs"), "1\n2\n");

			var analyzer = new RepoMapAnalyzer();
			// RootPath in Temp is outside git repo
			var options = new RepoMapOptions { RootPath = _testDir, IncludeGitHotSpots = true };

			var result = await analyzer.AnalyzeAsync(options);

			Assert.IsFalse(result.IsGitRepository);
			Assert.IsNotNull(result.GitStatusMessage);
			Assert.IsTrue(result.GitStatusMessage.Contains("não é um repositório Git", StringComparison.OrdinalIgnoreCase));
			// Language summary remains available
			Assert.AreEqual(1, result.TotalFiles);
			Assert.AreEqual("C#", result.Languages[0].LanguageName);
		}

		[TestMethod]
		public async Task AnalyzeAsync_Cancellation_StopsWithinOneSecond_AC7()
		{
			// Generate many files
			for (int i = 0; i < 50; i++)
			{
				File.WriteAllText(Path.Combine(_testDir, $"Test_{i}.cs"), "public class Test { }\n");
			}

			var analyzer = new RepoMapAnalyzer();
			var options = new RepoMapOptions { RootPath = _testDir, IncludeGitHotSpots = false };
			using var cts = new CancellationTokenSource();
			cts.Cancel(); // Pre-cancelled or immediate

			var sw = System.Diagnostics.Stopwatch.StartNew();
			var result = await analyzer.AnalyzeAsync(options, cancellationToken: cts.Token);
			sw.Stop();

			Assert.IsTrue(sw.Elapsed.TotalSeconds <= 1.0, $"Cancellation took {sw.Elapsed.TotalSeconds}s which exceeds 1s limit");
			Assert.IsTrue(result.WasCancelled);
		}

		[TestMethod]
		public void ExtensionMap_Customization_FR003()
		{
			var options = new RepoMapOptions();
			options.SetLanguageForExtension(".myext", "My Custom DSL");
			options.SetLanguageForExtension("cool", "Cool Lang");

			Assert.AreEqual("My Custom DSL", options.ExtensionToLanguageMap[".myext"]);
			Assert.AreEqual("Cool Lang", options.ExtensionToLanguageMap[".cool"]);
		}

		[TestMethod]
		public async Task Export_MarkdownAndCsv_FR002()
		{
			File.WriteAllText(Path.Combine(_testDir, "App.cs"), "line1\nline2\nline3\n");
			File.WriteAllText(Path.Combine(_testDir, "Styles.xaml"), "<ResourceDictionary />\n");

			var analyzer = new RepoMapAnalyzer();
			var options = new RepoMapOptions { RootPath = _testDir, IncludeGitHotSpots = false };

			var result = await analyzer.AnalyzeAsync(options);

			// Markdown Export
			string md = result.ToMarkdownSummary();
			Assert.IsFalse(string.IsNullOrWhiteSpace(md));
			Assert.IsTrue(md.Contains("# Resumo do Repositório:"));
			Assert.IsTrue(md.Contains("| C# |"));
			Assert.IsTrue(md.Contains("| XAML |"));

			// CSV Languages Export
			string csvLang = result.ToCsvLanguages();
			Assert.IsTrue(csvLang.StartsWith("Linguagem,Arquivos,Linhas,PercentualLinhas,Bytes,PercentualBytes"));
			Assert.IsTrue(csvLang.Contains("\"C#\""));

			// CSV Files Export
			string csvFiles = result.ToCsvFiles();
			Assert.IsTrue(csvFiles.StartsWith("Arquivo,Linguagem,Linhas,Bytes,Mudancas12Meses,PontosQuentesScore"));
			Assert.IsTrue(csvFiles.Contains("App.cs"));
		}

		[TestMethod]
		public async Task BinaryFiles_DetectedAndSkippedFromLines()
		{
			// Write binary file with null bytes
			byte[] binaryData = new byte[] { 0x00, 0x01, 0x02, 0x00, 0xFF, 0xAA };
			File.WriteAllBytes(Path.Combine(_testDir, "image.png"), binaryData);

			var analyzer = new RepoMapAnalyzer();
			var options = new RepoMapOptions { RootPath = _testDir, IncludeGitHotSpots = false };

			var result = await analyzer.AnalyzeAsync(options);

			var binLang = result.Languages.FirstOrDefault(l => l.LanguageName == "Binary");
			Assert.IsNotNull(binLang);
			Assert.AreEqual(1, binLang.FileCount);
			Assert.AreEqual(0, binLang.TotalLines); // Binary has 0 lines
			Assert.AreEqual(6, binLang.TotalBytes);
		}
	}
}
