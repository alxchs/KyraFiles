// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using KyraFiles.Shared.Helpers;

namespace KyraFiles.App.ViewModels.HexEditor
{
	/// <summary>
	/// ViewModel for the Hex Editor window and virtualized grid (F060 M2 / M2-AC-1..3).
	/// Maintains constant memory usage regardless of file size by materializing only visible rows.
	/// </summary>
	public sealed partial class HexEditorViewModel : ObservableObject, IDisposable
	{
		public const int DefaultVisibleRowCount = 30;
		public const int BytesPerRow = 16;

		private HexFileBuffer? _buffer;
		private readonly HexNibbleEditor _nibbleEditor = new();
		private readonly object _lock = new();

		[ObservableProperty] public partial string FilePath { get; set; } = string.Empty;
		[ObservableProperty] public partial string FileName { get; set; } = string.Empty;
		[ObservableProperty] public partial long FileLength { get; set; }
		[ObservableProperty] public partial long TotalRows { get; set; }
		[ObservableProperty] public partial int VisibleRowCount { get; set; } = DefaultVisibleRowCount;
		[ObservableProperty] public partial long CurrentTopRow { get; set; }
		[ObservableProperty] public partial long CurrentOffset { get; set; }
		[ObservableProperty] public partial bool HasUnsavedChanges { get; set; }
		[ObservableProperty] public partial int ModifiedBytesCount { get; set; }
		[ObservableProperty] public partial bool CanUndo { get; set; }
		[ObservableProperty] public partial bool CanRedo { get; set; }
		[ObservableProperty] public partial string OffsetStatusText { get; set; } = "Offset: 0x00000000 (0)";
		[ObservableProperty] public partial string SizeStatusText { get; set; } = "Size: 0 bytes";
		[ObservableProperty] public partial string ModifiedStatusText { get; set; } = "Modified: 0 bytes";
		[ObservableProperty] public partial string StatusMessage { get; set; } = string.Empty;
		[ObservableProperty] public partial bool IsBusy { get; set; }

		public ObservableCollection<HexRowViewModel> VisibleRows { get; } = new();

		public HexFileBuffer? Buffer => _buffer;

		public async Task InitializeAsync(string targetFilePath, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(targetFilePath);

			IsBusy = true;
			try
			{
				FilePath = targetFilePath;
				FileName = Path.GetFileName(targetFilePath);

				_buffer = await Task.Run(() => new HexFileBuffer(targetFilePath), cancellationToken);

				FileLength = _buffer.Length;
				TotalRows = FileLength > 0 ? (FileLength + BytesPerRow - 1) / BytesPerRow : 1;

				SizeStatusText = $"Size: {FormatSize(FileLength)} ({FileLength:N0} bytes)";
				CurrentOffset = 0;
				CurrentTopRow = 0;

				EnsureVisibleRowsAllocated();
				RefreshVisibleRows();
				UpdateStatus();
			}
			finally
			{
				IsBusy = false;
			}
		}

		public void InitializeWithBuffer(HexFileBuffer buffer)
		{
			ArgumentNullException.ThrowIfNull(buffer);

			_buffer = buffer;
			FilePath = buffer.FilePath ?? string.Empty;
			FileName = Path.GetFileName(FilePath);
			FileLength = buffer.Length;
			TotalRows = FileLength > 0 ? (FileLength + BytesPerRow - 1) / BytesPerRow : 1;

			SizeStatusText = $"Size: {FormatSize(FileLength)} ({FileLength:N0} bytes)";
			CurrentOffset = 0;
			CurrentTopRow = 0;

			EnsureVisibleRowsAllocated();
			RefreshVisibleRows();
			UpdateStatus();
		}

		private void EnsureVisibleRowsAllocated()
		{
			if (VisibleRows.Count == VisibleRowCount)
				return;

			VisibleRows.Clear();
			for (int r = 0; r < VisibleRowCount; r++)
			{
				VisibleRows.Add(new HexRowViewModel());
			}
		}

		public void ScrollToRow(long topRow)
		{
			long maxTop = Math.Max(0, TotalRows - VisibleRowCount);
			long clamped = Math.Clamp(topRow, 0, maxTop);

			if (clamped != CurrentTopRow)
			{
				CurrentTopRow = clamped;
				RefreshVisibleRows();
			}
		}

