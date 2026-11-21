// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.Controls;
using KyraFiles.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using WinRT;

namespace KyraFiles.App.Data.Models
{
	public sealed partial class SuggestionModel : ObservableObject, IOmnibarTextMemberPathProvider
	{
		public bool IsRecentSearch { get; set; } = false;

		public bool IsShortcut { get; set; } = false;

		public bool LoadFileIcon { get; set; } = false;

		public bool NeedsPlaceholderGlyph { get; set; } = true;

		public string? ItemPath { get; set; }

		public string Name { get; set; }

		public string? Description { get; set; }

		public Visibility DescriptionVisibility => string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;

		public SettingsSearchResult? SettingsResult { get; set; }

		private BitmapImage? fileImage;
		public BitmapImage? FileImage
		{
			get => fileImage;
			set
			{
				if (fileImage is BitmapImage imgOld)
				{
					imgOld.ImageOpened -= Img_ImageOpened;
				}

				if (SetProperty(ref fileImage, value))
				{
					if (value is BitmapImage img)
					{
						if (img.PixelWidth > 0)
						{
							Img_ImageOpened(img, null);
						}
						else
						{
							img.ImageOpened += Img_ImageOpened;
						}
					}
				}
			}
		}

		[DynamicWindowsRuntimeCast(typeof(BitmapImage))]
		private void Img_ImageOpened(object sender, Microsoft.UI.Xaml.RoutedEventArgs? e)
		{
			if (sender is BitmapImage image)
			{
				image.ImageOpened -= Img_ImageOpened;

				if (image.PixelWidth > 0)
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						LoadFileIcon = true;
						NeedsPlaceholderGlyph = false;
					}, App.Logger); // 2009482836u
				}
			}
		}

		public SuggestionModel(ListedItem item)
		{
			IsShortcut = item.IsShortcut;
			LoadFileIcon = item.LoadFileIcon;
			NeedsPlaceholderGlyph = item.FileImage is null;
			ItemPath = item.ItemPath;
			Name = item.Name!;
			FileImage = item.FileImage;
		}

		public SuggestionModel(string itemName, bool isRecentSearch)
		{
			IsRecentSearch = isRecentSearch;
			Name = itemName;
		}

		public SuggestionModel(KyraFiles.Shared.SearchHistory.SearchHistoryEntry entry, string? description)
		{
			IsRecentSearch = true;
			HistoryEntry = entry;
			Name = entry.Query;
			Description = description;
		}

		/// <summary>Set for search history suggestions; carries scope and engine to repeat the search (F094).</summary>
		public KyraFiles.Shared.SearchHistory.SearchHistoryEntry? HistoryEntry { get; }

		public bool IsHistoryEntry => HistoryEntry is not null;

		/// <summary>Identity used to diff suggestion lists; history entries have no item path.</summary>
		public string SuggestionKey => HistoryEntry is { } h
			? $"history|{h.Query}|{h.ScopePath}"
			: ItemPath ?? $"name|{Name}";

		public SuggestionModel(SettingsSearchResult settingsResult)
		{
			SettingsResult = settingsResult;
			Name = settingsResult.HeaderText;
			Description = settingsResult.DisplayPath;
			NeedsPlaceholderGlyph = false;
		}

		public string GetTextMemberPath(string textMemberPath)
		{
			return Name;
		}
	}
}
