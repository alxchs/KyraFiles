// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System.Text.Json.Serialization;

namespace Files.App.Data.Models
{
	[WinRT.GeneratedBindableCustomProperty([nameof(LengthPixels), nameof(LengthIncludingGridSplitterPixels), nameof(Visibility)], [])]
	public sealed partial class DynamicColumnDefinition : ObservableObject
	{
		private const int GRID_SPLITTER_WIDTH = 12;

		public string CanonicalName { get; set; } = string.Empty;

		public string DisplayName { get; set; } = string.Empty;

		public string Category { get; set; } = string.Empty;

		[JsonConverter(typeof(JsonStringEnumConverter<TextAlignment>))]
		public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

		private GridLength userLength = new(140d, GridUnitType.Pixel);
		[JsonIgnore]
		public GridLength UserLength
		{
			get => userLength;
			set
			{
				if (SetProperty(ref userLength, value))
				{
					OnPropertyChanged(nameof(Length));
					OnPropertyChanged(nameof(LengthPixels));
					OnPropertyChanged(nameof(LengthIncludingGridSplitterPixels));
					OnPropertyChanged(nameof(UserLengthPixels));
				}
			}
		}

		public double UserLengthPixels
		{
			get => userLength.Value;
			set => UserLength = new GridLength(value, GridUnitType.Pixel);
		}

		[JsonIgnore]
		public GridLength Length => userLength;

		[JsonIgnore]
		public double LengthPixels => userLength.Value;

		[JsonIgnore]
		public double LengthIncludingGridSplitterPixels => IsVisible ? userLength.Value + GRID_SPLITTER_WIDTH : 0;

		private bool isVisible = true;
		public bool IsVisible
		{
			get => isVisible;
			set
			{
				if (SetProperty(ref isVisible, value))
				{
					OnPropertyChanged(nameof(Visibility));
					OnPropertyChanged(nameof(LengthIncludingGridSplitterPixels));
				}
			}
		}

		[JsonIgnore]
		public Visibility Visibility => isVisible ? Visibility.Visible : Visibility.Collapsed;
	}
}
