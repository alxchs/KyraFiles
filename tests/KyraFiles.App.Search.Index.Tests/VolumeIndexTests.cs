// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.App.Search.Index.Storage;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class VolumeIndexTests
{
	[TestMethod]
	public void VolumeIndex_AddAndGetFullPath_ResolvesHierarchy()
	{
		var volume = new VolumeIndex(Guid.NewGuid(), "C:\\");

		// Adiciona pasta raiz C:\Users
		int usersDir = volume.Add(1001, -1, "Users", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);

		// Adiciona subpasta C:\Users\Alexandre
		int alexandreDir = volume.Add(1002, usersDir, "Alexandre", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);

		// Adiciona subpasta C:\Users\Alexandre\Downloads
		int downloadsDir = volume.Add(1003, alexandreDir, "Downloads", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);

		// Adiciona arquivo C:\Users\Alexandre\Downloads\relatorio.pdf
		int fileIndex = volume.Add(1004, downloadsDir, "relatorio.pdf", 1024, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);

		Assert.AreEqual("C:\\Users", volume.GetFullPath(usersDir));
		Assert.AreEqual("C:\\Users\\Alexandre", volume.GetFullPath(alexandreDir));
		Assert.AreEqual("C:\\Users\\Alexandre\\Downloads", volume.GetFullPath(downloadsDir));
		Assert.AreEqual("C:\\Users\\Alexandre\\Downloads\\relatorio.pdf", volume.GetFullPath(fileIndex));

		var item = volume.GetItem(fileIndex);
		Assert.AreEqual("relatorio.pdf", item.Name);
		Assert.AreEqual("C:\\Users\\Alexandre\\Downloads\\relatorio.pdf", item.FullPath);
		Assert.AreEqual(1024, item.FileSize);
		Assert.IsFalse(item.IsDirectory);
	}
}

