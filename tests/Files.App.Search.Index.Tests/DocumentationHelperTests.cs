// Copyright (c) Files Community
// Licensed under the MIT License.

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

