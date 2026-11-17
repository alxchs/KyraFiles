// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

using KyraFiles.App.Data.Contracts;
using KyraFiles.App.Helpers;
using KyraFiles.App.Services.PreviewPopupProviders;
using KyraFiles.App.ViewModels.Previews;
using KyraFiles.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage.Streams;
using Windows.System;

namespace KyraFiles.App.UserControls
{
	public sealed partial class QuickLookOverlay : UserControl
	{
		private CancellationTokenSource? loadCts;
		private string? currentPath;

		public bool IsShowing { get; private set; }

		public QuickLookOverlay()
		{
			InitializeComponent();
			Loaded += QuickLookOverlay_Loaded;
			Unloaded += QuickLookOverlay_Unloaded;
		}

		private void QuickLookOverlay_Loaded(object sender, RoutedEventArgs e)
		{
			BuiltInPreviewPopupProvider.Instance.RegisterOverlay(this);
		}

		private void QuickLookOverlay_Unloaded(object sender, RoutedEventArgs e)
		{
			BuiltInPreviewPopupProvider.Instance.UnregisterOverlay(this);
			Close();
		}

		public void Close()
		{
			loadCts?.Cancel();
			loadCts?.Dispose();
			loadCts = null;

			currentPath = null;
			IsShowing = false;
			Visibility = Visibility.Collapsed;

			ContentHost.Child = null;
			HeaderIcon.Source = null;
			HeaderTitle.Text = string.Empty;
			HeaderDetails.Text = string.Empty;
			LoadingRing.IsActive = false;
			LoadingRing.Visibility = Visibility.Collapsed;

			// Restore focus to active file list
			var context = Ioc.Default.GetRequiredService<IContentPageContext>();
			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.FocusFileList();
		}

		public async Task ShowAsync(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			loadCts?.Cancel();
			loadCts?.Dispose();
			loadCts = new CancellationTokenSource();
			var token = loadCts.Token;

			currentPath = path;
			IsShowing = true;
			Visibility = Visibility.Visible;
			ContentHost.Child = null;
			LoadingRing.IsActive = true;
			LoadingRing.Visibility = Visibility.Visible;

			// Make sure overlay is focusable to capture Esc, Space, and Arrow keys
			Focus(FocusState.Programmatic);

			try
			{
				bool isDirectory = Directory.Exists(path);
				bool isFile = File.Exists(path);

				if (!isDirectory && !isFile)
				{
					ShowNotFound(path);
					return;
				}

				var name = Path.GetFileName(path);
				if (string.IsNullOrEmpty(name))
					name = path;

				HeaderTitle.Text = name;

				// Load icon
				_ = LoadHeaderIconAsync(path, isDirectory, token);

				if (isDirectory)
				{
					await ShowDirectoryPreviewAsync(path, token);
				}
				else
				{
					await ShowFilePreviewAsync(path, token);
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "QuickLook preview failed for {Path}", path);
				ShowFallbackCard(path, "Erro ao carregar prévia: " + ex.Message);
			}
			finally
			{
				if (!token.IsCancellationRequested)
				{
					LoadingRing.IsActive = false;
					LoadingRing.Visibility = Visibility.Collapsed;
				}
			}
		}

		private async Task LoadHeaderIconAsync(string path, bool isDirectory, CancellationToken token)
		{
			try
			{
				var iconResult = await FileThumbnailHelper.GetIconAsync(
					path,
					Constants.ShellIconSizes.Small,
					isDirectory,
					IconOptions.None);

				if (iconResult is not null && !token.IsCancellationRequested)
				{
					var bmp = await iconResult.ToBitmapAsync();
					if (!token.IsCancellationRequested)
						HeaderIcon.Source = bmp;
				}
			}
			catch
			{
				// Ignore icon load failures
			}
		}

		private async Task ShowDirectoryPreviewAsync(string path, CancellationToken token)
		{
			var dirInfo = new DirectoryInfo(path);
			var modified = dirInfo.LastWriteTime.ToString("g");
			HeaderDetails.Text = $"Pasta • Modificado: {modified}";

			var countText = new TextBlock
			{
				Text = "Calculando itens e tamanho...",
				Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
				HorizontalAlignment = HorizontalAlignment.Center
			};

			var stack = new StackPanel
			{
				Orientation = Orientation.Vertical,
				Spacing = 16,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			var folderIcon = new FontIcon
			{
				Glyph = "\uE8B7",
				FontSize = 64,
				HorizontalAlignment = HorizontalAlignment.Center,
				Foreground = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
			};

			stack.Children.Add(folderIcon);
			stack.Children.Add(countText);
			ContentHost.Child = stack;

			// AC-5: Background item count and size calculation, non-blocking and cancelable
			_ = Task.Run(() =>
			{
				long totalSize = 0;
				long fileCount = 0;
				long folderCount = 0;

				try
				{
					var options = new EnumerationOptions
					{
						IgnoreInaccessible = true,
						RecurseSubdirectories = true,
						AttributesToSkip = FileAttributes.ReparsePoint
					};

					foreach (var file in dirInfo.EnumerateFiles("*", options))
					{
						if (token.IsCancellationRequested)
							return;

						totalSize += file.Length;
						fileCount++;
					}

					foreach (var dir in dirInfo.EnumerateDirectories("*", options))
					{
						if (token.IsCancellationRequested)
							return;

						folderCount++;
					}

					if (!token.IsCancellationRequested)
					{
						MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
						{
							if (!token.IsCancellationRequested)
							{
								countText.Text = $"{fileCount} arquivo(s), {folderCount} pasta(s)\nTamanho total: {totalSize.ToSizeString()}";
								HeaderDetails.Text = $"Pasta • {totalSize.ToSizeString()} • Modificado: {modified}";
							}
						});
					}
				}
				catch (Exception ex)
				{
					if (!token.IsCancellationRequested)
					{
						MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
						{
							countText.Text = $"Resumo da pasta (acesso parcial):\n{ex.Message}";
						});
					}
				}
			}, token);

