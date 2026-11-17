// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Input;

namespace KyraFiles.App.Controls
{
	public record class BreadcrumbBarItemClickedEventArgs(BreadcrumbBarItem Item, int Index, bool IsRootItem = false, PointerRoutedEventArgs? PointerRoutedEventArgs = null);

	public record class BreadcrumbBarItemDropDownFlyoutEventArgs(MenuFlyout Flyout, BreadcrumbBarItem? Item = null, int Index = -1, bool IsRootItem = false);
}
