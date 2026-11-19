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
	public sealed partial class DuplicateFinderDialog : ContentDialog, IDialog<DuplicateFinderDialogViewModel>
	{
		private FrameworkElement RootAppElement
		{
			[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
			get => (FrameworkElement)MainWindow.Instance.Content;
		}

		public DuplicateFinderDialogViewModel ViewModel
		{
			get => (DuplicateFinderDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public DuplicateFinderDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this);
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
				// Keep open if user still has items or wants to inspect results
				if (ViewModel.Groups.Count > 0 && ViewModel.CanDelete)
				{
					args.Cancel = true;
				}
			}
			finally
			{
				deferral.Complete();
			}
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel?.CancelScan();
		}
	}
}
