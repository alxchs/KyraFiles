// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;

namespace KyraFiles.App.ViewModels.HexEditor
{
	/// <summary>
	/// Represents a row of 16 bytes in the virtualized hex editor grid (F060 M2 / M2-AC-1..3).
	/// </summary>
	public sealed partial class HexRowViewModel : ObservableObject
	{
		[ObservableProperty] public partial long RowOffset { get; set; }
		[ObservableProperty] public partial string OffsetHex { get; set; } = string.Empty;
		[ObservableProperty] public partial HexByteCellViewModel[] Cells { get; set; }
		[ObservableProperty] public partial string AsciiText { get; set; } = string.Empty;

		public HexRowViewModel()
		{
			Cells = new HexByteCellViewModel[16];
			for (int i = 0; i < 16; i++)
			{
				Cells[i] = new HexByteCellViewModel { ColumnIndex = i };
			}
		}
	}
}
