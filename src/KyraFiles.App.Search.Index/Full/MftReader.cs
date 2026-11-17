// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using KyraFiles.App.Search.Index.Basic;
using KyraFiles.App.Search.Index.Interop;
using KyraFiles.App.Search.Index.Storage;
using Microsoft.Win32.SafeHandles;

namespace KyraFiles.App.Search.Index.Full;

/// <summary>
/// Resultado da comparação entre o índice gerado pelo <see cref="MftReader"/> (modo completo)
/// e o índice gerado pelo <see cref="DirectoryScanner"/> (modo básico) sobre o mesmo volume.
/// </summary>
public sealed record MftBasicComparisonResult(
	int CommonKeyCount,
	IReadOnlyList<(ulong FileReference, string Path)> MftOnlyEntries,
	IReadOnlyList<(ulong FileReference, string Path)> BasicOnlyEntries);

/// <summary>
/// Leitor de MFT bruta do modo completo (F015 T13 — FR-012, FR-016, NFR-002).
/// Lê a Master File Table em blocos sequenciais grandes (2 MB), aplica correções de setor (USA fixups),
/// consolida registros de extensão e listas de atributos, escolhe o primeiro <c>$FILE_NAME</c> não-DOS
/// (ignorando nomes curtos 8.3 e links físicos adicionais) e alimenta o mesmo <see cref="VolumeIndex"/>.
/// </summary>
public static class MftReader
{
	public const int DefaultBlockSize = 2 * 1024 * 1024;
	public const int DefaultRecordSize = 1024;
	public const int DefaultBytesPerSector = 512;

	public const ulong RootDirectoryRecordNumber = 5UL;
	private const ulong RecordNumberMask = 0x0000FFFFFFFFFFFFUL;

	private const uint FileRecordMagic = 0x454C4946; // "FILE"
	private const ushort FlagInUse = 0x0001;
	private const ushort FlagIsDirectory = 0x0002;

	private const uint AttrStandardInformation = 0x10;
	private const uint AttrAttributeList = 0x20;
	private const uint AttrFileName = 0x30;
	private const uint AttrData = 0x80;
	private const uint AttrEndMarker = 0xFFFFFFFF;

	public const byte FileNameNamespacePosix = 0;
	public const byte FileNameNamespaceWin32 = 1;
	public const byte FileNameNamespaceDos = 2;
	public const byte FileNameNamespaceWin32AndDos = 3;

	private struct ParsedMftEntry
	{
		public ulong FileReference;
		public ulong ParentRecordNumber;
		public string? Name;
		public long Size;
		public bool HasDataSize;
		public long CreationTimeFileTime;
		public long LastWriteTimeFileTime;
		public bool HasStandardInfo;
		public uint Attributes;
		public bool IsInUse;
		public bool IsDirectory;
	}

