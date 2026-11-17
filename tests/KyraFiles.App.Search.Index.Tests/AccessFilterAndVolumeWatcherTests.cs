// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.App.Search.Index.Security;
using KyraFiles.App.Search.Index.Storage;
using KyraFiles.App.Search.Index.Volumes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public class AccessFilterAndVolumeWatcherTests
{
	[TestMethod]
	public void AccessFilter_AllowsAllowedSubtree_AndCachesResults()
	{
		var index = new VolumeIndex(Guid.NewGuid(), @"C:\");
		int folderA = index.Add(1, VolumeIndex.RootParentIndex, "FolderA", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);
		int subB = index.Add(2, folderA, "SubB", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);
		int file1 = index.Add(3, subB, "doc.txt", 100, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);

		int customCheckerCalls = 0;
		var filter = new AccessFilter(index, path =>
		{
			customCheckerCalls++;
			return true;
		});

		Assert.IsTrue(filter.CanAccessEntry(file1));
		int checksAfterFirst = filter.NativeChecksCount;
		Assert.IsTrue(checksAfterFirst > 0);

		// Segunda verificação do mesmo arquivo deve bater no cache sem invocar mais chamadas nativas
		Assert.IsTrue(filter.CanAccessEntry(file1));
		Assert.AreEqual(checksAfterFirst, filter.NativeChecksCount);
	}

	[TestMethod]
	public void AccessFilter_DeniesRestrictedDirectory_AndDescendants()
	{
		var index = new VolumeIndex(Guid.NewGuid(), @"C:\");
		int secretDir = index.Add(10, VolumeIndex.RootParentIndex, "Secret", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x10);
		int fileInSecret = index.Add(11, secretDir, "password.txt", 50, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);

		var filter = new AccessFilter(index, path =>
		{
			// Nega acesso à pasta Secret
			return !path.Contains("Secret", StringComparison.OrdinalIgnoreCase);
		});

		Assert.IsFalse(filter.CanAccessEntry(secretDir));
		Assert.IsFalse(filter.CanAccessEntry(fileInSecret));
	}

	[TestMethod]
	public void AccessFilter_PipeSecurityAndCacheAcl_AreConfigured()
	{
		var pipeSecurity = AccessFilter.CreateLocalAuthenticatedPipeSecurity();
		Assert.IsNotNull(pipeSecurity);

		string defaultCache = AccessFilter.GetDefaultFullModeCacheDirectory();
		Assert.IsTrue(defaultCache.Contains("KyraFiles", StringComparison.OrdinalIgnoreCase));
		Assert.IsTrue(defaultCache.Contains("Index", StringComparison.OrdinalIgnoreCase));

		string tempDir = Path.Combine(Path.GetTempPath(), "f015_acl_test_" + Guid.NewGuid().ToString("N")[..8]);
		try
		{
			AccessFilter.EnsureSecureCacheDirectoryAcl(tempDir);
			Assert.IsTrue(Directory.Exists(tempDir));
		}
		finally
		{
			if (Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}

	[TestMethod]
	public void VolumeWatcher_DetectsArrivalAndRemoval_AndReusesSnapshot()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "f015_watcher_test_" + Guid.NewGuid().ToString("N")[..8]);
		Directory.CreateDirectory(tempDir);

		try
		{
			uint testSerial = 0x87654321;
			string snapshotFile = Path.Combine(tempDir, "volume_test.fcsn");

			// Grava cabeçalho simulado de snapshot com o número de série
			using (var fs = File.Create(snapshotFile))
			using (var writer = new BinaryWriter(fs))
			{
				writer.Write((uint)0x4E534346); // Magic 'FCSN'
				writer.Write((uint)1);          // Versão 1
				writer.Write(testSerial);       // Serial do volume
				writer.Write((long)0);          // Checksum
			}

			var guid1 = Guid.NewGuid();
			var vol1 = new VolumeInfo(guid1, @"\\?\Volume{" + guid1 + @"}\", @"T:\", new[] { @"T:\" }, 3);

			var volTable = new VolumeTable(new[] { vol1 });
			using var watcher = new VolumeWatcher(volTable, tempDir);

			// Testa detecção de reaproveitamento de snapshot pelo número de série
			bool foundSnapshot = watcher.TryFindReusableSnapshot(vol1, testSerial, out string? reusedPath);
			Assert.IsTrue(foundSnapshot);
			Assert.AreEqual(snapshotFile, reusedPath);

			bool nonExistent = watcher.TryFindReusableSnapshot(vol1, 0x99999999, out _);
			Assert.IsFalse(nonExistent);
		}
		finally
		{
			if (Directory.Exists(tempDir))
				Directory.Delete(tempDir, recursive: true);
		}
	}
}
