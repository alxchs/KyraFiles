// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.IO;
using KyraFiles.App.Data.Items;
using KyraFiles.App.Helpers;
using KyraFiles.Shared.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace KyraFiles.App.Views.HexEditor
{
	/// <summary>
	/// Standalone window hosting the hex editor (F060 M2 / M2-AC-1..3).
	/// Prompts to save/discard/cancel when closed with unsaved changes.
	/// </summary>
	public sealed partial class HexEditorWindow : WindowEx
	{
		private readonly HexEditorPage _page;
		private readonly string _filePath;
		private bool _forceClose;

		public static HexEditorWindow Open(string filePath)
		{
			ArgumentNullException.ThrowIfNull(filePath);

			var window = new HexEditorWindow(filePath);
			window.AppWindow.Show();
			return window;
		}

		public HexEditorWindow(string filePath) : base(minWidth: 700, minHeight: 450)
		{
			_filePath = filePath;
			_page = new HexEditorPage(filePath, this);

			Content = _page;
			SystemBackdrop = new AppSystemBackdrop(true);

			string fileName = Path.GetFileName(filePath);
			AppWindow.Title = $"{fileName} - {Strings.HexEditor.GetLocalizedResource()}";
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
			AppWindow.SetIcon(AppLifecycleHelper.AppIconPath);

			var width = Math.Max(1, Convert.ToInt32(960 * App.AppModel.AppWindowDPI));
			var height = Math.Max(1, Convert.ToInt32(640 * App.AppModel.AppWindowDPI));
			AppWindow.Resize(new SizeInt32(width, height));

			AppWindow.Closing += AppWindow_Closing;
		}

		private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
		{
			if (_forceClose)
				return;

			if (_page.ViewModel.HasUnsavedChanges)
			{
				args.Cancel = true;

				var dialog = new ContentDialog
				{
					Title = Strings.HexEditorSaveChangesTitle.GetLocalizedResource(),
					Content = string.Format(Strings.HexEditorSaveChangesMessage.GetLocalizedResource(), Path.GetFileName(_filePath)),
					PrimaryButtonText = Strings.Save.GetLocalizedResource(),
					SecondaryButtonText = Strings.HexEditorDiscard.GetLocalizedResource(),
					CloseButtonText = Strings.Cancel.GetLocalizedResource(),
					DefaultButton = ContentDialogButton.Primary,
					XamlRoot = _page.XamlRoot
				};

				var result = await dialog.ShowAsync();
				if (result == ContentDialogResult.Primary)
				{
					bool saved = await _page.SaveAsync();
					if (saved)
					{
						_forceClose = true;
						_page.ViewModel.Dispose();
						AppWindow.Destroy();
					}
				}
				else if (result == ContentDialogResult.Secondary)
				{
					_forceClose = true;
					_page.ViewModel.Dispose();
					AppWindow.Destroy();
				}
				// If CloseButton (Cancel), window remains open (args.Cancel is already true)
			}
			else
			{
				_page.ViewModel.Dispose();
			}
		}
	}
}
