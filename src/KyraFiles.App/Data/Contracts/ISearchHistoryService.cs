// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.Shared.SearchHistory;

namespace KyraFiles.App.Data.Contracts
{
	/// <summary>
	/// Last searches with what is needed to repeat them (F094).
	/// </summary>
	public interface ISearchHistoryService
	{
		event EventHandler? Changed;

		IReadOnlyList<SearchHistoryEntry> GetAll();

		void Record(string query, string scopePath, string? engineId);

		bool Remove(SearchHistoryEntry entry);

		void Clear();
	}
}
