// Copyright (c) Files Community. Licensed under the MIT License.

using System.Buffers.Binary;
using System.Diagnostics;
using Files.App.Search.Index.Engine;
using Files.App.Search.Index.Models;
using Files.App.Search.Index.Query;
using Files.App.Search.Index.Storage;

namespace Files.App.Search.Index.Tests;

[TestClass]
public sealed class VolumeSnapshotTests
{
	private static string GetTempTestDirectory()
	{
		string baseDir = Path.Combine(
			Path.GetTempPath(),
			"claude",
			"desenv",
			"utils",
			"FilesApp",
			"f015",
			"snapshot_" + Guid.NewGuid().ToString("N")[..8]);

		Directory.CreateDirectory(baseDir);
		return baseDir;
	}

	[TestMethod]
	public void SaveAndLoad_PreservesAllFieldsAndFolderDates()
	{
		string tempDir = GetTempTestDirectory();
		try
		{
			Guid volGuid = Guid.NewGuid();
			var index = new VolumeIndex(volGuid, @"C:\");
			var rootTime = new DateTimeOffset(2026, 10, 12, 14, 30, 0, TimeSpan.Zero);
			var folderTime = new DateTimeOffset(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);
			var fileTime = new DateTimeOffset(2026, 10, 12, 16, 0, 0, TimeSpan.Zero);

			int dirIdx = index.Add(100UL, -1, "Projetos", 0L, folderTime, folderTime, 0x10u);
			index.Add(101UL, dirIdx, "Relatório_Ação.pdf", 4096L, fileTime, fileTime, 0x20u);

			string filePath = VolumeSnapshot.GetSnapshotFilePath(tempDir, volGuid);
			VolumeSnapshot.Save(
				filePath,
				index,
				volumeSerialNumber: 0x1234ABCDu,
				rootLastWriteTime: rootTime,
				usnJournalId: 999888777UL,
				lastUsn: 123456789L);

			Assert.IsTrue(File.Exists(filePath));

			bool loaded = VolumeSnapshot.TryLoad(
				filePath,
				out VolumeSnapshotData? data,
				expectedVolumeSerialNumber: 0x1234ABCDu);

			Assert.IsTrue(loaded);
			Assert.IsNotNull(data);
			Assert.AreEqual(0x1234ABCDu, data.VolumeSerialNumber);
			Assert.AreEqual(rootTime, data.RootLastWriteTime);
			Assert.AreEqual(999888777UL, data.UsnJournalId);
			Assert.AreEqual(123456789L, data.LastUsn);
			Assert.AreEqual(2, data.Index.Count);
			Assert.AreEqual(folderTime, data.Index.GetLastWriteTime(0));
			Assert.AreEqual(@"C:\Projetos\Relatório_Ação.pdf", data.Index.GetFullPath(1));

			var batches = new List<IReadOnlyList<SearchResultItem>>();
			new QueryEngine().Execute(data.Index, new QueryMatcher("relatorio acao"), b => batches.Add(b));
			Assert.AreEqual(1, batches.Sum(b => b.Count));
		}
		finally
		{
			try { Directory.Delete(tempDir, recursive: true); } catch { }
		}
	}

	[TestMethod]
	public void TryLoad_WrongVersion_DiscardsAndDeletesFile()
	{
		string tempDir = GetTempTestDirectory();
		try
		{
			Guid volGuid = Guid.NewGuid();
			var index = new VolumeIndex(volGuid, @"C:\");
			index.Add(1UL, -1, "teste.txt", 10L, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20u);

			string filePath = VolumeSnapshot.GetSnapshotFilePath(tempDir, volGuid);
			VolumeSnapshot.Save(filePath, index, volumeSerialNumber: 0x11112222u);

			// Adultera os bytes de versão [4..8] para uma versão incompatível (999)
			byte[] bytes = File.ReadAllBytes(filePath);
			BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), 999u);
			File.WriteAllBytes(filePath, bytes);

			bool loaded = VolumeSnapshot.TryLoad(filePath, out VolumeSnapshotData? data, deleteIfInvalid: true);

			Assert.IsFalse(loaded, "Snapshot com versão errada deve ser rejeitado.");
			Assert.IsNull(data);
			Assert.IsFalse(File.Exists(filePath), "Arquivo de snapshot com versão errada deve ser descartado do disco.");
		}
		finally
		{
			try { Directory.Delete(tempDir, recursive: true); } catch { }
		}
	}

	[TestMethod]
	public void TryLoad_CDriveScaleSnapshot_LoadsInUnderTwoSeconds_NFR004()
	{
		string tempDir = GetTempTestDirectory();
		try
		{
			const int cDriveItemCount = 2_500_000;
			Guid volGuid = Guid.NewGuid();
			var index = new VolumeIndex(volGuid, @"C:\", initialCapacity: cDriveItemCount);
			var now = DateTimeOffset.UtcNow;

			int rootDir = index.Add(5UL, -1, "Windows", 0, now, now, 0x10u);
			for (int i = 1; i < cDriveItemCount; i++)
			{
				index.Add((ulong)(100 + i), rootDir, "arquivo_sistema_indice.dll", 16384L, now, now, 0x20u);
			}

			string filePath = VolumeSnapshot.GetSnapshotFilePath(tempDir, volGuid);
			uint serial = VolumeSnapshot.GetVolumeSerialNumber(@"C:\");
			VolumeSnapshot.Save(filePath, index, volumeSerialNumber: serial);

			long fileSizeBytes = new FileInfo(filePath).Length;

			var sw = Stopwatch.StartNew();
			bool loaded = VolumeSnapshot.TryLoad(filePath, out VolumeSnapshotData? data, expectedVolumeSerialNumber: serial);
			sw.Stop();

			Assert.IsTrue(loaded);
			Assert.IsNotNull(data);
			Assert.AreEqual(cDriveItemCount, data.Index.Count);

			Console.WriteLine($"[T8 Benchmark] Itens: {cDriveItemCount:N0} | Tamanho snapshot: {fileSizeBytes / (1024.0 * 1024.0):F2} MB | Tempo de carga: {sw.ElapsedMilliseconds} ms");
			Assert.IsTrue(sw.ElapsedMilliseconds < 2000, $"Carga do snapshot do C: levou {sw.ElapsedMilliseconds} ms (meta NFR-004 < 2000 ms).");
		}
		finally
		{
			try { Directory.Delete(tempDir, recursive: true); } catch { }
		}
	}
}
