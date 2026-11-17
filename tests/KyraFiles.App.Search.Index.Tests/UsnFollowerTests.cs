// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Buffers.Binary;
using System.Text;
using KyraFiles.App.Search.Index.Full;
using KyraFiles.App.Search.Index.Interop;
using KyraFiles.App.Search.Index.Storage;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class UsnFollowerTests
{
	[TestMethod]
	public void ValidateResumeState_DetectsCanResume_Recreated_AndTruncatedWithExplicitReason()
	{
		var currentJournal = new NativeMethods.USN_JOURNAL_DATA_V0
		{
			UsnJournalID = 0x0192837465564738UL,
			FirstUsn = 5000L,
			LowestValidUsn = 4000L,
			NextUsn = 25000L,
			MaxUsn = 1000000L
		};

		// Caso 1: Retomada normal (AC-11)
		var resumeOk = UsnFollower.ValidateResumeState(0x0192837465564738UL, 12000L, currentJournal);
		Assert.AreEqual(UsnResumeStatus.CanResume, resumeOk.Status);
		Assert.IsFalse(resumeOk.RequiresFullReindex);
		Assert.AreEqual(12000L, resumeOk.ResumeStartUsn);
		StringAssert.Contains(resumeOk.StatusReason, "Retomado do USN 12000");

		// Caso 2: Diário recriado (ID mudou) -> reindexa e explica por quê (FR-031)
		var recreated = UsnFollower.ValidateResumeState(0x0000000011112222UL, 12000L, currentJournal);
		Assert.AreEqual(UsnResumeStatus.JournalRecreated, recreated.Status);
		Assert.IsTrue(recreated.RequiresFullReindex);
		StringAssert.Contains(recreated.StatusReason, "foi recriado");

		// Caso 3: Diário truncado (USN salvo ficou abaixo de FirstUsn) -> reindexa e explica por quê (FR-031)
		var truncated = UsnFollower.ValidateResumeState(0x0192837465564738UL, 3500L, currentJournal);
		Assert.AreEqual(UsnResumeStatus.JournalTruncated, truncated.Status);
		Assert.IsTrue(truncated.RequiresFullReindex);
		StringAssert.Contains(truncated.StatusReason, "truncado");
	}

	[TestMethod]
	public void ApplyUsnBuffer_WithStoppedAndRestartedSnapshot_ReflectsCreateRenameMoveAndDeleteWithoutReindexing()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), "f015_usn_test_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);

		try
		{
			Guid volGuid = Guid.NewGuid();
			var initialIndex = new VolumeIndex(volGuid, @"C:\");
			int dirSrc = initialIndex.Add(20UL | (1UL << 48), VolumeIndex.RootParentIndex, "Src", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, NativeMethods.FILE_ATTRIBUTE_DIRECTORY);
			int dirDst = initialIndex.Add(21UL | (1UL << 48), VolumeIndex.RootParentIndex, "Dst", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, NativeMethods.FILE_ATTRIBUTE_DIRECTORY);
			initialIndex.Add(50UL | (1UL << 48), dirSrc, "antigo.txt", 100, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);
			initialIndex.Add(51UL | (1UL << 48), dirSrc, "apagar.txt", 200, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);

			// 1. Simula serviço parando e salvando snapshot com UsnJournalId e LastUsn = 10000
			string snapPath = VolumeSnapshot.GetSnapshotFilePath(tempDir, volGuid);
			const ulong journalId = 0xCAFEBABE12345678UL;
			const long savedUsn = 10_000L;
			VolumeSnapshot.Save(snapPath, initialIndex, volumeSerialNumber: 0x12345678u, usnJournalId: journalId, lastUsn: savedUsn);

			// 2. Simula serviço reiniciando: carrega snapshot e valida retomada pelo USN
			Assert.IsTrue(VolumeSnapshot.TryLoad(snapPath, out VolumeSnapshotData? loaded, expectedVolumeSerialNumber: 0x12345678u));
			Assert.IsNotNull(loaded);
			Assert.AreEqual(journalId, loaded.UsnJournalId);
			Assert.AreEqual(savedUsn, loaded.LastUsn);

			var currentJournal = new NativeMethods.USN_JOURNAL_DATA_V0
			{
				UsnJournalID = journalId,
				FirstUsn = 1000L,
				LowestValidUsn = 1000L,
				NextUsn = 10_500L
			};
			var decision = UsnFollower.ValidateResumeState(loaded.UsnJournalId, loaded.LastUsn, currentJournal);
			Assert.AreEqual(UsnResumeStatus.CanResume, decision.Status);
			Assert.IsFalse(decision.RequiresFullReindex);

			// 3. Constrói um buffer FSCTL_READ_USN_JOURNAL com 3 eventos ocurridos enquanto o serviço estava parado:
			//    - Criação de "novo_arquivo.cs" em Dst (FileRef 60, ParentRef 21, USN 10100)
			//    - Renomeação + Movimentação de "antigo.txt" (FileRef 50) de Src (20) para Dst (21) como "renomeado.txt" (USN 10200)
			//    - Exclusão de "apagar.txt" (FileRef 51, USN 10300)
			byte[] usnBatch = BuildUsnReadBuffer(
				nextStartUsn: 10_500L,
				records:
				[
					(60UL | (1UL << 48), 21UL | (1UL << 48), 10_100L, NativeMethods.USN_REASON_FILE_CREATE | NativeMethods.USN_REASON_CLOSE, 0x20u, "novo_arquivo.cs"),
					(50UL | (1UL << 48), 21UL | (1UL << 48), 10_200L, NativeMethods.USN_REASON_RENAME_NEW_NAME | NativeMethods.USN_REASON_CLOSE, 0x20u, "renomeado.txt"),
					(51UL | (1UL << 48), 20UL | (1UL << 48), 10_300L, NativeMethods.USN_REASON_FILE_DELETE | NativeMethods.USN_REASON_CLOSE, 0x20u, "apagar.txt")
				]);

			int applied = UsnFollower.ApplyUsnBuffer(
				loaded.Index,
				usnBatch,
				out long nextUsn,
				statProvider: path => path.EndsWith("novo_arquivo.cs", StringComparison.Ordinal)
					? (true, 4096L, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20u)
					: (false, 0L, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 0u));

			Assert.AreEqual(3, applied);
			Assert.AreEqual(10_500L, nextUsn);

			// Verifica que "novo_arquivo.cs" existe em C:\Dst\novo_arquivo.cs com tamanho 4096
			int newFileIdx = loaded.Index.FindByFileReference(60UL | (1UL << 48));
			Assert.IsTrue(newFileIdx >= 0);
			Assert.AreEqual(@"C:\Dst\novo_arquivo.cs", loaded.Index.GetFullPath(newFileIdx));
			Assert.AreEqual(4096L, loaded.Index.GetFileSize(newFileIdx));

			// Verifica que "antigo.txt" virou "renomeado.txt" em C:\Dst\renomeado.txt
			int renamedIdx = loaded.Index.FindByFileReference(50UL | (1UL << 48));
			Assert.IsTrue(renamedIdx >= 0);
			Assert.AreEqual("renomeado.txt", loaded.Index.GetName(renamedIdx));
			Assert.AreEqual(@"C:\Dst\renomeado.txt", loaded.Index.GetFullPath(renamedIdx));

			// Verifica que "apagar.txt" foi marcado como excluído
			Assert.AreEqual(-1, loaded.Index.FindByFileReference(51UL | (1UL << 48)));
		}
		finally
		{
			try { Directory.Delete(tempDir, recursive: true); } catch { }
		}
	}

	private static byte[] BuildUsnReadBuffer(
		long nextStartUsn,
		(ulong FileRef, ulong ParentRef, long Usn, uint Reason, uint Attributes, string Name)[] records)
	{
		using var ms = new MemoryStream();
		Span<byte> header = stackalloc byte[8];
		BinaryPrimitives.WriteInt64LittleEndian(header, nextStartUsn);
		ms.Write(header);

		long nowFt = DateTimeOffset.UtcNow.ToFileTime();
		foreach (var r in records)
		{
			byte[] nameBytes = Encoding.Unicode.GetBytes(r.Name);
			int recLen = (60 + nameBytes.Length + 7) & ~7;
			byte[] rec = new byte[recLen];
			Span<byte> s = rec;

			BinaryPrimitives.WriteUInt32LittleEndian(s[0..4], (uint)recLen);
			BinaryPrimitives.WriteUInt16LittleEndian(s[4..6], 2); // MajorVersion = 2
			BinaryPrimitives.WriteUInt16LittleEndian(s[6..8], 0); // MinorVersion = 0
			BinaryPrimitives.WriteUInt64LittleEndian(s[8..16], r.FileRef);
			BinaryPrimitives.WriteUInt64LittleEndian(s[16..24], r.ParentRef);
			BinaryPrimitives.WriteInt64LittleEndian(s[24..32], r.Usn);
			BinaryPrimitives.WriteInt64LittleEndian(s[32..40], nowFt);
			BinaryPrimitives.WriteUInt32LittleEndian(s[40..44], r.Reason);
			BinaryPrimitives.WriteUInt32LittleEndian(s[52..56], r.Attributes);
			BinaryPrimitives.WriteUInt16LittleEndian(s[56..58], (ushort)nameBytes.Length);
			BinaryPrimitives.WriteUInt16LittleEndian(s[58..60], 60); // FileNameOffset
			nameBytes.CopyTo(s[60..]);

			ms.Write(rec);
		}

		return ms.ToArray();
	}
}
