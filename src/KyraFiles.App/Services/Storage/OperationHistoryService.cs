// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System.IO;
using KyraFiles.Shared.OperationHistory;
using Windows.Storage;

namespace KyraFiles.App.Services
{
	internal sealed class OperationHistoryService : IOperationHistoryService
	{
		private readonly OperationHistoryStore _store;

		public OperationHistoryService()
		{
			string? filePath = null;
			try
			{
				filePath = Path.Combine(
					ApplicationData.Current.LocalFolder.Path,
					Constants.LocalSettings.SettingsFolderName,
					"operation-history.json");
			}
			catch
			{
				// Fallback to in-memory if LocalFolder is unavailable in unit/unpackaged context.
			}

			_store = new OperationHistoryStore(filePath);
			_store.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
			_store.Load();
		}

		public event EventHandler? Changed;

		public int MaxEntries
		{
			get => _store.MaxEntries;
			set => _store.MaxEntries = value;
		}

		public bool PersistenceEnabled
		{
			get => _store.PersistenceEnabled;
			set => _store.PersistenceEnabled = value;
		}

		public string? LastLoadError => _store.LastLoadError;

		public void RecordFromStorageHistory(IStorageHistory? history, OperationHistoryResult result = OperationHistoryResult.Success)
		{
			if (history is null)
				return;

			var kind = MapKind(history.OperationType);
			var reversible = kind != OperationHistoryKind.DeletePermanent && result != OperationHistoryResult.Failed;

			var sources = (history.Source ?? [])
				.Where(i => i is not null && !string.IsNullOrWhiteSpace(i.Path))
				.Select(i => new OperationHistoryItemPath(i.Path, i.ItemType == FilesystemItemType.Directory))
				.ToList();

			var destinations = (history.Destination ?? [])
				.Where(i => i is not null && !string.IsNullOrWhiteSpace(i.Path))
				.Select(i => new OperationHistoryItemPath(i.Path, i.ItemType == FilesystemItemType.Directory))
				.ToList();

			if (sources.Count == 0 && destinations.Count == 0)
				return;

			_store.Add(new OperationHistoryEntry
			{
				TimestampUtc = DateTimeOffset.UtcNow,
				Kind = kind,
				Result = result,
				Reversible = reversible,
				IrreversibleReason = reversible ? null : "Operacao nao reversivel.",
				TotalItemCount = Math.Max(sources.Count, destinations.Count),
				Sources = sources,
				Destinations = destinations,
				LiveHistory = history,
			});
		}

		public void RecordPermanentDelete(IEnumerable<IStorageItemWithPath> sources, OperationHistoryResult result = OperationHistoryResult.Success)
		{
			var list = (sources ?? [])
				.Where(i => i is not null && !string.IsNullOrWhiteSpace(i.Path))
				.Select(i => new OperationHistoryItemPath(i.Path, i.ItemType == FilesystemItemType.Directory))
				.ToList();

			if (list.Count == 0)
				return;

			_store.Add(new OperationHistoryEntry
			{
				TimestampUtc = DateTimeOffset.UtcNow,
				Kind = OperationHistoryKind.DeletePermanent,
				Result = result,
				Reversible = false,
				IrreversibleReason = "Exclusao definitiva nao pode ser desfeita.",
				TotalItemCount = list.Count,
				Sources = list,
			});
		}

		public IReadOnlyList<OperationHistoryEntry> GetSnapshot()
			=> _store.GetSnapshot();

		public IReadOnlyList<OperationHistoryEntry> Filter(OperationHistoryKind? kind = null, string? searchText = null)
			=> _store.Filter(kind, searchText);

		public UndoEligibilityResult CheckUndoEligibility(OperationHistoryEntry entry)
			=> UndoEligibility.Evaluate(entry);

		public async Task<(bool Succeeded, string? Message)> UndoEntryAsync(
			OperationHistoryEntry entry,
			IShellPage? associatedInstance,
			CancellationToken cancellationToken = default)
		{
			var eligibility = UndoEligibility.Evaluate(entry);
			if (!eligibility.CanUndo)
				return (false, eligibility.Reason);

			if (associatedInstance is null)
				return (false, "Nenhuma aba ativa disponivel para executar o desfazer.");

			var storageHistory = entry.LiveHistory as IStorageHistory ?? ReconstructStorageHistory(entry);
			if (storageHistory is null)
				return (false, "Nao foi possivel reconstruir esta operacao para desfazer apos reinicio.");

			var operations = new StorageHistoryOperations(associatedInstance, cancellationToken);
			var returnResult = await operations.Undo(storageHistory);

			if (returnResult is ReturnResult.Success or ReturnResult.InProgress)
			{
				_store.MarkUndone(entry.Id);
				return (true, null);
			}

			return (false, $"Nao foi possivel desfazer a operacao ({returnResult}).");
		}

		public void Clear()
			=> _store.Clear();

		private static OperationHistoryKind MapKind(FileOperationType opType) => opType switch
		{
			FileOperationType.Copy => OperationHistoryKind.Copy,
			FileOperationType.Move => OperationHistoryKind.Move,
			FileOperationType.Recycle => OperationHistoryKind.Recycle,
			FileOperationType.Delete => OperationHistoryKind.DeletePermanent,
			FileOperationType.Rename => OperationHistoryKind.Rename,
			FileOperationType.CreateNew => OperationHistoryKind.CreateNew,
			FileOperationType.CreateLink => OperationHistoryKind.CreateLink,
			FileOperationType.Restore => OperationHistoryKind.Restore,
			_ => OperationHistoryKind.DeletePermanent,
		};

		private static IStorageHistory? ReconstructStorageHistory(OperationHistoryEntry entry)
		{
			FileOperationType? opType = entry.Kind switch
			{
				OperationHistoryKind.Move => FileOperationType.Move,
				OperationHistoryKind.Rename => FileOperationType.Rename,
				OperationHistoryKind.CreateNew => FileOperationType.CreateNew,
				OperationHistoryKind.CreateLink => FileOperationType.CreateLink,
				OperationHistoryKind.Copy => FileOperationType.Copy,
				OperationHistoryKind.Recycle => FileOperationType.Recycle,
				_ => null,
			};

			if (opType is null)
				return null;

			var sources = entry.Sources
				.Select(s => StorageHelpers.FromPathAndType(s.Path, s.IsDirectory ? FilesystemItemType.Directory : FilesystemItemType.File))
				.ToList();

			var destinations = entry.Destinations
				.Select(d => StorageHelpers.FromPathAndType(d.Path, d.IsDirectory ? FilesystemItemType.Directory : FilesystemItemType.File))
				.ToList();

			if (sources.Count == 0)
				return null;

			return new StorageHistory(opType.Value, sources, destinations);
		}
	}
}
