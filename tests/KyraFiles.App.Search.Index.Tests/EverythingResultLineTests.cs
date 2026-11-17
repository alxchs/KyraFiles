// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.App.Services.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	/// <summary>
	/// F065: lines copied from a real es.exe run (-tsv -no-header -attrib -size -dm), read by the same code the app uses.
	/// </summary>
	[TestClass]
	public sealed class EverythingResultLineTests
	{
		[TestMethod]
		public void AC1_NormalFile_HasNamePathAndIsVisible()
		{
			Assert.IsTrue(EverythingResultLine.TryParse("D:\\DriveD\\Downloads\\SubFolders\\arquivos\\EXE\\7z1701-x64.exe\t8224\t1407310\t24/11/2017 08:02:38", out var row));

			Assert.AreEqual("7z1701-x64.exe", row.Name);
			Assert.AreEqual("D:\\DriveD\\Downloads\\SubFolders\\arquivos\\EXE\\7z1701-x64.exe", row.FullPath);
			Assert.AreEqual(".exe", row.Extension);
			Assert.AreEqual(1407310L, row.SizeBytes);
			Assert.IsFalse(row.IsFolder);
			Assert.IsFalse(row.IsHidden);
		}

		[TestMethod]
		public void AC6_HiddenAttribute_IsReadFromSecondColumn()
		{
			// 8226 = Archive | Hidden | NotContentIndexed
			Assert.IsTrue(EverythingResultLine.TryParse("D:\\DriveD\\Downloads\\oculto.exe\t8226\t749608\t19/12/2019 17:05:38", out var row));

			Assert.IsTrue(row.IsHidden);
			Assert.AreEqual("oculto.exe", row.Name);
			Assert.AreEqual(749608L, row.SizeBytes);
		}

		[TestMethod]
		public void Folder_FromDirectoryAttribute_HasNameAndNoSize()
		{
			// 8208 = Directory | NotContentIndexed
			Assert.IsTrue(EverythingResultLine.TryParse("D:\\DriveD\\Downloads\\SubFolders\t8208\t4096\t19/12/2019 17:05:38", out var row));

			Assert.IsTrue(row.IsFolder);
			Assert.AreEqual("SubFolders", row.Name);
			Assert.AreEqual(0L, row.SizeBytes);
			Assert.AreEqual(string.Empty, row.Extension);
		}

		[TestMethod]
		public void Folder_FromTrailingSeparator_DropsSeparatorFromPathAndName()
		{
			Assert.IsTrue(EverythingResultLine.TryParse("D:\\DriveD\\Downloads\\SubFolders\\\t\t\t", out var row));

			Assert.IsTrue(row.IsFolder);
			Assert.AreEqual("D:\\DriveD\\Downloads\\SubFolders", row.FullPath);
			Assert.AreEqual("SubFolders", row.Name);
		}

		[TestMethod]
		public void QuotedPathWithoutOtherColumns_IsStillAResult()
		{
			Assert.IsTrue(EverythingResultLine.TryParse("\"D:\\DriveD\\Downloads\\leia me.txt\"", out var row));

			Assert.AreEqual("leia me.txt", row.Name);
			Assert.IsFalse(row.IsHidden);
		}

		[TestMethod]
		public void BlankLines_AreSkipped()
		{
			Assert.IsFalse(EverythingResultLine.TryParse(null, out _));
			Assert.IsFalse(EverythingResultLine.TryParse("   ", out _));
			Assert.IsFalse(EverythingResultLine.TryParse("\t8224\t10\t", out _));
		}
	}
}
