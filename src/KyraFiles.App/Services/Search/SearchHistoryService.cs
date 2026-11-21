// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.IO;
using KyraFiles.Shared.SearchHistory;
using Windows.Storage;

namespace KyraFiles.App.Services
{
	internal sealed class SearchHistoryService : ISearchHistoryService
	{
		private readonly SearchHistoryStore _store;

		public SearchHistoryService(IUserSettingsService userSettingsService)
		{
			string? filePath = null;
			try
			{
				filePath = Path.Combine(
					ApplicationData.Current.LocalFolder.Path,
					Constants.LocalSettings.SettingsFolderName,
					"search-history.json");
			}
			catch
			{
				// Fallback to in-memory outside packaged runtime.
			}

			_store = new SearchHistoryStore(filePath);
			_store.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
			_store.Load();

			// One-time import of the plain queries kept before F094.
			if (_store.IsNew)
				_store.ImportLegacy(userSettingsService.GeneralSettingsService.PreviousSearchQueriesList, DateTimeOffset.Now);
		}

		public event EventHandler? Changed;

		public IReadOnlyList<SearchHistoryEntry> GetAll() => _store.GetAll();

		public void Record(string query, string scopePath, string? engineId)
			=> _store.Record(new SearchHistoryEntry
			{
				Query = query,
				ScopePath = scopePath,
				EngineId = engineId,
				LastRunAt = DateTimeOffset.Now,
			});

		public bool Remove(SearchHistoryEntry entry) => _store.Remove(entry);

		public void Clear() => _store.Clear();
	}
}
