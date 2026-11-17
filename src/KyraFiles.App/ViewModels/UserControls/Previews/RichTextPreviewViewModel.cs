// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.ViewModels.Properties;
using Windows.Storage.Streams;

namespace KyraFiles.App.ViewModels.Previews
{
	public sealed partial class RichTextPreviewViewModel : BasePreviewModel
	{
		public IRandomAccessStream? Stream { get; set; }

		public RichTextPreviewViewModel(ListedItem item) : base(item) { }

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			Stream = await PreviewFile.OpenReadAsync();

			return [];
		}
	}
}
