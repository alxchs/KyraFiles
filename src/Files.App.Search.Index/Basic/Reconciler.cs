// Copyright (c) Files Community. Licensed under the MIT License.

using System.Buffers;
using System.Runtime.InteropServices;
using Files.App.Search.Index.Interop;
using Files.App.Search.Index.Storage;
using Microsoft.Win32.SafeHandles;

namespace Files.App.Search.Index.Basic;

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
public static class Reconciler
{
	private const int BufferSize = 64 * 1024;

	/// <summary>
	/// Reconfere incrementalmente um volume comparando a data de modificação de cada pasta no disco
	/// com a data gravada no índice/snapshot. Revarre apenas as pastas cuja data mudou.
	/// </summary>
	public static ReconcileResult ReconcileVolume(
		VolumeIndex index,
		ref DateTimeOffset rootLastWriteTime,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(index);

		lock (index.SyncRoot)
		{
			int dirsChecked = 0;
			int dirsRescanned = 0;
			int itemsAdded = 0;
			int itemsUpdated = 0;
			int itemsDeleted = 0;

			var scannedFolders = new List<(int DirIndex, HashSet<int> SeenChildren)>();
			byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

			try
			{
				// Captura a quantidade de itens existentes antes de iniciar (novas pastas adicionadas durante a passada 1 já nascem varridas)
				int initialCount = index.Count;

				// 1. Verifica a raiz do volume (-1)
				string rootPath = index.VolumeRoot;
				dirsChecked++;
				if (TryGetDirectoryLastWriteTimeUtc(rootPath, out DateTimeOffset currentRootTime))
				{
					if (currentRootTime != rootLastWriteTime)
					{
						ScanSingleDirectoryAndNewSubtrees(
							index,
							VolumeIndex.RootParentIndex,
							rootPath,
							buffer,
							scannedFolders,
							ref dirsRescanned,
							ref itemsAdded,
							ref itemsUpdated,
							cancellationToken);
						rootLastWriteTime = currentRootTime;
					}
				}

				// 2. Verifica cada pasta já registrada no índice
				for (int i = 0; i < initialCount; i++)
				{
					if (cancellationToken.IsCancellationRequested)
						break;

					if (index.IsDeleted(i) || !index.IsDirectory(i))
						continue;

					// Não desce em pontos de reanálise (FR-012)
					if ((index.GetFileAttributes(i) & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
						continue;

					dirsChecked++;
					string dirPath = index.GetFullPath(i);

					if (!TryGetDirectoryLastWriteTimeUtc(dirPath, out DateTimeOffset currentDirTime))
					{
						// A pasta não existe mais no disco
						itemsDeleted += index.MarkDeleted(i);
						continue;
					}

					DateTimeOffset storedDirTime = index.GetLastWriteTime(i);
					if (currentDirTime != storedDirTime)
					{
						ScanSingleDirectoryAndNewSubtrees(
							index,
							i,
							dirPath,
							buffer,
							scannedFolders,
							ref dirsRescanned,
							ref itemsAdded,
							ref itemsUpdated,
							cancellationToken);
						index.SetLastWriteTime(i, currentDirTime);
					}
				}

				// 3. Passada 2: Remove filhos que sumiram das pastas revarridas (e não foram movidos para outra pasta)
				foreach (var (dirIdx, seenChildren) in scannedFolders)
				{
					if (dirIdx >= 0 && index.IsDeleted(dirIdx))
						continue;

					int currentTotal = index.Count;
					for (int i = 0; i < currentTotal; i++)
					{
						if (index.IsDeleted(i))
							continue;

						if (index.GetParentIndex(i) == dirIdx && !seenChildren.Contains(i))
						{
							itemsDeleted += index.MarkDeleted(i);
						}
					}
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}

			return new ReconcileResult(
				DirectoriesChecked: dirsChecked,
				DirectoriesRescanned: dirsRescanned,
				ItemsAdded: itemsAdded,
				ItemsUpdated: itemsUpdated,
				ItemsDeleted: itemsDeleted);
		}
	}

	/// <summary>
	/// Reconfere uma subárvore específica (usado após estouro de buffer do <see cref="ChangeWatcher"/>).
	/// </summary>
	public static ReconcileResult ReconcileSubtree(
		VolumeIndex index,
		int subtreeRootIndex,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(index);

		lock (index.SyncRoot)
		{
			int dirsChecked = 0;
			int dirsRescanned = 0;
			int itemsAdded = 0;
			int itemsUpdated = 0;
			int itemsDeleted = 0;

			string rootPath = subtreeRootIndex < 0 ? index.VolumeRoot : index.GetFullPath(subtreeRootIndex);
			if (!Directory.Exists(rootPath))
			{
				if (subtreeRootIndex >= 0)
					itemsDeleted += index.MarkDeleted(subtreeRootIndex);
				return new ReconcileResult(1, 0, 0, 0, itemsDeleted);
			}

			var scannedFolders = new List<(int DirIndex, HashSet<int> SeenChildren)>();
			byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);

			try
			{
				var queue = new Queue<(int DirIdx, string Path)>();
				queue.Enqueue((subtreeRootIndex, rootPath));

				while (queue.Count > 0 && !cancellationToken.IsCancellationRequested)
				{
					var (dirIdx, dirPath) = queue.Dequeue();
					dirsChecked++;

					var seen = ScanDirectoryImmediate(
						index,
						dirIdx,
						dirPath,
						buffer,
						ref dirsRescanned,
						ref itemsAdded,
						ref itemsUpdated,
						out List<(int SubDirIdx, string SubDirPath)> subDirs);

					scannedFolders.Add((dirIdx, seen));

					if (dirIdx >= 0 && TryGetDirectoryLastWriteTimeUtc(dirPath, out DateTimeOffset dt))
						index.SetLastWriteTime(dirIdx, dt);

					foreach (var sub in subDirs)
						queue.Enqueue(sub);
				}

				foreach (var (dirIdx, seenChildren) in scannedFolders)
				{
					if (dirIdx >= 0 && index.IsDeleted(dirIdx))
						continue;

					int currentTotal = index.Count;
					for (int i = 0; i < currentTotal; i++)
					{
						if (!index.IsDeleted(i) && index.GetParentIndex(i) == dirIdx && !seenChildren.Contains(i))
						{
							itemsDeleted += index.MarkDeleted(i);
						}
					}
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}

			return new ReconcileResult(dirsChecked, dirsRescanned, itemsAdded, itemsUpdated, itemsDeleted);
		}
	}

	private static void ScanSingleDirectoryAndNewSubtrees(
		VolumeIndex index,
		int dirIdx,
		string dirPath,
		byte[] buffer,
		List<(int DirIndex, HashSet<int> SeenChildren)> scannedFolders,
		ref int dirsRescanned,
		ref int itemsAdded,
		ref int itemsUpdated,
		CancellationToken cancellationToken)
	{
		var seen = ScanDirectoryImmediate(
			index,
			dirIdx,
			dirPath,
			buffer,
			ref dirsRescanned,
			ref itemsAdded,
			ref itemsUpdated,
			out List<(int SubDirIdx, string SubDirPath)> newSubDirs,
			onlyReturnNewlyCreatedSubDirs: true);

		scannedFolders.Add((dirIdx, seen));

		// Qualquer subpasta recém-criada precisa ter sua subárvore inteira varrida
		var queue = new Queue<(int DirIdx, string Path)>(newSubDirs);
		while (queue.Count > 0 && !cancellationToken.IsCancellationRequested)
		{
			var (subIdx, subPath) = queue.Dequeue();
			var subSeen = ScanDirectoryImmediate(
				index,
				subIdx,
				subPath,
				buffer,
				ref dirsRescanned,
				ref itemsAdded,
				ref itemsUpdated,
				out List<(int SubDirIdx, string SubDirPath)> deeperSubDirs,
				onlyReturnNewlyCreatedSubDirs: false);

			scannedFolders.Add((subIdx, subSeen));
			foreach (var d in deeperSubDirs)
				queue.Enqueue(d);
		}
	}

	private static unsafe HashSet<int> ScanDirectoryImmediate(
		VolumeIndex index,
		int parentIndex,
		string dirPath,
		byte[] buffer,
		ref int dirsRescanned,
		ref int itemsAdded,
		ref int itemsUpdated,
		out List<(int SubDirIdx, string SubDirPath)> discoveredSubDirs,
		bool onlyReturnNewlyCreatedSubDirs = false)
	{
		var seen = new HashSet<int>();
		discoveredSubDirs = [];

		using SafeFileHandle hDir = NativeMethods.CreateFileW(
			dirPath,
			NativeMethods.FILE_LIST_DIRECTORY,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
			IntPtr.Zero,
			NativeMethods.OPEN_EXISTING,
			NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
			IntPtr.Zero);

		if (hDir.IsInvalid)
			return seen;

		dirsRescanned++;
		bool restartScan = true;

		fixed (byte* pBuf = buffer)
		{
			while (true)
			{
				int infoClass = restartScan
					? NativeMethods.FileIdBothDirectoryRestartInfo
					: NativeMethods.FileIdBothDirectoryInfo;
				restartScan = false;

				if (!NativeMethods.GetFileInformationByHandleEx(hDir, infoClass, pBuf, (uint)buffer.Length))
					break;

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
								index.UpdateEntry(existingIdx, parentIndex, name, info->EndOfFile, creationTime, lastWriteTime, attrs);
								seen.Add(existingIdx);
								itemsUpdated++;

								if (isDir && !isReparse && !onlyReturnNewlyCreatedSubDirs)
								{
									discoveredSubDirs.Add((existingIdx, Path.Combine(dirPath, name)));
								}
							}
							else
							{
								int newIdx = index.Add(fileId, parentIndex, name, info->EndOfFile, creationTime, lastWriteTime, attrs);
								seen.Add(newIdx);
								itemsAdded++;

								if (isDir && !isReparse)
								{
									discoveredSubDirs.Add((newIdx, Path.Combine(dirPath, name)));
								}
							}
						}
					}

					if (info->NextEntryOffset == 0)
						break;

					offset += info->NextEntryOffset;
				}
			}
		}

		return seen;
	}

	private static bool TryGetDirectoryLastWriteTimeUtc(string path, out DateTimeOffset lastWriteTimeUtc)
	{
		try
		{
			if (!Directory.Exists(path))
			{
				lastWriteTimeUtc = default;
				return false;
			}

			DateTime dt = Directory.GetLastWriteTimeUtc(path);
			lastWriteTimeUtc = new DateTimeOffset(dt, TimeSpan.Zero);
			return true;
		}
		catch
		{
			lastWriteTimeUtc = default;
			return false;
		}
	}

	private static bool IsDotOrDotDot(ReadOnlySpan<char> name)
		=> (name.Length == 1 && name[0] == '.') || (name.Length == 2 && name[0] == '.' && name[1] == '.');

	private static DateTimeOffset SafeFromFileTime(long fileTime)
	{
		if (fileTime <= 0)
			return DateTimeOffset.MinValue;

		try
		{
			return DateTimeOffset.FromFileTime(fileTime);
		}
		catch
		{
			return DateTimeOffset.MinValue;
		}
	}
}
