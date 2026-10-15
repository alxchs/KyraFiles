// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Help;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
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

