// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Buffers;
using System.Runtime.InteropServices;
using KyraFiles.App.Search.Index.Interop;
using KyraFiles.App.Search.Index.Storage;
using Microsoft.Win32.SafeHandles;

namespace KyraFiles.App.Search.Index.Basic;

/// <summary>
/// Monitora alterações em tempo real em um volume (modo básico) usando <c>ReadDirectoryChangesExW</c>
/// recursivo e recupera de estouro de buffer via <see cref="Reconciler"/> (F015 T9 — FR-030, AC-10).
/// </summary>
public sealed class ChangeWatcher : IDisposable
{
	// 512 KB em volume local (o limite de 64 KB do ReadDirectoryChangesW vale só para rede): menos estouros num C: movimentado (T17c)
	private const int DefaultBufferSize = 512 * 1024;

	private const uint NotifyFilter =
		NativeMethods.FILE_NOTIFY_CHANGE_FILE_NAME |
		NativeMethods.FILE_NOTIFY_CHANGE_DIR_NAME |
		NativeMethods.FILE_NOTIFY_CHANGE_ATTRIBUTES |
		NativeMethods.FILE_NOTIFY_CHANGE_SIZE |
		NativeMethods.FILE_NOTIFY_CHANGE_LAST_WRITE |
		NativeMethods.FILE_NOTIFY_CHANGE_CREATION;

	private readonly VolumeIndex _index;
	private readonly string _watchedPath;
	private readonly int _watchedDirIndex;
	private readonly int _bufferSize;
	private readonly CancellationTokenSource _cts = new();
	private readonly ManualResetEventSlim _readyEvent = new(false);
	private readonly VolumeReconciler? _reconciler;

	private SafeFileHandle? _directoryHandle;
	private Thread? _workerThread;
	private int _eventsProcessed;
	private int _overflowReconcileCount;
	private bool _disposed;

	public int EventsProcessed => Volatile.Read(ref _eventsProcessed);
	public int OverflowReconcileCount => Volatile.Read(ref _overflowReconcileCount);

	public ChangeWatcher(
		VolumeIndex index,
		string? subtreePath = null,
		int subtreeDirectoryIndex = VolumeIndex.RootParentIndex,
		int bufferSize = DefaultBufferSize,
		VolumeReconciler? reconciler = null)
	{
		ArgumentNullException.ThrowIfNull(index);
		_index = index;
		_reconciler = reconciler;
		_watchedPath = string.IsNullOrWhiteSpace(subtreePath) ? index.VolumeRoot : subtreePath;
		_watchedDirIndex = subtreeDirectoryIndex;
		_bufferSize = Math.Max(4096, bufferSize);
	}

