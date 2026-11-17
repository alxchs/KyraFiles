// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Runtime.InteropServices;
using KyraFiles.App.Search.Index.Interop;
using KyraFiles.App.Search.Index.Storage;

namespace KyraFiles.App.Search.Index.Volumes;

/// <summary>
/// Monitora a chegada, remoção e estados de volumes (F015 T16 — FR-033, AC-13).
/// Detecta volumes removíveis, reaproveitamento de snapshots por número de série e ID de diário USN,
/// e volumes BitLocker bloqueados.
/// </summary>
public sealed class VolumeWatcher : IDisposable
{
	public const int ERROR_FVE_LOCKED_VOLUME = unchecked((int)0x80310000);
	public const int ERROR_NOT_READY = 21;
	public const int ERROR_DEVICE_NOT_CONNECTED = 1167;

	public enum VolumeStatus
	{
		Ready,
		Indexing,
		Reconciling,
		BitLockerLocked,
		Disconnected,
		Error
	}

	public sealed class VolumeStateChangeEventArgs : EventArgs
	{
		public VolumeInfo Volume { get; }
		public VolumeStatus Status { get; }
		public string Message { get; }

		public VolumeStateChangeEventArgs(VolumeInfo volume, VolumeStatus status, string message)
		{
			Volume = volume;
			Status = status;
			Message = message;
		}
	}

	public sealed class VolumeArrivalEventArgs : EventArgs
	{
		public VolumeInfo Volume { get; }
		public uint SerialNumber { get; }
		public bool SnapshotReused { get; }

		public VolumeArrivalEventArgs(VolumeInfo volume, uint serialNumber, bool snapshotReused)
		{
			Volume = volume;
			SerialNumber = serialNumber;
			SnapshotReused = snapshotReused;
		}
	}

	public sealed class VolumeRemovalEventArgs : EventArgs
	{
		public VolumeInfo Volume { get; }

		public VolumeRemovalEventArgs(VolumeInfo volume)
		{
			Volume = volume;
		}
	}

	private readonly VolumeTable _volumeTable;
	private readonly string _snapshotDirectory;
	private readonly Dictionary<Guid, (VolumeInfo Volume, VolumeStatus Status, uint SerialNumber)> _trackedVolumes = new();
	private readonly object _lock = new();
	private bool _disposed;

	public event EventHandler<VolumeStateChangeEventArgs>? VolumeStateChanged;
	public event EventHandler<VolumeArrivalEventArgs>? VolumeArrived;
	public event EventHandler<VolumeRemovalEventArgs>? VolumeRemoved;

	public IReadOnlyDictionary<Guid, (VolumeInfo Volume, VolumeStatus Status, uint SerialNumber)> TrackedVolumes
	{
		get
		{
			lock (_lock)
			{
				return new Dictionary<Guid, (VolumeInfo, VolumeStatus, uint)>(_trackedVolumes);
			}
		}
	}

	public VolumeWatcher(VolumeTable volumeTable, string snapshotDirectory)
	{
		ArgumentNullException.ThrowIfNull(volumeTable);
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotDirectory);

		_volumeTable = volumeTable;
		_snapshotDirectory = snapshotDirectory;

		// Estado inicial
		Poll();
	}

	/// <summary>
	/// Realiza uma varredura de sondagem dos volumes físicos e detecta chegadas, saídas ou mudanças de estado.
	/// </summary>
	public void Poll()
	{
		lock (_lock)
		{
			if (_disposed)
				return;

			_volumeTable.Refresh();
			var currentVolumes = _volumeTable.Volumes.ToDictionary(v => v.VolumeGuid);

			// 1. Detectar volumes removidos ou desconectados
			var removedGuids = new List<Guid>();
			foreach (var (guid, tracked) in _trackedVolumes)
			{
				if (!currentVolumes.TryGetValue(guid, out var current))
				{
					removedGuids.Add(guid);
					VolumeRemoved?.Invoke(this, new VolumeRemovalEventArgs(tracked.Volume));
				}
				else
				{
					// Volume ainda listado, verificar se está acessível ou bloqueado
					var (status, serial, msg) = ProbeVolume(current);
					if (status != tracked.Status)
					{
						_trackedVolumes[guid] = (current, status, serial);
						VolumeStateChanged?.Invoke(this, new VolumeStateChangeEventArgs(current, status, msg));
					}
				}
			}

			foreach (var guid in removedGuids)
			{
				_trackedVolumes.Remove(guid);
			}

			// 2. Detectar novos volumes que chegaram
			foreach (var (guid, current) in currentVolumes)
			{
				if (!_trackedVolumes.ContainsKey(guid))
				{
					var (status, serial, msg) = ProbeVolume(current);
					bool reused = false;

					if (status == VolumeStatus.Ready)
					{
						reused = TryFindReusableSnapshot(current, serial, out _);
					}

					_trackedVolumes[guid] = (current, status, serial);

					VolumeArrived?.Invoke(this, new VolumeArrivalEventArgs(current, serial, reused));
					VolumeStateChanged?.Invoke(this, new VolumeStateChangeEventArgs(current, status, msg));
				}
			}
		}
	}

	/// <summary>
	/// Avalia se determinado volume é acessível, está bloqueado por BitLocker ou desconectado.
	/// </summary>
	public static (VolumeStatus Status, uint SerialNumber, string Message) ProbeVolume(VolumeInfo volume)
	{
		ArgumentNullException.ThrowIfNull(volume);

		string root = volume.DisplayRoot.TrimEnd('\\') + '\\';

		Span<char> volName = stackalloc char[260];
		Span<char> fsName = stackalloc char[260];

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
			return (VolumeStatus.Ready, serialNumber, "Pronto");
		}

		int error = Marshal.GetLastWin32Error();
		if (error == ERROR_FVE_LOCKED_VOLUME)
		{
			return (VolumeStatus.BitLockerLocked, 0, "Bloqueado pelo BitLocker");
		}
		if (error == ERROR_NOT_READY || error == ERROR_DEVICE_NOT_CONNECTED)
		{
			return (VolumeStatus.Disconnected, 0, "Dispositivo desconectado ou não pronto");
		}

		return (VolumeStatus.Error, 0, $"Erro de acesso ao volume (0x{error:X8})");
	}

	/// <summary>
	/// Verifica se já existe um snapshot em disco (.fcsn) correspondente ao número de série do volume.
	/// </summary>
	public bool TryFindReusableSnapshot(VolumeInfo volume, uint serialNumber, out string? snapshotPath)
	{
		snapshotPath = null;
		if (serialNumber == 0 || !Directory.Exists(_snapshotDirectory))
			return false;

		// Busca arquivos .fcsn no diretório de snapshot
		string[] files = Directory.GetFiles(_snapshotDirectory, "*.fcsn");
		foreach (string file in files)
		{
			try
			{
				using var fs = File.OpenRead(file);
				using var reader = new BinaryReader(fs);

				if (fs.Length < 16)
					continue;

				uint magic = reader.ReadUInt32();
				if (magic != 0x4E534346) // FCSN
					continue;

				uint version = reader.ReadUInt32();
				if (version != 1)
					continue;

				uint fileSerial = reader.ReadUInt32();
				if (fileSerial == serialNumber)
				{
					snapshotPath = file;
					return true;
				}
			}
			catch
			{
				// Ignora snapshots com falha de leitura
			}
		}

		return false;
	}

	public void Dispose()
	{
		lock (_lock)
		{
			_disposed = true;
			_trackedVolumes.Clear();
		}
	}
}
