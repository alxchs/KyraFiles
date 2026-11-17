// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.ViewModels.Help;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.UserControls
{
	public sealed partial class HelpPane : UserControl
	{
		public HelpViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<HelpViewModel>();

		public HelpPane()
		{
			InitializeComponent();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.CloseHelp();
		}
	}
}

