// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Contracts;
using Files.App.Data.Models;
using System.Collections.ObjectModel;

namespace Files.App.ViewModels.Dialogs
{
	public sealed partial class ChooseDetailsItemViewModel : ObservableObject
	{
		public string CanonicalName { get; init; } = string.Empty;

		public string DisplayName { get; init; } = string.Empty;

		public string Category { get; init; } = string.Empty;

		public Microsoft.UI.Xaml.GridLength UserLength { get; set; } = new(140);

		public Microsoft.UI.Xaml.TextAlignment TextAlignment { get; init; } = Microsoft.UI.Xaml.TextAlignment.Left;

		private bool isChecked;
		public bool IsChecked
		{
			get => isChecked;
			set => SetProperty(ref isChecked, value);
		}
	}

	public sealed partial class ChooseDetailsDialogViewModel : ObservableObject
	{
		private readonly IExplorerPropertyService propertyService;
		private readonly ColumnsViewModel columnsViewModel;
		private readonly List<ChooseDetailsItemViewModel> allItems = [];

		public ObservableCollection<ChooseDetailsItemViewModel> FilteredItems { get; } = [];

		private string searchText = string.Empty;
		public string SearchText
		{
			get => searchText;
			set
			{
				if (SetProperty(ref searchText, value))
					FilterItems();
			}
		}

		public ChooseDetailsDialogViewModel(ColumnsViewModel columnsViewModel, IExplorerPropertyService propertyService)
		{
			this.columnsViewModel = columnsViewModel;
			this.propertyService = propertyService;

			var activeNames = new HashSet<string>(columnsViewModel.DynamicColumns.Select(c => c.CanonicalName), StringComparer.OrdinalIgnoreCase);

			foreach (var prop in propertyService.GetAllAvailableProperties())
			{
				var item = new ChooseDetailsItemViewModel
				{
					CanonicalName = prop.CanonicalName,
					DisplayName = prop.DisplayName,
					Category = prop.Category,
					UserLength = prop.UserLength,
					TextAlignment = prop.TextAlignment,
					IsChecked = activeNames.Contains(prop.CanonicalName),
				};
				allItems.Add(item);
			}

			FilterItems();
		}

		private void FilterItems()
		{
			FilteredItems.Clear();
			var query = searchText.Trim();

			var queryItems = string.IsNullOrEmpty(query)
				? allItems
				: allItems.Where(i => i.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
					|| i.Category.Contains(query, StringComparison.CurrentCultureIgnoreCase)
					|| i.CanonicalName.Contains(query, StringComparison.OrdinalIgnoreCase));

			foreach (var item in queryItems)
				FilteredItems.Add(item);
		}

		public void ApplyChanges()
		{
			var currentLengthMap = columnsViewModel.DynamicColumns.ToDictionary(c => c.CanonicalName, c => c.UserLength, StringComparer.OrdinalIgnoreCase);

			columnsViewModel.DynamicColumns.Clear();
			foreach (var item in allItems.Where(i => i.IsChecked))
			{
				var length = currentLengthMap.TryGetValue(item.CanonicalName, out var savedLength) ? savedLength : item.UserLength;
				columnsViewModel.DynamicColumns.Add(new DynamicColumnDefinition
				{
					CanonicalName = item.CanonicalName,
					DisplayName = item.DisplayName,
					Category = item.Category,
					UserLength = length,
					TextAlignment = item.TextAlignment,
					IsVisible = true,
				});
			}
		}
	}
}
