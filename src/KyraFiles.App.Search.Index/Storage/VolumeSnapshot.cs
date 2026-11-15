// Copyright (c) Files Community. Licensed under the MIT License.

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using KyraFiles.App.Search.Index.Interop;
using Microsoft.Win32.SafeHandles;

namespace KyraFiles.App.Search.Index.Storage;

/// <summary>
/// Dados carregados de um snapshot de volume em disco (F015 T8 — NFR-004, NFR-006, FR-033).
/// </summary>
public sealed record VolumeSnapshotData(
	VolumeIndex Index,
	uint VolumeSerialNumber,
	DateTimeOffset RootLastWriteTime,
	ulong UsnJournalId,
	long LastUsn);

/// <summary>
/// Serialização binária versionada de alta velocidade para <see cref="VolumeIndex"/> (NFR-004: carga do C: &lt; 2 s).
/// Armazena em LocalCache no modo básico e em %ProgramData%\FilesCustom\Index no modo completo (NFR-006).
/// </summary>
public static class VolumeSnapshot
{
	public const uint MagicValue = 0x4E534346; // "FCSN" (Files Custom Snapshot)
	public const uint CurrentVersion = 1;

	/// <summary>
	/// Retorna o diretório padrão de snapshots para o modo básico (LocalCache do pacote quando disponível,
	/// ou %LOCALAPPDATA%\FilesCustom\Index).
	/// </summary>
	public static string GetDefaultBasicCacheDirectory()
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		// Quando rodando empacotado em MSIX, LocalApplicationData aponta para LocalState ou virtualizado;
		// verificamos se existe a pasta irmã LocalCache do pacote.
		string? parent = Path.GetDirectoryName(localAppData);
		if (!string.IsNullOrEmpty(parent))
		{
			string packageLocalCache = Path.Combine(parent, "LocalCache");
			if (Directory.Exists(packageLocalCache))
				return Path.Combine(packageLocalCache, "KyraFiles", "Index");
		}

