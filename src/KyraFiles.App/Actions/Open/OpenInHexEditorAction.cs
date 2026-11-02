// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Files.App.Views.HexEditor;
using Files.Shared.Helpers;
using Windows.Storage;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenInHexEditorAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.OpenInHexEditor.GetLocalizedResource();

		public string Description
			=> Strings.OpenInHexEditorDescription.GetLocalizedResource();

		public ActionCategory Category
			=> ActionCategory.Open;

		public RichGlyph Glyph
			=> new("\uE70F");

		public bool IsExecutable =>
			context.SelectedItems.Count == 1 &&
			context.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.File &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder;

		public OpenInHexEditorAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var selectedItem = context.SelectedItem;
			var path = selectedItem?.ItemPath;
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
				return Task.CompletedTask;

			HexEditorWindow.Open(path);
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.SelectedItem):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