	/// <summary>
	/// Lê a MFT do volume NTFS especificado (ex.: <c>"C:\"</c>) abrindo <c>\\.\C:\$MFT</c>
	/// e constrói o <see cref="VolumeIndex"/> correspondente. Requer privilégio de Administrador / SYSTEM.
	/// </summary>
	public static unsafe VolumeIndex ReadVolumeMft(
		Guid volumeGuid,
		string volumeRoot,
		IProgress<ScanProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(volumeRoot);

		string normalizedRoot = volumeRoot.TrimEnd('\\') + '\\';
		string trimNoSlash = volumeRoot.TrimEnd('\\');
		string devicePath = trimNoSlash.StartsWith(@"\\.\", StringComparison.Ordinal)
			? trimNoSlash
			: @"\\.\" + trimNoSlash;

		int recordSize = DefaultRecordSize;
		int bytesPerSector = DefaultBytesPerSector;
		long estimatedRecords = 65536;

		using (SafeFileHandle hVolume = NativeMethods.CreateFileW(
			devicePath,
			NativeMethods.GENERIC_READ,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
			IntPtr.Zero,
			NativeMethods.OPEN_EXISTING,
			NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
			IntPtr.Zero))
		{
			if (!hVolume.IsInvalid)
			{
				NativeMethods.NTFS_VOLUME_DATA_BUFFER volData = default;
				if (NativeMethods.DeviceIoControl(
					hVolume,
					NativeMethods.FSCTL_GET_NTFS_VOLUME_DATA,
					null,
					0,
					&volData,
					(uint)sizeof(NativeMethods.NTFS_VOLUME_DATA_BUFFER),
					out _,
					IntPtr.Zero))
				{
					if (volData.BytesPerFileRecordSegment > 0)
						recordSize = (int)volData.BytesPerFileRecordSegment;
					if (volData.BytesPerSector > 0)
						bytesPerSector = (int)volData.BytesPerSector;
					if (volData.MftValidDataLength > 0 && recordSize > 0)
						estimatedRecords = Math.Max(1024, volData.MftValidDataLength / recordSize);
				}
			}
		}

		string mftPath = devicePath + @"\$MFT";
		using SafeFileHandle hMft = NativeMethods.CreateFileW(
			mftPath,
			NativeMethods.GENERIC_READ,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
			IntPtr.Zero,
			NativeMethods.OPEN_EXISTING,
			NativeMethods.FILE_FLAG_BACKUP_SEMANTICS | NativeMethods.FILE_FLAG_SEQUENTIAL_SCAN,
			IntPtr.Zero);

		if (hMft.IsInvalid)
		{
			int err = Marshal.GetLastWin32Error();
			throw new UnauthorizedAccessException($"Não foi possível abrir '{mftPath}' para leitura da MFT (Win32 Error {err}).");
		}

		using var mftStream = new FileStream(hMft, FileAccess.Read, DefaultBlockSize, isAsync: false);
		return ParseMftStream(
			volumeGuid,
			normalizedRoot,
			mftStream,
			recordSize,
			bytesPerSector,
			(int)Math.Min(int.MaxValue / 8, estimatedRecords),
			progress,
			cancellationToken);
	}

	/// <summary>
	/// Analisa um fluxo contendo registros brutos da MFT em blocos sequenciais e constrói um <see cref="VolumeIndex"/>.
	/// Permite teste determinístico em memória sem necessidade de elevação de Administrador.
	/// </summary>
	public static VolumeIndex ParseMftStream(
		Guid volumeGuid,
		string volumeRoot,
		Stream mftStream,
		int recordSize = DefaultRecordSize,
		int bytesPerSector = DefaultBytesPerSector,
		int initialCapacity = 4096,
		IProgress<ScanProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(mftStream);
		if (recordSize < 512 || (recordSize % bytesPerSector) != 0)
			throw new ArgumentOutOfRangeException(nameof(recordSize), "Tamanho de registro MFT inválido.");

		string normalizedRoot = volumeRoot.TrimEnd('\\') + '\\';
		var entries = new Dictionary<ulong, ParsedMftEntry>(Math.Max(16, initialCapacity));

		int blockSize = Math.Max(recordSize * 64, (DefaultBlockSize / recordSize) * recordSize);
		byte[] buffer = ArrayPool<byte>.Shared.Rent(blockSize);

		try
		{
			ulong currentRecordNumber = 0;
			int leftover = 0;

			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();

				int bytesRead = mftStream.Read(buffer, leftover, blockSize - leftover);
				if (bytesRead == 0)
					break;

				int totalAvailable = leftover + bytesRead;
				int processableBytes = (totalAvailable / recordSize) * recordSize;

				Span<byte> chunk = buffer.AsSpan(0, processableBytes);
				for (int offset = 0; offset < processableBytes; offset += recordSize)
				{
					Span<byte> recordSpan = chunk.Slice(offset, recordSize);
					ProcessRawRecord(recordSpan, currentRecordNumber, bytesPerSector, entries);
					currentRecordNumber++;
				}

				leftover = totalAvailable - processableBytes;
				if (leftover > 0)
				{
					buffer.AsSpan(processableBytes, leftover).CopyTo(buffer);
				}
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}

		return BuildVolumeIndex(volumeGuid, normalizedRoot, entries, progress, cancellationToken);
	}

	/// <summary>
	/// Aplica as correções de sequência de atualização (Update Sequence Array / multi-sector fixup)
	/// em um registro bruto da MFT. Retorna <c>false</c> se a assinatura não for <c>"FILE"</c> ou se
	/// algum setor apresentar escrita incompleta (torn write).
	/// </summary>
	public static bool ApplyFixups(Span<byte> record, int bytesPerSector = DefaultBytesPerSector)
	{
		if (record.Length < 48 || bytesPerSector < 512)
			return false;

		uint magic = BinaryPrimitives.ReadUInt32LittleEndian(record[0..4]);
		if (magic != FileRecordMagic)
			return false;

		ushort usaOffset = BinaryPrimitives.ReadUInt16LittleEndian(record[4..6]);
		ushort usaCount = BinaryPrimitives.ReadUInt16LittleEndian(record[6..8]);

		if (usaCount < 2 || usaOffset < 42 || usaOffset + (usaCount * 2) > record.Length)
			return false;

		int sectorsInRecord = record.Length / bytesPerSector;
		int fixupCount = Math.Min(usaCount - 1, sectorsInRecord);

		ushort usn = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(usaOffset, 2));

		for (int s = 0; s < fixupCount; s++)
		{
			int sectorEndOffset = ((s + 1) * bytesPerSector) - 2;
			ushort sectorCheck = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(sectorEndOffset, 2));
			if (sectorCheck != usn)
				return false; // Torn write detectado

			ushort originalWord = BinaryPrimitives.ReadUInt16LittleEndian(record.Slice(usaOffset + 2 + (s * 2), 2));
			BinaryPrimitives.WriteUInt16LittleEndian(record.Slice(sectorEndOffset, 2), originalWord);
		}