		return Path.Combine(localAppData, "KyraFiles", "Index");
	}

	/// <summary>
	/// Retorna o caminho do arquivo de snapshot para um GUID de volume.
	/// </summary>
	public static string GetSnapshotFilePath(string directoryPath, Guid volumeGuid)
		=> Path.Combine(directoryPath, $"vol_{volumeGuid:N}.fcsn");

	/// <summary>
	/// Obtém o número de série Win32 do volume a partir de sua raiz (ex.: "C:\").
	/// </summary>
	public static uint GetVolumeSerialNumber(string volumeRoot)
	{
		if (string.IsNullOrWhiteSpace(volumeRoot))
			return 0;

		string root = volumeRoot.EndsWith('\\') ? volumeRoot : volumeRoot + "\\";
		Span<char> volName = stackalloc char[261];
		Span<char> fsName = stackalloc char[64];

		if (NativeMethods.GetVolumeInformationW(
			root,
			volName,
			(uint)volName.Length,
			out uint serialNumber,
			out _,
			out _,
			fsName,
			(uint)fsName.Length))
		{
			return serialNumber;
		}

		return 0;
	}

	/// <summary>
	/// Salva o <see cref="VolumeIndex"/> em disco de forma atômica e vetorizada.
	/// </summary>
	public static void Save(
		string filePath,
		VolumeIndex index,
		uint? volumeSerialNumber = null,
		DateTimeOffset? rootLastWriteTime = null,
		ulong usnJournalId = 0,
		long lastUsn = 0)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
		ArgumentNullException.ThrowIfNull(index);

		string? dir = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(dir))
			Directory.CreateDirectory(dir);

		uint serial = volumeSerialNumber ?? GetVolumeSerialNumber(index.VolumeRoot);
		long rootWriteTicks;
		if (rootLastWriteTime.HasValue)
		{
			rootWriteTicks = rootLastWriteTime.Value.UtcTicks;
		}
		else
		{
			try
			{
				rootWriteTicks = Directory.Exists(index.VolumeRoot)
					? Directory.GetLastWriteTimeUtc(index.VolumeRoot).Ticks
					: 0L;
			}
			catch
			{
				rootWriteTicks = 0L;
			}
		}

		var raw = index.GetRawArrays();
		int count = raw.Count;
		int bytesCount = raw.BytesCount;
		byte[] rootUtf8 = Encoding.UTF8.GetBytes(index.VolumeRoot);

		string tempPath = $"{filePath}.{Environment.ProcessId}.tmp";

		using (SafeFileHandle handle = File.OpenHandle(
			tempPath,
			FileMode.Create,
			FileAccess.Write,
			FileShare.None,
			FileOptions.None))
		{
			// Header fixo:
			// [0..4]   uint Magic
			// [4..8]   uint Version
			// [8..24]  Guid VolumeGuid (16 bytes)
			// [24..28] uint VolumeSerialNumber
			// [28..36] long RootLastWriteTimeUtcTicks
			// [36..44] ulong UsnJournalId
			// [44..52] long LastUsn
			// [52..56] int Count
			// [56..60] int BytesCount
			// [60..64] int RootUtf8Length
			// [64..64+RootUtf8Length] byte[] RootUtf8
			int headerSize = 64 + rootUtf8.Length;
			byte[] header = new byte[headerSize];
			Span<byte> hSpan = header;

			BinaryPrimitives.WriteUInt32LittleEndian(hSpan[0..4], MagicValue);
			BinaryPrimitives.WriteUInt32LittleEndian(hSpan[4..8], CurrentVersion);
			index.VolumeGuid.TryWriteBytes(hSpan[8..24]);
			BinaryPrimitives.WriteUInt32LittleEndian(hSpan[24..28], serial);
			BinaryPrimitives.WriteInt64LittleEndian(hSpan[28..36], rootWriteTicks);
			BinaryPrimitives.WriteUInt64LittleEndian(hSpan[36..44], usnJournalId);
			BinaryPrimitives.WriteInt64LittleEndian(hSpan[44..52], lastUsn);
			BinaryPrimitives.WriteInt32LittleEndian(hSpan[52..56], count);
			BinaryPrimitives.WriteInt32LittleEndian(hSpan[56..60], bytesCount);
			BinaryPrimitives.WriteInt32LittleEndian(hSpan[60..64], rootUtf8.Length);
			rootUtf8.CopyTo(hSpan[64..]);

			long offset = 0;
			WriteAll(handle, hSpan, ref offset);

			if (count > 0)
			{
				WriteAll(handle, MemoryMarshal.AsBytes(raw.FileReferences.AsSpan(0, count)), ref offset);
				WriteAll(handle, MemoryMarshal.AsBytes(raw.ParentIndices.AsSpan(0, count)), ref offset);
				WriteAll(handle, MemoryMarshal.AsBytes(raw.FileSizes.AsSpan(0, count)), ref offset);
				WriteAll(handle, MemoryMarshal.AsBytes(raw.CreationTimesUtcTicks.AsSpan(0, count)), ref offset);
				WriteAll(handle, MemoryMarshal.AsBytes(raw.LastWriteTimesUtcTicks.AsSpan(0, count)), ref offset);
				WriteAll(handle, MemoryMarshal.AsBytes(raw.FileAttributes.AsSpan(0, count)), ref offset);
				WriteAll(handle, MemoryMarshal.AsBytes(raw.NameOffsets.AsSpan(0, count)), ref offset);
				WriteAll(handle, MemoryMarshal.AsBytes(raw.NormalizedNameLengths.AsSpan(0, count)), ref offset);
				WriteAll(handle, MemoryMarshal.AsBytes(raw.OriginalNameLengths.AsSpan(0, count)), ref offset);
			}

			if (bytesCount > 0)
			{
				WriteAll(handle, raw.NamesBuffer.AsSpan(0, bytesCount), ref offset);
			}
		}

		File.Move(tempPath, filePath, overwrite: true);
	}

	/// <summary>
	/// Tenta carregar um snapshot de volume do disco.
	/// Se o arquivo tiver versão incorreta, estiver corrompido ou o número de série do volume divergir,
	/// descarta o arquivo e retorna <c>false</c> para que o volume seja reindexado.
	/// </summary>
	public static bool TryLoad(
		string filePath,
		out VolumeSnapshotData? snapshot,
		uint? expectedVolumeSerialNumber = null,
		bool deleteIfInvalid = true)
	{
		snapshot = null;

		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
			return false;

		bool valid = false;
		try
		{
			using SafeFileHandle handle = File.OpenHandle(
				filePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				FileOptions.SequentialScan);

			long fileLength = RandomAccess.GetLength(handle);
			if (fileLength < 64)
				return false;

			Span<byte> fixedHeader = stackalloc byte[64];
			long offset = 0;
			if (!TryReadAll(handle, fixedHeader, ref offset))
				return false;

			uint magic = BinaryPrimitives.ReadUInt32LittleEndian(fixedHeader[0..4]);
			uint version = BinaryPrimitives.ReadUInt32LittleEndian(fixedHeader[4..8]);
			if (magic != MagicValue || version != CurrentVersion)
				return false;

			Guid volumeGuid = new(fixedHeader[8..24]);
			uint serialNumber = BinaryPrimitives.ReadUInt32LittleEndian(fixedHeader[24..28]);
			long rootWriteTicks = BinaryPrimitives.ReadInt64LittleEndian(fixedHeader[28..36]);
			ulong usnJournalId = BinaryPrimitives.ReadUInt64LittleEndian(fixedHeader[36..44]);
			long lastUsn = BinaryPrimitives.ReadInt64LittleEndian(fixedHeader[44..52]);
			int count = BinaryPrimitives.ReadInt32LittleEndian(fixedHeader[52..56]);
			int bytesCount = BinaryPrimitives.ReadInt32LittleEndian(fixedHeader[56..60]);
			int rootUtf8Length = BinaryPrimitives.ReadInt32LittleEndian(fixedHeader[60..64]);

			if (count < 0 || bytesCount < 0 || rootUtf8Length <= 0 || rootUtf8Length > 4096)
				return false;

			if (expectedVolumeSerialNumber.HasValue &&
				expectedVolumeSerialNumber.Value != 0 &&
				serialNumber != expectedVolumeSerialNumber.Value)
			{
				return false;
			}

			// Verifica tamanho exato esperado do arquivo (evita leitura parcial ou truncada)
			// Por item: 8 (ulong) + 4 (int) + 8 (long) + 8 (long) + 8 (long) + 4 (uint) + 4 (int) + 2 (ushort) + 2 (ushort) = 48 bytes
			long expectedFileSize = 64L + rootUtf8Length + ((long)count * 48L) + bytesCount;
			if (fileLength != expectedFileSize)
				return false;

			byte[] rootUtf8 = new byte[rootUtf8Length];
			if (!TryReadAll(handle, rootUtf8, ref offset))
				return false;

			string volumeRoot = Encoding.UTF8.GetString(rootUtf8);

			var fileReferences = new ulong[count];
			var parentIndices = new int[count];
			var fileSizes = new long[count];
			var creationTimesUtcTicks = new long[count];
			var lastWriteTimesUtcTicks = new long[count];
			var fileAttributes = new uint[count];
			var nameOffsets = new int[count];
			var normalizedNameLengths = new ushort[count];
			var originalNameLengths = new ushort[count];
			var namesBuffer = new byte[bytesCount];

			if (count > 0)
			{
				if (!TryReadAll(handle, MemoryMarshal.AsBytes(fileReferences.AsSpan()), ref offset) ||
					!TryReadAll(handle, MemoryMarshal.AsBytes(parentIndices.AsSpan()), ref offset) ||
					!TryReadAll(handle, MemoryMarshal.AsBytes(fileSizes.AsSpan()), ref offset) ||
					!TryReadAll(handle, MemoryMarshal.AsBytes(creationTimesUtcTicks.AsSpan()), ref offset) ||
					!TryReadAll(handle, MemoryMarshal.AsBytes(lastWriteTimesUtcTicks.AsSpan()), ref offset) ||
					!TryReadAll(handle, MemoryMarshal.AsBytes(fileAttributes.AsSpan()), ref offset) ||
					!TryReadAll(handle, MemoryMarshal.AsBytes(nameOffsets.AsSpan()), ref offset) ||
					!TryReadAll(handle, MemoryMarshal.AsBytes(normalizedNameLengths.AsSpan()), ref offset) ||
					!TryReadAll(handle, MemoryMarshal.AsBytes(originalNameLengths.AsSpan()), ref offset))
				{
					return false;
				}
			}

			if (bytesCount > 0)
			{
				if (!TryReadAll(handle, namesBuffer.AsSpan(), ref offset))
					return false;
			}

			var index = VolumeIndex.FromRawArrays(
				volumeGuid,
				volumeRoot,
				count,
				bytesCount,
				fileReferences,
				parentIndices,
				fileSizes,
				creationTimesUtcTicks,
				lastWriteTimesUtcTicks,
				fileAttributes,
				nameOffsets,
				normalizedNameLengths,
				originalNameLengths,
				namesBuffer);

			snapshot = new VolumeSnapshotData(
				Index: index,
				VolumeSerialNumber: serialNumber,
				RootLastWriteTime: new DateTimeOffset(rootWriteTicks, TimeSpan.Zero),
				UsnJournalId: usnJournalId,
				LastUsn: lastUsn);

			valid = true;
			return true;
		}
		catch
		{
			valid = false;
			return false;
		}
		finally
		{
			if (!valid && deleteIfInvalid)
			{
				try
				{
					File.Delete(filePath);
				}
				catch
				{
					// Ignora falha ao apagar arquivo inválido
				}
			}
		}
	}

	private static void WriteAll(SafeFileHandle handle, ReadOnlySpan<byte> buffer, ref long fileOffset)
	{
		RandomAccess.Write(handle, buffer, fileOffset);
		fileOffset += buffer.Length;
	}

	private static bool TryReadAll(SafeFileHandle handle, Span<byte> buffer, ref long fileOffset)
	{
		int totalRead = 0;
		while (totalRead < buffer.Length)
		{
			int read = RandomAccess.Read(handle, buffer[totalRead..], fileOffset + totalRead);
			if (read <= 0)
				return false;
			totalRead += read;
		}

		fileOffset += totalRead;
		return true;
	}
}
