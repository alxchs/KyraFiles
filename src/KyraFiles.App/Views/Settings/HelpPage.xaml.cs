// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Help;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Settings
{
	public sealed partial class HelpPage : Page
	{
		public HelpViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<HelpViewModel>();

		public HelpPage()
		{
			InitializeComponent();
		}
	}
}

