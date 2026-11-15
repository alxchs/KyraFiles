// Copyright (c) Files Community. Licensed under the MIT License.

using KyraFiles.App.Search.Index.Models;
using KyraFiles.App.Search.Index.Query;

namespace KyraFiles.App.Search.Index.Storage;

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
	private const int NoLink = -1;

	// Acima deste tamanho os arrays crescem 1/8 por vez em vez de dobrar (o primeiro Add depois do
	// TrimExcess de um índice de milhões de itens dobrava toda a memória — T17e)
	private const int LargeArrayThreshold = 1 << 20;

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

	// Filhos de cada pasta, construídos sob demanda na primeira operação estrutural (F015 T17a — NFR-005):
	// primeiro filho por item e irmãos duplamente ligados (12 bytes por item). Trocam as varreduras do
	// índice inteiro por percursos locais em FindByParentAndName, MarkDeleted e Reconciler.
	private int[]? _firstChild;
	private int[]? _nextSibling;
	private int[]? _prevSibling;
	private int _rootFirstChild = NoLink;
	private int _structureVersion;

	public object SyncRoot { get; } = new();
	public Guid VolumeGuid { get; }
	public string VolumeRoot { get; set; }
	public int Count => _count;

	/// <summary>
	/// Muda sempre que um item troca de pasta, é renomeado ou apagado: caminhos montados antes deixam de valer.
	/// </summary>
	public int StructureVersion => Volatile.Read(ref _structureVersion);

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

		if (_firstChild is not null)
		{
			_firstChild[index] = NoLink;
			LinkChild(index, parentIndex);
		}

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

		SetParent(index, parentIndex);
		WriteNames(index, name);
		_structureVersion++;
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

		SetParent(index, parentIndex);
		_fileSizes[index] = size;
		_creationTimesUtcTicks[index] = creationTime.UtcTicks;
		_lastWriteTimesUtcTicks[index] = lastWriteTime.UtcTicks;
		_fileAttributes[index] = attributes;

		if (!OriginalNameEquals(index, name, StringComparison.Ordinal))
		{
			WriteNames(index, name);
			_structureVersion++;
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

		_structureVersion++;

		if (!IsDirectory(index))
		{
			DeleteSingleEntry(index);
			return 1;
		}

		// Desce só pela subárvore (listas de filhos), em vez de varrer o índice inteiro a cada nível
		EnsureChildLinks();
		int deletedCount = 0;
		var pending = new Stack<int>();
		pending.Push(index);

		while (pending.Count > 0)
		{
			int current = pending.Pop();
			if (_parentIndices[current] == DeletedParentIndex)
				continue;

			int guard = 0;
			for (int child = _firstChild![current]; child != NoLink && guard++ < _count; child = _nextSibling![child])
				pending.Push(child);

			DeleteSingleEntry(current);
			deletedCount++;
		}

		return deletedCount;
	}

	private void DeleteSingleEntry(int index)
	{
		ulong fileRef = _fileReferences[index];
		if (_indexByFileRef is not null && fileRef != 0)
			_indexByFileRef.Remove(fileRef);

		if (_firstChild is not null)
		{
			UnlinkChild(index, _parentIndices[index]);
			_firstChild[index] = NoLink;
		}

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
		=> FindChild(parentIndex, name, directoriesOnly: false);

	/// <summary>
	/// Primeiro filho imediato de <paramref name="parentIndex"/> (-1 se não houver). Percorra com <see cref="GetNextSibling"/>.
	/// </summary>
	public int GetFirstChild(int parentIndex)
	{
		if (!IsLinkableParent(parentIndex))
			return NoLink;

		EnsureChildLinks();
		return GetHead(parentIndex);
	}

	/// <summary>
	/// Próximo irmão de <paramref name="index"/> na pasta (-1 no fim).
	/// </summary>
	public int GetNextSibling(int index)
		=> _nextSibling is null || index < 0 || index >= _count ? NoLink : _nextSibling[index];

	private int FindChild(int parentIndex, ReadOnlySpan<char> name, bool directoriesOnly)
	{
		if (!IsLinkableParent(parentIndex))
			return -1;

		EnsureChildLinks();
		int guard = 0;
		for (int child = GetHead(parentIndex); child != NoLink && guard++ < _count; child = _nextSibling![child])
		{
			if (_originalNameLengths[child] > 0 &&
				(!directoriesOnly || IsDirectory(child)) &&
				OriginalNameEquals(child, name, StringComparison.OrdinalIgnoreCase))
			{
				return child;
			}
		}

		return -1;
	}

	private bool OriginalNameEquals(int index, ReadOnlySpan<char> name, StringComparison comparison)
	{
		ReadOnlySpan<byte> utf8 = _namesBuffer.AsSpan(_nameOffsets[index] + _normalizedNameLengths[index], _originalNameLengths[index]);

		// Cada char UTF-16 ocupa de 1 a 3 bytes em UTF-8 (par substituto: 2 chars, 4 bytes)
		if (utf8.Length < name.Length || utf8.Length > name.Length * 3)
			return false;

		if (utf8.Length <= 512)
		{
			Span<char> chars = stackalloc char[utf8.Length];
			int written = Encoding.UTF8.GetChars(utf8, chars);
			return chars[..written].Equals(name, comparison);
		}

		return Encoding.UTF8.GetString(utf8).AsSpan().Equals(name, comparison);
	}

	private static bool IsLinkableParentValue(int parentIndex, int count)
		=> parentIndex == RootParentIndex || (parentIndex >= 0 && parentIndex < count);

	private bool IsLinkableParent(int parentIndex) => IsLinkableParentValue(parentIndex, _count);

	private int GetHead(int parentIndex)
		=> parentIndex == RootParentIndex ? _rootFirstChild : _firstChild![parentIndex];

	private void SetHead(int parentIndex, int child)
	{
		if (parentIndex == RootParentIndex)
			_rootFirstChild = child;
		else
			_firstChild![parentIndex] = child;
	}

	private void SetParent(int index, int newParent)
	{
		int oldParent = _parentIndices[index];
		if (oldParent == newParent || newParent == index)
			return;

		if (_firstChild is not null)
			UnlinkChild(index, oldParent);

		_parentIndices[index] = newParent;

		if (_firstChild is not null)
			LinkChild(index, newParent);

		_structureVersion++;
	}

	private void LinkChild(int index, int parentIndex)
	{
		_nextSibling![index] = NoLink;
		_prevSibling![index] = NoLink;

		if (parentIndex == index || !IsLinkableParent(parentIndex))
			return;

		int head = GetHead(parentIndex);
		_nextSibling[index] = head;
		if (head != NoLink)
			_prevSibling[head] = index;
		SetHead(parentIndex, index);
	}

	private void UnlinkChild(int index, int parentIndex)
	{
		int prev = _prevSibling![index];
		int next = _nextSibling![index];

		if (prev != NoLink)
			_nextSibling[prev] = next;
		else if (IsLinkableParent(parentIndex) && GetHead(parentIndex) == index)
			SetHead(parentIndex, next);
		else
			return; // não estava em nenhuma lista

		if (next != NoLink)
			_prevSibling[next] = prev;

		_prevSibling[index] = NoLink;
		_nextSibling[index] = NoLink;
	}

	/// <summary>
	/// Monta as listas de filhos a partir dos pais (O(n), uma vez por índice; depois são mantidas a cada mudança).
	/// </summary>
	private void EnsureChildLinks()
	{
		if (_firstChild is not null)
			return;

		lock (SyncRoot)
		{
			if (_firstChild is not null)
				return;

			int capacity = _fileReferences.Length;
			var first = new int[capacity];
			var next = new int[capacity];
			var prev = new int[capacity];
			first.AsSpan().Fill(NoLink);
			next.AsSpan().Fill(NoLink);
			prev.AsSpan().Fill(NoLink);
			int rootFirst = NoLink;

			// De trás para frente: cada lista fica em ordem crescente de índice
			for (int i = _count - 1; i >= 0; i--)
			{
				int parent = _parentIndices[i];
				if (parent == i || !IsLinkableParentValue(parent, _count))
					continue;

				int head = parent == RootParentIndex ? rootFirst : first[parent];
				next[i] = head;
				if (head != NoLink)
					prev[head] = i;

				if (parent == RootParentIndex)
					rootFirst = i;
				else
					first[parent] = i;
			}

			_nextSibling = next;
			_prevSibling = prev;
			_rootFirstChild = rootFirst;
			_firstChild = first;
		}
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

		// Chamado pelo pipe fora do lock do índice: as listas de filhos só são lidas com ele
		lock (SyncRoot)
		{
			int currentParent = RootParentIndex;
			while (!trimmed.IsEmpty)
			{
				int slash = trimmed.IndexOf('\\');
				ReadOnlySpan<char> segment = slash >= 0 ? trimmed[..slash] : trimmed;
				trimmed = slash >= 0 ? trimmed[(slash + 1)..].TrimStart('\\') : ReadOnlySpan<char>.Empty;

				int found = FindChild(currentParent, segment, directoriesOnly: true);
				if (found < 0)
					return DeletedParentIndex;

				currentParent = found;
			}

			return currentParent;
		}
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
		int capacity = _fileReferences.Length;
		if (required <= capacity)
			return;

		int growth = capacity < LargeArrayThreshold ? Math.Max(capacity, 16) : Math.Max(capacity / 8, 65_536);
		int newCap = (int)Math.Min(Array.MaxLength, Math.Max((long)required, (long)capacity + growth));

		if (_firstChild is not null)
		{
			Array.Resize(ref _firstChild, newCap);
			Array.Resize(ref _nextSibling, newCap);
			Array.Resize(ref _prevSibling, newCap);
			_firstChild.AsSpan(capacity).Fill(NoLink);
		}

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

		long length = _namesBuffer.Length;
		long growth = length < 64L * LargeArrayThreshold ? Math.Max(length, 1024) : Math.Max(length / 8, 4L * LargeArrayThreshold);
		long newCapLong = Math.Max((long)required, length + growth);
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

			if (_firstChild is not null)
			{
				Array.Resize(ref _firstChild, _count);
				Array.Resize(ref _nextSibling, _count);
				Array.Resize(ref _prevSibling, _count);
			}
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
