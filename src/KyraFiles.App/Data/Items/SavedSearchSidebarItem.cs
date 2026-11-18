// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using KyraFiles.App.Controls;
using KyraFiles.Shared.SavedSearches;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace KyraFiles.App.Data.Items
{
	public sealed partial class SavedSearchSidebarItem : ObservableObject, INavigationControlItem
	{
		public SavedSearchDefinition Definition { get; }

		public string? Text { get; set; }

		public string? Path { get; set; }

		public SectionType Section => SectionType.SavedSearches;

		public NavigationControlItemType ItemType => NavigationControlItemType.SavedSearch;

		public ContextMenuOptions? MenuOptions { get; set; }

		public object? Children => null;

		public IconElement? IconElement => new FontIcon
		{
			Glyph = "\uE721",
			FontSize = 14,
			FontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"],
		};

		FrameworkElement? ISidebarItemPresentationModel.IconElement => IconElement;

		public object? ToolTip => string.IsNullOrWhiteSpace(Definition.ScopePath)
			? $"{Definition.Name} ({Definition.Query})"
			: $"{Definition.Name} — {Definition.ScopePath}";

		FrameworkElement? ISidebarItemPresentationModel.ItemDecorator => null;

		public bool IsExpanded { get => false; set { } }

		public SavedSearchSidebarItem(SavedSearchDefinition definition)
		{
			Definition = definition;
			Text = definition.Name;
			Path = SavedSearchQueryEvaluator.GetVirtualPath(definition.Id);
			MenuOptions = new ContextMenuOptions
			{
				ShowHideSection = false,
				IsLocationItem = false,
			};
		}

		public int CompareTo(INavigationControlItem? other)
		{
			if (other is SavedSearchSidebarItem otherSearch)
				return Definition.Order.CompareTo(otherSearch.Definition.Order);

			return string.Compare(Text, other?.Text, StringComparison.OrdinalIgnoreCase);
		}
	}
}
