// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System.IO;
using KyraFiles.Shared.SavedSearches;
using Windows.Storage;

namespace KyraFiles.App.Services
{
	internal sealed class SavedSearchesService : ISavedSearchesService
	{
		private readonly SavedSearchStore _store;

		public SavedSearchesService()
		{
			string? filePath = null;
			try
			{
				filePath = Path.Combine(
					ApplicationData.Current.LocalFolder.Path,
					Constants.LocalSettings.SettingsFolderName,
					"saved-searches.json");
			}
			catch
			{
				// Fallback to in-memory outside packaged runtime.
			}

			_store = new SavedSearchStore(filePath);
			_store.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
			_store.Load();
		}

		public event EventHandler? Changed;

		public IReadOnlyList<SavedSearchDefinition> GetAll() => _store.GetAll();

		public SavedSearchDefinition? GetById(string id) => _store.GetById(id);

		public SavedSearchDefinition Add(SavedSearchDefinition definition) => _store.Add(definition);

		public bool Update(SavedSearchDefinition definition) => _store.Update(definition);

		public bool Remove(string id) => _store.Remove(id);

		public bool Reorder(int oldIndex, int newIndex) => _store.Reorder(oldIndex, newIndex);

		public EvaluatedSavedSearch Evaluate(SavedSearchDefinition definition, DateTimeOffset? referenceNow = null)
			=> SavedSearchQueryEvaluator.Evaluate(definition, referenceNow ?? DateTimeOffset.Now);
	}
}
