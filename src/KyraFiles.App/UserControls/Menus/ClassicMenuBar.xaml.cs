// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Data.Commands;
using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.UserControls.Menus
{
	public sealed partial class ClassicMenuBar : UserControl
	{
		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public ClassicMenuBar()
		{
			InitializeComponent();
		}
	}
}
