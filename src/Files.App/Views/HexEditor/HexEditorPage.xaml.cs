// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Globalization;
using System.IO;
using Files.App.Helpers;
using Files.App.ViewModels.HexEditor;
using Files.Shared.Helpers;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;
using WinRT;

namespace Files.App.Views.HexEditor
{
	public sealed partial class HexEditorPage : Page
	{
		private readonly string _filePath;
		private readonly Window _window;
		private bool _isInitialized;

		public HexEditorViewModel ViewModel { get; } = new();

		public HexEditorPage(string filePath, Window window)
		{
			InitializeComponent();
			_filePath = filePath;
			_window = window;
		}

		private async void Page_Loaded(object sender, RoutedEventArgs e)
		{
			if (_isInitialized)
				return;

			_isInitialized = true;

			// Check for pending recovery journal from previous abnormal termination (F060 M2 / D-F060-1)
			if (HexFileSaver.HasPendingJournal(_filePath))
			{
				var journalDialog = new ContentDialog
				{
					Title = Strings.HexEditorPendingJournalTitle.GetLocalizedResource(),
					Content = Strings.HexEditorPendingJournalMessage.GetLocalizedResource(),
					PrimaryButtonText = Strings.HexEditorRestore.GetLocalizedResource(),
					SecondaryButtonText = Strings.HexEditorDiscard.GetLocalizedResource(),
					DefaultButton = ContentDialogButton.Primary,
					XamlRoot = this.XamlRoot
				};

				var result = await journalDialog.ShowAsync();
				if (result == ContentDialogResult.Primary)
				{
					await Task.Run(() => HexFileSaver.Recover(_filePath));
				}
				else
				{
					string journalPath = HexFileSaver.GetDefaultJournalPath(_filePath);
					if (File.Exists(journalPath))
					{
						try
						{
							File.Delete(journalPath);
						}
						catch
						{
							// Ignore failure deleting journal
						}
					}
				}
			}

			await ViewModel.InitializeAsync(_filePath);

			UpdateScrollBarProperties();
		}

		private void UpdateScrollBarProperties()
		{
			long maxTop = Math.Max(0, ViewModel.TotalRows - ViewModel.VisibleRowCount);
			RowScrollBar.Maximum = maxTop;
			RowScrollBar.LargeChange = Math.Max(1, ViewModel.VisibleRowCount - 1);
		}

		private void ScrollBar_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
		{
			ViewModel.ScrollToRow((long)e.NewValue);
		}

		private void Grid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
		{
			var properties = e.GetCurrentPoint(null).Properties;
			int delta = properties.MouseWheelDelta;

			if (delta > 0)
			{
				RowScrollBar.Value = Math.Max(0, RowScrollBar.Value - 3);
			}
			else if (delta < 0)
			{
				RowScrollBar.Value = Math.Min(RowScrollBar.Maximum, RowScrollBar.Value + 3);
			}

			e.Handled = true;
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void Cell_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.DataContext is HexByteCellViewModel cell && cell.HasByte)
			{
				ViewModel.SelectOffset(cell.Offset);
				this.Focus(FocusState.Programmatic);
			}
		}