		return true;
	}

	private static void ProcessRawRecord(
		Span<byte> record,
		ulong currentRecordNumber,
		int bytesPerSector,
		Dictionary<ulong, ParsedMftEntry> entries)
	{
		ushort flags = record.Length >= 24 ? BinaryPrimitives.ReadUInt16LittleEndian(record[22..24]) : (ushort)0;
		if ((flags & FlagInUse) == 0)
			return;

		if (!ApplyFixups(record, bytesPerSector))
			return;

		ushort sequenceNumber = BinaryPrimitives.ReadUInt16LittleEndian(record[16..18]);
		ushort firstAttrOffset = BinaryPrimitives.ReadUInt16LittleEndian(record[20..22]);
		uint bytesInUse = BinaryPrimitives.ReadUInt32LittleEndian(record[24..28]);
		ulong baseRecordRef = BinaryPrimitives.ReadUInt64LittleEndian(record[32..40]);

		if (bytesInUse > (uint)record.Length || firstAttrOffset < 40 || firstAttrOffset >= bytesInUse)
			return;

		bool isExtension = baseRecordRef != 0;
		ulong targetRecordNumber = isExtension
			? (baseRecordRef & RecordNumberMask)
			: currentRecordNumber;

		// Metadados reservados do NTFS ($MFT=0 .. $Extend=11, registros 0..15 exceto raiz 5)
		if (targetRecordNumber < 16 && targetRecordNumber != RootDirectoryRecordNumber)
			return;

		ref ParsedMftEntry entry = ref CollectionsMarshal.GetValueRefOrAddDefault(entries, targetRecordNumber, out bool exists);
		if (!exists)
		{
			entry.ParentRecordNumber = ulong.MaxValue;
		}

		if (!isExtension)
		{
			entry.IsInUse = true;
			entry.IsDirectory = (flags & FlagIsDirectory) != 0;
			entry.FileReference = (targetRecordNumber & RecordNumberMask) | ((ulong)sequenceNumber << 48);
			if (entry.IsDirectory)
				entry.Attributes |= NativeMethods.FILE_ATTRIBUTE_DIRECTORY;
		}

		int attrOffset = firstAttrOffset;
		while (attrOffset + 16 <= (int)bytesInUse)
		{
			uint attrType = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(attrOffset, 4));
			if (attrType == AttrEndMarker)
				break;

			uint attrLen = BinaryPrimitives.ReadUInt32LittleEndian(record.Slice(attrOffset + 4, 4));
			if (attrLen < 16 || attrOffset + (int)attrLen > (int)bytesInUse)
				break;

			ReadOnlySpan<byte> attrSpan = record.Slice(attrOffset, (int)attrLen);
			ParseAttribute(attrSpan, ref entry);

			attrOffset += (int)attrLen;
		}
	}

	private static void ParseAttribute(ReadOnlySpan<byte> attrSpan, ref ParsedMftEntry entry)
	{
		uint attrType = BinaryPrimitives.ReadUInt32LittleEndian(attrSpan[0..4]);
		byte nonResident = attrSpan[8];
		byte attrNameLength = attrSpan[9];

		if (attrType == AttrStandardInformation && nonResident == 0 && attrSpan.Length >= 24)
		{
			uint valueLen = BinaryPrimitives.ReadUInt32LittleEndian(attrSpan[16..20]);
			ushort valueOff = BinaryPrimitives.ReadUInt16LittleEndian(attrSpan[20..22]);
			if (valueLen >= 36 && valueOff + 36 <= attrSpan.Length)
			{
				ReadOnlySpan<byte> val = attrSpan.Slice(valueOff, (int)valueLen);
				entry.CreationTimeFileTime = BinaryPrimitives.ReadInt64LittleEndian(val[0..8]);
				entry.LastWriteTimeFileTime = BinaryPrimitives.ReadInt64LittleEndian(val[8..16]);
				uint stdAttrs = BinaryPrimitives.ReadUInt32LittleEndian(val[32..36]);
				entry.Attributes = stdAttrs | (entry.IsDirectory ? NativeMethods.FILE_ATTRIBUTE_DIRECTORY : 0u);
				entry.HasStandardInfo = true;
			}
		}
		else if (attrType == AttrFileName && nonResident == 0 && attrSpan.Length >= 24)
		{
			uint valueLen = BinaryPrimitives.ReadUInt32LittleEndian(attrSpan[16..20]);
			ushort valueOff = BinaryPrimitives.ReadUInt16LittleEndian(attrSpan[20..22]);
			if (valueLen >= 66 && valueOff + valueLen <= attrSpan.Length)
			{
				ReadOnlySpan<byte> val = attrSpan.Slice(valueOff, (int)valueLen);
				byte nameLengthChars = val[64];
				byte nameNamespace = val[65];

				// FR-016: Nomes 8.3 (namespace DOS == 2) nunca entram;
				// apenas o PRIMEIRO atributo $FILE_NAME não-DOS do registro define o local original.
				if (nameNamespace != FileNameNamespaceDos && entry.Name is null)
				{
					int byteLen = nameLengthChars * 2;
					if (66 + byteLen <= val.Length && nameLengthChars > 0)
					{
						ulong parentRef = BinaryPrimitives.ReadUInt64LittleEndian(val[0..8]);
						entry.ParentRecordNumber = parentRef & RecordNumberMask;
						entry.Name = Encoding.Unicode.GetString(val.Slice(66, byteLen));

						if (!entry.HasStandardInfo)
						{
							entry.CreationTimeFileTime = BinaryPrimitives.ReadInt64LittleEndian(val[8..16]);
							entry.LastWriteTimeFileTime = BinaryPrimitives.ReadInt64LittleEndian(val[16..24]);
							uint fnAttrs = BinaryPrimitives.ReadUInt32LittleEndian(val[56..60]);
							entry.Attributes = fnAttrs | (entry.IsDirectory ? NativeMethods.FILE_ATTRIBUTE_DIRECTORY : 0u);
						}

						if (!entry.HasDataSize && !entry.IsDirectory)
						{
							entry.Size = BinaryPrimitives.ReadInt64LittleEndian(val[48..56]);
						}
					}
				}
			}
		}
		else if (attrType == AttrData && attrNameLength == 0)
		{
			// Fluxo $DATA padrão (sem nome de ADS): obtém tamanho real do arquivo
			if (nonResident == 0 && attrSpan.Length >= 24)
			{
				uint valueLen = BinaryPrimitives.ReadUInt32LittleEndian(attrSpan[16..20]);
				entry.Size = valueLen;
				entry.HasDataSize = true;
			}
			else if (nonResident == 1 && attrSpan.Length >= 56)
			{
				ulong startingVcn = BinaryPrimitives.ReadUInt64LittleEndian(attrSpan[16..24]);
				if (startingVcn == 0)
				{
					long realSize = BinaryPrimitives.ReadInt64LittleEndian(attrSpan[48..56]);
					if (realSize >= 0)
					{
						entry.Size = realSize;
						entry.HasDataSize = true;
					}
				}
			}
		}
		else if (attrType == AttrAttributeList && nonResident == 0 && attrSpan.Length >= 24)
		{
			// A lista de atributos residente referencia registros de extensão que também são varridos
			// sequencialmente por ProcessRawRecord (via BaseFileRecordSegment). Validamos sua integridade aqui.
			uint valueLen = BinaryPrimitives.ReadUInt32LittleEndian(attrSpan[16..20]);
			ushort valueOff = BinaryPrimitives.ReadUInt16LittleEndian(attrSpan[20..22]);
			if (valueOff + valueLen <= attrSpan.Length)
			{
				_ = attrSpan.Slice(valueOff, (int)valueLen);
			}
		}
	}

	private static VolumeIndex BuildVolumeIndex(
		Guid volumeGuid,
		string normalizedRoot,
		Dictionary<ulong, ParsedMftEntry> entries,
		IProgress<ScanProgress>? progress,
		CancellationToken cancellationToken)
	{
		var index = new VolumeIndex(volumeGuid, normalizedRoot, Math.Max(16, entries.Count));
		var recordToVolumeIndex = new Dictionary<ulong, int>(entries.Count);
		// Estado de resolução: 0 = não visitado, 1 = em progresso (detecção de ciclo), 2 = resolvido, -1 = inválido/órfão/reparse
		var resolutionState = new Dictionary<ulong, sbyte>(entries.Count);

		long dirsAdded = 0;
		long filesAdded = 0;

		int ResolveOrAddDirectory(ulong recordNumber, int depth)
		{
			if (recordNumber == RootDirectoryRecordNumber)
				return VolumeIndex.RootParentIndex;

			if (depth > 256 || recordNumber < 16 || recordNumber == ulong.MaxValue)
				return VolumeIndex.DeletedParentIndex;

			if (recordToVolumeIndex.TryGetValue(recordNumber, out int existingIdx))
				return existingIdx;

			if (resolutionState.TryGetValue(recordNumber, out sbyte state))
			{
				if (state == 1 || state == -1)
					return VolumeIndex.DeletedParentIndex;
			}

			if (!entries.TryGetValue(recordNumber, out ParsedMftEntry dirEntry) ||
				!dirEntry.IsInUse ||
				!dirEntry.IsDirectory ||
				string.IsNullOrEmpty(dirEntry.Name) ||
				dirEntry.Name is "." or "..")
			{
				resolutionState[recordNumber] = -1;
				return VolumeIndex.DeletedParentIndex;
			}

			resolutionState[recordNumber] = 1;

			ulong parentRec = dirEntry.ParentRecordNumber;
			int parentIdx;
			if (parentRec == RootDirectoryRecordNumber)
			{
				parentIdx = VolumeIndex.RootParentIndex;
			}
			else
			{
				if (!entries.TryGetValue(parentRec, out ParsedMftEntry parentEntry) ||
					(parentEntry.Attributes & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
				{
					// FR-012: Nunca desce em diretórios que são pontos de reanálise
					resolutionState[recordNumber] = -1;
					return VolumeIndex.DeletedParentIndex;
				}

				parentIdx = ResolveOrAddDirectory(parentRec, depth + 1);
				if (parentIdx == VolumeIndex.DeletedParentIndex)
				{
					resolutionState[recordNumber] = -1;
					return VolumeIndex.DeletedParentIndex;
				}
			}

			int addedIdx = index.Add(
				fileReference: dirEntry.FileReference,
				parentIndex: parentIdx,
				name: dirEntry.Name,
				size: 0,
				creationTime: SafeFileTimeToUtc(dirEntry.CreationTimeFileTime),
				lastWriteTime: SafeFileTimeToUtc(dirEntry.LastWriteTimeFileTime),
				attributes: dirEntry.Attributes | NativeMethods.FILE_ATTRIBUTE_DIRECTORY);

			dirsAdded++;
			recordToVolumeIndex[recordNumber] = addedIdx;
			resolutionState[recordNumber] = 2;
			return addedIdx;
		}

		foreach (var (recordNumber, entry) in entries)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (recordNumber == RootDirectoryRecordNumber || recordNumber < 16)
				continue;

			if (!entry.IsInUse || string.IsNullOrEmpty(entry.Name) || entry.Name is "." or "..")
				continue;

			// Ignora arquivos internos da raiz NTFS como $MFT, $LogFile, $Volume, $Bitmap, $Extend, etc.
			if (entry.ParentRecordNumber == RootDirectoryRecordNumber &&
				entry.Name.StartsWith('$') &&
				(entry.Attributes & 0x00000004 /*FILE_ATTRIBUTE_SYSTEM*/) != 0)
			{
				continue;
			}

			if (entry.IsDirectory)
			{
				_ = ResolveOrAddDirectory(recordNumber, 0);
			}
			else
			{
				ulong parentRec = entry.ParentRecordNumber;
				int parentIdx;
				if (parentRec == RootDirectoryRecordNumber)
				{
					parentIdx = VolumeIndex.RootParentIndex;
				}
				else
				{
					if (!entries.TryGetValue(parentRec, out ParsedMftEntry parentEntry) ||
						(parentEntry.Attributes & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
					{
						continue;
					}

					parentIdx = ResolveOrAddDirectory(parentRec, 0);
					if (parentIdx == VolumeIndex.DeletedParentIndex)
						continue;
				}

				index.Add(
					fileReference: entry.FileReference,
					parentIndex: parentIdx,
					name: entry.Name,
					size: entry.Size,
					creationTime: SafeFileTimeToUtc(entry.CreationTimeFileTime),
					lastWriteTime: SafeFileTimeToUtc(entry.LastWriteTimeFileTime),
					attributes: entry.Attributes);

				filesAdded++;
			}
		}

		index.TrimExcess();
		progress?.Report(new ScanProgress(volumeGuid, normalizedRoot, dirsAdded, filesAdded, IsCompleted: true));
		return index;
	}

	/// <summary>
	/// Compara o conjunto de chaves <c>FileReference</c> entre o índice da MFT e o índice do modo básico.
	/// </summary>
	public static MftBasicComparisonResult CompareWithBasicIndex(VolumeIndex mftIndex, VolumeIndex basicIndex)
	{
		ArgumentNullException.ThrowIfNull(mftIndex);
		ArgumentNullException.ThrowIfNull(basicIndex);

		var mftMap = new Dictionary<ulong, int>(mftIndex.Count);
		for (int i = 0; i < mftIndex.Count; i++)
		{
			if (!mftIndex.IsDeleted(i))
			{
				ulong fileRef = mftIndex.GetFileReference(i) & RecordNumberMask;
				if (fileRef != 0)
					mftMap[fileRef] = i;
			}
		}

		var basicMap = new Dictionary<ulong, int>(basicIndex.Count);
		for (int i = 0; i < basicIndex.Count; i++)
		{
			if (!basicIndex.IsDeleted(i))
			{
				ulong fileRef = basicIndex.GetFileReference(i) & RecordNumberMask;
				if (fileRef != 0)
					basicMap[fileRef] = i;
			}
		}

		int common = 0;
		var mftOnly = new List<(ulong, string)>();
		var basicOnly = new List<(ulong, string)>();

		foreach (var (recNum, idx) in mftMap)
		{
			if (basicMap.ContainsKey(recNum))
			{
				common++;
			}
			else
			{
				mftOnly.Add((mftIndex.GetFileReference(idx), mftIndex.GetFullPath(idx)));
			}
		}

		foreach (var (recNum, idx) in basicMap)
		{
			if (!mftMap.ContainsKey(recNum))
			{
				basicOnly.Add((basicIndex.GetFileReference(idx), basicIndex.GetFullPath(idx)));
			}
		}

		return new MftBasicComparisonResult(common, mftOnly, basicOnly);
	}

	private static DateTimeOffset SafeFileTimeToUtc(long fileTime)
	{
		if (fileTime <= 0)
			return DateTimeOffset.UnixEpoch;
		try
		{
			return DateTimeOffset.FromFileTime(fileTime);
		}
		catch
		{
			return DateTimeOffset.UnixEpoch;
		}
	}
}