		public void SelectOffset(long offset)
		{
			if (_buffer is null || _buffer.Length == 0)
				return;

			CancelPendingEdit();

			CurrentOffset = Math.Clamp(offset, 0, _buffer.Length - 1);
			EnsureOffsetVisible(CurrentOffset);
			RefreshVisibleRows();
			UpdateStatus();
		}

		public void EnsureOffsetVisible(long offset)
		{
			long targetRow = offset / BytesPerRow;
			if (targetRow < CurrentTopRow)
			{
				ScrollToRow(targetRow);
			}
			else if (targetRow >= CurrentTopRow + VisibleRowCount)
			{
				ScrollToRow(targetRow - VisibleRowCount + 1);
			}
		}

		public void MoveCursor(int deltaRows, int deltaCols)
		{
			if (_buffer is null || _buffer.Length == 0)
				return;

			long row = CurrentOffset / BytesPerRow;
			long col = CurrentOffset % BytesPerRow;

			long newRow = row + deltaRows;
			long newCol = col + deltaCols;

			if (newCol < 0)
			{
				newCol += BytesPerRow;
				newRow--;
			}
			else if (newCol >= BytesPerRow)
			{
				newCol -= BytesPerRow;
				newRow++;
			}

			long newOffset = (newRow * BytesPerRow) + newCol;
			SelectOffset(newOffset);
		}

		public void PageUp()
		{
			MoveCursor(-(VisibleRowCount - 1), 0);
		}

		public void PageDown()
		{
			MoveCursor(VisibleRowCount - 1, 0);
		}

		public void Home()
		{
			if (_buffer is null)
				return;

			long rowStart = (CurrentOffset / BytesPerRow) * BytesPerRow;
			SelectOffset(rowStart);
		}

		public void End()
		{
			if (_buffer is null)
				return;

			long rowStart = (CurrentOffset / BytesPerRow) * BytesPerRow;
			long rowEnd = Math.Min(_buffer.Length - 1, rowStart + BytesPerRow - 1);
			SelectOffset(rowEnd);
		}

		public void GoToOffset(long offset)
		{
			if (_buffer is null)
				return;

			SelectOffset(offset);
		}

		public bool ProcessHexChar(char c)
		{
			if (_buffer is null || _buffer.Length == 0)
				return false;

			if (!_nibbleEditor.ProcessKey(c, out byte? completedByte))
				return false;

			if (completedByte.HasValue)
			{
				_buffer.SetByte(CurrentOffset, completedByte.Value);
				StatusMessage = string.Empty;

				if (CurrentOffset + 1 < _buffer.Length)
				{
					CurrentOffset++;
					EnsureOffsetVisible(CurrentOffset);
				}

				RefreshVisibleRows();
				UpdateStatus();
			}
			else if (_nibbleEditor.HasPendingNibble)
			{
				UpdateSelectedCellPendingNibble();
			}

			return true;
		}

		private void UpdateSelectedCellPendingNibble()
		{
			long selectedRow = CurrentOffset / BytesPerRow;
			int selectedCol = (int)(CurrentOffset % BytesPerRow);

			int visibleRowIndex = (int)(selectedRow - CurrentTopRow);
			if (visibleRowIndex >= 0 && visibleRowIndex < VisibleRows.Count)
			{
				var rowVm = VisibleRows[visibleRowIndex];
				var cell = rowVm.Cells[selectedCol];
				cell.HexText = $"{_nibbleEditor.PendingChar}_";
			}
		}

		public void CancelPendingEdit()
		{
			if (_nibbleEditor.HasPendingNibble)
			{
				_nibbleEditor.Cancel();
				RefreshVisibleRows();
			}
		}

		public bool Undo()
		{
			if (_buffer is null)
				return false;

			CancelPendingEdit();
			bool undone = _buffer.Undo();
			if (undone)
			{
				RefreshVisibleRows();
				UpdateStatus();
			}
			return undone;
		}

		public bool Redo()
		{
			if (_buffer is null)
				return false;

			CancelPendingEdit();
			bool redone = _buffer.Redo();
			if (redone)
			{
				RefreshVisibleRows();
				UpdateStatus();
			}
			return redone;
		}

