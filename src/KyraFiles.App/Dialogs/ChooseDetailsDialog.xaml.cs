// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;

namespace Files.App.Dialogs
{
	public sealed partial class ChooseDetailsDialog : ContentDialog, IDialog<ChooseDetailsDialogViewModel>
	{
		private FrameworkElement RootAppElement
		{
			[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
			get => (FrameworkElement)MainWindow.Instance.Content;
		}

		public ChooseDetailsDialogViewModel ViewModel
		{
			get => (ChooseDetailsDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ChooseDetailsDialog()
		{
			InitializeComponent();
			DialogShortcuts.Attach(this); // Files Custom: F057
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel?.ApplyChanges();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			if (XamlRoot is null && MainWindow.Instance?.Content?.XamlRoot is not null)
				XamlRoot = MainWindow.Instance.Content.XamlRoot;

			return (DialogResult)await base.ShowAsync();
		}
	}
}
