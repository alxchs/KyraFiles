// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KyraFiles.App.ViewModels.Dialogs;
using Windows.Storage;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenDuplicateFinderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IDialogService dialogService;

		public string Label
			=> Strings.DuplicateFinder.GetLocalizedResource();

		public string Description
			=> Strings.DuplicateFinderDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Folder");

		public bool IsExecutable => GetIsExecutable();

		public OpenDuplicateFinderAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			dialogService = Ioc.Default.GetRequiredService<IDialogService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var targetFolders = GetTargetFolders();
			if (targetFolders.Count == 0)
				return;

			var viewModel = new DuplicateFinderDialogViewModel(targetFolders);
			await dialogService.ShowDialogAsync(viewModel);
		}

		private List<string> GetTargetFolders()
		{
			var folders = new List<string>();

			if (context.HasSelection)
			{
				foreach (var item in context.SelectedItems)
				{
					if (item != null && item.PrimaryItemAttribute is StorageItemTypes.Folder && !item.IsArchive && Directory.Exists(item.ItemPath))
					{
						folders.Add(item.ItemPath);
					}
				}
			}

			if (folders.Count == 0 && context.Folder is not null && Directory.Exists(context.Folder.ItemPath))
			{
				folders.Add(context.Folder.ItemPath);
			}

			return folders;
		}

		private bool GetIsExecutable()
		{
			if (context.PageType is ContentPageTypes.None or ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.ZipFolder or ContentPageTypes.ReleaseNotes or ContentPageTypes.Settings)
				return false;

			if (context.HasSelection)
			{
				return context.SelectedItems.Any(item => item is not null && item.PrimaryItemAttribute is StorageItemTypes.Folder && !item.IsArchive);
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
