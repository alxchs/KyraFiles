// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using KyraFiles.App.Helpers;
using KyraFiles.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.Dialogs
{
	public sealed partial class ProjectActionsDialog : ContentDialog, IDialog<ProjectActionsDialogViewModel>
	{
		public ProjectActionsDialogViewModel ViewModel
		{
			get => (ProjectActionsDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ProjectActionsDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this); // KyraFiles: F057
		}

		public new async Task<DialogResult> ShowAsync()
			=> (DialogResult)await base.ShowAsync();

		private void AddCommandButton_Click(object sender, RoutedEventArgs e)
			=> ViewModel.AddCommand();

		private void RemoveCommandButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement { Tag: string id })
				ViewModel.RemoveCommand(id);
		}

		private void ImportJsonButton_Click(object sender, RoutedEventArgs e)
			=> ViewModel.ImportJson();
	}
}
