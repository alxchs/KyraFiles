// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.System;
using WinRT;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenLogFileAction : IAction
	{
		public string Label
			=> Strings.OpenLogFile.GetLocalizedResource();

		public string Description
			=> Strings.OpenLogFileDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public HotKey HotKey
			=> new(Keys.OemPeriod, KeyModifiers.Ctrl);

		[DynamicWindowsRuntimeCast(typeof(StorageFile))]
		public async Task ExecuteAsync(object? parameter = null)
		{
			try
			{
				var debugFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync("debug.log") as StorageFile;

				if (debugFile != null && !await Launcher.LaunchFileAsync(debugFile))
				{
					// Fallback to Process.Start if Launcher fails
					using var process = Process.Start(new ProcessStartInfo(debugFile.Path)
					{
						UseShellExecute = true,
						Verb = "open"
					});
				}
			}
			catch (Exception ex)
			{
				var targetXamlRoot = UIHelpers.CurrentActiveXamlRoot ?? MainWindow.Instance?.Content?.XamlRoot;
				if (targetXamlRoot is not null && !VisualTreeHelper.GetOpenPopupsForXamlRoot(targetXamlRoot).Any())
				{
					var errorDialog = new ContentDialog()
					{
						Title = Strings.FailedToOpenLogFile.GetLocalizedResource(),
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
