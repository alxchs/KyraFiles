// Copyright (c) Files Community
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
	internal sealed partial class CombineFilesAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;
		private readonly StatusCenterViewModel statusCenterViewModel;

		public string Label
			=> Strings.CombineFiles.GetLocalizedResource();

		public string Description
			=> Strings.CombineFilesDescription.GetLocalizedResource();

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
			UIHelpers.CanShowDialog &&
			IsCombineTarget(context.SelectedItem);

		public CombineFilesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public static bool IsCombineTarget(ListedItem? item)
		{
			if (item is null)
				return false;

			var ext = item.FileExtension;
			var name = item.Name;
			return (!string.IsNullOrEmpty(name) && name.EndsWith(".manifest.json", StringComparison.OrdinalIgnoreCase))
				|| string.Equals(ext, ".001", StringComparison.OrdinalIgnoreCase)
				|| (!string.IsNullOrEmpty(name) && name.EndsWith(".001", StringComparison.OrdinalIgnoreCase));
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var selectedItem = context.SelectedItem;
			var rawPath = selectedItem?.ItemPath;
			if (string.IsNullOrWhiteSpace(rawPath) || !File.Exists(rawPath))
				return;

			string itemPath = rawPath;

			string manifestFilePath;
			if (itemPath.EndsWith(".manifest.json", StringComparison.OrdinalIgnoreCase))
			{
				manifestFilePath = itemPath;
			}
			else if (itemPath.EndsWith(".001", StringComparison.OrdinalIgnoreCase))
			{
				string directory = Path.GetDirectoryName(itemPath) ?? string.Empty;
				string baseName = Path.GetFileNameWithoutExtension(itemPath);
				manifestFilePath = Path.Combine(directory, $"{baseName}.manifest.json");

				if (!File.Exists(manifestFilePath))
				{
					var notFoundDialog = new ContentDialog
					{
						Title = Strings.CombineFilesDialogTitle.GetLocalizedResource(),
						Content = string.Format(Strings.CombineManifestNotFoundError.GetLocalizedResource(), Path.GetFileName(manifestFilePath)),
						PrimaryButtonText = Strings.OK.GetLocalizedResource(),
						DefaultButton = ContentDialogButton.Primary
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						notFoundDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await notFoundDialog.TryShowAsync();
					return;
				}
			}
			else
			{
				return;
			}

			SplitManifest manifest;
			try
			{
				var loaded = await SplitManifest.LoadAsync(manifestFilePath);
				if (loaded is null)
					throw new InvalidDataException("Manifest file could not be parsed.");
				manifest = loaded;
			}
			catch (Exception ex)
			{
				App.Logger?.LogError(ex, "Failed to load split manifest {Path}", manifestFilePath);
				var parseErrorDialog = new ContentDialog
				{
					Title = Strings.CombineValidationErrorTitle.GetLocalizedResource(),
					Content = ex.Message,
					PrimaryButtonText = Strings.OK.GetLocalizedResource(),
					DefaultButton = ContentDialogButton.Primary
				};

				if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
					parseErrorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

				await parseErrorDialog.TryShowAsync();
				return;
			}

			using var dialog = DynamicDialogFactory.GetFor_CombineFilesDialog(manifestFilePath, manifest);
			await dialog.TryShowAsync();

			if (dialog.DynamicResult != DynamicDialogResult.Primary ||
				dialog.ViewModel.AdditionalData is not CombineDialogResult dialogResult)
			{
				return;
			}

			string partsDirectory = Path.GetDirectoryName(manifestFilePath) ?? Directory.GetCurrentDirectory();
			string targetFileName = manifest.OriginalFileName;
			long totalSize = manifest.TotalSize;
			int partsCount = manifest.Parts.Count;

			var cts = new CancellationTokenSource();
			var banner = StatusCenterHelper.AddCard_Combine(
				targetFileName,
				ReturnResult.InProgress,
				dialogResult.DestinationDirectory,
				totalSize,
				partsCount,
				cts);

			_ = Task.Run(async () =>
			{
				try
				{
					var options = new CombineOptions
					{
						CollisionPolicy = dialogResult.CollisionPolicy,
						DestinationDirectory = dialogResult.DestinationDirectory,
						VerifySha256 = true
					};

					var progress = new Progress<CombineProgress>(p =>
					{
						if (banner.CancellationToken.IsCancellationRequested)
							return;

						int percentage = p.TotalBytes > 0
							? (int)Math.Clamp((p.TotalBytesWritten * 100.0) / p.TotalBytes, 0, 99)
							: 100;

						banner.ProgressPercentage = percentage;
						banner.Message = $"{p.CurrentPartIndex}/{p.TotalParts} ({p.TotalBytesWritten.ToSizeString()} / {p.TotalBytes.ToSizeString()})";
					});

					await CombineEngine.CombineAsync(manifest, partsDirectory, options, progress, banner.CancellationToken);

					statusCenterViewModel.RemoveItem(banner);
					StatusCenterHelper.AddCard_Combine(targetFileName, ReturnResult.Success, dialogResult.DestinationDirectory, totalSize, partsCount);
				}
				catch (OperationCanceledException)
				{
					statusCenterViewModel.RemoveItem(banner);
					StatusCenterHelper.AddCard_Combine(targetFileName, ReturnResult.Cancelled, dialogResult.DestinationDirectory, totalSize, partsCount);
				}
				catch (CombineValidationException cve)
				{
					App.Logger?.LogWarning(cve, "Combine validation failed for {Manifest}: {Reason}", manifestFilePath, cve.Reason);
					statusCenterViewModel.RemoveItem(banner);
					StatusCenterHelper.AddCard_Combine(targetFileName, ReturnResult.Failed, dialogResult.DestinationDirectory, totalSize, partsCount);

					string errorMessage = FormatCombineValidationErrorMessage(cve);
					MainWindow.Instance.DispatcherQueue.TryEnqueue(async () =>
					{
						var errorDialog = new ContentDialog
						{
							Title = Strings.CombineValidationErrorTitle.GetLocalizedResource(),
							Content = errorMessage,
							PrimaryButtonText = Strings.OK.GetLocalizedResource(),
							DefaultButton = ContentDialogButton.Primary
						};

						if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
							errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

						await errorDialog.TryShowAsync();
					});
				}
				catch (Exception ex)
				{
					App.Logger?.LogError(ex, "Failed to combine parts for {Manifest}", manifestFilePath);
					statusCenterViewModel.RemoveItem(banner);
					StatusCenterHelper.AddCard_Combine(targetFileName, ReturnResult.Failed, dialogResult.DestinationDirectory, totalSize, partsCount);

					MainWindow.Instance.DispatcherQueue.TryEnqueue(async () =>
					{
						var errorDialog = new ContentDialog
						{
							Title = Strings.CombineFilesDialogTitle.GetLocalizedResource(),
							Content = ex.Message,
							PrimaryButtonText = Strings.OK.GetLocalizedResource(),
							DefaultButton = ContentDialogButton.Primary
						};

						if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
							errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

						await errorDialog.TryShowAsync();
					});
				}
				finally
				{
					cts.Dispose();
				}
			});
		}

		public static string FormatCombineValidationErrorMessage(CombineValidationException cve)
		{
			string reason = cve.Reason switch
			{
				CombineValidationFailureReason.MissingPart => Strings.CombineReasonMissingPart.GetLocalizedResource(),
				CombineValidationFailureReason.DuplicatePart => Strings.CombineReasonDuplicatePart.GetLocalizedResource(),
				CombineValidationFailureReason.TruncatedPart => Strings.CombineReasonTruncatedPart.GetLocalizedResource(),
				CombineValidationFailureReason.CorruptedPart => Strings.CombineReasonCorruptedPart.GetLocalizedResource(),
				CombineValidationFailureReason.OutOfOrder => Strings.CombineReasonOutOfOrder.GetLocalizedResource(),
				CombineValidationFailureReason.InvalidManifest => Strings.CombineReasonInvalidManifest.GetLocalizedResource(),
				_ => Strings.CombineReasonGeneral.GetLocalizedResource()
			};

			// O detalhe técnico (CRC, tamanhos) vai para o log, não para o usuário.
			if (cve.PartIndex.HasValue && !string.IsNullOrWhiteSpace(cve.PartFileName))
				return string.Format(Strings.CombineErrorPartFormat.GetLocalizedResource(), cve.PartIndex.Value, cve.PartFileName, reason);

			return reason;
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
