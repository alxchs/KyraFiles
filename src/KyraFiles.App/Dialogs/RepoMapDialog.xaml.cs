// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using KyraFiles.App.Helpers;
using KyraFiles.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT;

namespace KyraFiles.App.Dialogs
{
	public sealed partial class RepoMapDialog : ContentDialog, IDialog<RepoMapDialogViewModel>
	{
		private FrameworkElement RootAppElement
		{
			[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
			get => (FrameworkElement)MainWindow.Instance.Content;
		}

		public RepoMapDialogViewModel ViewModel
		{
			get => (RepoMapDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public RepoMapDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this);
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			args.Cancel = true; // Keep dialog open after copying markdown
			ViewModel.CopyMarkdownCommand.Execute(null);
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			args.Cancel = true; // Keep dialog open after copying CSV
			ViewModel.CopyCsvCommand.Execute(null);
		}

		private void FilesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (sender is ListView listView && listView.SelectedItem is RepoFileInfoViewModel fileVm)
			{
				ViewModel.OpenFileCommand.Execute(fileVm);
			}
		}
	}
}
