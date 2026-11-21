// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using KyraFiles.App.Extensions;
using KyraFiles.App.ViewModels.Dialogs;
using Windows.Storage;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenCodeSearchAction : ObservableObject, IAction
	{
		private readonly IContentPageContext _context;
		private readonly IDialogService _dialogService;

		public string Label
			=> Strings.CodeSearch.GetLocalizedResource();

		public string Description
			=> Strings.CodeSearchDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.FileSystem;

		public RichGlyph Glyph
			=> new(baseGlyph: "\uE721");

		public bool IsExecutable => GetIsExecutable();

		public OpenCodeSearchAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();
			_dialogService = Ioc.Default.GetRequiredService<IDialogService>();

			_context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var targetFolder = GetTargetFolder();
			if (string.IsNullOrWhiteSpace(targetFolder))
				return;

			var viewModel = new CodeSearchDialogViewModel(targetFolder);
			await _dialogService.ShowDialogAsync(viewModel);
		}

		private string GetTargetFolder()
		{
			if (_context.HasSelection)
			{
				foreach (var item in _context.SelectedItems)
				{
					if (item != null && item.PrimaryItemAttribute is StorageItemTypes.Folder && !item.IsArchive && Directory.Exists(item.ItemPath))
					{
						return item.ItemPath;
					}
				}
			}

			if (_context.Folder is not null && Directory.Exists(_context.Folder.ItemPath))
			{
				return _context.Folder.ItemPath;
			}

			return _context.ShellPage?.ShellViewModel?.WorkingDirectory ?? string.Empty;
		}

		private bool GetIsExecutable()
		{
			if (_context.PageType is ContentPageTypes.None or ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.ZipFolder or ContentPageTypes.ReleaseNotes or ContentPageTypes.Settings)
				return false;

			if (_context.HasSelection)
			{
				return _context.SelectedItems.Any(item => item is not null && item.PrimaryItemAttribute is StorageItemTypes.Folder && !item.IsArchive);
			}

			return _context.Folder is not null;
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
