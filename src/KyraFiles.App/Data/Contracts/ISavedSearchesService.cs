// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.Shared.SavedSearches;

namespace KyraFiles.App.Data.Contracts
{
	public interface ISavedSearchesService
	{
		event EventHandler? Changed;

		IReadOnlyList<SavedSearchDefinition> GetAll();

		SavedSearchDefinition? GetById(string id);

		SavedSearchDefinition Add(SavedSearchDefinition definition);

		bool Update(SavedSearchDefinition definition);

		bool Remove(string id);

		bool Reorder(int oldIndex, int newIndex);

		EvaluatedSavedSearch Evaluate(SavedSearchDefinition definition, DateTimeOffset? referenceNow = null);
	}
}
