// Copyright (c) Files Community. Licensed under the MIT License.

using System.Buffers;
using System.Runtime.InteropServices;
using KyraFiles.App.Search.Index.Interop;
using KyraFiles.App.Search.Index.Storage;
using Microsoft.Win32.SafeHandles;

namespace KyraFiles.App.Search.Index.Basic;

/// <summary>
/// Resultado da reconferência incremental de um volume (F015 T9 — FR-032, AC-12).
/// </summary>
public readonly record struct ReconcileResult(
	int DirectoriesChecked,
	int DirectoriesRescanned,
	int ItemsAdded,
	int ItemsUpdated,
	int ItemsDeleted);

/// <summary>
/// Reconfere pastas cuja data de modificação mudou enquanto o aplicativo esteve fechado
/// ou após estouro de buffer do <see cref="ChangeWatcher"/>, sem reindexar todo o volume (FR-032, AC-12).
/// </summary>
/// <remarks>
/// F015 T17a/T17b: tudo é local à pasta revarrida (listas de filhos do <see cref="VolumeIndex"/>), sem passes
/// sobre o índice inteiro; a data de uma subpasta só muda depois que ela própria é revarrida.
/// </remarks>
public static class Reconciler
{
	private const int BufferSize = 64 * 1024;

	// Pastas conferidas por vez: as datas são lidas do disco fora do lock e aplicadas dentro dele
	private const int SliceDirectories = 1024;

	private const int ERROR_NO_MORE_FILES = 18;

