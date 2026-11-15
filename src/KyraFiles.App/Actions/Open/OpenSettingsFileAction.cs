// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenSettingsFileAction : IAction
	{
		public string Label
			=> Strings.EditSettingsFile.GetLocalizedResource();

		public string Description
			=> Strings.EditSettingsFileDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public HotKey HotKey
			=> new(Keys.OemComma, KeyModifiers.CtrlShift);

		public RichGlyph Glyph
			=> new("\uE8DA");

		public async Task ExecuteAsync(object? parameter = null)
		{
			try
			{
				var settingsJsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appdata:///local/{Constants.LocalSettings.SettingsFolderName}/{Constants.LocalSettings.UserSettingsFileName}"));
				if (!await Launcher.LaunchFileAsync(settingsJsonFile))
					await ContextMenu.InvokeVerb("open", settingsJsonFile.Path);
			}
			catch (Exception ex)
			{
				var targetXamlRoot = UIHelpers.CurrentActiveXamlRoot ?? MainWindow.Instance?.Content?.XamlRoot;
				if (targetXamlRoot is not null && !VisualTreeHelper.GetOpenPopupsForXamlRoot(targetXamlRoot).Any())
				{
					var errorDialog = new ContentDialog()
					{
						Title = Strings.FailedToOpenSettingsFile.GetLocalizedResource(),
						Content = ex.Message,
						PrimaryButtonText = Strings.OK.GetLocalizedResource(),
						XamlRoot = targetXamlRoot
					};

					await errorDialog.TryShowAsync();
				}
			}
		}
	}
}
