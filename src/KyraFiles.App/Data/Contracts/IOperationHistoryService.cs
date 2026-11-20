// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.Shared.OperationHistory;

namespace KyraFiles.App.Data.Contracts
{
	public interface IOperationHistoryService
	{
		event EventHandler? Changed;

		int MaxEntries { get; set; }

		bool PersistenceEnabled { get; set; }

		string? LastLoadError { get; }

		void RecordFromStorageHistory(IStorageHistory? history, OperationHistoryResult result = OperationHistoryResult.Success);

		void RecordPermanentDelete(IEnumerable<IStorageItemWithPath> sources, OperationHistoryResult result = OperationHistoryResult.Success);

		IReadOnlyList<OperationHistoryEntry> GetSnapshot();

		IReadOnlyList<OperationHistoryEntry> Filter(OperationHistoryKind? kind = null, string? searchText = null);

		UndoEligibilityResult CheckUndoEligibility(OperationHistoryEntry entry);

		Task<(bool Succeeded, string? Message)> UndoEntryAsync(
			OperationHistoryEntry entry,
			IShellPage? associatedInstance,
			CancellationToken cancellationToken = default);

		void Clear();
	}
}
