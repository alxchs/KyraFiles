// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using Files.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.App.Search.Index.Tests
{
	[TestClass]
	public class PathFormatterTests
	{
		[TestMethod]
		public void Test_AC1_Windows()
		{
			var path = @"C:\desenv\x y\a.txt";
			var result = PathFormatter.Format(path, PathFormat.Windows);
			Assert.AreEqual(@"C:\desenv\x y\a.txt", result);
		}

		[TestMethod]
		public void Test_AC2_Wsl()
		{
			var path = @"C:\desenv\x y\a.txt";
			var result = PathFormatter.Format(path, PathFormat.Wsl);
			Assert.AreEqual("/mnt/c/desenv/x y/a.txt", result);
		}

		[TestMethod]
		public void Test_AC3_Json()
		{
			var path = @"C:\desenv\x y\a.txt";
			var result = PathFormatter.Format(path, PathFormat.Json);
			Assert.AreEqual("\"C:\\\\desenv\\\\x y\\\\a.txt\"", result);
		}

		[TestMethod]
		public void Test_AC4_CSharpLiteral()
		{
			var path = @"C:\desenv\x y\a.txt";
			var result = PathFormatter.Format(path, PathFormat.CSharpLiteral);
			Assert.AreEqual("@\"C:\\desenv\\x y\\a.txt\"", result);
		}

		[TestMethod]
		public void Test_AC5_FileUrl()
		{
			var path = @"C:\desenv\x y\a.txt";
			var result = PathFormatter.Format(path, PathFormat.FileUrl);
			Assert.AreEqual("file:///C:/desenv/x%20y/a.txt", result);
		}

		[TestMethod]
		public void Test_AC6_RelativeToRepo()
		{
			var path = @"C:\desenv\x y\a.txt";
			var repoRoot = @"C:\desenv";
			var result = PathFormatter.Format(path, PathFormat.RelativeToRepo, repositoryRoot: repoRoot);
			Assert.AreEqual("x y/a.txt", result);
		}

		[TestMethod]
		public void Test_AC6_RelativeToRepo_OutsideRepo_Throws()
		{
			var path = @"D:\other\a.txt";
			var repoRoot = @"C:\desenv";
			try
			{
				PathFormatter.Format(path, PathFormat.RelativeToRepo, repositoryRoot: repoRoot);
				Assert.Fail("Expected InvalidOperationException was not thrown.");
			}
			catch (InvalidOperationException)
			{
				// Expected
			}
		}

		[TestMethod]
		public void Test_AC8_Wsl_UncPath()
		{
			var path = @"\\srv\pasta\a.txt";
			var result = PathFormatter.Format(path, PathFormat.Wsl);
			Assert.AreEqual("//srv/pasta/a.txt", result);
		}

		[TestMethod]
		public void Test_RelativeToCurrentFolder()
		{
			var path = @"C:\desenv\x y\a.txt";
			var resultDirect = PathFormatter.Format(path, PathFormat.RelativeToCurrentFolder, currentFolder: @"C:\desenv\x y");
			Assert.AreEqual("a.txt", resultDirect);

			var resultSubfolder = PathFormatter.Format(path, PathFormat.RelativeToCurrentFolder, currentFolder: @"C:\desenv");
			Assert.AreEqual("x y/a.txt", resultSubfolder);
		}

		[TestMethod]
		public void Test_SpecialCharactersAndAccents()
		{
			var path = @"C:\desenv\projeto áéí\arquivo ç.txt";
			Assert.AreEqual(@"C:\desenv\projeto áéí\arquivo ç.txt", PathFormatter.Format(path, PathFormat.Windows));
			Assert.AreEqual("/mnt/c/desenv/projeto áéí/arquivo ç.txt", PathFormatter.Format(path, PathFormat.Wsl));
			Assert.AreEqual("\"C:\\\\desenv\\\\projeto áéí\\\\arquivo ç.txt\"", PathFormatter.Format(path, PathFormat.Json));
			Assert.AreEqual("@\"C:\\desenv\\projeto áéí\\arquivo ç.txt\"", PathFormatter.Format(path, PathFormat.CSharpLiteral));
			Assert.AreEqual("file:///C:/desenv/projeto%20%C3%A1%C3%A9%C3%AD/arquivo%20%C3%A7.txt", PathFormatter.Format(path, PathFormat.FileUrl));
		}

		[TestMethod]
		public void Test_LongPath_PrefixStripped()
		{
			var longPath = @"\\?\C:\desenv\x y\a.txt";
			Assert.AreEqual(@"C:\desenv\x y\a.txt", PathFormatter.Format(longPath, PathFormat.Windows));
			Assert.AreEqual("/mnt/c/desenv/x y/a.txt", PathFormatter.Format(longPath, PathFormat.Wsl));

			var longUnc = @"\\?\UNC\srv\pasta\a.txt";
			Assert.AreEqual(@"\\srv\pasta\a.txt", PathFormatter.Format(longUnc, PathFormat.Windows));
			Assert.AreEqual("//srv/pasta/a.txt", PathFormatter.Format(longUnc, PathFormat.Wsl));
		}

		[TestMethod]
		public void Test_FormatAll_MultiSelection()
		{
			var items = new[] { @"C:\desenv\a.txt", @"C:\desenv\b.txt" };
			var resultWindows = PathFormatter.FormatAll(items, PathFormat.Windows);
			Assert.AreEqual("C:\\desenv\\a.txt\nC:\\desenv\\b.txt", resultWindows);

			var resultWsl = PathFormatter.FormatAll(items, PathFormat.Wsl);
			Assert.AreEqual("/mnt/c/desenv/a.txt\n/mnt/c/desenv/b.txt", resultWsl);
		}

		[TestMethod]
		public void Test_AC7_MultiSelection_AllFormats()
		{
			var items = new[] { @"C:\desenv\a.txt", @"C:\desenv\sub\b.txt", @"C:\desenv\c d.txt" };
			var repoRoot = @"C:\desenv";
			var currentFolder = @"C:\desenv";

			// Windows
			var win = PathFormatter.FormatAll(items, PathFormat.Windows);
			Assert.AreEqual("C:\\desenv\\a.txt\nC:\\desenv\\sub\\b.txt\nC:\\desenv\\c d.txt", win);

			// WSL
			var wsl = PathFormatter.FormatAll(items, PathFormat.Wsl);
			Assert.AreEqual("/mnt/c/desenv/a.txt\n/mnt/c/desenv/sub/b.txt\n/mnt/c/desenv/c d.txt", wsl);

			// JSON
			var json = PathFormatter.FormatAll(items, PathFormat.Json);
			Assert.AreEqual("\"C:\\\\desenv\\\\a.txt\"\n\"C:\\\\desenv\\\\sub\\\\b.txt\"\n\"C:\\\\desenv\\\\c d.txt\"", json);

			// C# literal
			var cs = PathFormatter.FormatAll(items, PathFormat.CSharpLiteral);
			Assert.AreEqual("@\"C:\\desenv\\a.txt\"\n@\"C:\\desenv\\sub\\b.txt\"\n@\"C:\\desenv\\c d.txt\"", cs);

			// File URL
			var url = PathFormatter.FormatAll(items, PathFormat.FileUrl);
			Assert.AreEqual("file:///C:/desenv/a.txt\nfile:///C:/desenv/sub/b.txt\nfile:///C:/desenv/c%20d.txt", url);

			// Relative to repository
			var relRepo = PathFormatter.FormatAll(items, PathFormat.RelativeToRepo, repositoryRoot: repoRoot);
			Assert.AreEqual("a.txt\nsub/b.txt\nc d.txt", relRepo);

			// Relative to current folder
			var relFolder = PathFormatter.FormatAll(items, PathFormat.RelativeToCurrentFolder, currentFolder: currentFolder);
			Assert.AreEqual("a.txt\nsub/b.txt\nc d.txt", relFolder);
		}
	}
}
