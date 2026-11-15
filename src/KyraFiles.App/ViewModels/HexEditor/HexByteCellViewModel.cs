// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace KyraFiles.App.ViewModels.HexEditor
{
	/// <summary>
	/// Represents a single byte cell in the hex editor grid (F060 M2 / M2-AC-1..3).
	/// </summary>
	public sealed partial class HexByteCellViewModel : ObservableObject
	{
		private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);
		private static readonly SolidColorBrush SelectedBackgroundBrush = new(Color.FromArgb(255, 0, 120, 215)); // WinUI accent blue
		private static readonly SolidColorBrush SelectedForegroundBrush = new(Colors.White);
		private static readonly SolidColorBrush ModifiedBackgroundBrush = new(Color.FromArgb(50, 255, 140, 0)); // Subtle amber
		private static readonly SolidColorBrush ModifiedForegroundBrush = new(Color.FromArgb(255, 255, 140, 0)); // Amber text
		private static readonly SolidColorBrush NormalForegroundBrush = new(Color.FromArgb(255, 220, 220, 220));

		[ObservableProperty] public partial long Offset { get; set; }
		[ObservableProperty] public partial int ColumnIndex { get; set; }
		[ObservableProperty] public partial byte ByteValue { get; set; }
		[ObservableProperty] public partial string HexText { get; set; } = string.Empty;
		[ObservableProperty] public partial char AsciiChar { get; set; } = ' ';
		[ObservableProperty] public partial bool IsModified { get; set; }
		[ObservableProperty] public partial bool IsSelected { get; set; }
		[ObservableProperty] public partial bool HasByte { get; set; }

		public Brush BackgroundBrush =>
			IsSelected ? SelectedBackgroundBrush : (IsModified ? ModifiedBackgroundBrush : TransparentBrush);

		public Brush ForegroundBrush =>
			IsSelected ? SelectedForegroundBrush : (IsModified ? ModifiedForegroundBrush : NormalForegroundBrush);

		public Brush TextBackgroundBrush =>
			IsSelected ? SelectedBackgroundBrush : (IsModified ? ModifiedBackgroundBrush : TransparentBrush);

		public Brush TextForegroundBrush =>
			IsSelected ? SelectedForegroundBrush : (IsModified ? ModifiedForegroundBrush : NormalForegroundBrush);

		public double RightMargin => ColumnIndex == 7 ? 10.0 : 4.0;

		partial void OnIsSelectedChanged(bool value)
		{
			OnPropertyChanged(nameof(BackgroundBrush));
			OnPropertyChanged(nameof(ForegroundBrush));
			OnPropertyChanged(nameof(TextBackgroundBrush));
			OnPropertyChanged(nameof(TextForegroundBrush));
		}

		partial void OnIsModifiedChanged(bool value)
		{
			OnPropertyChanged(nameof(BackgroundBrush));
			OnPropertyChanged(nameof(ForegroundBrush));
			OnPropertyChanged(nameof(TextBackgroundBrush));
			OnPropertyChanged(nameof(TextForegroundBrush));
		}

		partial void OnColumnIndexChanged(int value)
		{
			OnPropertyChanged(nameof(RightMargin));
		}
	}
}
