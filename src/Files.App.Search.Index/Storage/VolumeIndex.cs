// Copyright (c) Files Community. Licensed under the MIT License.

using Files.App.Search.Index.Models;
using Files.App.Search.Index.Query;

namespace Files.App.Search.Index.Storage;

/// <summary>
/// Estrutura de dados de alto desempenho em arrays paralelos (Structure-of-Arrays) para armazenamento
/// em memória de milhões de arquivos sem overhead de objetos de GC (NFR-003: meta &lt; 100 MB / milhão).
/// </summary>
public sealed class VolumeIndex
{
	private const int DefaultInitialCapacity = 1024;
	private const int FileAttributeDirectory = 0x00000010;
	private const int MaxDirectoryDepth = 256;

	private ulong[] _fileReferences;
	private int[] _parentIndices;
	private long[] _fileSizes;
	private long[] _creationTimesUtcTicks;
	private long[] _lastWriteTimesUtcTicks;
	private uint[] _fileAttributes;

	// Metadados de nomes compactados: um único offset por item e dois comprimentos
	private int[] _nameOffsets;
	private ushort[] _normalizedNameLengths;
	private ushort[] _originalNameLengths;

	// Buffer contíguo único: [bytes normalizados UTF-8][bytes originais UTF-8]
	private byte[] _namesBuffer;

	private int _count;
	private int _bytesCount;

	public Guid VolumeGuid { get; }
	public string VolumeRoot { get; set; }
	public int Count => _count;

	public VolumeIndex(Guid volumeGuid, string volumeRoot, int initialCapacity = DefaultInitialCapacity)
	{
		VolumeGuid = volumeGuid;
		VolumeRoot = volumeRoot.TrimEnd('\\') + '\\';

		int capacity = Math.Max(16, initialCapacity);
		_fileReferences = new ulong[capacity];
		_parentIndices = new int[capacity];
		_fileSizes = new long[capacity];
		_creationTimesUtcTicks = new long[capacity];
		_lastWriteTimesUtcTicks = new long[capacity];
		_fileAttributes = new uint[capacity];

		_nameOffsets = new int[capacity];
		_normalizedNameLengths = new ushort[capacity];
		_originalNameLengths = new ushort[capacity];

		// Estima média de ~40 bytes por item (normalizado + original)
		int initialByteCapacity = Math.Min(int.MaxValue / 4, capacity * 40);
		_namesBuffer = new byte[initialByteCapacity];
	}

	/// <summary>
	/// Adiciona um item ao índice de volume.
	/// </summary>
	public int Add(
		ulong fileReference,
		int parentIndex,
		string name,
		long size,
		DateTimeOffset creationTime,
		DateTimeOffset lastWriteTime,
		uint attributes)
	{
		EnsureCapacity(_count + 1);

		int index = _count++;
		_fileReferences[index] = fileReference;
		_parentIndices[index] = parentIndex;
		_fileSizes[index] = size;
		_creationTimesUtcTicks[index] = creationTime.UtcTicks;
		_lastWriteTimesUtcTicks[index] = lastWriteTime.UtcTicks;
		_fileAttributes[index] = attributes;

		// 1. Gera bytes normalizados
		byte[] normalizedBytes = NameNormalizer.NormalizeToUtf8(name);
		ushort normLen = (ushort)Math.Min(ushort.MaxValue, normalizedBytes.Length);

		// 2. Calcula bytes originais
		int origLen = Encoding.UTF8.GetByteCount(name);
		ushort origLenU16 = (ushort)Math.Min(ushort.MaxValue, origLen);

		int totalBytesForItem = normLen + origLenU16;
		EnsureBufferCapacity(_bytesCount + totalBytesForItem);

		int offset = _bytesCount;
		_nameOffsets[index] = offset;
		_normalizedNameLengths[index] = normLen;
		_originalNameLengths[index] = origLenU16;

		// Copia normalizado
		Array.Copy(normalizedBytes, 0, _namesBuffer, offset, normLen);

		// Grava original logo em seguida
		Encoding.UTF8.GetBytes(name, 0, name.Length, _namesBuffer, offset + normLen);

		_bytesCount += totalBytesForItem;
		return index;
	}

	/// <summary>
	/// Retorna a referência de arquivo (FRN / FileId).
	/// </summary>
	public ulong GetFileReference(int index) => _fileReferences[index];

	/// <summary>
	/// Retorna o índice do nó pai (-1 para raiz).
	/// </summary>
	public int GetParentIndex(int index) => _parentIndices[index];

	/// <summary>
	/// Retorna o tamanho em bytes.
	/// </summary>
	public long GetFileSize(int index) => _fileSizes[index];

	/// <summary>
	/// Retorna a data e hora de criação (UTC).
	/// </summary>
	public DateTimeOffset GetCreationTime(int index) => new(_creationTimesUtcTicks[index], TimeSpan.Zero);

