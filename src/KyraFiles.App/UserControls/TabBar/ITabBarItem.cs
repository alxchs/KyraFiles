// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace KyraFiles.App.UserControls.TabBar
{
	/// <summary>
	/// Represents an interface for <see cref="TabBarItem"/>.
	/// </summary>
	public interface ITabBarItem
	{
		IconSource? IconSource { get; }

		string? Header { get; }

		string? Description { get; }

		bool AllowStorageItemDrop { get; }

		public TabBarItemParameter? NavigationParameter { get; }
	}
}