			await Task.CompletedTask;
		}

		private async Task ShowFilePreviewAsync(string path, CancellationToken token)
		{
			var fileInfo = new FileInfo(path);
			var sizeStr = fileInfo.Length.ToSizeString();
			var modified = fileInfo.LastWriteTime.ToString("g");
			var ext = fileInfo.Extension.ToLowerInvariant();

			HeaderDetails.Text = $"{sizeStr} • Modificado: {modified}";

			// Check if file is locked or in use (AC-4)
			bool isLocked = false;
			try
			{
				using var testStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			}
			catch (IOException)
			{
				isLocked = true;
			}
			catch (UnauthorizedAccessException)
			{
				isLocked = true;
			}

			if (isLocked)
			{
				ShowFallbackCard(path, "Arquivo bloqueado ou em uso por outro processo", isLocked: true);
				return;
			}

			// FR-001: Image files
			if (FileExtensionHelpers.IsImagePreviewFile(ext) || FileExtensionHelpers.IsImageFile(ext))
			{
				await ShowImagePreviewAsync(path, fileInfo, token);
				return;
			}

			// FR-001: Text and code files
			if (FileExtensionHelpers.IsTextFile(ext) || CodePreviewViewModel.IsCodeFile(ext) || ext is ".json" or ".xml" or ".yaml" or ".yml" or ".ps1" or ".bat" or ".cmd" or ".sh" or ".ini" or ".config" or ".xaml")
			{
				await ShowTextPreviewAsync(path, fileInfo, token);
				return;
			}

			// FR-001: Markdown files
			if (FileExtensionHelpers.IsMarkdownFile(ext))
			{
				await ShowMarkdownPreviewAsync(path, fileInfo, token);
				return;
			}

			// FR-001: Audio/Video files
			if (FileExtensionHelpers.IsAudioFile(ext) || FileExtensionHelpers.IsVideoFile(ext))
			{
				await ShowMediaPreviewAsync(path, fileInfo, token);
				return;
			}

			// AC-4: Unsupported binary files
			ShowFallbackCard(path, "Visualização prévia não disponível para este formato binário");
		}

		private async Task ShowImagePreviewAsync(string path, FileInfo fileInfo, CancellationToken token)
		{
			// Read bytes into memory with FileShare.ReadWrite | FileShare.Delete to avoid locking (FR-002)
			byte[] bytes;
			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				using var ms = new MemoryStream();
				await stream.CopyToAsync(ms, token);
				bytes = ms.ToArray();
			}

			if (token.IsCancellationRequested)
				return;

			var randomStream = new InMemoryRandomAccessStream();
			using (var writer = new DataWriter(randomStream.GetOutputStreamAt(0)))
			{
				writer.WriteBytes(bytes);
				await writer.StoreAsync();
			}

			var bitmap = new BitmapImage();
			await bitmap.SetSourceAsync(randomStream);

			if (token.IsCancellationRequested)
				return;

			var img = new Image
			{
				Source = bitmap,
				Stretch = Stretch.Uniform,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			var viewer = new ScrollViewer
			{
				Content = img,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center
			};

			ContentHost.Child = viewer;

			// Update header with dimensions
			var dims = $"{bitmap.PixelWidth} × {bitmap.PixelHeight}";
			HeaderDetails.Text = $"{dims} • {fileInfo.Length.ToSizeString()} • Modificado: {fileInfo.LastWriteTime:g}";
		}

		private async Task ShowTextPreviewAsync(string path, FileInfo fileInfo, CancellationToken token)
		{
			string text;
			bool isTruncated = false;
			const int maxBytes = 256 * 1024; // 256 KB max for instant loading (NFR-001)

			using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				int bytesToRead = (int)Math.Min(stream.Length, maxBytes);
				var buffer = new byte[bytesToRead];
				int read = await stream.ReadAsync(buffer.AsMemory(0, bytesToRead), token);

				using var reader = new StreamReader(new MemoryStream(buffer, 0, read), System.Text.Encoding.Default, detectEncodingFromByteOrderMarks: true);
				text = await reader.ReadToEndAsync(token);

				if (stream.Length > maxBytes)
					isTruncated = true;
			}

