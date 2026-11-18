// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace KyraFiles.Shared.SavedSearches
{
	public sealed class SavedSearchStore
	{
		private readonly object _syncRoot = new();
		private readonly string? _filePath;
		private readonly List<SavedSearchDefinition> _items = [];

		public SavedSearchStore(string? filePath = null)
		{
			_filePath = filePath;
		}

		public event EventHandler? Changed;

		public string? LastLoadError { get; private set; }

		public IReadOnlyList<SavedSearchDefinition> GetAll()
		{
			lock (_syncRoot)
				return _items.OrderBy(i => i.Order).ToList();
		}

		public SavedSearchDefinition? GetById(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;

			lock (_syncRoot)
				return _items.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.Ordinal));
		}

		public void Load()
		{
			if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
			{
				LastLoadError = null;
				return;
			}

			try
			{
				var json = File.ReadAllText(_filePath);
				var model = JsonSerializer.Deserialize(json, SavedSearchesJsonContext.Default.SavedSearchesFileModel);
				var loaded = model?.Items ?? [];

				lock (_syncRoot)
				{
					_items.Clear();
					foreach (var item in loaded.OrderBy(i => i.Order))
					{
						if (item is null || string.IsNullOrWhiteSpace(item.Name))
							continue;

						if (string.IsNullOrWhiteSpace(item.Id))
							item.Id = Guid.NewGuid().ToString("N");

						item.Extensions ??= [];
						_items.Add(item);
					}

					ReindexOrderLocked();
				}

				LastLoadError = null;
			}
			catch (Exception ex)
			{
				LastLoadError = ex.Message;
				lock (_syncRoot)
					_items.Clear();
			}

			OnChanged();
		}

		public SavedSearchDefinition Add(SavedSearchDefinition item)
		{
			ArgumentNullException.ThrowIfNull(item);
			if (string.IsNullOrWhiteSpace(item.Name))
				throw new ArgumentException("Nome da busca salva e obrigatorio.", nameof(item));

			lock (_syncRoot)
			{
				if (string.IsNullOrWhiteSpace(item.Id))
					item.Id = Guid.NewGuid().ToString("N");

				item.Name = EnsureUniqueNameLocked(item.Name.Trim(), excludeId: null);
				item.Extensions ??= [];
				item.Order = _items.Count;
				_items.Add(item);
			}

			Save();
			OnChanged();
			return item;
		}

		public bool Update(SavedSearchDefinition updated)
		{
			ArgumentNullException.ThrowIfNull(updated);
			if (string.IsNullOrWhiteSpace(updated.Name))
				throw new ArgumentException("Nome da busca salva e obrigatorio.", nameof(updated));

			bool found = false;
			lock (_syncRoot)
			{
				var idx = _items.FindIndex(i => string.Equals(i.Id, updated.Id, StringComparison.Ordinal));
				if (idx >= 0)
				{
					updated.Name = EnsureUniqueNameLocked(updated.Name.Trim(), excludeId: updated.Id);
					updated.Order = _items[idx].Order;
					updated.Extensions ??= [];
					_items[idx] = updated;
					found = true;
				}
			}

			if (found)
			{
				Save();
				OnChanged();
			}

			return found;
		}

		public bool Remove(string id)
		{
			bool removed;
			lock (_syncRoot)
			{
				removed = _items.RemoveAll(i => string.Equals(i.Id, id, StringComparison.Ordinal)) > 0;
				if (removed)
					ReindexOrderLocked();
			}

			if (removed)
			{
				Save();
				OnChanged();
			}

			return removed;
		}

		public bool Reorder(int oldIndex, int newIndex)
		{
			lock (_syncRoot)
			{
				if (oldIndex < 0 || oldIndex >= _items.Count || newIndex < 0 || newIndex >= _items.Count || oldIndex == newIndex)
					return false;

				var item = _items[oldIndex];
				_items.RemoveAt(oldIndex);
				_items.Insert(newIndex, item);
				ReindexOrderLocked();
			}

			Save();
			OnChanged();
			return true;
		}

		public void Save()
		{
			if (string.IsNullOrWhiteSpace(_filePath))
				return;

			SavedSearchesFileModel model;
			lock (_syncRoot)
			{
				model = new SavedSearchesFileModel
				{
					SchemaVersion = 1,
					Items = _items.OrderBy(i => i.Order).ToList(),
				};
			}

			var dir = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);

			var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
			var json = JsonSerializer.Serialize(model, SavedSearchesJsonContext.Default.SavedSearchesFileModel);
			File.WriteAllText(tempPath, json);
			File.Move(tempPath, _filePath, overwrite: true);
		}

		private string EnsureUniqueNameLocked(string baseName, string? excludeId)
		{
			bool Exists(string candidate) => _items.Any(i =>
				!string.Equals(i.Id, excludeId, StringComparison.Ordinal) &&
				string.Equals(i.Name, candidate, StringComparison.OrdinalIgnoreCase));

			if (!Exists(baseName))
				return baseName;

			int suffix = 2;
			while (Exists($"{baseName} ({suffix})"))
				suffix++;

			return $"{baseName} ({suffix})";
		}

		private void ReindexOrderLocked()
		{
			for (int i = 0; i < _items.Count; i++)
				_items[i].Order = i;
		}

		private void OnChanged()
			=> Changed?.Invoke(this, EventArgs.Empty);
	}
}