		public async Task<HexSaveResult> SaveAsync(CancellationToken cancellationToken = default)
		{
			if (_buffer is null)
			{
				return new HexSaveResult
				{
					Success = false,
					Status = HexSaveStatus.Failed,
					ErrorMessage = "Buffer is not initialized."
				};
			}

			CancelPendingEdit();
			IsBusy = true;
			try
			{
				var result = await HexFileSaver.SaveAsync(_buffer, cancellationToken: cancellationToken);
				RefreshVisibleRows();
				UpdateStatus();
				return result;
			}
			finally
			{
				IsBusy = false;
			}
		}

		public void RefreshVisibleRows()
		{
			if (_buffer is null)
				return;

			EnsureVisibleRowsAllocated();

			byte[] bufferRow = new byte[BytesPerRow];

			for (int r = 0; r < VisibleRowCount; r++)
			{
				long rowOffset = (CurrentTopRow + r) * BytesPerRow;
				var rowVm = VisibleRows[r];

				if (rowOffset >= _buffer.Length)
				{
					rowVm.RowOffset = rowOffset;
					rowVm.OffsetHex = string.Empty;
					rowVm.AsciiText = string.Empty;

					for (int c = 0; c < BytesPerRow; c++)
					{
						var cell = rowVm.Cells[c];
						cell.Offset = rowOffset + c;
						cell.ColumnIndex = c;
						cell.HasByte = false;
						cell.HexText = string.Empty;
						cell.AsciiChar = ' ';
						cell.IsModified = false;
						cell.IsSelected = false;
					}
					continue;
				}

				int bytesInRow = (int)Math.Min(BytesPerRow, _buffer.Length - rowOffset);
				_buffer.Read(rowOffset, bufferRow.AsSpan(0, bytesInRow));

				rowVm.RowOffset = rowOffset;
				rowVm.OffsetHex = HexRowFormatter.FormatOffset(rowOffset);
				rowVm.AsciiText = HexRowFormatter.FormatAscii(bufferRow.AsSpan(0, bytesInRow));

				for (int c = 0; c < BytesPerRow; c++)
				{
					long cellOffset = rowOffset + c;
					var cell = rowVm.Cells[c];
					cell.Offset = cellOffset;
					cell.ColumnIndex = c;

					if (c < bytesInRow)
					{
						byte b = bufferRow[c];
						cell.HasByte = true;
						cell.ByteValue = b;
						cell.HexText = HexRowFormatter.FormatByte(b);
						cell.AsciiChar = HexRowFormatter.FormatAsciiChar(b);
						cell.IsModified = _buffer.IsByteModified(cellOffset);
						cell.IsSelected = cellOffset == CurrentOffset;
					}
					else
					{
						cell.HasByte = false;
						cell.HexText = string.Empty;
						cell.AsciiChar = ' ';
						cell.IsModified = false;
						cell.IsSelected = false;
					}
				}
			}

			if (_nibbleEditor.HasPendingNibble)
			{
				UpdateSelectedCellPendingNibble();
			}
		}

		private void UpdateStatus()
		{
			if (_buffer is null)
				return;

			HasUnsavedChanges = _buffer.ModifiedBytesCount > 0;
			ModifiedBytesCount = _buffer.ModifiedBytesCount;
			CanUndo = _buffer.CanUndo;
			CanRedo = _buffer.CanRedo;

			OffsetStatusText = $"Offset: 0x{CurrentOffset:X8} ({CurrentOffset:N0})";
			ModifiedStatusText = $"Modified: {ModifiedBytesCount:N0} {(ModifiedBytesCount == 1 ? "byte" : "bytes")}";
		}

		private static string FormatSize(long bytes)
		{
			string[] sizes = ["B", "KB", "MB", "GB", "TB"];
			double len = bytes;
			int order = 0;
			while (len >= 1024 && order < sizes.Length - 1)
			{
				order++;
				len /= 1024;
			}
			return $"{len:0.##} {sizes[order]}";
		}

		public void Dispose()
		{
			_buffer?.Dispose();
			_buffer = null;
		}
	}
}
