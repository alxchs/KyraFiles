// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.Shared.Utils;
using Microsoft.UI.Xaml.Media.Imaging;

namespace KyraFiles.App.Data.Models
{
	/// <inheritdoc cref="IImage"/>
	internal sealed class BitmapImageModel : IImage
	{
		public BitmapImage? Image { get; }

		public BitmapImageModel(BitmapImage? image)
		{
			Image = image;
		}
	}
}
