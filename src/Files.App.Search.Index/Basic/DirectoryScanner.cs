using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Files.App.Search.Index.Interop;
using Files.App.Search.Index.Storage;
using Files.App.Search.Index.Volumes;

namespace Files.App.Search.Index.Basic;

/// <summary>
/// Progresso da varredura de diretório no modo básico.
/// </summary>
public readonly record struct ScanProgress(
	Guid VolumeGuid,
	string VolumeRoot,
	long DirectoriesScanned,
	long FilesScanned,
	bool IsCompleted)
{
	public long TotalItems => DirectoriesScanned + FilesScanned;
}

/// <summary>
/// Varredor de diretórios do modo básico (sem privilégio de administrador).
/// Atende:
/// - FR-004: nunca abre arquivos (lê apenas metadados do diretório via FileIdBothDirectoryInfo).
/// - FR-012 / AC-9: nunca desce em pontos de reanálise (junções, symlinks, mount points), evitando ciclos e duplicatas.
/// - FR-016 / AC-21: deduplica links físicos (mesmo FileId) preservando apenas o primeiro nome via <see cref="HardLinkResolver"/>.
/// - NFR-005: roda em prioridade baixa (<c>THREAD_MODE_BACKGROUND_BEGIN</c>) e paraleliza por disco físico.
/// </summary>
public static class DirectoryScanner
{
	private const int BufferSize = 64 * 1024;
	private const int ProgressReportInterval = 4096;

	/// <summary>
	/// Varre múltiplos volumes paralelizando entre discos físicos distintos e serializando
	/// volumes que compartilham o mesmo disco físico.
	/// </summary>
	public static async Task<IReadOnlyDictionary<Guid, VolumeIndex>> ScanVolumesAsync(
		IEnumerable<VolumeInfo> volumes,
		IProgress<ScanProgress>? progress = null,
		CancellationToken cancellationToken = default,
		Action<Guid, VolumeIndex>? onVolumeScanned = null)
	{
		ArgumentNullException.ThrowIfNull(volumes);

		int fallbackDiskId = -1;
		var groups = volumes
			.GroupBy(v => GetPhysicalDiskNumber(v) ?? Interlocked.Decrement(ref fallbackDiskId))
			.ToList();

		var results = new System.Collections.Concurrent.ConcurrentDictionary<Guid, VolumeIndex>();

		var tasks = groups.Select(group => Task.Run(() =>
		{
			foreach (var volume in group)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var index = ScanDirectory(volume.VolumeGuid, volume.DisplayRoot, progress, cancellationToken);
				results[volume.VolumeGuid] = index;

				// T17d (NFR-002): quem chamou pode publicar este volume já, sem esperar os outros discos
				onVolumeScanned?.Invoke(volume.VolumeGuid, index);
			}
		}, cancellationToken));

