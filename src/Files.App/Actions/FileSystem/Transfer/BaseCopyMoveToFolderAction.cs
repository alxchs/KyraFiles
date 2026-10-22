// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using System.IO;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions
{
	/// <summary>
	/// F033: copies or moves the selected items to a folder given as parameter (one of the recent destinations) or,
	/// without a parameter, to a folder picked in the Windows folder picker. Always asks first, with "No" as the default.
	/// </summary>
	internal abstract class BaseCopyMoveToFolderAction : ObservableObject, IAction
	{
		protected readonly IContentPageContext context;

		private readonly ICommonDialogService commonDialogService = Ioc.Default.GetRequiredService<ICommonDialogService>();

		protected abstract bool IsMove { get; }

		protected abstract string ConfirmationFormat { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public bool IsExecutable =>
			context.ShellPage is not null &&
			context.HasSelection &&
			context.PageType is not (ContentPageTypes.RecycleBin or ContentPageTypes.ZipFolder or ContentPageTypes.SearchResults or ContentPageTypes.Home or ContentPageTypes.ReleaseNotes or ContentPageTypes.Settings or ContentPageTypes.None);

		protected BaseCopyMoveToFolderAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is not { } shellPage || !context.HasSelection)
				return;

			var items = context.SelectedItems.ToList();
			var destination = parameter as string;
			if (string.IsNullOrEmpty(destination))
			{
				if (!commonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var pickedFolder)
					|| string.IsNullOrEmpty(pickedFolder))
					return;

				destination = pickedFolder;
			}

			var dialog = new ContentDialog()
			{
				Title = Label,
				Content = $"{string.Format(ConfirmationFormat.GetLocalizedFormatResource(items.Count), items.Count)}\n\n{destination}",
				PrimaryButtonText = Strings.Yes.GetLocalizedResource(),
				CloseButtonText = Strings.No.GetLocalizedResource(),
				// Enter and Esc both answer "No": a stray key press never copies or moves anything
				DefaultButton = ContentDialogButton.Close,
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			if (await dialog.TryShowAsync() != ContentDialogResult.Primary)
				return;

			CopyMoveDestinationHistory.Add(destination);

			var sources = items.Select(item => StorageHelpers.FromPathAndType(
				item.GetRequiredPath(),
				item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory)).ToList();
			var targets = sources.Select(source => PathNormalization.Combine(destination, Path.GetFileName(source.Path))).ToList();

			// Same operation as paste: progress, name conflicts and undo
			if (IsMove)
				await shellPage.FilesystemHelpers.MoveItemsAsync(sources, targets, true, true);
			else
				await shellPage.FilesystemHelpers.CopyItemsAsync(sources, targets, true, true);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
