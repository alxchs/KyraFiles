// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using KyraFiles.Shared.SavedSearches;

namespace KyraFiles.App.ViewModels.Dialogs
{
	public sealed partial class SavedSearchDialogViewModel : ObservableObject
	{
		public string? EditingId { get; }

		public string DialogTitle => string.IsNullOrEmpty(EditingId) ? "Salvar busca" : "Editar busca salva";

		[ObservableProperty]
		public partial string Name { get; set; } = string.Empty;

		[ObservableProperty]
		public partial string Query { get; set; } = string.Empty;

		[ObservableProperty]
		public partial string ScopePath { get; set; } = string.Empty;

		[ObservableProperty]
		public partial string ExtensionsText { get; set; } = string.Empty;

		[ObservableProperty]
		public partial int RelativeDateIndex { get; set; } = 0;

		[ObservableProperty]
		public partial int RelativeDaysCount { get; set; } = 7;

		[ObservableProperty]
		public partial string WarningMessage { get; set; } = string.Empty;

		public bool HasWarning => !string.IsNullOrWhiteSpace(WarningMessage);

		public SavedSearchDialogViewModel(SavedSearchDefinition? existing = null, string? warningMessage = null)
		{
			WarningMessage = warningMessage ?? string.Empty;
			if (existing is not null)
			{
				EditingId = existing.Id;
				Name = existing.Name;
				Query = existing.Query;
				ScopePath = existing.ScopePath;
				ExtensionsText = string.Join(", ", existing.Extensions ?? []);
				RelativeDateIndex = (int)existing.RelativeDateKind;
				RelativeDaysCount = existing.RelativeDaysCount > 0 ? existing.RelativeDaysCount : 7;
			}
		}

		public SavedSearchDefinition BuildDefinition()
		{
			var exts = (ExtensionsText ?? string.Empty)
				.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries)
				.Select(e => e.Trim())
				.Where(e => !string.IsNullOrEmpty(e))
				.ToList();

			var relKind = Enum.IsDefined(typeof(SavedSearchRelativeDateKind), RelativeDateIndex)
				? (SavedSearchRelativeDateKind)RelativeDateIndex
				: SavedSearchRelativeDateKind.None;

			return new SavedSearchDefinition
			{
				Id = EditingId ?? Guid.NewGuid().ToString("N"),
				Name = string.IsNullOrWhiteSpace(Name) ? (string.IsNullOrWhiteSpace(Query) ? "Busca salva" : Query.Trim()) : Name.Trim(),
				Query = Query?.Trim() ?? string.Empty,
				ScopePath = ScopePath?.Trim() ?? string.Empty,
				Extensions = exts,
				RelativeDateKind = relKind,
				RelativeDaysCount = Math.Max(1, RelativeDaysCount),
			};
		}
	}
}
