// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Buffers.Binary;
using System.Text;
using KyraFiles.App.Search.Index.Full;
using KyraFiles.App.Search.Index.Interop;
using KyraFiles.App.Search.Index.Storage;

namespace KyraFiles.App.Search.Index.Tests;

[TestClass]
public sealed class MftReaderTests
{
	[TestMethod]
	public void ApplyFixups_RestoresSectorEndWords_AndRejectsTornWrite()
	{
		byte[] record = CreateBlankRecord(recordNumber: 32, sequenceNumber: 1, isDirectory: false, baseRecordRef: 0);
		FinishRecordWithFixups(record, usn: 0xABCD);

		// Antes de ApplyFixups, os dois últimos bytes de cada setor de 512 bytes contêm o USN 0xABCD
		Assert.AreEqual((ushort)0xABCD, BinaryPrimitives.ReadUInt16LittleEndian(record.AsSpan(510, 2)));
		Assert.AreEqual((ushort)0xABCD, BinaryPrimitives.ReadUInt16LittleEndian(record.AsSpan(1022, 2)));

		byte[] torn = (byte[])record.Clone();
		BinaryPrimitives.WriteUInt16LittleEndian(torn.AsSpan(1022, 2), 0x1234);
		Assert.IsFalse(MftReader.ApplyFixups(torn));

		Assert.IsTrue(MftReader.ApplyFixups(record));
		Assert.AreEqual((ushort)0x1122, BinaryPrimitives.ReadUInt16LittleEndian(record.AsSpan(510, 2)));
		Assert.AreEqual((ushort)0x3344, BinaryPrimitives.ReadUInt16LittleEndian(record.AsSpan(1022, 2)));
	}

