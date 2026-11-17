// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls.Primitives;

namespace KyraFiles.App.Data.EventArguments
{
	public sealed class WidgetsRightClickedItemChangedEventArgs
	{
		public WidgetCardItem? Item { get; set; }

		public FlyoutBase? Flyout { get; set; }

		public WidgetsRightClickedItemChangedEventArgs(WidgetCardItem? item = null, FlyoutBase? flyout = null)
		{
			Item = item;
			Flyout = flyout;
		}
	}
}
