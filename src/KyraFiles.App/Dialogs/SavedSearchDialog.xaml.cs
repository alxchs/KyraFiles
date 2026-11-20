// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.App.Helpers;
using KyraFiles.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.Dialogs
{
	public sealed partial class SavedSearchDialog : ContentDialog, IDialog<SavedSearchDialogViewModel>
	{
		public SavedSearchDialogViewModel ViewModel
		{
			get => (SavedSearchDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public SavedSearchDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this); // KyraFiles: F057
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();
	}
}