		await Task.WhenAll(tasks).ConfigureAwait(false);
		return results;
	}

	/// <summary>
	/// Varre uma árvore de diretórios construindo um <see cref="VolumeIndex"/>.
	/// </summary>
	public static VolumeIndex ScanDirectory(
		Guid volumeGuid,
		string rootPath,
		IProgress<ScanProgress>? progress = null,
		CancellationToken cancellationToken = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

		string normalizedRoot = rootPath.TrimEnd('\\') + '\\';
		var index = new VolumeIndex(volumeGuid, normalizedRoot, initialCapacity: 4096);

		IntPtr currentThread = NativeMethods.GetCurrentThread();
		bool enteredBackground = NativeMethods.SetThreadPriority(currentThread, NativeMethods.THREAD_MODE_BACKGROUND_BEGIN);

		byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
		try
		{
			// Mapa FileId -> índice no VolumeIndex para deduplicar hardlinks (FR-016 / AC-21)
			var fileIdToIndex = new Dictionary<ulong, int>(4096);
			// Proteção adicional contra ciclos de diretório mesmo sem flag de reparse
			var visitedDirIds = new HashSet<ulong>();

			var stack = new Stack<(string FullPath, int ParentIndex)>(256);
			stack.Push((normalizedRoot.TrimEnd('\\'), -1));

			long dirsScanned = 0;
			long filesScanned = 0;
			long sinceLastReport = 0;

			while (stack.Count > 0)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var (currentDir, parentIdx) = stack.Pop();
				string openPath = currentDir.Length == 2 && currentDir[1] == ':'
					? currentDir + "\\"
					: currentDir;

				using var dirHandle = NativeMethods.CreateFileW(
					openPath,
					NativeMethods.FILE_LIST_DIRECTORY,
					NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
					IntPtr.Zero,
					NativeMethods.OPEN_EXISTING,
					NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
					IntPtr.Zero);

				if (dirHandle.IsInvalid)
					continue;

				bool firstCall = true;
				while (true)
				{
					cancellationToken.ThrowIfCancellationRequested();

					int infoClass = firstCall
						? NativeMethods.FileIdBothDirectoryRestartInfo
						: NativeMethods.FileIdBothDirectoryInfo;
					firstCall = false;

					bool ok;
					unsafe
					{
						fixed (byte* pBuf = rentedBuffer)
						{
							ok = NativeMethods.GetFileInformationByHandleEx(
								dirHandle,
								infoClass,
								pBuf,
								(uint)BufferSize);
						}
					}

					if (!ok)
						break;

					EnumerateBuffer(
						rentedBuffer,
						currentDir,
						parentIdx,
						normalizedRoot,
						index,
						fileIdToIndex,
						visitedDirIds,
						stack,
						ref dirsScanned,
						ref filesScanned);

					sinceLastReport = (dirsScanned + filesScanned);
					if (progress is not null && sinceLastReport >= ProgressReportInterval)
					{
						sinceLastReport = 0;
						progress.Report(new ScanProgress(volumeGuid, normalizedRoot, dirsScanned, filesScanned, IsCompleted: false));
					}
				}
			}

			index.TrimExcess();
			progress?.Report(new ScanProgress(volumeGuid, normalizedRoot, dirsScanned, filesScanned, IsCompleted: true));
			return index;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rentedBuffer);
			if (enteredBackground)
			{
				NativeMethods.SetThreadPriority(currentThread, NativeMethods.THREAD_MODE_BACKGROUND_END);
			}
		}
	}

	private static unsafe void EnumerateBuffer(
		byte[] buffer,
		string currentDir,
		int parentIdx,
		string normalizedRoot,
		VolumeIndex index,
		Dictionary<ulong, int> fileIdToIndex,
		HashSet<ulong> visitedDirIds,
		Stack<(string FullPath, int ParentIndex)> stack,
		ref long dirsScanned,
		ref long filesScanned)
	{
		fixed (byte* pBase = buffer)
		{
			byte* pCurrent = pBase;
			while (true)
			{
				var info = (NativeMethods.FILE_ID_BOTH_DIR_INFO*)pCurrent;
				int charCount = (int)(info->FileNameLength / sizeof(char));

				if (charCount > 0)
				{
					char* pName = &info->FileName;
					ReadOnlySpan<char> nameSpan = new(pName, charCount);

					if (nameSpan.Length == 1 && nameSpan[0] == '.')
					{
						if (parentIdx >= 0)
						{
							index.SetLastWriteTime(parentIdx, SafeFileTimeToDateTimeOffset(info->LastWriteTime));
						}
					}
					else if (!IsDotOrDotDot(nameSpan))
					{
						string name = new(nameSpan);
						uint attributes = info->FileAttributes;
						bool isDir = (attributes & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0;
						bool isReparse = (attributes & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0;
						ulong fileId = info->FileId;

						var creationTime = SafeFileTimeToDateTimeOffset(info->CreationTime);
						var lastWriteTime = SafeFileTimeToDateTimeOffset(info->LastWriteTime);

						if (isDir)
						{
							dirsScanned++;
							int dirIndex = index.Add(
								fileReference: fileId,
								parentIndex: parentIdx,
								name: name,
								size: 0,
								creationTime: creationTime,
								lastWriteTime: lastWriteTime,
								attributes: attributes);

							// FR-012 / AC-9: NUNCA desce em ponto de reanálise (junções, symlinks, mount points)
							if (!isReparse && (fileId == 0 || visitedDirIds.Add(fileId)))
							{
								string childFullPath = currentDir.EndsWith('\\')
									? currentDir + name
									: currentDir + "\\" + name;
								stack.Push((childFullPath, dirIndex));
							}
						}
						else
						{
							filesScanned++;
							if (fileId != 0 && fileIdToIndex.TryGetValue(fileId, out int existingIndex))
							{
								// FR-016 / AC-21: Hardlink detectado! Mantém apenas o primeiro nome (FindFirstFileNameW)
								string candidateFullPath = currentDir.EndsWith('\\')
									? currentDir + name
									: currentDir + "\\" + name;
								string? primaryFullPath = HardLinkResolver.ResolvePrimaryFullPath(candidateFullPath, normalizedRoot);

								if (primaryFullPath is not null &&
									(string.Equals(candidateFullPath, primaryFullPath, StringComparison.OrdinalIgnoreCase) ||
									 string.Equals(Path.GetFileName(primaryFullPath), name, StringComparison.OrdinalIgnoreCase)))
								{
									index.UpdateLocationAndName(existingIndex, parentIdx, name);
								}
							}
							else
							{
								int fileIndex = index.Add(
									fileReference: fileId,
									parentIndex: parentIdx,
									name: name,
									size: info->EndOfFile,
									creationTime: creationTime,
									lastWriteTime: lastWriteTime,
									attributes: attributes);

								if (fileId != 0)
								{
									fileIdToIndex[fileId] = fileIndex;
								}
							}
						}
					}
				}

				if (info->NextEntryOffset == 0)
					break;

				pCurrent += info->NextEntryOffset;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsDotOrDotDot(ReadOnlySpan<char> name)
	{
		if (name.Length == 1 && name[0] == '.')
			return true;
		if (name.Length == 2 && name[0] == '.' && name[1] == '.')
			return true;
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static DateTimeOffset SafeFileTimeToDateTimeOffset(long fileTime)
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

	/// <summary>
	/// Obtém o número do disco físico do volume via <c>IOCTL_STORAGE_GET_DEVICE_NUMBER</c>.
	/// </summary>
	public static unsafe int? GetPhysicalDiskNumber(VolumeInfo volume)
	{
		if (volume is null)
			return null;

		string devicePath = volume.VolumeGuidPath.TrimEnd('\\');
		using var handle = NativeMethods.CreateFileW(
			devicePath,
			0,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
			IntPtr.Zero,
			NativeMethods.OPEN_EXISTING,
			0,
			IntPtr.Zero);

		if (handle.IsInvalid)
			return null;

		NativeMethods.STORAGE_DEVICE_NUMBER sdn = default;
		if (NativeMethods.DeviceIoControl(
			handle,
			NativeMethods.IOCTL_STORAGE_GET_DEVICE_NUMBER,
			null,
			0,
			&sdn,
			(uint)sizeof(NativeMethods.STORAGE_DEVICE_NUMBER),
			out _,
			IntPtr.Zero))
		{
			return (int)sdn.DeviceNumber;
		}

		return null;
	}
}
