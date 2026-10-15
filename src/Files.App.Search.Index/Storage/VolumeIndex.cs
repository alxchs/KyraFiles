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
	public const int RootParentIndex = -1;
	public const int DeletedParentIndex = -2;

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

	// Mapa opcional construído sob demanda para atualizações em tempo real (ChangeWatcher / Reconciler)
	private Dictionary<ulong, int>? _indexByFileRef;

	public object SyncRoot { get; } = new();
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

		WriteNames(index, name);

		if (_indexByFileRef is not null && fileReference != 0)
			_indexByFileRef[fileReference] = index;

		return index;
	}

	/// <summary>
	/// Atualiza o pai e o nome de uma entrada já registrada (usado quando um hardlink secundário
	/// foi encontrado antes do caminho primário retornado por FindFirstFileNameW — FR-016).
	/// </summary>
	public void UpdateLocationAndName(int index, int parentIndex, string name)
	{
		if (index < 0 || index >= _count)
			return;

		_parentIndices[index] = parentIndex;
		WriteNames(index, name);
	}

	/// <summary>
	/// Atualiza todos os metadados de uma entrada existente no índice (usado por ChangeWatcher e Reconciler).
	/// </summary>
	public void UpdateEntry(
		int index,
		int parentIndex,
		string name,
		long size,
		DateTimeOffset creationTime,
		DateTimeOffset lastWriteTime,
		uint attributes)
	{
		if (index < 0 || index >= _count)
			return;

		_parentIndices[index] = parentIndex;
		_fileSizes[index] = size;
		_creationTimesUtcTicks[index] = creationTime.UtcTicks;
		_lastWriteTimesUtcTicks[index] = lastWriteTime.UtcTicks;
		_fileAttributes[index] = attributes;

		if (!string.Equals(GetName(index), name, StringComparison.Ordinal))
		{
			WriteNames(index, name);
		}
	}

	/// <summary>
	/// Atualiza a data de última modificação de uma entrada (ex.: pasta reconciliada).
	/// </summary>
	public void SetLastWriteTime(int index, DateTimeOffset lastWriteTime)
	{
		if (index >= 0 && index < _count)
			_lastWriteTimesUtcTicks[index] = lastWriteTime.UtcTicks;
	}

	/// <summary>
	/// Marca um item (e todos os seus descendentes, se for diretório) como excluído.
	/// </summary>
	public int MarkDeleted(int index)
	{
		if (index < 0 || index >= _count || _parentIndices[index] == DeletedParentIndex)
			return 0;

		bool isDir = IsDirectory(index);
		DeleteSingleEntry(index);
		int deletedCount = 1;

		if (isDir)
		{
			var targetDirs = new HashSet<int> { index };
			bool addedSubDir = true;
			while (addedSubDir)
			{
				addedSubDir = false;
				for (int i = 0; i < _count; i++)
				{
					int p = _parentIndices[i];
					if (p >= 0 && targetDirs.Contains(p))
					{
						if (IsDirectory(i) && targetDirs.Add(i))
							addedSubDir = true;

						if (_parentIndices[i] != DeletedParentIndex)
						{
							DeleteSingleEntry(i);
							deletedCount++;
						}
					}
				}
			}
		}

		return deletedCount;
	}

	private void DeleteSingleEntry(int index)
	{
		ulong fileRef = _fileReferences[index];
		if (_indexByFileRef is not null && fileRef != 0)
			_indexByFileRef.Remove(fileRef);

		_fileReferences[index] = 0;
		_parentIndices[index] = DeletedParentIndex;
		_normalizedNameLengths[index] = 0;
		_originalNameLengths[index] = 0;
	}

	private void WriteNames(int index, string name)
	{
		byte[] normalizedBytes = NameNormalizer.NormalizeToUtf8(name);
		ushort normLen = (ushort)Math.Min(ushort.MaxValue, normalizedBytes.Length);
		int origLen = Encoding.UTF8.GetByteCount(name);
		ushort origLenU16 = (ushort)Math.Min(ushort.MaxValue, origLen);

		int totalBytesForItem = normLen + origLenU16;
		EnsureBufferCapacity(_bytesCount + totalBytesForItem);

		int offset = _bytesCount;
		_nameOffsets[index] = offset;
		_normalizedNameLengths[index] = normLen;
		_originalNameLengths[index] = origLenU16;

		Array.Copy(normalizedBytes, 0, _namesBuffer, offset, normLen);
		Encoding.UTF8.GetBytes(name, 0, name.Length, _namesBuffer, offset + normLen);

		_bytesCount += totalBytesForItem;
	}

	/// <summary>
	/// Busca o índice de um item pelo seu FileReference (O(1) após inicialização do mapa).
	/// Retorna -1 se não encontrado ou excluído.
	/// </summary>
	public int FindByFileReference(ulong fileReference)
	{
		if (fileReference == 0)
			return -1;

		if (_indexByFileRef is null)
		{
			_indexByFileRef = new Dictionary<ulong, int>(_count);
			for (int i = 0; i < _count; i++)
			{
				if (_parentIndices[i] != DeletedParentIndex && _fileReferences[i] != 0)
					_indexByFileRef[_fileReferences[i]] = i;
			}
		}

		return _indexByFileRef.TryGetValue(fileReference, out int idx) && _parentIndices[idx] != DeletedParentIndex
			? idx
			: -1;
	}

	/// <summary>
	/// Busca o índice de um filho imediato de <paramref name="parentIndex"/> pelo nome (case-insensitive).
	/// Retorna -1 se não encontrado.
	/// </summary>
	public int FindByParentAndName(int parentIndex, ReadOnlySpan<char> name)
	{
		for (int i = 0; i < _count; i++)
		{
			if (_parentIndices[i] == parentIndex && _originalNameLengths[i] > 0)
			{
				if (GetName(i).AsSpan().Equals(name, StringComparison.OrdinalIgnoreCase))
					return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// Resolve o índice de um diretório a partir do caminho relativo à raiz do volume (ex.: "SubA\SubB").
	/// Retorna -1 para a própria raiz do volume, ou -2 se algum segmento não existir.
	/// </summary>
	public int FindDirectoryIndexByRelativePath(ReadOnlySpan<char> relativePath)
	{
		ReadOnlySpan<char> trimmed = relativePath.Trim('\\');
		if (trimmed.IsEmpty)
			return RootParentIndex;

		int currentParent = RootParentIndex;
		while (!trimmed.IsEmpty)
		{
			int slash = trimmed.IndexOf('\\');
			ReadOnlySpan<char> segment = slash >= 0 ? trimmed[..slash] : trimmed;
			trimmed = slash >= 0 ? trimmed[(slash + 1)..].TrimStart('\\') : ReadOnlySpan<char>.Empty;

			int found = -1;
			for (int i = 0; i < _count; i++)
			{
				if (_parentIndices[i] == currentParent && IsDirectory(i) && _originalNameLengths[i] > 0)
				{
					if (GetName(i).AsSpan().Equals(segment, StringComparison.OrdinalIgnoreCase))
					{
						found = i;
						break;
					}
				}
			}

			if (found < 0)
				return DeletedParentIndex;

			currentParent = found;
		}

		return currentParent;
	}

	/// <summary>
	/// Indica se a entrada na posição <paramref name="index"/> foi marcada como excluída.
	/// </summary>
	public bool IsDeleted(int index) => index < 0 || index >= _count || _parentIndices[index] == DeletedParentIndex;

	/// <summary>
	/// Retorna a referência de arquivo (FRN / FileId).
	/// </summary>
	public ulong GetFileReference(int index) => _fileReferences[index];

	/// <summary>
	/// Retorna o índice do nó pai (-1 para raiz, -2 para excluído).
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

	private VolumeIndex(
		Guid volumeGuid,
		string volumeRoot,
		int count,
		int bytesCount,
		ulong[] fileReferences,
		int[] parentIndices,
		long[] fileSizes,
		long[] creationTimesUtcTicks,
		long[] lastWriteTimesUtcTicks,
		uint[] fileAttributes,
		int[] nameOffsets,
		ushort[] normalizedNameLengths,
		ushort[] originalNameLengths,
		byte[] namesBuffer)
	{
		VolumeGuid = volumeGuid;
		VolumeRoot = volumeRoot.TrimEnd('\\') + '\\';
		_count = count;
		_bytesCount = bytesCount;
		_fileReferences = fileReferences;
		_parentIndices = parentIndices;
		_fileSizes = fileSizes;
		_creationTimesUtcTicks = creationTimesUtcTicks;
		_lastWriteTimesUtcTicks = lastWriteTimesUtcTicks;
		_fileAttributes = fileAttributes;
		_nameOffsets = nameOffsets;
		_normalizedNameLengths = normalizedNameLengths;
		_originalNameLengths = originalNameLengths;
		_namesBuffer = namesBuffer;
	}

	internal static VolumeIndex FromRawArrays(
		Guid volumeGuid,
		string volumeRoot,
		int count,
		int bytesCount,
		ulong[] fileReferences,
		int[] parentIndices,
		long[] fileSizes,
		long[] creationTimesUtcTicks,
		long[] lastWriteTimesUtcTicks,
		uint[] fileAttributes,
		int[] nameOffsets,
		ushort[] normalizedNameLengths,
		ushort[] originalNameLengths,
		byte[] namesBuffer)
		=> new(
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

	internal (
		int Count,
		int BytesCount,
		ulong[] FileReferences,
		int[] ParentIndices,
		long[] FileSizes,
		long[] CreationTimesUtcTicks,
		long[] LastWriteTimesUtcTicks,
		uint[] FileAttributes,
		int[] NameOffsets,
		ushort[] NormalizedNameLengths,
		ushort[] OriginalNameLengths,
		byte[] NamesBuffer) GetRawArrays()
		=> (
			_count,
			_bytesCount,
			_fileReferences,
			_parentIndices,
			_fileSizes,
			_creationTimesUtcTicks,
			_lastWriteTimesUtcTicks,
			_fileAttributes,
			_nameOffsets,
			_normalizedNameLengths,
			_originalNameLengths,
			_namesBuffer);
}
