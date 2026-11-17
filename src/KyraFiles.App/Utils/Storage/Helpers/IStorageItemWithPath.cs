// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using Windows.Storage;

namespace KyraFiles.App.Utils.Storage
{
	public interface IStorageItemWithPath
	{
		public string Name { get; }

		public string Path { get; }

		public IStorageItem? Item { get; }

		public FilesystemItemType ItemType { get; }
	}
}
