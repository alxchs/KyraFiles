// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace Files.App.Data.Models
{
	[WinRT.GeneratedBindableCustomProperty(
		[
			nameof(IconColumn),
			nameof(GitStatusColumn),
			nameof(GitLastCommitDateColumn),
			nameof(GitLastCommitMessageColumn),
			nameof(GitCommitAuthorColumn),
			nameof(GitLastCommitShaColumn),
			nameof(TagColumn),
			nameof(NameColumn),
			nameof(StatusColumn),
			nameof(DateModifiedColumn),
			nameof(PathColumn),
			nameof(OriginalPathColumn),
			nameof(ItemTypeColumn),
			nameof(DateDeletedColumn),
			nameof(DateCreatedColumn),
			nameof(SizeColumn),
			nameof(DynamicColumns),
		],
		[])]
	public sealed partial class ColumnsViewModel : ObservableObject
	{
		private DetailsLayoutColumnItem iconColumn = new()
		{
			UserLength = new GridLength(24, GridUnitType.Pixel),
			IsResizable = false,
		};

		[RegistryIgnore]
		public DetailsLayoutColumnItem IconColumn
		{
			get => iconColumn;
			set => SetProperty(ref iconColumn, value);
		}

		private DetailsLayoutColumnItem _GitStatusColumn = new();
		public DetailsLayoutColumnItem GitStatusColumn
		{
			get => _GitStatusColumn;
			set => SetProperty(ref _GitStatusColumn, value);
		}

		private DetailsLayoutColumnItem _GitLastCommitDateColumn = new();
		public DetailsLayoutColumnItem GitLastCommitDateColumn
		{
			get => _GitLastCommitDateColumn;
			set => SetProperty(ref _GitLastCommitDateColumn, value);
		}

		private DetailsLayoutColumnItem _GitLastCommitMessageColumn = new();
		public DetailsLayoutColumnItem GitLastCommitMessageColumn
		{
			get => _GitLastCommitMessageColumn;
			set => SetProperty(ref _GitLastCommitMessageColumn, value);
		}

		private DetailsLayoutColumnItem _GitCommitAuthorColumn = new();
		public DetailsLayoutColumnItem GitCommitAuthorColumn
		{
			get => _GitCommitAuthorColumn;
			set => SetProperty(ref _GitCommitAuthorColumn, value);
		}

		private DetailsLayoutColumnItem _GitLastCommitShaColumn = new();
		public DetailsLayoutColumnItem GitLastCommitShaColumn
		{
			get => _GitLastCommitShaColumn;
			set => SetProperty(ref _GitLastCommitShaColumn, value);
		}

		private DetailsLayoutColumnItem tagColumn = new();
		public DetailsLayoutColumnItem TagColumn
		{
			get => tagColumn;
			set => SetProperty(ref tagColumn, value);
		}

		private DetailsLayoutColumnItem nameColumn = new()
		{
			NormalMaxLength = 1000d
		};
		public DetailsLayoutColumnItem NameColumn
		{
			get => nameColumn;
			set => SetProperty(ref nameColumn, value);
		}

		private DetailsLayoutColumnItem statusColumn = new()
		{
			UserLength = new GridLength(50),
			NormalMaxLength = 80,
		};
		public DetailsLayoutColumnItem StatusColumn
		{
			get => statusColumn;
			set => SetProperty(ref statusColumn, value);
		}

		private DetailsLayoutColumnItem dateModifiedColumn = new();
		public DetailsLayoutColumnItem DateModifiedColumn
		{
			get => dateModifiedColumn;
			set => SetProperty(ref dateModifiedColumn, value);
		}

		private DetailsLayoutColumnItem pathColumn = new()
		{
			NormalMaxLength = 500,
		};
		public DetailsLayoutColumnItem PathColumn
		{
			get => pathColumn;
			set => SetProperty(ref pathColumn, value);
		}

		private DetailsLayoutColumnItem originalPathColumn = new()
		{
			NormalMaxLength = 500,
		};
		public DetailsLayoutColumnItem OriginalPathColumn
		{
			get => originalPathColumn;
			set => SetProperty(ref originalPathColumn, value);
		}

		private DetailsLayoutColumnItem itemTypeColumn = new();
		public DetailsLayoutColumnItem ItemTypeColumn
		{
			get => itemTypeColumn;
			set => SetProperty(ref itemTypeColumn, value);
		}

		private DetailsLayoutColumnItem dateDeletedColumn = new();
		public DetailsLayoutColumnItem DateDeletedColumn
		{
			get => dateDeletedColumn;
			set => SetProperty(ref dateDeletedColumn, value);
		}

		private DetailsLayoutColumnItem dateCreatedColumn = new()
		{
			UserCollapsed = true
		};
		public DetailsLayoutColumnItem DateCreatedColumn
		{
			get => dateCreatedColumn;
			set => SetProperty(ref dateCreatedColumn, value);
		}

		private DetailsLayoutColumnItem sizeColumn = new();
		public DetailsLayoutColumnItem SizeColumn
		{
			get => sizeColumn;
			set => SetProperty(ref sizeColumn, value);
		}

		private ObservableCollection<DynamicColumnDefinition> dynamicColumns = [];

		[RegistryIgnore]
		public ObservableCollection<DynamicColumnDefinition> DynamicColumns
		{
			get => dynamicColumns;
			set => SetProperty(ref dynamicColumns, value);
		}

		public double TotalWidth =>
			IconColumn.Length.Value +
			GitStatusColumn.Length.Value +
			GitLastCommitDateColumn.Length.Value +
			GitLastCommitMessageColumn.Length.Value +
			GitCommitAuthorColumn.Length.Value +
			GitLastCommitShaColumn.Length.Value +
			TagColumn.Length.Value +
			NameColumn.Length.Value +
			DateModifiedColumn.Length.Value +
			PathColumn.Length.Value +
			OriginalPathColumn.Length.Value +
			ItemTypeColumn.Length.Value +
			DateDeletedColumn.Length.Value +
			DateCreatedColumn.Length.Value +
			SizeColumn.Length.Value +
			StatusColumn.Length.Value +
			DynamicColumns.Where(c => c.IsVisible).Sum(c => c.LengthPixels);

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is ColumnsViewModel model)
			{
				return
					model.DateCreatedColumn.Equals(DateCreatedColumn) &&
					model.DateDeletedColumn.Equals(DateDeletedColumn) &&
					model.DateModifiedColumn.Equals(DateModifiedColumn) &&
					model.ItemTypeColumn.Equals(ItemTypeColumn) &&
					model.NameColumn.Equals(NameColumn) &&
					model.PathColumn.Equals(PathColumn) &&
					model.OriginalPathColumn.Equals(OriginalPathColumn) &&
					model.SizeColumn.Equals(SizeColumn) &&
					model.StatusColumn.Equals(StatusColumn) &&
					model.TagColumn.Equals(TagColumn) &&
					model.GitStatusColumn.Equals(GitStatusColumn) &&
					model.GitLastCommitDateColumn.Equals(GitLastCommitDateColumn) &&
					model.GitLastCommitMessageColumn.Equals(GitLastCommitMessageColumn) &&
					model.GitCommitAuthorColumn.Equals(GitCommitAuthorColumn) &&
					model.GitLastCommitShaColumn.Equals(GitLastCommitShaColumn) &&
					model.DynamicColumns.Count == DynamicColumns.Count &&
					model.DynamicColumns.Zip(DynamicColumns).All(p => p.First.CanonicalName == p.Second.CanonicalName && p.First.UserLengthPixels == p.Second.UserLengthPixels && p.First.IsVisible == p.Second.IsVisible);
			}

			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			var hashCode = DateCreatedColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ DateDeletedColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ DateModifiedColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ ItemTypeColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ NameColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ PathColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ OriginalPathColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ SizeColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ StatusColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ TagColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ GitStatusColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ GitLastCommitDateColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ GitLastCommitMessageColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ GitCommitAuthorColumn.GetHashCode();
			hashCode = (hashCode * 397) ^ GitLastCommitShaColumn.GetHashCode();

			return hashCode;
		}

		public string ToDynamicColumnsJson()
		{
			if (DynamicColumns.Count == 0)
				return string.Empty;

			try
			{
				return System.Text.Json.JsonSerializer.Serialize(DynamicColumns.ToList(), Utils.Serialization.AppJsonSerializerContext.Default.ListDynamicColumnDefinition);
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "Failed to serialize dynamic columns");
				return string.Empty;
			}
		}

		public void LoadDynamicColumnsFromJson(string? json)
		{
			DynamicColumns.Clear();
			if (string.IsNullOrWhiteSpace(json))
				return;

			try
			{
				var list = System.Text.Json.JsonSerializer.Deserialize(json, Utils.Serialization.AppJsonSerializerContext.Default.ListDynamicColumnDefinition);
				if (list is not null)
				{
					foreach (var item in list)
					{
						item.UserLength = new GridLength(item.UserLengthPixels > 0 ? item.UserLengthPixels : 140, GridUnitType.Pixel);
						DynamicColumns.Add(item);
					}
				}
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "Failed to deserialize dynamic columns");
			}
		}
	}
}
