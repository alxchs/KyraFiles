// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.IO;
using KyraFiles.Shared.OperationHistory;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class OperationHistoryRowViewModel : ObservableObject
	{
		public OperationHistoryEntry Entry { get; }

		public string TimestampText => Entry.TimestampUtc.ToLocalTime().ToString("dd/MM HH:mm:ss");

		public string KindLabel => Entry.Kind switch
		{
			OperationHistoryKind.Copy => "Copiar",
			OperationHistoryKind.Move => "Mover",
			OperationHistoryKind.Recycle => "Lixeira",
			OperationHistoryKind.DeletePermanent => "Excluir definitivo",
			OperationHistoryKind.Rename => "Renomear",
			OperationHistoryKind.CreateNew => "Criar",
			OperationHistoryKind.CreateLink => "Atalho",
			OperationHistoryKind.Restore => "Restaurar",
			_ => Entry.Kind.ToString(),
		};

		public string SummaryText
		{
			get
			{
				var firstSource = Entry.Sources.FirstOrDefault()?.Path;
				var firstDest = Entry.Destinations.FirstOrDefault()?.Path;
				var itemLabel = !string.IsNullOrEmpty(firstSource)
					? Path.GetFileName(firstSource)
					: (!string.IsNullOrEmpty(firstDest) ? Path.GetFileName(firstDest) : "(sem caminho)");

				if (Entry.TotalItemCount > 1)
					return $"{itemLabel} (+{Entry.TotalItemCount - 1} itens)";

				if (!string.IsNullOrEmpty(firstDest) && Entry.Kind is OperationHistoryKind.Rename or OperationHistoryKind.Move)
					return $"{itemLabel} -> {Path.GetFileName(firstDest)}";

				return itemLabel;
			}
		}

		public string DetailPathText => Entry.Sources.FirstOrDefault()?.Path ?? Entry.Destinations.FirstOrDefault()?.Path ?? string.Empty;

		public bool ShowUndoButton => Entry.Reversible && !Entry.IsUndone && Entry.Kind != OperationHistoryKind.DeletePermanent;

		public string StatusText => Entry.IsUndone
			? "Desfeita"
			: (!Entry.Reversible ? (Entry.IrreversibleReason ?? "Nao reversivel") : Entry.Result.ToString());

		public OperationHistoryRowViewModel(OperationHistoryEntry entry)
		{
			Entry = entry;
		}
	}

	public sealed partial class OperationHistoryDialogViewModel : ObservableObject
	{
		private readonly IOperationHistoryService _historyService;
		private readonly IShellPage? _associatedInstance;

		public ObservableCollection<OperationHistoryRowViewModel> Entries { get; } = [];

		[ObservableProperty]
		public partial string SearchQuery { get; set; } = string.Empty;

		[ObservableProperty]
		public partial int SelectedKindFilterIndex { get; set; } = 0;

		[ObservableProperty]
		public partial string StatusBanner { get; set; } = string.Empty;

		[ObservableProperty]
		public partial bool HasStatusBanner { get; set; }

		[ObservableProperty]
		public partial bool PersistenceEnabled { get; set; }

		[ObservableProperty]
		public partial int MaxEntries { get; set; }

		public OperationHistoryDialogViewModel(
			IOperationHistoryService historyService,
			IShellPage? associatedInstance)
		{
			_historyService = historyService;
			_associatedInstance = associatedInstance;
			PersistenceEnabled = _historyService.PersistenceEnabled;
			MaxEntries = _historyService.MaxEntries;
			Refresh();
		}

		partial void OnSearchQueryChanged(string value) => Refresh();

		partial void OnSelectedKindFilterIndexChanged(int value) => Refresh();

		partial void OnPersistenceEnabledChanged(bool value)
		{
			_historyService.PersistenceEnabled = value;
		}

		partial void OnMaxEntriesChanged(int value)
		{
			if (value > 0)
				_historyService.MaxEntries = value;
		}

		public void Refresh()
		{
			OperationHistoryKind? kind = SelectedKindFilterIndex switch
			{
				1 => OperationHistoryKind.Copy,
				2 => OperationHistoryKind.Move,
				3 => OperationHistoryKind.Rename,
				4 => OperationHistoryKind.Recycle,
				5 => OperationHistoryKind.DeletePermanent,
				6 => OperationHistoryKind.CreateNew,
				_ => null,
			};

			var items = _historyService.Filter(kind, SearchQuery);
			Entries.Clear();
			foreach (var item in items)
				Entries.Add(new OperationHistoryRowViewModel(item));
		}

		public async Task UndoRowAsync(OperationHistoryRowViewModel row)
		{
			if (row is null)
				return;

			var eligibility = _historyService.CheckUndoEligibility(row.Entry);
			if (!eligibility.CanUndo)
			{
				StatusBanner = eligibility.Reason ?? "Esta operacao nao pode ser desfeita.";
				HasStatusBanner = true;
				return;
			}

			if (eligibility.RequiresConfirmation)
			{
				var confirmed = await DialogDisplayHelper.ShowDialogAsync(
					"Confirmar desfazer",
					$"{eligibility.Reason}\nDeseja prosseguir com o desfazer?",
					Strings.Yes.GetLocalizedResource(),
					Strings.No.GetLocalizedResource());
				if (!confirmed)
					return;
			}

			var (succeeded, message) = await _historyService.UndoEntryAsync(row.Entry, _associatedInstance);
			StatusBanner = succeeded ? "Operacao desfeita com sucesso." : (message ?? "Falha ao desfazer operacao.");
			HasStatusBanner = true;
			Refresh();
		}

		public void ClearHistory()
		{
			_historyService.Clear();
			StatusBanner = "Historico limpo.";
			HasStatusBanner = true;
			Refresh();
		}
	}
}