			if (isTruncated)
				text += "\n\n--- [Prévia truncada em 256 KB para manter alta performance] ---";

			if (token.IsCancellationRequested)
				return;

			var textBox = new TextBox
			{
				Text = text,
				IsReadOnly = true,
				AcceptsReturn = true,
				TextWrapping = TextWrapping.Wrap,
				FontFamily = new FontFamily("Consolas, Courier New, monospace"),
				FontSize = 13,
				Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
				BorderThickness = new Thickness(0),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			var scroll = new ScrollViewer
			{
				Content = textBox,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			ContentHost.Child = scroll;
		}

		private async Task ShowMarkdownPreviewAsync(string path, FileInfo fileInfo, CancellationToken token)
		{
			// Read text safely
			await ShowTextPreviewAsync(path, fileInfo, token);
		}

		private async Task ShowMediaPreviewAsync(string path, FileInfo fileInfo, CancellationToken token)
		{
			var mediaPlayer = new MediaPlayerElement
			{
				Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(path)),
				AreTransportControlsEnabled = true,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};

			ContentHost.Child = mediaPlayer;
			await Task.CompletedTask;
		}

		private void ShowFallbackCard(string path, string reason, bool isLocked = false)
		{
			var fileInfo = new FileInfo(path);
			var name = Path.GetFileName(path);
			var ext = fileInfo.Extension;
			var size = File.Exists(path) ? fileInfo.Length.ToSizeString() : string.Empty;

			var stack = new StackPanel
			{
				Orientation = Orientation.Vertical,
				Spacing = 14,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			var icon = new FontIcon
			{
				Glyph = isLocked ? "\uE72E" : "\uE7C3",
				FontSize = 56,
				HorizontalAlignment = HorizontalAlignment.Center,
				Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
			};

			var title = new TextBlock
			{
				Text = name,
				Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
				HorizontalAlignment = HorizontalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis,
				MaxWidth = 450
			};

			var subtitle = new TextBlock
			{
				Text = $"{ext} • {size}",
				Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
				Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
				HorizontalAlignment = HorizontalAlignment.Center
			};

			var reasonBorder = new Border
			{
				Background = (Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
				BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(12, 8, 12, 8),
				HorizontalAlignment = HorizontalAlignment.Center,
				Child = new TextBlock
				{
					Text = reason,
					Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
					Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
					TextWrapping = TextWrapping.Wrap,
					MaxWidth = 400
				}
			};

			stack.Children.Add(icon);
			stack.Children.Add(title);
			stack.Children.Add(subtitle);
			stack.Children.Add(reasonBorder);

			ContentHost.Child = stack;
		}

		private void ShowNotFound(string path)
		{
			HeaderTitle.Text = Path.GetFileName(path);
			HeaderDetails.Text = "Item não encontrado";
			ShowFallbackCard(path, "O arquivo ou pasta selecionado não existe mais no disco.");
		}

		private void Backdrop_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			e.Handled = true;
			Close();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Overlay_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Escape or VirtualKey.Space)
			{
				e.Handled = true;
				Close();
			}
			else if (e.Key is VirtualKey.Down)
			{
				e.Handled = true;
				NavigateNext();
			}
			else if (e.Key is VirtualKey.Up)
			{
				e.Handled = true;
				NavigatePrevious();
			}
		}

		private void NavigateNext()
		{
			var context = Ioc.Default.GetRequiredService<IContentPageContext>();
			var shellPage = context.ShellPage;
			if (shellPage?.ShellViewModel?.FilesAndFolders is { } items && items.Count > 0)
			{
				var current = shellPage.SlimContentPage?.SelectedItem;
				var idx = current is not null ? items.IndexOf(current) : -1;
				if (idx >= 0 && idx < items.Count - 1)
				{
					var next = items[idx + 1];
					shellPage.SlimContentPage?.ItemManipulationModel.SetSelectedItem(next);
					shellPage.SlimContentPage?.ItemManipulationModel.ScrollIntoView(next);
				}
				else if (idx == -1 && items.Count > 0)
				{
					var first = items[0];
					shellPage.SlimContentPage?.ItemManipulationModel.SetSelectedItem(first);
					shellPage.SlimContentPage?.ItemManipulationModel.ScrollIntoView(first);
				}
			}
		}

		private void NavigatePrevious()
		{
			var context = Ioc.Default.GetRequiredService<IContentPageContext>();
			var shellPage = context.ShellPage;
			if (shellPage?.ShellViewModel?.FilesAndFolders is { } items && items.Count > 0)
			{
				var current = shellPage.SlimContentPage?.SelectedItem;
				var idx = current is not null ? items.IndexOf(current) : -1;
				if (idx > 0)
				{
					var prev = items[idx - 1];
					shellPage.SlimContentPage?.ItemManipulationModel.SetSelectedItem(prev);
					shellPage.SlimContentPage?.ItemManipulationModel.ScrollIntoView(prev);
				}
			}
		}
	}
}
