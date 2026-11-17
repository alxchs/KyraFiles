// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using KyraFiles.App.Helpers;
using KyraFiles.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace KyraFiles.App.Dialogs
{
	public sealed partial class CleanBuildFoldersDialog : ContentDialog, IDialog<CleanBuildFoldersDialogViewModel>
	{
		private FrameworkElement RootAppElement
		{
			[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
			get => (FrameworkElement)MainWindow.Instance.Content;
		}

		public CleanBuildFoldersDialogViewModel ViewModel
		{
			get => (CleanBuildFoldersDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CleanBuildFoldersDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this); // KyraFiles: F057
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}

		private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			var deferral = args.GetDeferral();
			try
			{
				await ViewModel.ExecuteDeleteAsync();
				if (ViewModel.HasInUseItems)
				{
					// Keep dialog open so user sees which items are in use (AC-4)
					args.Cancel = true;
				}
			}
			finally
			{
				deferral.Complete();
			}
		}
	}
}
