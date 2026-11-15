// Copyright (c) Files Community
// Licensed under the MIT License.

using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using KyraFiles.App.ViewModels.Dialogs;
using Windows.Storage;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CleanBuildFoldersAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IDialogService dialogService;

		public string Label
			=> Strings.CleanBuildFolders.GetLocalizedResource();

		public string Description
			=> Strings.CleanBuildFoldersDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Delete");

		public bool IsExecutable => GetIsExecutable();

		public CleanBuildFoldersAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			dialogService = Ioc.Default.GetRequiredService<IDialogService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			string? targetPath = GetTargetPath();
			if (string.IsNullOrEmpty(targetPath) || !Directory.Exists(targetPath))
				return;

			var viewModel = new CleanBuildFoldersDialogViewModel(targetPath);
			await dialogService.ShowDialogAsync(viewModel);
		}

		private string? GetTargetPath()
		{
			if (context.HasSelection && context.SelectedItems.Count == 1)
			{
				var item = context.SelectedItem;
				if (item is not null && item.PrimaryItemAttribute is StorageItemTypes.Folder && !item.IsArchive)
					return item.ItemPath;
			}
			else if (context.Folder is not null)
			{
				return context.Folder.ItemPath;
			}

			return null;
		}

		private bool GetIsExecutable()
		{
			if (context.PageType is ContentPageTypes.None or ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.ZipFolder or ContentPageTypes.ReleaseNotes or ContentPageTypes.Settings)
				return false;

			if (context.HasSelection)
			{
				return context.SelectedItems.Count == 1
					&& context.SelectedItem is { } item
					&& item.PrimaryItemAttribute is StorageItemTypes.Folder
					&& !item.IsArchive;
			}

			return context.Folder is not null;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
