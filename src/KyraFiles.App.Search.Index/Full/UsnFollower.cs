// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using KyraFiles.App.Search.Index.Interop;
using KyraFiles.App.Search.Index.Storage;
using Microsoft.Win32.SafeHandles;

namespace KyraFiles.App.Search.Index.Full;

/// <summary>
/// Estado da validação de retomada do diário USN ao iniciar o serviço/motor (F015 T14 — FR-031, AC-11).
/// </summary>
public enum UsnResumeStatus
{
	CanResume = 0,
	JournalRecreated = 1,
	JournalTruncated = 2,
	InvalidSavedUsn = 3,
	JournalUnavailable = 4
}

/// <summary>
/// Resultado detalhado da decisão de retomada ou reindexação pelo diário USN.
/// </summary>
public readonly record struct UsnResumeDecision(
	UsnResumeStatus Status,
	bool RequiresFullReindex,
	ulong CurrentJournalId,
	long ResumeStartUsn,
	string StatusReason);

/// <summary>
/// Seguidor do diário de alterações NTFS (<c>USN Journal</c>) para o modo completo (F015 T14 — FR-030, FR-031, AC-10, AC-11).
/// Acompanha criações, exclusões, renomeações, movimentações e alterações de tamanho/data em tempo real (≤ 2 s),
/// persiste <c>(UsnJournalId, LastUsn)</c> no snapshot e decide na partida se pode retomar incrementalmente
/// ou se o volume precisa ser reindexado por recriação/truncamento do diário, expondo o motivo legível no estado.
/// </summary>
public sealed class UsnFollower : IDisposable
{
	private const int ReadBufferSize = 64 * 1024;
	private const ulong RecordNumberMask = 0x0000FFFFFFFFFFFFUL;

	public const uint WatchReasonMask =
		NativeMethods.USN_REASON_FILE_CREATE |
		NativeMethods.USN_REASON_FILE_DELETE |
		NativeMethods.USN_REASON_RENAME_OLD_NAME |
		NativeMethods.USN_REASON_RENAME_NEW_NAME |
		NativeMethods.USN_REASON_DATA_EXTEND |
		NativeMethods.USN_REASON_DATA_TRUNCATION |
		NativeMethods.USN_REASON_DATA_OVERWRITE |
		NativeMethods.USN_REASON_BASIC_INFO_CHANGE |
		NativeMethods.USN_REASON_CLOSE;

	private readonly VolumeIndex _index;
	private readonly string _volumeDevicePath;
	private readonly Action<ulong, long, string>? _onCheckpoint;
	private readonly Action<string>? _onReindexRequired;
	private readonly CancellationTokenSource _cts = new();
	private Task? _loopTask;
	private bool _disposed;

	public ulong UsnJournalId { get; private set; }
	public long LastUsn { get; private set; }
	public string StatusReason { get; private set; } = string.Empty;
	public bool RequiresFullReindex { get; private set; }

