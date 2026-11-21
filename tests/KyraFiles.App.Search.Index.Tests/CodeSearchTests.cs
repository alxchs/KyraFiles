// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.Shared.CodeSearch;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class CodeSearchTests
	{
		private string _testDir = null!;

		[TestInitialize]
		public void Setup()
		{
			_testDir = Path.Combine(Path.GetTempPath(), "agy", "f051_" + Guid.NewGuid().ToString("N"), "KyraFiles");
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
		public void GitIgnore_DefaultRules_IgnoresBinAndObjAndNodeModules()
		{
			var matcher = new GitIgnoreMatcher(includeDefaults: true);

			Assert.IsTrue(matcher.IsIgnored("bin/Release/app.dll", isDirectory: false));
			Assert.IsTrue(matcher.IsIgnored("src/bin", isDirectory: true));
			Assert.IsTrue(matcher.IsIgnored("obj/project.assets.json", isDirectory: false));
			Assert.IsTrue(matcher.IsIgnored("node_modules/package/index.js", isDirectory: false));
			Assert.IsTrue(matcher.IsIgnored(".git/HEAD", isDirectory: false));

			Assert.IsFalse(matcher.IsIgnored("src/Controllers/HomeController.cs", isDirectory: false));
			Assert.IsFalse(matcher.IsIgnored("README.md", isDirectory: false));
		}

		[TestMethod]
		public void GitIgnore_CustomRules_SupportsWildcardsAndNegation()
		{
			var lines = new[]
			{
				"*.log",
				"temp/",
				"!important.log"
			};

			var matcher = GitIgnoreMatcher.FromLines(lines, includeDefaults: false);

			Assert.IsTrue(matcher.IsIgnored("debug.log", isDirectory: false));
			Assert.IsTrue(matcher.IsIgnored("logs/trace.log", isDirectory: false));
			Assert.IsTrue(matcher.IsIgnored("temp", isDirectory: true));
			Assert.IsTrue(matcher.IsIgnored("temp/file.txt", isDirectory: false));

			// Negated pattern re-includes
			Assert.IsFalse(matcher.IsIgnored("important.log", isDirectory: false));
			Assert.IsFalse(matcher.IsIgnored("src/app.cs", isDirectory: false));
		}

		[TestMethod]
		public async Task CodeSearch_SimpleQuery_FindsMatchesWithLineAndColumn()
		{
			var file1 = Path.Combine(_testDir, "test1.cs");
			File.WriteAllLines(file1, new[]
			{
				"using System;",
				"namespace TestApp",
				"{",
				"    // TODO: implement logic here",
				"    class Program { }",
				"}"
			});

			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "TODO",
				RespectGitIgnore = false
			};

			var streamedMatches = new List<CodeSearchResultItem>();
			var result = await CodeSearchEngine.SearchAsync(
				options,
				onMatch: m => streamedMatches.Add(m));

			Assert.IsNull(result.ErrorMessage);
			Assert.AreEqual(1, result.Matches.Count);
			Assert.AreEqual(1, streamedMatches.Count);

			var match = result.Matches[0];
			Assert.AreEqual(4, match.LineNumber);
			Assert.IsTrue(match.ColumnNumber > 0);
			Assert.AreEqual(4, match.MatchLength);
			Assert.IsTrue(match.LineText.Contains("TODO: implement logic here"));
		}

		[TestMethod]
		public async Task CodeSearch_InvalidRegex_ReturnsErrorMessageWithoutSearching()
		{
			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "[unclosed_regex",
				IsRegex = true
			};

			var result = await CodeSearchEngine.SearchAsync(options);

			Assert.IsNotNull(result.ErrorMessage);
			Assert.IsTrue(result.ErrorMessage.Contains("Expressão regular inválida"));
			Assert.AreEqual(0, result.Matches.Count);
			Assert.AreEqual(0, result.TotalFilesScanned);
		}

		[TestMethod]
		public async Task CodeSearch_BinaryFile_SkippedAndCountedAsIgnored()
		{
			var binFile = Path.Combine(_testDir, "sample.bin");
			var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x57, 0x6F, 0x72, 0x6C, 0x64 }; // Contains null byte
			File.WriteAllBytes(binFile, bytes);

			var txtFile = Path.Combine(_testDir, "sample.txt");
			File.WriteAllText(txtFile, "Hello World");

			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "Hello",
				RespectGitIgnore = false
			};

			var result = await CodeSearchEngine.SearchAsync(options);

			Assert.AreEqual(1, result.Matches.Count);
			Assert.AreEqual(Path.GetFileName(txtFile), Path.GetFileName(result.Matches[0].FilePath));
			Assert.AreEqual(1, result.TotalFilesIgnored);
		}

		[TestMethod]
		public async Task CodeSearch_LargeFileAboveLimit_SkippedAndCountedAsIgnored()
		{
			var largeFile = Path.Combine(_testDir, "large.txt");
			File.WriteAllText(largeFile, new string('A', 5000) + " SEARCH_TARGET " + new string('B', 5000));

			var normalFile = Path.Combine(_testDir, "normal.txt");
			File.WriteAllText(normalFile, "SEARCH_TARGET in normal file");

			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "SEARCH_TARGET",
				MaxFileSizeBytes = 1000, // Limit to 1 KB
				RespectGitIgnore = false
			};

			var result = await CodeSearchEngine.SearchAsync(options);

			Assert.AreEqual(1, result.Matches.Count);
			Assert.AreEqual(Path.GetFileName(normalFile), Path.GetFileName(result.Matches[0].FilePath));
			Assert.AreEqual(1, result.TotalFilesIgnored);
		}

		[TestMethod]
		public async Task CodeSearch_MultiEncoding_FindsAccentsInUtf8Utf8BomAndUtf16()
		{
			var targetWord = "Configuração";

			// UTF-8 without BOM
			var utf8File = Path.Combine(_testDir, "file_utf8.txt");
			File.WriteAllText(utf8File, $"Linha 1 com {targetWord} sem BOM.", new UTF8Encoding(false));

			// UTF-8 with BOM
			var utf8BomFile = Path.Combine(_testDir, "file_utf8_bom.txt");
			File.WriteAllText(utf8BomFile, $"Linha 2 com {targetWord} com BOM.", new UTF8Encoding(true));

			// UTF-16 LE
			var utf16File = Path.Combine(_testDir, "file_utf16.txt");
			File.WriteAllText(utf16File, $"Linha 3 com {targetWord} UTF-16.", Encoding.Unicode);

			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = targetWord,
				RespectGitIgnore = false
			};

			var result = await CodeSearchEngine.SearchAsync(options);

			Assert.AreEqual(3, result.Matches.Count);
			var matchedFiles = result.Matches.Select(m => Path.GetFileName(m.FilePath)).ToHashSet();
			Assert.IsTrue(matchedFiles.Contains("file_utf8.txt"));
			Assert.IsTrue(matchedFiles.Contains("file_utf8_bom.txt"));
			Assert.IsTrue(matchedFiles.Contains("file_utf16.txt"));
		}

		[TestMethod]
		public async Task CodeSearch_Cancellation_HaltsUnder1Second()
		{
			// Create 200 files
			for (int i = 0; i < 200; i++)
			{
				File.WriteAllText(Path.Combine(_testDir, $"file_{i}.txt"), $"Content for file {i}");
			}

			using var cts = new CancellationTokenSource();
			cts.Cancel(); // Pre-cancel to verify immediate halt within 1s

			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "Content",
				RespectGitIgnore = false
			};

			var sw = System.Diagnostics.Stopwatch.StartNew();
			var result = await CodeSearchEngine.SearchAsync(options, cancellationToken: cts.Token);
			sw.Stop();

			Assert.IsTrue(result.WasCancelled);
			Assert.IsTrue(sw.ElapsedMilliseconds < 1000, $"Expected cancellation under 1s, took {sw.ElapsedMilliseconds} ms");
		}

		[TestMethod]
		public async Task CodeSearch_RespectGitIgnore_ToggleAllowsReincludingIgnored()
		{
			var binDir = Path.Combine(_testDir, "bin");
			Directory.CreateDirectory(binDir);
			File.WriteAllText(Path.Combine(binDir, "compiled.txt"), "TARGET_KEYWORD in bin");

			var srcDir = Path.Combine(_testDir, "src");
			Directory.CreateDirectory(srcDir);
			File.WriteAllText(Path.Combine(srcDir, "source.txt"), "TARGET_KEYWORD in src");

			// 1. With RespectGitIgnore = true (default) -> bin is ignored
			var optionsIgnored = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "TARGET_KEYWORD",
				RespectGitIgnore = true
			};
			var resultIgnored = await CodeSearchEngine.SearchAsync(optionsIgnored);
			Assert.AreEqual(1, resultIgnored.Matches.Count);
			Assert.IsTrue(resultIgnored.Matches[0].FilePath.Contains("source.txt"));

			// 2. With RespectGitIgnore = false -> bin is included
			var optionsAll = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "TARGET_KEYWORD",
				RespectGitIgnore = false
			};
			var resultAll = await CodeSearchEngine.SearchAsync(optionsAll);
			Assert.AreEqual(2, resultAll.Matches.Count);
		}

		[TestMethod]
		public async Task CodeSearch_MatchWholeWord_DoesNotMatchSubstrings()
		{
			var file = Path.Combine(_testDir, "words.txt");
			File.WriteAllLines(file, new[]
			{
				"var item = 1;",
				"var itemList = 2;",
				"var my_item = 3;",
				"var items = 4;"
			});

			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "item",
				MatchWholeWord = true,
				RespectGitIgnore = false
			};

			var result = await CodeSearchEngine.SearchAsync(options);

			// Only "item" and "my_item" (separated by underscore/boundary) should match whole word
			Assert.IsTrue(result.Matches.Count >= 1);
			Assert.IsTrue(result.Matches.Any(m => m.LineText.Contains("var item = 1;")));
			Assert.IsFalse(result.Matches.Any(m => m.LineText.Contains("var itemList = 2;")));
			Assert.IsFalse(result.Matches.Any(m => m.LineText.Contains("var items = 4;")));
		}

		[TestMethod]
		public async Task CodeSearch_FileExtensionFilter_RestrictsMatchingFiles()
		{
			File.WriteAllText(Path.Combine(_testDir, "match.cs"), "COMMON_TOKEN");
			File.WriteAllText(Path.Combine(_testDir, "ignore.txt"), "COMMON_TOKEN");
			File.WriteAllText(Path.Combine(_testDir, "match.xaml"), "COMMON_TOKEN");

			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "COMMON_TOKEN",
				FileExtensions = "*.cs, *.xaml",
				RespectGitIgnore = false
			};

			var result = await CodeSearchEngine.SearchAsync(options);

			Assert.AreEqual(2, result.Matches.Count);
			Assert.IsTrue(result.Matches.All(m => m.FilePath.EndsWith(".cs") || m.FilePath.EndsWith(".xaml")));
		}

		[TestMethod]
		public async Task CodeSearch_Benchmark_ScansManyFilesUnder3Seconds()
		{
			// Generate 1000 synthetic code files (NFR-001)
			for (int i = 0; i < 1000; i++)
			{
				var sb = new StringBuilder();
				for (int line = 1; line <= 10; line++)
				{
					if (line == 5 && i % 100 == 0)
						sb.AppendLine("public void TargetMethod() { }");
					else
						sb.AppendLine($"// Comment line {line} in file {i}");
				}
				File.WriteAllText(Path.Combine(_testDir, $"code_{i:D4}.cs"), sb.ToString());
			}

			var options = new CodeSearchOptions
			{
				SearchDirectory = _testDir,
				Query = "TargetMethod",
				RespectGitIgnore = false
			};

			var sw = System.Diagnostics.Stopwatch.StartNew();
			var result = await CodeSearchEngine.SearchAsync(options);
			sw.Stop();

			Assert.AreEqual(10, result.Matches.Count);
			Assert.AreEqual(1000, result.TotalFilesScanned);
			Assert.IsTrue(sw.ElapsedMilliseconds < 3000, $"Expected under 3000 ms, took {sw.ElapsedMilliseconds} ms");
		}
	}
}
