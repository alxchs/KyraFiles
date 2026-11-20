// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.Shared.OperationHistory
{
	public sealed class OperationHistoryStore
	{
		public const int DefaultMaxEntries = 200;
		public const int DefaultMaxPathsPerEntry = 50;

		private readonly object _syncRoot = new();
		private readonly List<OperationHistoryEntry> _entries = [];
		private readonly string? _storageFilePath;
		private readonly SemaphoreSlim _saveLock = new(1, 1);

		private int _maxEntries;
		private int _maxPathsPerEntry;
		private bool _persistenceEnabled;
		private Task _pendingSaveTask = Task.CompletedTask;

		public OperationHistoryStore(
			string? storageFilePath = null,
			int maxEntries = DefaultMaxEntries,
			int maxPathsPerEntry = DefaultMaxPathsPerEntry,
			bool persistenceEnabled = true)
		{
			_storageFilePath = storageFilePath;
			_maxEntries = Math.Max(1, maxEntries);
			_maxPathsPerEntry = Math.Max(1, maxPathsPerEntry);
			_persistenceEnabled = persistenceEnabled;
		}

		public event EventHandler? Changed;

		public string? LastLoadError { get; private set; }

		public string? LastSaveError { get; private set; }

		public int MaxEntries
		{
			get
			{
				lock (_syncRoot)
					return _maxEntries;
			}
			set
			{
				var clamped = Math.Max(1, value);
				bool trimmed;
				lock (_syncRoot)
				{
					_maxEntries = clamped;
					trimmed = TrimLocked();
				}

				if (trimmed)
				{
					ScheduleSave();
					OnChanged();
				}
			}
		}

		public bool PersistenceEnabled
		{
			get
			{
				lock (_syncRoot)
					return _persistenceEnabled;
			}
			set
			{
				lock (_syncRoot)
					_persistenceEnabled = value;
			}
		}

		public int Count
		{
			get
			{
				lock (_syncRoot)
					return _entries.Count;
			}
		}

		public void Load()
		{
			if (string.IsNullOrWhiteSpace(_storageFilePath))
				return;

			LastLoadError = null;

			if (!File.Exists(_storageFilePath))
			{
				lock (_syncRoot)
					_entries.Clear();
				return;
			}

			try
			{
				var json = File.ReadAllText(_storageFilePath);
				var model = JsonSerializer.Deserialize(json, OperationHistoryJsonContext.Default.OperationHistoryFileModel);
				var loaded = model?.Entries ?? [];

				lock (_syncRoot)
				{
					_entries.Clear();
					foreach (var entry in loaded)
					{
						if (entry is null)
							continue;

						NormalizeEntryLocked(entry);
						_entries.Add(entry);
					}

					_entries.Sort((a, b) => b.TimestampUtc.CompareTo(a.TimestampUtc));
					TrimLocked();
				}
			}
			catch (Exception ex)
			{
				LastLoadError = ex.Message;
				lock (_syncRoot)
					_entries.Clear();
			}

			OnChanged();
		}

		public OperationHistoryEntry Add(OperationHistoryEntry entry)
		{
			ArgumentNullException.ThrowIfNull(entry);

			lock (_syncRoot)
			{
				NormalizeEntryLocked(entry);
				_entries.Insert(0, entry);
				TrimLocked();
			}

			ScheduleSave();
			OnChanged();
			return entry;
		}

		public IReadOnlyList<OperationHistoryEntry> GetSnapshot()
		{
			lock (_syncRoot)
				return [.. _entries];
		}

		public IReadOnlyList<OperationHistoryEntry> Filter(OperationHistoryKind? kind = null, string? searchText = null)
		{
			var query = searchText?.Trim();
			var hasQuery = !string.IsNullOrEmpty(query);

			List<OperationHistoryEntry> snapshot;
			lock (_syncRoot)
				snapshot = [.. _entries];

			if (kind is null && !hasQuery)
				return snapshot;

			var filtered = new List<OperationHistoryEntry>(snapshot.Count);
			foreach (var entry in snapshot)
			{
				if (kind is not null && entry.Kind != kind.Value)
					continue;

				if (hasQuery && !MatchesQuery(entry, query!))
					continue;

				filtered.Add(entry);
			}

			return filtered;
		}

		public bool MarkUndone(string entryId)
		{
			if (string.IsNullOrWhiteSpace(entryId))
				return false;

			bool updated = false;
			lock (_syncRoot)
			{
				foreach (var entry in _entries)
				{
					if (string.Equals(entry.Id, entryId, StringComparison.Ordinal))
					{
						entry.IsUndone = true;
						entry.UndoneAtUtc = DateTimeOffset.UtcNow;
						updated = true;
						break;
					}
				}
			}

			if (updated)
			{
				ScheduleSave();
				OnChanged();
			}

			return updated;
		}

		public void Clear()
		{
			lock (_syncRoot)
				_entries.Clear();

			ScheduleSave();
			OnChanged();
		}

		public Task FlushAsync()
		{
			lock (_syncRoot)
				return _pendingSaveTask;
		}

		private void NormalizeEntryLocked(OperationHistoryEntry entry)
		{
			if (string.IsNullOrWhiteSpace(entry.Id))
				entry.Id = Guid.NewGuid().ToString("N");

			if (entry.TimestampUtc == default)
				entry.TimestampUtc = DateTimeOffset.UtcNow;

			entry.Sources ??= [];
			entry.Destinations ??= [];

			var maxCount = Math.Max(entry.Sources.Count, entry.Destinations.Count);
			if (entry.TotalItemCount < maxCount)
				entry.TotalItemCount = maxCount;

			if (entry.Sources.Count > _maxPathsPerEntry)
				entry.Sources = entry.Sources.GetRange(0, _maxPathsPerEntry);

			if (entry.Destinations.Count > _maxPathsPerEntry)
				entry.Destinations = entry.Destinations.GetRange(0, _maxPathsPerEntry);

			if (entry.Kind == OperationHistoryKind.DeletePermanent)
			{
				entry.Reversible = false;
				entry.IrreversibleReason ??= "Exclusao definitiva nao pode ser desfeita.";
			}
		}

		private bool TrimLocked()
		{
			if (_entries.Count <= _maxEntries)
				return false;

			_entries.RemoveRange(_maxEntries, _entries.Count - _maxEntries);
			return true;
		}

		private static bool MatchesQuery(OperationHistoryEntry entry, string query)
		{
			foreach (var s in entry.Sources)
			{
				if (s.Path.Contains(query, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			foreach (var d in entry.Destinations)
			{
				if (d.Path.Contains(query, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			if (!string.IsNullOrEmpty(entry.IrreversibleReason) &&
				entry.IrreversibleReason.Contains(query, StringComparison.OrdinalIgnoreCase))
				return true;

			return false;
		}

		private void ScheduleSave()
		{
			if (string.IsNullOrWhiteSpace(_storageFilePath))
				return;

			bool enabled;
			lock (_syncRoot)
				enabled = _persistenceEnabled;

			if (!enabled)
				return;

			var task = SaveInternalAsync(_storageFilePath);
			lock (_syncRoot)
				_pendingSaveTask = task;
		}

		private async Task SaveInternalAsync(string targetPath)
		{
			await _saveLock.WaitAsync().ConfigureAwait(false);
			try
			{
				OperationHistoryFileModel model;
				lock (_syncRoot)
				{
					if (!_persistenceEnabled)
						return;

					model = new OperationHistoryFileModel
					{
						SchemaVersion = 1,
						Entries = [.. _entries],
					};
				}

				var directory = Path.GetDirectoryName(targetPath);
				if (!string.IsNullOrEmpty(directory))
					Directory.CreateDirectory(directory);

				var tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
				var json = JsonSerializer.Serialize(model, OperationHistoryJsonContext.Default.OperationHistoryFileModel);
				await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
				File.Move(tempPath, targetPath, overwrite: true);
				LastSaveError = null;
			}
			catch (Exception ex)
			{
				LastSaveError = ex.Message;
			}
			finally
			{
				_saveLock.Release();
			}
		}

		private void OnChanged()
			=> Changed?.Invoke(this, EventArgs.Empty);
	}
}
