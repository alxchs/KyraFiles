// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;
using Files.App.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.App.Search.Index.Tests
{
	[TestClass]
	public class DocumentationHelperTests
	{
		private string? _tempTestDir;

		[TestInitialize]
		public void Setup()
		{
			_tempTestDir = Path.Combine(Path.GetTempPath(), "agy", Guid.NewGuid().ToString(), "DocumentationHelperTests");
			Directory.CreateDirectory(_tempTestDir);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (_tempTestDir is not null && Directory.Exists(_tempTestDir))
			{
				try
				{
					Directory.Delete(_tempTestDir, true);
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}

		[TestMethod]
		public void NormalizeCultureCode_HandlesSupportedAndFallbackLanguages()
		{
			Assert.AreEqual("pt-BR", DocumentationHelper.NormalizeCultureCode("pt-BR"));
			Assert.AreEqual("pt-BR", DocumentationHelper.NormalizeCultureCode("pt"));
			Assert.AreEqual("pt-BR", DocumentationHelper.NormalizeCultureCode("pt-PT"));

			Assert.AreEqual("es-ES", DocumentationHelper.NormalizeCultureCode("es-ES"));
			Assert.AreEqual("es-ES", DocumentationHelper.NormalizeCultureCode("es"));
			Assert.AreEqual("es-ES", DocumentationHelper.NormalizeCultureCode("es-MX"));

			Assert.AreEqual("de-DE", DocumentationHelper.NormalizeCultureCode("de-DE"));
			Assert.AreEqual("de-DE", DocumentationHelper.NormalizeCultureCode("de"));
			Assert.AreEqual("de-DE", DocumentationHelper.NormalizeCultureCode("de-AT"));

			Assert.AreEqual("en-US", DocumentationHelper.NormalizeCultureCode("en-US"));
			Assert.AreEqual("en-US", DocumentationHelper.NormalizeCultureCode("en-GB"));
			Assert.AreEqual("en-US", DocumentationHelper.NormalizeCultureCode("fr-FR"));
			Assert.AreEqual("en-US", DocumentationHelper.NormalizeCultureCode(null));
			Assert.AreEqual("en-US", DocumentationHelper.NormalizeCultureCode(string.Empty));
		}

		[TestMethod]
		public void ResolveDocumentationTarget_WhenLocalFileExists_ReturnsLocalPath()
		{
			var docsDir = Path.Combine(_tempTestDir!, "Docs", "getting-started");
			Directory.CreateDirectory(docsDir);
			var overviewFile = Path.Combine(docsDir, "overview.html");
			File.WriteAllText(overviewFile, "<html><body>Overview Test</body></html>");

			var (isLocal, target) = DocumentationHelper.ResolveDocumentationTarget("getting-started/overview.html", _tempTestDir);

			Assert.IsTrue(isLocal, "Target should be resolved locally when the file exists.");
			Assert.AreEqual(overviewFile, target, "Target path should match the created local file path.");
		}

		[TestMethod]
		public void ResolveDocumentationTarget_Multilingual_ResolvesSpecificLanguage()
		{
			var esDir = Path.Combine(_tempTestDir!, "Docs", "es-ES", "features");
			Directory.CreateDirectory(esDir);
			var tabsEs = Path.Combine(esDir, "tabs.html");
			File.WriteAllText(tabsEs, "<html><body>Pestañas ES</body></html>");

			var deDir = Path.Combine(_tempTestDir!, "Docs", "de-DE", "features");
			Directory.CreateDirectory(deDir);
			var tabsDe = Path.Combine(deDir, "tabs.html");
			File.WriteAllText(tabsDe, "<html><body>Tabs DE</body></html>");

			// Query for Spanish
			var (isLocalEs, targetEs) = DocumentationHelper.ResolveDocumentationTarget("features/tabs.html", _tempTestDir, "es-ES");
			Assert.IsTrue(isLocalEs);
			Assert.AreEqual(tabsEs, targetEs);

			// Query for German
			var (isLocalDe, targetDe) = DocumentationHelper.ResolveDocumentationTarget("features/tabs.html", _tempTestDir, "de-DE");
			Assert.IsTrue(isLocalDe);
			Assert.AreEqual(tabsDe, targetDe);
		}

		[TestMethod]
		public void ResolveDocumentationTarget_WithExplicitLanguageInSubPath_ResolvesDirectly()
		{
			var ptDir = Path.Combine(_tempTestDir!, "Docs", "pt-BR", "getting-started");
			Directory.CreateDirectory(ptDir);
			var ptOverview = Path.Combine(ptDir, "overview.html");
			File.WriteAllText(ptOverview, "<html><body>Visao Geral PT</body></html>");

			var (isLocal, target) = DocumentationHelper.ResolveDocumentationTarget("pt-BR/getting-started/overview.html", _tempTestDir);
			Assert.IsTrue(isLocal);
			Assert.AreEqual(ptOverview, target);
		}

		[TestMethod]
		public void ResolveDocumentationTarget_WhenRequestedLangMissing_FallsBackToPtOrEn()
		{
			var enDir = Path.Combine(_tempTestDir!, "Docs", "en-US", "features");
			Directory.CreateDirectory(enDir);
			var tabsEn = Path.Combine(enDir, "tabs.html");
			File.WriteAllText(tabsEn, "<html><body>Tabs EN</body></html>");

			// Ask for German, which doesn't exist on disk, should fallback to en-US
			var (isLocal, target) = DocumentationHelper.ResolveDocumentationTarget("features/tabs.html", _tempTestDir, "de-DE");
			Assert.IsTrue(isLocal);
			Assert.AreEqual(tabsEn, target);
		}

		[TestMethod]
		public void ResolveDocumentationTarget_WhenLocalFileDoesNotExist_ReturnsWebFallback()
		{
			var (isLocal, target) = DocumentationHelper.ResolveDocumentationTarget("getting-started/overview.html", _tempTestDir);

			Assert.IsFalse(isLocal, "Target should not be marked local when file does not exist.");
			Assert.AreEqual("https://files.community/docs/getting-started/overview", target, "Should fall back to web URL.");
		}

		[TestMethod]
		public void ResolveDocumentationTarget_WhenRootRequested_AndIndexHtmlExists_ReturnsLocalIndex()
		{
			var docsDir = Path.Combine(_tempTestDir!, "Docs");
			Directory.CreateDirectory(docsDir);
			var indexFile = Path.Combine(docsDir, "index.html");
			File.WriteAllText(indexFile, "<html><body>Index Test</body></html>");

			var (isLocal, target) = DocumentationHelper.ResolveDocumentationTarget(null, _tempTestDir);

			Assert.IsTrue(isLocal, "Target should be resolved locally for root documentation.");
			Assert.AreEqual(indexFile, target, "Target should match index.html path.");
		}

		[TestMethod]
		public void ResolveDocumentationTarget_WhenRootRequested_AndNoLocalFile_ReturnsWebDocsRoot()
		{
			var (isLocal, target) = DocumentationHelper.ResolveDocumentationTarget(null, _tempTestDir);

			Assert.IsFalse(isLocal, "Target should fall back to web when root docs do not exist.");
			Assert.AreEqual("https://files.community/docs", target, "Should fall back to main web docs URL.");
		}
	}
}
