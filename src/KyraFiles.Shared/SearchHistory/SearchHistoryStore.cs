// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.SearchHistory
{
	/// <summary>
	/// One executed search with what is needed to repeat it exactly (F094 FR-013).
	/// </summary>
	public sealed class SearchHistoryEntry
	{
		public string Query { get; set; } = string.Empty;

		/// <summary>Folder the search ran in; empty means the whole index.</summary>
		public string ScopePath { get; set; } = string.Empty;

		/// <summary>Search provider id (e.g. "everything", "native"); null when unknown (imported entries).</summary>
		public string? EngineId { get; set; }

		public DateTimeOffset LastRunAt { get; set; }

		public bool IsSameSearch(SearchHistoryEntry other)
			=> string.Equals(Query.Trim(), other.Query.Trim(), StringComparison.OrdinalIgnoreCase)
			&& string.Equals(NormalizeScope(ScopePath), NormalizeScope(other.ScopePath), StringComparison.OrdinalIgnoreCase);

		internal static string NormalizeScope(string? path)
		{
			var p = (path ?? string.Empty).Trim().Replace('/', '\\');
			return p.Length > 3 ? p.TrimEnd('\\') : p;
		}
	}

	public sealed class SearchHistoryFileModel
	{
		public int SchemaVersion { get; set; } = 1;

		public List<SearchHistoryEntry> Items { get; set; } = [];
	}

	[JsonSerializable(typeof(SearchHistoryFileModel))]
	public sealed partial class SearchHistoryJsonContext : JsonSerializerContext
	{
	}

	/// <summary>
	/// Most-recent-first search history persisted as JSON; holds at most <see cref="MaxEntries"/> distinct searches.
	/// </summary>
	public sealed class SearchHistoryStore
	{
		public const int MaxEntries = 20;

		private readonly object _syncRoot = new();
		private readonly string? _filePath;
		private readonly List<SearchHistoryEntry> _items = [];

		public SearchHistoryStore(string? filePath = null)
		{
			_filePath = filePath;
		}

		public event EventHandler? Changed;

		/// <summary>True when the history file did not exist at load time (used to import legacy entries once).</summary>
		public bool IsNew { get; private set; } = true;

		public IReadOnlyList<SearchHistoryEntry> GetAll()
		{
			lock (_syncRoot)
				return _items.ToList();
		}

		public void Load()
		{
			IsNew = string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath);
			if (IsNew)
				return;

			try
			{
				var model = JsonSerializer.Deserialize(File.ReadAllText(_filePath!), SearchHistoryJsonContext.Default.SearchHistoryFileModel);
				lock (_syncRoot)
				{
					_items.Clear();
					foreach (var item in model?.Items ?? [])
					{
						if (item is null || string.IsNullOrWhiteSpace(item.Query) || _items.Any(x => x.IsSameSearch(item)))
							continue;

						item.ScopePath ??= string.Empty;
						_items.Add(item);
						if (_items.Count == MaxEntries)
							break;
					}
				}
			}
			catch
			{
				lock (_syncRoot)
					_items.Clear();
			}

			OnChanged();
		}

		/// <summary>Adds the search at the top, replacing an identical older one.</summary>
		public bool Record(SearchHistoryEntry entry)
		{
			ArgumentNullException.ThrowIfNull(entry);
			if (string.IsNullOrWhiteSpace(entry.Query))
				return false;

			entry.Query = entry.Query.Trim();
			entry.ScopePath = SearchHistoryEntry.NormalizeScope(entry.ScopePath);

			lock (_syncRoot)
			{
				_items.RemoveAll(x => x.IsSameSearch(entry));
				_items.Insert(0, entry);
				if (_items.Count > MaxEntries)
					_items.RemoveRange(MaxEntries, _items.Count - MaxEntries);
			}

			SaveAndNotify();
			return true;
		}

		/// <summary>Imports plain legacy queries (oldest last) without overwriting existing entries.</summary>
		public void ImportLegacy(IEnumerable<string>? queries, DateTimeOffset now)
		{
			if (queries is null)
				return;

			bool changed = false;
			lock (_syncRoot)
			{
				foreach (var q in queries)
				{
					if (string.IsNullOrWhiteSpace(q) || _items.Count >= MaxEntries)
						continue;

					var entry = new SearchHistoryEntry { Query = q.Trim(), LastRunAt = now };
					if (_items.Any(x => x.IsSameSearch(entry)))
						continue;

					_items.Add(entry);
					changed = true;
				}
			}

			IsNew = false;
			if (changed)
				SaveAndNotify();
		}

		public bool Remove(SearchHistoryEntry entry)
		{
			ArgumentNullException.ThrowIfNull(entry);
			bool removed;
			lock (_syncRoot)
				removed = _items.RemoveAll(x => x.IsSameSearch(entry)) > 0;

			if (removed)
				SaveAndNotify();
			return removed;
		}

		public void Clear()
		{
			lock (_syncRoot)
				_items.Clear();

			SaveAndNotify();
		}

		private void SaveAndNotify()
		{
			Save();
			OnChanged();
		}

		private void Save()
		{
			if (string.IsNullOrWhiteSpace(_filePath))
				return;

			SearchHistoryFileModel model;
			lock (_syncRoot)
				model = new SearchHistoryFileModel { Items = _items.ToList() };

			var tempPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
			try
			{
				var dir = Path.GetDirectoryName(_filePath);
				if (!string.IsNullOrEmpty(dir))
					Directory.CreateDirectory(dir);

				File.WriteAllText(tempPath, JsonSerializer.Serialize(model, SearchHistoryJsonContext.Default.SearchHistoryFileModel));
				File.Move(tempPath, _filePath, overwrite: true);
			}
			catch
			{
				// History is a convenience; a failed write must never break the search itself.
				try { File.Delete(tempPath); } catch { }
			}
		}

		private void OnChanged()
			=> Changed?.Invoke(this, EventArgs.Empty);
	}
}