	/// <summary>
	/// Reconfere incrementalmente um volume comparando a data de modificação de cada pasta no disco
	/// com a data gravada no índice/snapshot. Revarre apenas as pastas cuja data mudou.
	/// </summary>
	/// <remarks>
	/// Percorre a árvore de cima para baixo em fatias de <see cref="SliceDirectories"/> pastas: o lock do índice
	/// só é segurado para aplicar o resultado, então watcher, consultas e snapshot seguem durante a reconferência.
	/// Roda em prioridade de segundo plano (CPU e disco — NFR-005).
	/// </remarks>
	public static ReconcileResult ReconcileVolume(
		VolumeIndex index,
		ref DateTimeOffset rootLastWriteTime,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(index);

		var counters = new Counters();
		byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
		IntPtr currentThread = NativeMethods.GetCurrentThread();
		bool enteredBackground = NativeMethods.SetThreadPriority(currentThread, NativeMethods.THREAD_MODE_BACKGROUND_BEGIN);

		try
		{
			var pending = new Stack<PendingDirectory>();
			var batch = new CheckedDirectory[SliceDirectories];

			// 1. Raiz do volume (a data dela fica fora do índice)
			string rootPath = index.VolumeRoot;
			counters.DirectoriesChecked++;
			bool rootExists = TryGetDirectoryLastWriteTimeUtc(rootPath, out DateTimeOffset currentRootTime);

			lock (index.SyncRoot)
			{
				int existingBelow = index.Count;
				if (rootExists && currentRootTime != rootLastWriteTime)
				{
					existingBelow = RescanFolder(index, VolumeIndex.RootParentIndex, rootPath, buffer, counters, cancellationToken);
					rootLastWriteTime = currentRootTime;
				}

				PushChildDirectories(index, VolumeIndex.RootParentIndex, rootPath, existingBelow, pending);
			}

			// 2. Demais pastas, de cima para baixo, em fatias
			while (pending.Count > 0 && !cancellationToken.IsCancellationRequested)
			{
				int batchCount = 0;
				lock (index.SyncRoot)
				{
					int version = index.StructureVersion;
					while (batchCount < SliceDirectories && pending.Count > 0)
					{
						PendingDirectory next = pending.Pop();
						if (index.IsDeleted(next.Index) || !index.IsDirectory(next.Index))
							continue;

						string path = next.Version == version ? next.Path : index.GetFullPath(next.Index);
						batch[batchCount++] = new CheckedDirectory(next.Index, path, version);
					}
				}

				// Leitura das datas no disco sem o lock
				for (int b = 0; b < batchCount; b++)
				{
					ref CheckedDirectory checkedDir = ref batch[b];
					checkedDir.Exists = TryGetDirectoryLastWriteTimeUtc(checkedDir.Path, out checkedDir.CurrentTime);
				}

				lock (index.SyncRoot)
				{
					for (int b = 0; b < batchCount && !cancellationToken.IsCancellationRequested; b++)
					{
						ref CheckedDirectory checkedDir = ref batch[b];
						if (index.IsDeleted(checkedDir.Index))
							continue;

						counters.DirectoriesChecked++;

						// Algo mudou de lugar desde a leitura: confirma caminho e data sob o lock antes de apagar ou revarrer
						if (index.StructureVersion != checkedDir.Version)
						{
							string fresh = index.GetFullPath(checkedDir.Index);
							if (!string.Equals(fresh, checkedDir.Path, StringComparison.Ordinal))
							{
								checkedDir.Path = fresh;
								checkedDir.Exists = TryGetDirectoryLastWriteTimeUtc(fresh, out checkedDir.CurrentTime);
							}
						}

						if (!checkedDir.Exists)
						{
							counters.ItemsDeleted += index.MarkDeleted(checkedDir.Index);
							continue;
						}

						int existingBelow = index.Count;
						if (checkedDir.CurrentTime != index.GetLastWriteTime(checkedDir.Index))
						{
							existingBelow = RescanFolder(index, checkedDir.Index, checkedDir.Path, buffer, counters, cancellationToken);
							index.SetLastWriteTime(checkedDir.Index, checkedDir.CurrentTime);
						}

						PushChildDirectories(index, checkedDir.Index, checkedDir.Path, existingBelow, pending);
					}
				}

				Array.Clear(batch, 0, batchCount);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
			if (enteredBackground)
				NativeMethods.SetThreadPriority(currentThread, NativeMethods.THREAD_MODE_BACKGROUND_END);
		}

		return counters.ToResult();
	}

	/// <summary>
	/// Reconfere uma subárvore específica por inteiro (pasta nova vinda de fora, ou estouro sem reconciliador).
	/// </summary>
	public static ReconcileResult ReconcileSubtree(
		VolumeIndex index,
		int subtreeRootIndex,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(index);

		lock (index.SyncRoot)
		{
			var counters = new Counters();
			string rootPath = subtreeRootIndex < 0 ? index.VolumeRoot : index.GetFullPath(subtreeRootIndex);
			if (!TryGetDirectoryLastWriteTimeUtc(rootPath, out _))
			{
				counters.DirectoriesChecked++;
				if (subtreeRootIndex >= 0)
					counters.ItemsDeleted += index.MarkDeleted(subtreeRootIndex);
				return counters.ToResult();
			}

			byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
			try
			{
				ScanWholeSubtrees(index, [(subtreeRootIndex, rootPath)], buffer, counters, cancellationToken);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}

			return counters.ToResult();
		}
	}

	/// <summary>
	/// Revarre uma pasta cuja data mudou: atualiza os filhos, apaga os que sumiram e varre por inteiro as subpastas novas.
	/// Devolve o tamanho do índice antes da revarredura: filhos com índice menor já existiam e ainda precisam ser conferidos.
	/// </summary>
	private static int RescanFolder(
		VolumeIndex index,
		int dirIndex,
		string dirPath,
		byte[] buffer,
		Counters counters,
		CancellationToken cancellationToken)
	{
		int existingBelow = index.Count;
		HashSet<int>? seen = ScanDirectoryImmediate(
			index,
			dirIndex,
			dirPath,
			buffer,
			counters,
			out List<(int SubDirIdx, string SubDirPath)> newSubDirs,
			onlyReturnNewlyCreatedSubDirs: true);

		if (seen is not null)
			RemoveUnseenChildren(index, dirIndex, seen, counters);

		ScanWholeSubtrees(index, newSubDirs, buffer, counters, cancellationToken);
		return existingBelow;
	}

	private static void ScanWholeSubtrees(
		VolumeIndex index,
		IEnumerable<(int DirIdx, string Path)> roots,
		byte[] buffer,
		Counters counters,
		CancellationToken cancellationToken)
	{
		var queue = new Queue<(int DirIdx, string Path)>(roots);
		var visited = new HashSet<int>();

		while (queue.Count > 0 && !cancellationToken.IsCancellationRequested)
		{
			var (dirIdx, dirPath) = queue.Dequeue();
			if (!visited.Add(dirIdx) || (dirIdx >= 0 && index.IsDeleted(dirIdx)))
				continue;

			counters.DirectoriesChecked++;
			HashSet<int>? seen = ScanDirectoryImmediate(
				index,
				dirIdx,
				dirPath,
				buffer,
				counters,
				out List<(int SubDirIdx, string SubDirPath)> subDirs,
				onlyReturnNewlyCreatedSubDirs: false);

			if (seen is not null)
				RemoveUnseenChildren(index, dirIdx, seen, counters);

			// Data lida da mesma fonte que a reconferência usa, para não revarrer à toa na próxima abertura
			if (dirIdx >= 0 && TryGetDirectoryLastWriteTimeUtc(dirPath, out DateTimeOffset dt))
				index.SetLastWriteTime(dirIdx, dt);

			foreach (var sub in subDirs)
				queue.Enqueue(sub);
		}
	}

	/// <summary>
	/// Apaga os filhos de <paramref name="dirIndex"/> que não apareceram na listagem (percorre só os filhos da pasta).
	/// </summary>
	private static void RemoveUnseenChildren(VolumeIndex index, int dirIndex, HashSet<int> seen, Counters counters)
	{
		int child = index.GetFirstChild(dirIndex);
		while (child != -1)
		{
			int next = index.GetNextSibling(child);
			if (!seen.Contains(child))
				counters.ItemsDeleted += index.MarkDeleted(child);
			child = next;
		}
	}

	private static void PushChildDirectories(
		VolumeIndex index,
		int dirIndex,
		string dirPath,
		int existingBelow,
		Stack<PendingDirectory> pending)
	{
		int version = index.StructureVersion;
		string prefix = dirPath.EndsWith('\\') ? dirPath : dirPath + '\\';

		for (int child = index.GetFirstChild(dirIndex); child != -1; child = index.GetNextSibling(child))
		{
			// Subpastas novas (índice >= existingBelow) já foram varridas por inteiro
			if (child >= existingBelow || index.IsDeleted(child) || !index.IsDirectory(child))
				continue;

			// Não desce em pontos de reanálise (FR-012)
			if ((index.GetFileAttributes(child) & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
				continue;

			pending.Push(new PendingDirectory(child, prefix + index.GetName(child), version));
		}
	}

	/// <summary>
	/// Lista uma pasta e aplica o que encontrou. Devolve os filhos vistos, ou <c>null</c> se a listagem não
	/// pôde ser feita por inteiro (sem permissão, pasta sumiu no meio): nesse caso nada deve ser apagado.
	/// </summary>
	private static unsafe HashSet<int>? ScanDirectoryImmediate(
		VolumeIndex index,
		int parentIndex,
		string dirPath,
		byte[] buffer,
		Counters counters,
		out List<(int SubDirIdx, string SubDirPath)> discoveredSubDirs,
		bool onlyReturnNewlyCreatedSubDirs)
	{
		discoveredSubDirs = [];

		using SafeFileHandle hDir = NativeMethods.CreateFileW(
			ToExtendedPath(dirPath),
			NativeMethods.FILE_LIST_DIRECTORY,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
			IntPtr.Zero,
			NativeMethods.OPEN_EXISTING,
			NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
			IntPtr.Zero);

		if (hDir.IsInvalid)
			return null;

		var seen = new HashSet<int>();
		counters.DirectoriesRescanned++;
		bool restartScan = true;
		bool complete = true;
		string prefix = dirPath.EndsWith('\\') ? dirPath : dirPath + '\\';

		fixed (byte* pBuf = buffer)
		{
			while (true)
			{
				int infoClass = restartScan
					? NativeMethods.FileIdBothDirectoryRestartInfo
					: NativeMethods.FileIdBothDirectoryInfo;
				restartScan = false;

				if (!NativeMethods.GetFileInformationByHandleEx(hDir, infoClass, pBuf, (uint)buffer.Length))
				{
					complete = Marshal.GetLastWin32Error() == ERROR_NO_MORE_FILES;
					break;
				}

				uint offset = 0;
				while (true)
				{
					var info = (NativeMethods.FILE_ID_BOTH_DIR_INFO*)(pBuf + offset);
					int charCount = (int)(info->FileNameLength / 2);

					if (charCount > 0)
					{
						var nameSpan = new ReadOnlySpan<char>(&info->FileName, charCount);
						if (!IsDotOrDotDot(nameSpan))
						{
							string name = new(nameSpan);
							uint attrs = info->FileAttributes;
							bool isDir = (attrs & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0;
							bool isReparse = (attrs & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0;
							ulong fileId = info->FileId;

							DateTimeOffset creationTime = SafeFromFileTime(info->CreationTime);
							DateTimeOffset lastWriteTime = SafeFromFileTime(info->LastWriteTime);

							int existingIdx = fileId != 0
								? index.FindByFileReference(fileId)
								: index.FindByParentAndName(parentIndex, nameSpan);

							if (existingIdx >= 0)
							{
								// T17b: a data de uma subpasta que já existe só muda quando ela mesma for revarrida;
								// gravar aqui a data nova faria a reconferência pular a subpasta e perder o que mudou nela
								DateTimeOffset storedWriteTime = isDir && index.IsDirectory(existingIdx)
									? index.GetLastWriteTime(existingIdx)
									: lastWriteTime;

								index.UpdateEntry(existingIdx, parentIndex, name, info->EndOfFile, creationTime, storedWriteTime, attrs);
								seen.Add(existingIdx);
								counters.ItemsUpdated++;

								if (isDir && !isReparse && !onlyReturnNewlyCreatedSubDirs)
									discoveredSubDirs.Add((existingIdx, prefix + name));
							}
							else
							{
								int newIdx = index.Add(fileId, parentIndex, name, info->EndOfFile, creationTime, lastWriteTime, attrs);
								seen.Add(newIdx);
								counters.ItemsAdded++;

								if (isDir && !isReparse)
									discoveredSubDirs.Add((newIdx, prefix + name));
							}
						}
					}

					if (info->NextEntryOffset == 0)
						break;

					offset += info->NextEntryOffset;
				}
			}
		}

		return complete ? seen : null;
	}

	/// <summary>
	/// Data de modificação de uma pasta numa única chamada ao sistema; <c>false</c> se não existe ou não é mais pasta.
	/// </summary>
	private static bool TryGetDirectoryLastWriteTimeUtc(string path, out DateTimeOffset lastWriteTimeUtc)
	{
		if (NativeMethods.GetFileAttributesExW(ToExtendedPath(path), NativeMethods.GetFileExInfoStandard, out NativeMethods.WIN32_FILE_ATTRIBUTE_DATA data) &&
			(data.dwFileAttributes & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0)
		{
			lastWriteTimeUtc = SafeFromFileTime(((long)data.ftLastWriteTimeHigh << 32) | data.ftLastWriteTimeLow);
			return true;
		}

		lastWriteTimeUtc = default;
		return false;
	}

	/// <summary>
	/// Prefixo <c>\\?\</c> para caminhos longos (as chamadas diretas ao Win32 não o acrescentam sozinhas).
	/// </summary>
	private static string ToExtendedPath(string path)
		=> path.Length >= 248 && !path.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\" + path : path;

	private static bool IsDotOrDotDot(ReadOnlySpan<char> name)
		=> (name.Length == 1 && name[0] == '.') || (name.Length == 2 && name[0] == '.' && name[1] == '.');

	private static DateTimeOffset SafeFromFileTime(long fileTime)
	{
		if (fileTime <= 0)
			return DateTimeOffset.MinValue;

		try
		{
			// UTC direto: o instante é o mesmo, sem consultar o fuso horário a cada item
			return new DateTimeOffset(DateTime.FromFileTimeUtc(fileTime), TimeSpan.Zero);
		}
		catch
		{
			return DateTimeOffset.MinValue;
		}
	}

	private readonly record struct PendingDirectory(int Index, string Path, int Version);

	private struct CheckedDirectory(int index, string path, int version)
	{
		public int Index = index;
		public string Path = path;
		public int Version = version;
		public bool Exists;
		public DateTimeOffset CurrentTime;
	}

	private sealed class Counters
	{
		public int DirectoriesChecked;
		public int DirectoriesRescanned;
		public int ItemsAdded;
		public int ItemsUpdated;
		public int ItemsDeleted;

		public ReconcileResult ToResult()
			=> new(DirectoriesChecked, DirectoriesRescanned, ItemsAdded, ItemsUpdated, ItemsDeleted);
	}
}
