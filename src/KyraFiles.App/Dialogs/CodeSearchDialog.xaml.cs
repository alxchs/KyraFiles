// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using KyraFiles.App.Helpers;
using KyraFiles.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using WinRT;

namespace KyraFiles.App.Dialogs
{
	public sealed partial class CodeSearchDialog : ContentDialog, IDialog<CodeSearchDialogViewModel>
	{
		private FrameworkElement RootAppElement
		{
			[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
			get => (FrameworkElement)MainWindow.Instance.Content;
		}

		public CodeSearchDialogViewModel ViewModel
		{
			get => (CodeSearchDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CodeSearchDialog()
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
				if (ViewModel.CanOpenInIDE)
				{
					await ViewModel.OpenInIDEAsync();
				}
			}
			finally
			{
				deferral.Complete();
			}
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel?.CancelSearch();
		}

		private async void SearchButton_Click(object sender, RoutedEventArgs e)
		{
			await ViewModel.StartSearchAsync();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.CancelSearch();
		}

		private async void QueryTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				e.Handled = true;
				await ViewModel.StartSearchAsync();
			}
		}

		private async void MatchesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (ViewModel.CanOpenInIDE)
			{
				await ViewModel.OpenInIDEAsync();
			}
		}

		private async void OpenInIDEMenuItem_Click(object sender, RoutedEventArgs e)
		{
			await ViewModel.OpenInIDEAsync();
		}

		private void OpenFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.OpenFile();
		}

		private void CopyLocationMenuItem_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.CopyLocation();
		}
	}
}