		private async void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			await SaveAsync();
		}

		private void UndoButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Undo();
		}

		private void RedoButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Redo();
		}

		private async void GoToOffsetButton_Click(object sender, RoutedEventArgs e)
		{
			await ShowGoToOffsetDialogAsync();
		}

		public async Task<bool> SaveAsync()
		{
			var result = await ViewModel.SaveAsync();
			if (result.Success)
			{
				ViewModel.StatusMessage = Strings.HexEditorSaveSuccess.GetLocalizedResource();
				return true;
			}

			string errorMsg = result.Status switch
			{
				HexSaveStatus.FileIdentityChanged => Strings.HexEditorSaveIdentityChanged.GetLocalizedResource(),
				HexSaveStatus.ReadOnly => Strings.HexEditorSaveReadOnly.GetLocalizedResource(),
				HexSaveStatus.FileInUse => Strings.HexEditorSaveFileInUse.GetLocalizedResource(),
				HexSaveStatus.DiskFullOrIoError => Strings.HexEditorSaveDiskFull.GetLocalizedResource(),
				HexSaveStatus.TargetNotFound => Strings.HexEditorSaveNotFound.GetLocalizedResource(),
				_ => Strings.HexEditorSaveFailed.GetLocalizedResource()
			};

			ViewModel.StatusMessage = errorMsg;

			var errorDialog = new ContentDialog
			{
				Title = Strings.HexEditor.GetLocalizedResource(),
				Content = errorMsg,
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = this.XamlRoot
			};

			await errorDialog.ShowAsync();
			return false;
		}

		private async Task ShowGoToOffsetDialogAsync()
		{
			var inputTextBox = new TextBox
			{
				PlaceholderText = "e.g. 1048576 or 0x1000",
				Text = $"0x{ViewModel.CurrentOffset:X8}"
			};
			inputTextBox.SelectAll();

			var contentPanel = new StackPanel { Spacing = 8 };
			contentPanel.Children.Add(new TextBlock { Text = Strings.HexEditorGoToOffsetPrompt.GetLocalizedResource() });
			contentPanel.Children.Add(inputTextBox);

			var dialog = new ContentDialog
			{
				Title = Strings.HexEditorGoToOffsetTitle.GetLocalizedResource(),
				Content = contentPanel,
				PrimaryButtonText = Strings.HexEditorGo.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary,
				XamlRoot = this.XamlRoot
			};

			var result = await dialog.ShowAsync();
			if (result == ContentDialogResult.Primary)
			{
				string text = inputTextBox.Text.Trim();
				if (TryParseOffset(text, out long offset))
				{
					ViewModel.GoToOffset(offset);
					RowScrollBar.Value = ViewModel.CurrentTopRow;
				}
			}
		}

		private static bool TryParseOffset(string text, out long offset)
		{
			offset = 0;
			if (string.IsNullOrWhiteSpace(text))
				return false;

			if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				return long.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
			}

			if (text.EndsWith("h", StringComparison.OrdinalIgnoreCase))
			{
				return long.TryParse(text.AsSpan(0, text.Length - 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
			}

			if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out offset))
			{
				return true;
			}

			return long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
		}

		private async void Page_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			bool isCtrlDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

			if (isCtrlDown)
			{
				switch (e.Key)
				{
					case VirtualKey.S:
						e.Handled = true;
						await SaveAsync();
						return;

					case VirtualKey.Z:
						e.Handled = true;
						ViewModel.Undo();
						return;

					case VirtualKey.Y:
						e.Handled = true;
						ViewModel.Redo();
						return;

					case VirtualKey.G:
						e.Handled = true;
						await ShowGoToOffsetDialogAsync();
						return;

					case VirtualKey.V:
						e.Handled = true;
						await HandlePasteAsync();
						return;
				}
			}

			switch (e.Key)
			{
				case VirtualKey.Up:
					e.Handled = true;
					ViewModel.MoveCursor(-1, 0);
					RowScrollBar.Value = ViewModel.CurrentTopRow;
					return;

				case VirtualKey.Down:
					e.Handled = true;
					ViewModel.MoveCursor(1, 0);
					RowScrollBar.Value = ViewModel.CurrentTopRow;
					return;

				case VirtualKey.Left:
					e.Handled = true;
					ViewModel.MoveCursor(0, -1);
					RowScrollBar.Value = ViewModel.CurrentTopRow;
					return;

				case VirtualKey.Right:
					e.Handled = true;
					ViewModel.MoveCursor(0, 1);
					RowScrollBar.Value = ViewModel.CurrentTopRow;
					return;

				case VirtualKey.PageUp:
					e.Handled = true;
					ViewModel.PageUp();
					RowScrollBar.Value = ViewModel.CurrentTopRow;
					return;

				case VirtualKey.PageDown:
					e.Handled = true;
					ViewModel.PageDown();
					RowScrollBar.Value = ViewModel.CurrentTopRow;
					return;

				case VirtualKey.Home:
					e.Handled = true;
					ViewModel.Home();
					RowScrollBar.Value = ViewModel.CurrentTopRow;
					return;

				case VirtualKey.End:
					e.Handled = true;
					ViewModel.End();
					RowScrollBar.Value = ViewModel.CurrentTopRow;
					return;

				case VirtualKey.Back:
				case VirtualKey.Escape:
					e.Handled = true;
					ViewModel.CancelPendingEdit();
					return;
			}

			// Hex character typing (0-9, A-F)
			char? hexChar = GetHexCharFromKey(e.Key);
			if (hexChar.HasValue)
			{
				e.Handled = true;
				if (ViewModel.ProcessHexChar(hexChar.Value))
				{
					RowScrollBar.Value = ViewModel.CurrentTopRow;
				}
			}
		}

		private static char? GetHexCharFromKey(VirtualKey key)
		{
			if (key >= VirtualKey.Number0 && key <= VirtualKey.Number9)
				return (char)('0' + (key - VirtualKey.Number0));

			if (key >= VirtualKey.NumberPad0 && key <= VirtualKey.NumberPad9)
				return (char)('0' + (key - VirtualKey.NumberPad0));

			if (key >= VirtualKey.A && key <= VirtualKey.F)
				return (char)('A' + (key - VirtualKey.A));

			return null;
		}

		private async Task HandlePasteAsync()
		{
			if (ViewModel.Buffer is null)
				return;

			var dataPackageView = Clipboard.GetContent();
			if (!dataPackageView.Contains(StandardDataFormats.Text))
				return;

			string text = await dataPackageView.GetTextAsync();
			if (string.IsNullOrEmpty(text))
				return;

			if (!HexPasteLimit.TryParseHexBytes(text, out byte[]? bytes, out string? errorMessage))
			{
				if (!HexPasteLimit.TryGetRawTextBytes(text, out bytes, out errorMessage))
				{
					var pasteErrorDialog = new ContentDialog
					{
						Title = Strings.HexEditorPasteError.GetLocalizedResource(),
						Content = errorMessage ?? "Invalid data to paste.",
						PrimaryButtonText = Strings.OK.GetLocalizedResource(),
						DefaultButton = ContentDialogButton.Primary,
						XamlRoot = this.XamlRoot
					};
					await pasteErrorDialog.ShowAsync();
					return;
				}
			}

			if (bytes is not null && bytes.Length > 0)
			{
				long availableBytes = ViewModel.Buffer.Length - ViewModel.CurrentOffset;
				int bytesToPaste = (int)Math.Min(bytes.Length, availableBytes);

				if (bytesToPaste > 0)
				{
					ViewModel.Buffer.SetBytes(ViewModel.CurrentOffset, bytes.AsSpan(0, bytesToPaste));
					ViewModel.SelectOffset(Math.Min(ViewModel.Buffer.Length - 1, ViewModel.CurrentOffset + bytesToPaste));
					ViewModel.RefreshVisibleRows();
					RowScrollBar.Value = ViewModel.CurrentTopRow;
				}
			}
		}
	}
}
