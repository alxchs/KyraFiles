// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.App.ViewModels.Properties;

namespace KyraFiles.App.ViewModels.Previews
{
	public sealed partial class HtmlPreviewViewModel : BasePreviewModel
	{
		public HtmlPreviewViewModel(ListedItem item)
			: base(item)
		{
		}

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
			=> [];
	}
}