	[TestMethod]
	public void ParseMftStream_IgnoresDos83_AndKeepsFirstNonDosHardLink_AndExtensionRecordSize()
	{
		// Constrói um fluxo sintético de 40 registros de 1024 bytes:
		// - Registro 5: raiz "."
		// - Registro 20: pasta "Projetos" (filha de 5)
		// - Registro 21: pasta "OutraPasta" (filha de 5)
		// - Registro 22: pasta com FILE_ATTRIBUTE_REPARSE_POINT "LinkJunção" (filha de 5)
		// - Registro 23: arquivo dentro de "LinkJunção" (NÃO deve entrar por FR-012)
		// - Registro 30: arquivo com 3 nomes ($FILE_NAME DOS "RELATO~1.TXT", $FILE_NAME Win32 "Relatório_Original.txt" em 20, $FILE_NAME Win32 "Relatório_Link2.txt" em 21)
		//                e registro de extensão 31 contendo o atributo $DATA não-residente de 987654 bytes.
		const int totalRecords = 40;
		byte[] streamBytes = new byte[totalRecords * MftReader.DefaultRecordSize];

		// Registro 5: raiz "."
		WriteRecordToStream(streamBytes, 5, CreateDirectoryRecord(5, 5, 5, ".", 0x10));

		// Registro 20: "Projetos"
		WriteRecordToStream(streamBytes, 20, CreateDirectoryRecord(20, 1, 5, "Projetos", 0x10));

		// Registro 21: "OutraPasta"
		WriteRecordToStream(streamBytes, 21, CreateDirectoryRecord(21, 1, 5, "OutraPasta", 0x10));

		// Registro 22: "LinkJunção" (diretório com reparse point)
		WriteRecordToStream(streamBytes, 22, CreateDirectoryRecord(22, 1, 5, "LinkJunção", 0x10 | NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT));

		// Registro 23: "DentroDaJuncao.txt" (filho de 22 — deve ser ignorado por FR-012)
		WriteRecordToStream(streamBytes, 23, CreateFileRecordWithNames(
			recordNumber: 23,
			sequenceNumber: 1,
			attributes: 0x20,
			names: [(22UL, MftReader.FileNameNamespaceWin32, "DentroDaJuncao.txt", 100L)],
			nonResidentDataSize: null));

		// Registro 30: base record com nome DOS 8.3 primeiro, depois primeiro Win32 em "Projetos" (20), depois segundo hardlink Win32 em "OutraPasta" (21)
		WriteRecordToStream(streamBytes, 30, CreateFileRecordWithNames(
			recordNumber: 30,
			sequenceNumber: 3,
			attributes: 0x20,
			names:
			[
				(20UL, MftReader.FileNameNamespaceDos, "RELATO~1.TXT", 10L),
				(20UL, MftReader.FileNameNamespaceWin32, "Relatório_Original.txt", 10L),
				(21UL, MftReader.FileNameNamespaceWin32, "Relatório_Link2.txt", 10L)
			],
			nonResidentDataSize: null,
			includeAttributeList: true));

		// Registro 31: registro de extensão apontando para o registro base 30 com $DATA não-residente de 987.654 bytes
		ulong baseRef30 = 30UL | (3UL << 48);
		WriteRecordToStream(streamBytes, 31, CreateExtensionDataRecord(31, 1, baseRef30, 987_654L));

		using var ms = new MemoryStream(streamBytes, writable: false);
		Guid volGuid = Guid.NewGuid();
		VolumeIndex index = MftReader.ParseMftStream(volGuid, @"C:\", ms);

		// Esperados:
		// 1. Projetos (dir)
		// 2. OutraPasta (dir)
		// 3. LinkJunção (dir reparse, registrado como item mas sem descer em 23)
		// 4. Relatório_Original.txt (em C:\Projetos\Relatório_Original.txt, tamanho 987654 vindo do registro de extensão 31)
		Assert.AreEqual(4, index.Count);

		int fileIdx = index.FindByFileReference(30UL | (3UL << 48));
		Assert.IsTrue(fileIdx >= 0, "Arquivo do registro 30 deve estar no índice.");
		Assert.AreEqual("Relatório_Original.txt", index.GetName(fileIdx));
		Assert.AreEqual(@"C:\Projetos\Relatório_Original.txt", index.GetFullPath(fileIdx));
		Assert.AreEqual(987_654L, index.GetFileSize(fileIdx));

		// Verifica que o filho da junção (registro 23) não entrou
		Assert.AreEqual(-1, index.FindByFileReference(23UL | (1UL << 48)));
	}

	[TestMethod]
	public void CompareWithBasicIndex_IdentifiesCommonAndExclusiveKeys()
	{
		Guid g = Guid.NewGuid();
		var mftIndex = new VolumeIndex(g, @"C:\");
		int dirA = mftIndex.Add(100UL | (1UL << 48), VolumeIndex.RootParentIndex, "Users", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, NativeMethods.FILE_ATTRIBUTE_DIRECTORY);
		mftIndex.Add(101UL | (1UL << 48), dirA, "public.txt", 128, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);
		mftIndex.Add(102UL | (1UL << 48), dirA, "restricted_admin.dat", 512, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);

		var basicIndex = new VolumeIndex(g, @"C:\");
		int bDirA = basicIndex.Add(100UL | (1UL << 48), VolumeIndex.RootParentIndex, "Users", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, NativeMethods.FILE_ATTRIBUTE_DIRECTORY);
		basicIndex.Add(101UL | (1UL << 48), bDirA, "public.txt", 128, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0x20);

		var diff = MftReader.CompareWithBasicIndex(mftIndex, basicIndex);
		Assert.AreEqual(2, diff.CommonKeyCount);
		Assert.AreEqual(1, diff.MftOnlyEntries.Count);
		Assert.AreEqual(@"C:\Users\restricted_admin.dat", diff.MftOnlyEntries[0].Path);
		Assert.AreEqual(0, diff.BasicOnlyEntries.Count);
	}

	private static void WriteRecordToStream(byte[] streamBytes, int recordIndex, byte[] record)
	{
		Buffer.BlockCopy(record, 0, streamBytes, recordIndex * MftReader.DefaultRecordSize, MftReader.DefaultRecordSize);
	}

	private static byte[] CreateBlankRecord(ulong recordNumber, ushort sequenceNumber, bool isDirectory, ulong baseRecordRef)
	{
		byte[] rec = new byte[MftReader.DefaultRecordSize];
		Span<byte> s = rec;
		BinaryPrimitives.WriteUInt32LittleEndian(s[0..4], 0x454C4946); // "FILE"
		BinaryPrimitives.WriteUInt16LittleEndian(s[4..6], 48); // USA offset
		BinaryPrimitives.WriteUInt16LittleEndian(s[6..8], 3);  // USA count (USN + 2 sectors)
		BinaryPrimitives.WriteUInt16LittleEndian(s[16..18], sequenceNumber);
		BinaryPrimitives.WriteUInt16LittleEndian(s[18..20], 1); // HardLinkCount
		BinaryPrimitives.WriteUInt16LittleEndian(s[20..22], 56); // FirstAttributeOffset (8-byte aligned after USA)
		ushort flags = (ushort)(0x0001 | (isDirectory ? 0x0002 : 0));
		BinaryPrimitives.WriteUInt16LittleEndian(s[22..24], flags);
		BinaryPrimitives.WriteUInt32LittleEndian(s[24..28], 64); // BytesInUse placeholder
		BinaryPrimitives.WriteUInt32LittleEndian(s[28..32], MftReader.DefaultRecordSize);
		BinaryPrimitives.WriteUInt64LittleEndian(s[32..40], baseRecordRef);
		BinaryPrimitives.WriteUInt32LittleEndian(s[44..48], (uint)recordNumber);

		// Palavras de teste no final dos setores 0 e 1 antes do fixup
		BinaryPrimitives.WriteUInt16LittleEndian(s.Slice(510, 2), 0x1122);
		BinaryPrimitives.WriteUInt16LittleEndian(s.Slice(1022, 2), 0x3344);
		return rec;
	}

	private static void FinishRecordWithFixups(byte[] rec, ushort usn = 0x55AA)
	{
		Span<byte> s = rec;
		ushort usaOffset = BinaryPrimitives.ReadUInt16LittleEndian(s[4..6]);
		ushort w0 = BinaryPrimitives.ReadUInt16LittleEndian(s.Slice(510, 2));
		ushort w1 = BinaryPrimitives.ReadUInt16LittleEndian(s.Slice(1022, 2));

		BinaryPrimitives.WriteUInt16LittleEndian(s.Slice(usaOffset, 2), usn);
		BinaryPrimitives.WriteUInt16LittleEndian(s.Slice(usaOffset + 2, 2), w0);
		BinaryPrimitives.WriteUInt16LittleEndian(s.Slice(usaOffset + 4, 2), w1);

		BinaryPrimitives.WriteUInt16LittleEndian(s.Slice(510, 2), usn);
		BinaryPrimitives.WriteUInt16LittleEndian(s.Slice(1022, 2), usn);
	}

	private static byte[] CreateDirectoryRecord(ulong recordNumber, ushort sequenceNumber, ulong parentRecordNumber, string name, uint attributes)
	{
		byte[] rec = CreateBlankRecord(recordNumber, sequenceNumber, isDirectory: true, baseRecordRef: 0);
		int offset = 56;
		offset = WriteStandardInfoAttribute(rec, offset, attributes);
		offset = WriteFileNameAttribute(rec, offset, parentRecordNumber, MftReader.FileNameNamespaceWin32, name, 0L, attributes);
		WriteEndMarker(rec, offset);
		FinishRecordWithFixups(rec);
		return rec;
	}

	private static byte[] CreateFileRecordWithNames(
		ulong recordNumber,
		ushort sequenceNumber,
		uint attributes,
		(ulong ParentRec, byte Namespace, string Name, long FnSize)[] names,
		long? nonResidentDataSize,
		bool includeAttributeList = false)
	{
		byte[] rec = CreateBlankRecord(recordNumber, sequenceNumber, isDirectory: false, baseRecordRef: 0);
		int offset = 56;
		offset = WriteStandardInfoAttribute(rec, offset, attributes);
		if (includeAttributeList)
		{
			offset = WriteDummyAttributeList(rec, offset);
		}
		foreach (var n in names)
		{
			offset = WriteFileNameAttribute(rec, offset, n.ParentRec, n.Namespace, n.Name, n.FnSize, attributes);
		}
		if (nonResidentDataSize.HasValue)
		{
			offset = WriteNonResidentDataAttribute(rec, offset, nonResidentDataSize.Value);
		}
		WriteEndMarker(rec, offset);
		FinishRecordWithFixups(rec);
		return rec;
	}

	private static byte[] CreateExtensionDataRecord(ulong recordNumber, ushort sequenceNumber, ulong baseRecordRef, long realSize)
	{
		byte[] rec = CreateBlankRecord(recordNumber, sequenceNumber, isDirectory: false, baseRecordRef: baseRecordRef);
		int offset = 56;
		offset = WriteNonResidentDataAttribute(rec, offset, realSize);
		WriteEndMarker(rec, offset);
		FinishRecordWithFixups(rec);
		return rec;
	}

	private static int WriteStandardInfoAttribute(byte[] rec, int offset, uint attributes)
	{
		const int valLen = 48;
		const int attrLen = 24 + valLen;
		Span<byte> s = rec.AsSpan(offset, attrLen);
		BinaryPrimitives.WriteUInt32LittleEndian(s[0..4], 0x10); // $STANDARD_INFORMATION
		BinaryPrimitives.WriteUInt32LittleEndian(s[4..8], (uint)attrLen);
		s[8] = 0; // Resident
		BinaryPrimitives.WriteUInt32LittleEndian(s[16..20], (uint)valLen);
		BinaryPrimitives.WriteUInt16LittleEndian(s[20..22], 24);

		long nowFt = DateTimeOffset.UtcNow.ToFileTime();
		Span<byte> val = s.Slice(24, valLen);
		BinaryPrimitives.WriteInt64LittleEndian(val[0..8], nowFt);
		BinaryPrimitives.WriteInt64LittleEndian(val[8..16], nowFt);
		BinaryPrimitives.WriteUInt32LittleEndian(val[32..36], attributes);
		return offset + attrLen;
	}

	private static int WriteFileNameAttribute(byte[] rec, int offset, ulong parentRec, byte nameNamespace, string name, long dataSize, uint attributes)
	{
		byte[] nameBytes = Encoding.Unicode.GetBytes(name);
		int valLen = 66 + nameBytes.Length;
		int attrLen = (24 + valLen + 7) & ~7;
		Span<byte> s = rec.AsSpan(offset, attrLen);
		BinaryPrimitives.WriteUInt32LittleEndian(s[0..4], 0x30); // $FILE_NAME
		BinaryPrimitives.WriteUInt32LittleEndian(s[4..8], (uint)attrLen);
		s[8] = 0; // Resident
		BinaryPrimitives.WriteUInt32LittleEndian(s[16..20], (uint)valLen);
		BinaryPrimitives.WriteUInt16LittleEndian(s[20..22], 24);

		long nowFt = DateTimeOffset.UtcNow.ToFileTime();
		Span<byte> val = s.Slice(24, valLen);
		BinaryPrimitives.WriteUInt64LittleEndian(val[0..8], parentRec | (1UL << 48));
		BinaryPrimitives.WriteInt64LittleEndian(val[8..16], nowFt);
		BinaryPrimitives.WriteInt64LittleEndian(val[16..24], nowFt);
		BinaryPrimitives.WriteInt64LittleEndian(val[48..56], dataSize);
		BinaryPrimitives.WriteUInt32LittleEndian(val[56..60], attributes);
		val[64] = (byte)name.Length;
		val[65] = nameNamespace;
		nameBytes.CopyTo(val[66..]);
		return offset + attrLen;
	}

	private static int WriteNonResidentDataAttribute(byte[] rec, int offset, long realSize)
	{
		const int attrLen = 64;
		Span<byte> s = rec.AsSpan(offset, attrLen);
		BinaryPrimitives.WriteUInt32LittleEndian(s[0..4], 0x80); // $DATA
		BinaryPrimitives.WriteUInt32LittleEndian(s[4..8], (uint)attrLen);
		s[8] = 1; // Non-resident
		s[9] = 0; // Unnamed default stream
		BinaryPrimitives.WriteUInt64LittleEndian(s[16..24], 0UL); // StartingVcn = 0
		BinaryPrimitives.WriteInt64LittleEndian(s[48..56], realSize);
		return offset + attrLen;
	}

	private static int WriteDummyAttributeList(byte[] rec, int offset)
	{
		const int valLen = 32;
		const int attrLen = 24 + valLen;
		Span<byte> s = rec.AsSpan(offset, attrLen);
		BinaryPrimitives.WriteUInt32LittleEndian(s[0..4], 0x20); // $ATTRIBUTE_LIST
		BinaryPrimitives.WriteUInt32LittleEndian(s[4..8], (uint)attrLen);
		s[8] = 0; // Resident
		BinaryPrimitives.WriteUInt32LittleEndian(s[16..20], (uint)valLen);
		BinaryPrimitives.WriteUInt16LittleEndian(s[20..22], 24);
		return offset + attrLen;
	}

	private static void WriteEndMarker(byte[] rec, int offset)
	{
		Span<byte> s = rec.AsSpan();
		BinaryPrimitives.WriteUInt32LittleEndian(s.Slice(offset, 4), 0xFFFFFFFF);
		BinaryPrimitives.WriteUInt32LittleEndian(s[24..28], (uint)(offset + 8));
	}
}