	/// <summary>
	/// Retorna a data e hora de modificação (UTC).
	/// </summary>
	public DateTimeOffset GetLastWriteTime(int index) => new(_lastWriteTimesUtcTicks[index], TimeSpan.Zero);

	/// <summary>
	/// Retorna os atributos de arquivo Win32.
	/// </summary>
	public uint GetFileAttributes(int index) => _fileAttributes[index];

	/// <summary>
	/// Indica se o item é um diretório.
	/// </summary>
	public bool IsDirectory(int index) => (_fileAttributes[index] & FileAttributeDirectory) != 0;

	/// <summary>
	/// Retorna a fatia de bytes UTF-8 do nome normalizado para avaliação vetorizada (zero alocações).
	/// </summary>
	public ReadOnlySpan<byte> GetNormalizedNameUtf8(int index)
	{
		int offset = _nameOffsets[index];
		int length = _normalizedNameLengths[index];
		return _namesBuffer.AsSpan(offset, length);
	}

	/// <summary>
	/// Retorna o nome original em formato string.
	/// </summary>
	public string GetName(int index)
	{
		int offset = _nameOffsets[index] + _normalizedNameLengths[index];
		int length = _originalNameLengths[index];
		return Encoding.UTF8.GetString(_namesBuffer, offset, length);
	}

	/// <summary>
	/// Constrói o caminho completo resolvendo a hierarquia de pais recursivamente até a raiz do volume.
	/// Possui proteção estrita contra ciclos e auto-referências.
	/// </summary>
	public string GetFullPath(int index)
	{
		if (index < 0 || index >= _count)
			return VolumeRoot;

		// Coleta os segmentos subindo pelos pais com limite de profundidade contra ciclos
		var segments = new List<string>(8);
		int current = index;
		int depth = 0;

		while (current >= 0 && current < _count && depth++ < MaxDirectoryDepth)
		{
			segments.Add(GetName(current));
			int parent = _parentIndices[current];
			if (parent == current)
				break;
			current = parent;
		}

		segments.Reverse();

		var sb = new StringBuilder(VolumeRoot.Length + segments.Count * 24);
		sb.Append(VolumeRoot);

		for (int i = 0; i < segments.Count; i++)
		{
			if (i > 0)
				sb.Append('\\');
			sb.Append(segments[i]);
		}

		return sb.ToString();
	}

	/// <summary>
	/// Constrói o SearchResultItem completo com metadados para exibição no Files.
	/// </summary>
	public SearchResultItem GetItem(int index)
	{
		return new SearchResultItem(
			Index: index,
			FileReference: _fileReferences[index],
			Name: GetName(index),
			FullPath: GetFullPath(index),
			FileSize: _fileSizes[index],
			CreationTime: GetCreationTime(index),
			LastWriteTime: GetLastWriteTime(index),
			FileAttributes: _fileAttributes[index],
			IsDirectory: IsDirectory(index));
	}

	private void EnsureCapacity(int required)
	{
		if (required <= _fileReferences.Length)
			return;

		int newCap = Math.Max(required, _fileReferences.Length * 2);

		Array.Resize(ref _fileReferences, newCap);
		Array.Resize(ref _parentIndices, newCap);
		Array.Resize(ref _fileSizes, newCap);
		Array.Resize(ref _creationTimesUtcTicks, newCap);
		Array.Resize(ref _lastWriteTimesUtcTicks, newCap);
		Array.Resize(ref _fileAttributes, newCap);

		Array.Resize(ref _nameOffsets, newCap);
		Array.Resize(ref _normalizedNameLengths, newCap);
		Array.Resize(ref _originalNameLengths, newCap);
	}

	private void EnsureBufferCapacity(int required)
	{
		if (required <= _namesBuffer.Length)
			return;

		long newCapLong = Math.Max((long)required, (long)_namesBuffer.Length * 2);
		int newCap = (int)Math.Min(int.MaxValue - 56, newCapLong);

		Array.Resize(ref _namesBuffer, newCap);
	}

	/// <summary>
	/// Libera capacidades excedentes não utilizadas dos arrays após término da indexação.
	/// </summary>
	public void TrimExcess()
	{
		if (_count < _fileReferences.Length)
		{
			Array.Resize(ref _fileReferences, _count);
			Array.Resize(ref _parentIndices, _count);
			Array.Resize(ref _fileSizes, _count);
			Array.Resize(ref _creationTimesUtcTicks, _count);
			Array.Resize(ref _lastWriteTimesUtcTicks, _count);
			Array.Resize(ref _fileAttributes, _count);
			Array.Resize(ref _nameOffsets, _count);
			Array.Resize(ref _normalizedNameLengths, _count);
			Array.Resize(ref _originalNameLengths, _count);
		}

		if (_bytesCount < _namesBuffer.Length)
		{
			Array.Resize(ref _namesBuffer, _bytesCount);
		}
	}
}
