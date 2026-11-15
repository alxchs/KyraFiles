// Copyright (c) Files Community
// Licensed under the MIT License.

using KyraFiles.Shared.Utils;

namespace KyraFiles.App.ViewModels.Dialogs.AddItemDialog
{
	public sealed partial class AddItemDialogListItemViewModel : ObservableObject
	{
		public string? Header { get; set; }

		public string? SubHeader { get; set; }

		public string? Glyph { get; set; }

		public IImage? Icon { get; set; }

		public bool IsItemEnabled { get; set; }

		public AddItemDialogResultModel? ItemResult { get; set; }
	}
}