	/// <summary>
	/// Inicia o monitoramento assíncrono em thread de baixa prioridade.
	/// </summary>
	public void Start()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_workerThread is not null)
			return;

		_directoryHandle = NativeMethods.CreateFileW(
			_watchedPath,
			NativeMethods.FILE_LIST_DIRECTORY,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
			IntPtr.Zero,
			NativeMethods.OPEN_EXISTING,
			NativeMethods.FILE_FLAG_BACKUP_SEMANTICS | NativeMethods.FILE_FLAG_OVERLAPPED,
			IntPtr.Zero);

		if (_directoryHandle.IsInvalid)
			throw new IOException($"Não foi possível abrir o diretório para monitoramento: {_watchedPath}");

		_workerThread = new Thread(WatchLoop)
		{
			IsBackground = true,
			Name = $"FilesIndexWatcher:{_watchedPath}"
		};
		_workerThread.Start();
		_readyEvent.Wait(TimeSpan.FromSeconds(5));
	}

	/// <summary>
	/// Simula ou trata um estouro de buffer do ReadDirectoryChangesExW reconferindo apenas a subárvore afetada.
	/// </summary>
	public ReconcileResult HandleBufferOverflow(int? affectedSubtreeIndex = null)
	{
		int targetDir = affectedSubtreeIndex ?? _watchedDirIndex;
		Interlocked.Increment(ref _overflowReconcileCount);
		return Reconciler.ReconcileSubtree(_index, targetDir, _cts.Token);
	}

	/// <summary>
	/// Eventos perdidos: com reconciliador, só pede a reconferência por data (sem bloquear o watcher, que volta a ler
	/// as mudanças na hora); sem ele (uso avulso e testes), reconfere a subárvore na própria thread.
	/// </summary>
	private void OnOverflow()
	{
		if (_reconciler is null)
		{
			HandleBufferOverflow(_watchedDirIndex);
			return;
		}

		Interlocked.Increment(ref _overflowReconcileCount);
		_reconciler.Request();
	}

	private unsafe void WatchLoop()
	{
		byte[] buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
		using var ioEvent = new ManualResetEvent(false);

		try
		{
			while (!_cts.IsCancellationRequested && _directoryHandle is { IsInvalid: false, IsClosed: false })
			{
				ioEvent.Reset();
				NativeOverlapped overlapped = default;
				overlapped.EventHandle = ioEvent.SafeWaitHandle.DangerousGetHandle();

				uint bytesReturned = 0;
				bool ok;

				fixed (byte* pBuf = buffer)
				{
					ok = NativeMethods.ReadDirectoryChangesExW(
						_directoryHandle,
						pBuf,
						(uint)buffer.Length,
						bWatchSubtree: true,
						NotifyFilter,
						out bytesReturned,
						&overlapped,
						IntPtr.Zero,
						NativeMethods.ReadDirectoryNotifyExtendedInformation);

					int err = ok ? 0 : Marshal.GetLastWin32Error();

					// Assim que o IRP está registrado no kernel (ok ou ERROR_IO_PENDING), libera o Start()!
					_readyEvent.Set();

					if (!ok)
					{
						if (err == NativeMethods.ERROR_IO_PENDING)
						{
							ok = NativeMethods.GetOverlappedResult(
								_directoryHandle,
								&overlapped,
								out bytesReturned,
								bWait: true);
							err = ok ? 0 : Marshal.GetLastWin32Error();
						}
					}

					if (_cts.IsCancellationRequested)
						break;

					if (!ok)
					{
						if (err == NativeMethods.ERROR_OPERATION_ABORTED)
							break;

						if (err == NativeMethods.ERROR_NOTIFY_ENUM_DIR)
						{
							OnOverflow();
							continue;
						}

						Thread.Sleep(50);
						continue;
					}
				}

				if (bytesReturned == 0)
				{
					// Estouro de buffer reportado pelo Windows (bytesReturned == 0)
					OnOverflow();
					continue;
				}

				ProcessNotificationBuffer(buffer, bytesReturned);
			}
		}
		finally
		{
			_readyEvent.Set();
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private unsafe void ProcessNotificationBuffer(byte[] buffer, uint bytesReturned)
	{
		lock (_index.SyncRoot)
		{
			fixed (byte* pBuf = buffer)
			{
				uint offset = 0;
				int pendingRenameOldIdx = -1;

				while (offset < bytesReturned)
				{
					var info = (NativeMethods.FILE_NOTIFY_EXTENDED_INFORMATION*)(pBuf + offset);
					int charCount = (int)(info->FileNameLength / 2);

					if (charCount > 0)
					{
						var relPathSpan = new ReadOnlySpan<char>(&info->FileName, charCount);
						ApplySingleNotification(info, relPathSpan, ref pendingRenameOldIdx);
						Interlocked.Increment(ref _eventsProcessed);
					}

					if (info->NextEntryOffset == 0)
						break;

					offset += info->NextEntryOffset;
				}

				// Se sobrou um OLD_NAME sem par NEW_NAME (ex.: movido para fora da árvore monitorada), remove-o
				if (pendingRenameOldIdx >= 0)
				{
					_index.MarkDeleted(pendingRenameOldIdx);
				}
			}
		}
	}

	private unsafe void ApplySingleNotification(
		NativeMethods.FILE_NOTIFY_EXTENDED_INFORMATION* info,
		ReadOnlySpan<char> relativePathFromWatchedRoot,
		ref int pendingRenameOldIdx)
	{
		SplitParentAndName(relativePathFromWatchedRoot, out ReadOnlySpan<char> relParentDir, out ReadOnlySpan<char> nameSpan);
		if (nameSpan.IsEmpty)
			return;

		string name = new(nameSpan);
		uint action = info->Action;
		ulong fileId = info->FileId;
		uint attrs = info->FileAttributes;
		DateTimeOffset creationTime = SafeFromFileTime(info->CreationTime);
		DateTimeOffset lastWriteTime = SafeFromFileTime(info->LastModificationTime);

		if (action == NativeMethods.FILE_ACTION_RENAMED_OLD_NAME)
		{
			if (pendingRenameOldIdx >= 0)
			{
				_index.MarkDeleted(pendingRenameOldIdx);
			}

			int parentIdx = ResolveRelativeDirectoryIndex(relParentDir, createMissing: false);
			int oldIdx = fileId != 0 ? _index.FindByFileReference(fileId) : -1;
			if (oldIdx < 0 && parentIdx != VolumeIndex.DeletedParentIndex)
			{
				oldIdx = _index.FindByParentAndName(parentIdx, nameSpan);
			}

			pendingRenameOldIdx = oldIdx;
			return;
		}

		if (action == NativeMethods.FILE_ACTION_REMOVED)
		{
			if (pendingRenameOldIdx >= 0)
			{
				_index.MarkDeleted(pendingRenameOldIdx);
				pendingRenameOldIdx = -1;
			}

			int parentIdx = ResolveRelativeDirectoryIndex(relParentDir, createMissing: false);
			int targetIdx = fileId != 0 ? _index.FindByFileReference(fileId) : -1;
			if (targetIdx < 0 && parentIdx != VolumeIndex.DeletedParentIndex)
			{
				targetIdx = _index.FindByParentAndName(parentIdx, nameSpan);
			}

			if (targetIdx >= 0)
			{
				_index.MarkDeleted(targetIdx);
			}

			return;
		}

		// FILE_ACTION_ADDED, FILE_ACTION_MODIFIED ou FILE_ACTION_RENAMED_NEW_NAME
		int resolvedParent = ResolveRelativeDirectoryIndex(relParentDir, createMissing: true);
		if (resolvedParent == VolumeIndex.DeletedParentIndex)
			resolvedParent = _watchedDirIndex;

		int existingIdx = -1;
		if (action == NativeMethods.FILE_ACTION_RENAMED_NEW_NAME && pendingRenameOldIdx >= 0)
		{
			existingIdx = pendingRenameOldIdx;
			pendingRenameOldIdx = -1;
		}
		else if (pendingRenameOldIdx >= 0)
		{
			_index.MarkDeleted(pendingRenameOldIdx);
			pendingRenameOldIdx = -1;
		}

		if (existingIdx < 0 && fileId != 0)
		{
			existingIdx = _index.FindByFileReference(fileId);
		}

		if (existingIdx < 0)
		{
			existingIdx = _index.FindByParentAndName(resolvedParent, nameSpan);
		}

		if (existingIdx >= 0)
		{
			_index.UpdateEntry(existingIdx, resolvedParent, name, info->FileSize, creationTime, lastWriteTime, attrs);
		}
		else
		{
			int newIdx = _index.Add(fileId, resolvedParent, name, info->FileSize, creationTime, lastWriteTime, attrs);

			// Se uma pasta inteira foi criada ou movida de fora com filhos já dentro, reconcilia sua subárvore
			bool isDir = (attrs & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0;
			bool isReparse = (attrs & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0;
			if (isDir && !isReparse && action is NativeMethods.FILE_ACTION_ADDED or NativeMethods.FILE_ACTION_RENAMED_NEW_NAME)
			{
				Reconciler.ReconcileSubtree(_index, newIdx, _cts.Token);
			}
		}
	}

	private int ResolveRelativeDirectoryIndex(ReadOnlySpan<char> relParentDir, bool createMissing)
	{
		ReadOnlySpan<char> trimmed = relParentDir.Trim('\\');
		int currentParent = _watchedDirIndex;
		if (trimmed.IsEmpty)
			return currentParent;

		while (!trimmed.IsEmpty)
		{
			int slash = trimmed.IndexOf('\\');
			ReadOnlySpan<char> segment = slash >= 0 ? trimmed[..slash] : trimmed;
			trimmed = slash >= 0 ? trimmed[(slash + 1)..].TrimStart('\\') : ReadOnlySpan<char>.Empty;

			int found = _index.FindByParentAndName(currentParent, segment);
			if (found < 0)
			{
				if (!createMissing)
					return VolumeIndex.DeletedParentIndex;

				var now = DateTimeOffset.UtcNow;
				found = _index.Add(0UL, currentParent, new string(segment), 0L, now, now, NativeMethods.FILE_ATTRIBUTE_DIRECTORY);
			}

			currentParent = found;
		}

		return currentParent;
	}

	private static void SplitParentAndName(
		ReadOnlySpan<char> relativePath,
		out ReadOnlySpan<char> parentDir,
		out ReadOnlySpan<char> fileName)
	{
		ReadOnlySpan<char> cleaned = relativePath.Trim('\\');
		int lastSlash = cleaned.LastIndexOf('\\');
		if (lastSlash < 0)
		{
			parentDir = ReadOnlySpan<char>.Empty;
			fileName = cleaned;
		}
		else
		{
			parentDir = cleaned[..lastSlash];
			fileName = cleaned[(lastSlash + 1)..];
		}
	}

	private static DateTimeOffset SafeFromFileTime(long fileTime)
	{
		if (fileTime <= 0)
			return DateTimeOffset.MinValue;

		try
		{
			return new DateTimeOffset(DateTime.FromFileTimeUtc(fileTime), TimeSpan.Zero);
		}
		catch
		{
			return DateTimeOffset.MinValue;
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_cts.Cancel();

		if (_directoryHandle is { IsInvalid: false, IsClosed: false })
		{
			try
			{
				NativeMethods.CancelIoEx(_directoryHandle, IntPtr.Zero);
			}
			catch
			{
			}
		}

		_workerThread?.Join(TimeSpan.FromSeconds(2));
		_directoryHandle?.Dispose();
		_readyEvent.Dispose();
		_cts.Dispose();
	}
}
