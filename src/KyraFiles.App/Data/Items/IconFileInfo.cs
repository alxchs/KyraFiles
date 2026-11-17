// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Items
{
	public sealed class IconFileInfo
	{
		public byte[] IconData { get; }

		public int Index { get; }

		public IconFileInfo(byte[] iconData, int index)
		{
			IconData = iconData;
			Index = index;
		}
	}
}
