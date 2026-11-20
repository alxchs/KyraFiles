// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using KyraFiles.App.Helpers;
using KyraFiles.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.Dialogs
{
	public sealed partial class OperationHistoryDialog : ContentDialog, IDialog<OperationHistoryDialogViewModel>
	{
		public OperationHistoryDialogViewModel ViewModel
		{
			get => (OperationHistoryDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public OperationHistoryDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this); // KyraFiles: F057
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();

		private async void UndoRowButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement { Tag: OperationHistoryRowViewModel row })
				await ViewModel.UndoRowAsync(row);
		}

		private void ClearButton_Click(object sender, RoutedEventArgs e)
			=> ViewModel.ClearHistory();
	}
}
