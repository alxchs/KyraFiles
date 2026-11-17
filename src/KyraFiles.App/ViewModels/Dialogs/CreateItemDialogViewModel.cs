// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.ViewModels.Dialogs
{
	[WinRT.GeneratedBindableCustomProperty([nameof(IsNameInvalid)], [])]
	partial class CreateItemDialogViewModel : ObservableObject
	{
		private bool isNameInvalid;
		public bool IsNameInvalid
		{
			get => isNameInvalid;
			set => SetProperty(ref isNameInvalid, value);
		}
	}
}