	public UsnFollower(
		VolumeIndex index,
		ulong initialJournalId,
		long initialUsn,
		Action<ulong, long, string>? onCheckpoint = null,
		Action<string>? onReindexRequired = null)
	{
		ArgumentNullException.ThrowIfNull(index);
		_index = index;
		UsnJournalId = initialJournalId;
		LastUsn = initialUsn;
		_onCheckpoint = onCheckpoint;
		_onReindexRequired = onReindexRequired;

		string trimRoot = index.VolumeRoot.TrimEnd('\\');
		_volumeDevicePath = trimRoot.StartsWith(@"\\.\", StringComparison.Ordinal)
			? trimRoot
			: @"\\.\" + trimRoot;
	}

	/// <summary>
	/// Avalia se o estado salvo no snapshot (<paramref name="savedJournalId"/>, <paramref name="savedLastUsn"/>)
	/// pode ser retomado incrementalmente frente aos metadados atuais do diário USN do volume (FR-031, AC-11).
	/// </summary>
	internal static UsnResumeDecision ValidateResumeState(
		ulong savedJournalId,
		long savedLastUsn,
		NativeMethods.USN_JOURNAL_DATA_V0 currentJournal)
	{
		if (currentJournal.UsnJournalID == 0)
		{
			return new UsnResumeDecision(
				UsnResumeStatus.JournalUnavailable,
				RequiresFullReindex: true,
				CurrentJournalId: 0,
				ResumeStartUsn: 0,
				StatusReason: "Reindexado porque o diário USN não está ativo no volume.");
		}

		if (savedJournalId == 0 || savedJournalId != currentJournal.UsnJournalID)
		{
			return new UsnResumeDecision(
				UsnResumeStatus.JournalRecreated,
				RequiresFullReindex: true,
				CurrentJournalId: currentJournal.UsnJournalID,
				ResumeStartUsn: currentJournal.NextUsn,
				StatusReason: $"Reindexado porque o diário USN do volume foi recriado (salvo=0x{savedJournalId:X16}, atual=0x{currentJournal.UsnJournalID:X16}).");
		}

		long minValidUsn = Math.Max(currentJournal.FirstUsn, currentJournal.LowestValidUsn);
		if (savedLastUsn < minValidUsn)
		{
			return new UsnResumeDecision(
				UsnResumeStatus.JournalTruncated,
				RequiresFullReindex: true,
				CurrentJournalId: currentJournal.UsnJournalID,
				ResumeStartUsn: currentJournal.NextUsn,
				StatusReason: $"Reindexado porque o diário USN foi truncado além do último ponto salvo (USN salvo={savedLastUsn}, mínimo disponível={minValidUsn}).");
		}

		if (savedLastUsn > currentJournal.NextUsn)
		{
			return new UsnResumeDecision(
				UsnResumeStatus.InvalidSavedUsn,
				RequiresFullReindex: true,
				CurrentJournalId: currentJournal.UsnJournalID,
				ResumeStartUsn: currentJournal.NextUsn,
				StatusReason: $"Reindexado porque o USN salvo ({savedLastUsn}) é maior que o próximo USN do volume ({currentJournal.NextUsn}).");
		}

		return new UsnResumeDecision(
			UsnResumeStatus.CanResume,
			RequiresFullReindex: false,
			CurrentJournalId: currentJournal.UsnJournalID,
			ResumeStartUsn: savedLastUsn,
			StatusReason: $"Retomado do USN {savedLastUsn} (diário 0x{currentJournal.UsnJournalID:X16}).");
	}

	/// <summary>
	/// Consulta os dados atuais do diário USN de um volume NTFS (<c>FSCTL_QUERY_USN_JOURNAL</c>).
	/// </summary>
	internal static unsafe bool TryQueryJournal(string volumeRoot, out NativeMethods.USN_JOURNAL_DATA_V0 journalData, out int win32Error)
	{
		journalData = default;
		win32Error = 0;

		if (string.IsNullOrWhiteSpace(volumeRoot))
			return false;

		string trimRoot = volumeRoot.TrimEnd('\\');
		string devicePath = trimRoot.StartsWith(@"\\.\", StringComparison.Ordinal)
			? trimRoot
			: @"\\.\" + trimRoot;

		using SafeFileHandle hVol = NativeMethods.CreateFileW(
			devicePath,
			NativeMethods.GENERIC_READ,
			NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
			IntPtr.Zero,
			NativeMethods.OPEN_EXISTING,
			NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
			IntPtr.Zero);

		if (hVol.IsInvalid)
		{
			win32Error = Marshal.GetLastWin32Error();
			return false;
		}

		fixed (NativeMethods.USN_JOURNAL_DATA_V0* pOut = &journalData)
		{
			bool ok = NativeMethods.DeviceIoControl(
				hVol,
				NativeMethods.FSCTL_QUERY_USN_JOURNAL,
				null,
				0,
				pOut,
				(uint)sizeof(NativeMethods.USN_JOURNAL_DATA_V0),
				out _,
				IntPtr.Zero);

			if (!ok)
			{
				win32Error = Marshal.GetLastWin32Error();
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Processa um buffer bruto retornado por <c>FSCTL_READ_USN_JOURNAL</c> (8 bytes iniciais de <c>NextStartUsn</c>
	/// seguidos de registros <c>USN_RECORD_V2</c>) e aplica as alterações no <see cref="VolumeIndex"/>.
	/// Retorna o número de registros USN aplicados e preenche <paramref name="nextUsn"/>.
	/// </summary>
	public static int ApplyUsnBuffer(
		VolumeIndex index,
		ReadOnlySpan<byte> buffer,
		out long nextUsn,
		Func<string, (bool Exists, long Size, DateTimeOffset CreationUtc, DateTimeOffset LastWriteUtc, uint Attributes)>? statProvider = null)
	{
		ArgumentNullException.ThrowIfNull(index);

		if (buffer.Length < 8)
		{
			nextUsn = 0;
			return 0;
		}

		nextUsn = BinaryPrimitives.ReadInt64LittleEndian(buffer[0..8]);
		int offset = 8;
		int appliedCount = 0;

		lock (index.SyncRoot)
		{
			while (offset + 60 <= buffer.Length)
			{
				uint recordLen = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));
				if (recordLen < 60 || offset + (int)recordLen > buffer.Length)
					break;

				ReadOnlySpan<byte> rec = buffer.Slice(offset, (int)recordLen);
				ushort majorVersion = BinaryPrimitives.ReadUInt16LittleEndian(rec[4..6]);
				if (majorVersion == 2)
				{
					ulong fileRef = BinaryPrimitives.ReadUInt64LittleEndian(rec[8..16]);
					ulong parentRef = BinaryPrimitives.ReadUInt64LittleEndian(rec[16..24]);
					long usn = BinaryPrimitives.ReadInt64LittleEndian(rec[24..32]);
					long timestampFt = BinaryPrimitives.ReadInt64LittleEndian(rec[32..40]);
					uint reason = BinaryPrimitives.ReadUInt32LittleEndian(rec[40..44]);
					uint attributes = BinaryPrimitives.ReadUInt32LittleEndian(rec[52..56]);
					ushort nameByteLen = BinaryPrimitives.ReadUInt16LittleEndian(rec[56..58]);
					ushort nameOffset = BinaryPrimitives.ReadUInt16LittleEndian(rec[58..60]);

					if (nameByteLen > 0 && nameOffset >= 60 && nameOffset + nameByteLen <= rec.Length)
					{
						string name = Encoding.Unicode.GetString(rec.Slice(nameOffset, nameByteLen));
						if (ApplySingleUsnRecord(index, fileRef, parentRef, timestampFt, reason, attributes, name, statProvider))
						{
							appliedCount++;
						}
					}

					if (usn > nextUsn)
						nextUsn = usn;
				}

				offset += (int)recordLen;
			}
		}

		return appliedCount;
	}

	private static bool ApplySingleUsnRecord(
		VolumeIndex index,
		ulong fileRef,
		ulong parentRef,
		long timestampFileTime,
		uint reason,
		uint attributes,
		string name,
		Func<string, (bool Exists, long Size, DateTimeOffset CreationUtc, DateTimeOffset LastWriteUtc, uint Attributes)>? statProvider)
	{
		if (string.IsNullOrEmpty(name) || name is "." or "..")
			return false;

		ulong fileRecordNum = fileRef & RecordNumberMask;
		ulong parentRecordNum = parentRef & RecordNumberMask;

		if (fileRecordNum < 16 && fileRecordNum != MftReader.RootDirectoryRecordNumber)
			return false;

		if (parentRecordNum == MftReader.RootDirectoryRecordNumber &&
			name.StartsWith('$') &&
			(attributes & 0x00000004 /*FILE_ATTRIBUTE_SYSTEM*/) != 0)
		{
			return false;
		}

		if ((reason & NativeMethods.USN_REASON_FILE_DELETE) != 0)
		{
			int existingIdx = FindEntryByFileRefOrRecordNumber(index, fileRef);
			if (existingIdx >= 0)
			{
				index.MarkDeleted(existingIdx);
				return true;
			}
			return false;
		}

		// Ignora RENAME_OLD_NAME isolado: a atualização de pai/nome acontece em RENAME_NEW_NAME
		if (reason == NativeMethods.USN_REASON_RENAME_OLD_NAME)
			return false;

		int parentIdx;
		if (parentRecordNum == MftReader.RootDirectoryRecordNumber)
		{
			parentIdx = VolumeIndex.RootParentIndex;
		}
		else
		{
			parentIdx = FindEntryByFileRefOrRecordNumber(index, parentRef);
			if (parentIdx < 0 || index.IsDeleted(parentIdx))
				return false;

			// FR-012: Nunca desce em diretórios que são pontos de reanálise
			if ((index.GetFileAttributes(parentIdx) & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
				return false;
		}

		bool isDir = (attributes & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0;
		DateTimeOffset eventTime = SafeFileTimeToUtc(timestampFileTime);

		int idx = FindEntryByFileRefOrRecordNumber(index, fileRef);
		long size = idx >= 0 ? index.GetFileSize(idx) : 0L;
		DateTimeOffset creationUtc = idx >= 0 ? index.GetCreationTime(idx) : eventTime;
		DateTimeOffset lastWriteUtc = eventTime;

		// Se o arquivo existe no disco (ou via statProvider de teste), obtém tamanho e datas exatos
		string candidateFullPath = BuildCandidatePath(index, parentIdx, name);
		if (!isDir)
		{
			if (statProvider is not null)
			{
				var st = statProvider(candidateFullPath);
				if (st.Exists)
				{
					size = st.Size;
					creationUtc = st.CreationUtc;
					lastWriteUtc = st.LastWriteUtc;
					attributes = st.Attributes;
				}
			}
			else if (NativeMethods.GetFileAttributesExW(candidateFullPath, 0, out var attrData))
			{
				size = ((long)attrData.nFileSizeHigh << 32) | attrData.nFileSizeLow;
				long cFt = ((long)attrData.ftCreationTimeHigh << 32) | attrData.ftCreationTimeLow;
				long wFt = ((long)attrData.ftLastWriteTimeHigh << 32) | attrData.ftLastWriteTimeLow;
				if (cFt > 0) creationUtc = SafeFileTimeToUtc(cFt);
				if (wFt > 0) lastWriteUtc = SafeFileTimeToUtc(wFt);
				attributes = attrData.dwFileAttributes;
			}
		}

		if (idx >= 0)
		{
			index.UpdateEntry(idx, parentIdx, name, isDir ? 0L : size, creationUtc, lastWriteUtc, attributes);
		}
		else
		{
			index.Add(fileRef, parentIdx, name, isDir ? 0L : size, creationUtc, lastWriteUtc, attributes);
		}

		return true;
	}

	private static int FindEntryByFileRefOrRecordNumber(VolumeIndex index, ulong fileRef)
	{
		int exact = index.FindByFileReference(fileRef);
		if (exact >= 0)
			return exact;

		ulong targetRec = fileRef & RecordNumberMask;
		if (targetRec == 0)
			return -1;

		for (int i = 0; i < index.Count; i++)
		{
			if (!index.IsDeleted(i) && (index.GetFileReference(i) & RecordNumberMask) == targetRec)
				return i;
		}

		return -1;
	}

	private static string BuildCandidatePath(VolumeIndex index, int parentIdx, string name)
	{
		if (parentIdx == VolumeIndex.RootParentIndex)
			return index.VolumeRoot + name;

		string parentPath = index.GetFullPath(parentIdx);
		return parentPath.EndsWith('\\') ? parentPath + name : parentPath + "\\" + name;
	}

	/// <summary>
	/// Inicia o acompanhamento contínuo do diário USN em segundo plano (≤ 2 s de latência).
	/// </summary>
	public void Start()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_loopTask is not null)
			return;

		_loopTask = Task.Run(() => FollowLoopAsync(_cts.Token));
	}

	private async Task FollowLoopAsync(CancellationToken token)
	{
		byte[] buffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
		try
		{
			using SafeFileHandle hVol = NativeMethods.CreateFileW(
				_volumeDevicePath,
				NativeMethods.GENERIC_READ,
				NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
				IntPtr.Zero,
				NativeMethods.OPEN_EXISTING,
				NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
				IntPtr.Zero);

			if (hVol.IsInvalid)
			{
				StatusReason = $"Não foi possível abrir {_volumeDevicePath} para o diário USN (erro {Marshal.GetLastWin32Error()}).";
				return;
			}

			if (!TryQueryJournal(_index.VolumeRoot, out var journalData, out int queryErr))
			{
				StatusReason = $"Diário USN indisponível em {_index.VolumeRoot} (erro {queryErr}).";
				RequiresFullReindex = true;
				_onReindexRequired?.Invoke(StatusReason);
				return;
			}

			var decision = ValidateResumeState(UsnJournalId, LastUsn, journalData);
			StatusReason = decision.StatusReason;
			UsnJournalId = decision.CurrentJournalId;

			if (decision.RequiresFullReindex)
			{
				RequiresFullReindex = true;
				LastUsn = decision.ResumeStartUsn;
				_onReindexRequired?.Invoke(StatusReason);
				return;
			}

			LastUsn = decision.ResumeStartUsn;

			while (!token.IsCancellationRequested)
			{
				int err = ReadAndApplyNextBatch(hVol, buffer, out int applied, out uint bytesReturned);
				if (err == NativeMethods.ERROR_JOURNAL_ENTRY_DELETED ||
					err == NativeMethods.ERROR_JOURNAL_NOT_ACTIVE ||
					err == NativeMethods.ERROR_JOURNAL_DELETE_IN_PROGRESS)
				{
					RequiresFullReindex = true;
					StatusReason = $"Reindexado porque o diário USN reportou perda de histórico (erro Win32 {err}).";
					_onReindexRequired?.Invoke(StatusReason);
					break;
				}

				if (applied > 0)
				{
					StatusReason = $"Pronto (USN {LastUsn})";
					_onCheckpoint?.Invoke(UsnJournalId, LastUsn, StatusReason);
				}

				if (bytesReturned <= 8)
				{
					await Task.Delay(500, token).ConfigureAwait(false);
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	private unsafe int ReadAndApplyNextBatch(SafeFileHandle hVol, byte[] buffer, out int applied, out uint bytesReturned)
	{
		applied = 0;
		bytesReturned = 0;

		var readData = new NativeMethods.READ_USN_JOURNAL_DATA_V0
		{
			StartUsn = LastUsn,
			ReasonMask = WatchReasonMask,
			ReturnOnlyOnClose = 0,
			Timeout = 0,
			BytesToWaitFor = 0,
			UsnJournalID = UsnJournalId
		};

		bool ok;
		fixed (byte* pOut = buffer)
		{
			ok = NativeMethods.DeviceIoControl(
				hVol,
				NativeMethods.FSCTL_READ_USN_JOURNAL,
				&readData,
				(uint)sizeof(NativeMethods.READ_USN_JOURNAL_DATA_V0),
				pOut,
				(uint)buffer.Length,
				out bytesReturned,
				IntPtr.Zero);
		}

		if (!ok)
		{
			return Marshal.GetLastWin32Error();
		}

		if (bytesReturned >= 8)
		{
			applied = ApplyUsnBuffer(_index, buffer.AsSpan(0, (int)bytesReturned), out long nextUsn);
			if (nextUsn > LastUsn)
				LastUsn = nextUsn;
		}

		return 0;
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

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_cts.Cancel();
		try
		{
			_loopTask?.Wait(TimeSpan.FromSeconds(1));
		}
		catch
		{
		}
		_cts.Dispose();
	}
}
