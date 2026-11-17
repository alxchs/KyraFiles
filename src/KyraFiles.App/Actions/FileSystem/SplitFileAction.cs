// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.IO;
using KyraFiles.App.Dialogs;
using KyraFiles.App.Utils.StatusCenter;
using KyraFiles.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class SplitFileAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;
		private readonly StatusCenterViewModel statusCenterViewModel;

		public string Label
			=> Strings.SplitFile.GetLocalizedResource();

		public string Description
			=> Strings.SplitFileDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Zip");

		public override bool IsExecutable =>
			context.HasSelection &&
			context.SelectedItems.Count == 1 &&
			context.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.File &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder &&
			UIHelpers.CanShowDialog;

		public SplitFileAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var selectedItem = context.SelectedItem;
			var rawPath = selectedItem?.ItemPath;
			if (string.IsNullOrWhiteSpace(rawPath) || !File.Exists(rawPath))
				return;

			string sourceFilePath = rawPath;

			long fileSize = 0;
			try
			{
				fileSize = new FileInfo(sourceFilePath).Length;
			}
			catch (Exception ex)
			{
				App.Logger?.LogWarning(ex, "Failed to read file size for {Path}", sourceFilePath);
				return;
			}

			using var dialog = DynamicDialogFactory.GetFor_SplitFileDialog(sourceFilePath, fileSize);
			await dialog.TryShowAsync();

			if (dialog.DynamicResult != DynamicDialogResult.Primary ||
				dialog.ViewModel.AdditionalData is not SplitDialogResult dialogResult)
			{
				return;
			}

			string sourceName = Path.GetFileName(sourceFilePath);
			string firstPartPath = Path.Combine(dialogResult.DestinationDirectory, SplitEngine.GetPartFileName(sourceName, 1));
			string manifestPath = Path.Combine(dialogResult.DestinationDirectory, SplitEngine.GetManifestFileName(sourceName));
			if (File.Exists(firstPartPath) || File.Exists(manifestPath))
			{
				var existsDialog = new ContentDialog
				{
					Title = Strings.SplitFileDialogTitle.GetLocalizedResource(),
					Content = Strings.SplitDestinationExistsError.GetLocalizedResource(),
					PrimaryButtonText = Strings.OK.GetLocalizedResource(),
					DefaultButton = ContentDialogButton.Primary
				};

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					existsDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

				await existsDialog.TryShowAsync();
				return;
			}

			var cts = new CancellationTokenSource();
			var banner = StatusCenterHelper.AddCard_Split(
				sourceFilePath,
				ReturnResult.InProgress,
				dialogResult.DestinationDirectory,
				fileSize,
				cts);

			_ = Task.Run(async () =>
			{
				try
				{
					var options = new SplitOptions
					{
						PartSize = dialogResult.PartSize,
						DestinationDirectory = dialogResult.DestinationDirectory,
						CalculateSha256 = dialogResult.CalculateSha256
					};

					var progress = new Progress<SplitProgress>(p =>
					{
						if (banner.CancellationToken.IsCancellationRequested)
							return;

						int percentage = p.TotalBytes > 0
							? (int)Math.Clamp((p.TotalBytesRead * 100.0) / p.TotalBytes, 0, 99)
							: 100;

						banner.ProgressPercentage = percentage;
						banner.Message = $"{p.CurrentPartIndex}/{p.EstimatedTotalParts} ({p.TotalBytesRead.ToSizeString()} / {p.TotalBytes.ToSizeString()})";
					});

					await SplitEngine.SplitAsync(sourceFilePath, options, progress, banner.CancellationToken);

					statusCenterViewModel.RemoveItem(banner);
					StatusCenterHelper.AddCard_Split(sourceFilePath, ReturnResult.Success, dialogResult.DestinationDirectory, fileSize);
				}
				catch (OperationCanceledException)
				{
					statusCenterViewModel.RemoveItem(banner);
					StatusCenterHelper.AddCard_Split(sourceFilePath, ReturnResult.Cancelled, dialogResult.DestinationDirectory, fileSize);
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, "Failed to split file {Path}", sourceFilePath);
					statusCenterViewModel.RemoveItem(banner);
					StatusCenterHelper.AddCard_Split(sourceFilePath, ReturnResult.Failed, dialogResult.DestinationDirectory, fileSize);
				}
				finally
				{
					cts.Dispose();
				}
			});
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.SelectedItem):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
