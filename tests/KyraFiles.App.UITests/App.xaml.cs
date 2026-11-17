// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace KyraFiles.App.UITests
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			MainWindow.Instance.Activate();
		}
	}
}
