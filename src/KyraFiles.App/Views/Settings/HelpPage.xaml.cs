// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.ViewModels.Help;
using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.Views.Settings
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

