// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

using System.IO;
using Windows.Foundation;
using Windows.Storage;

namespace KyraFiles.App.Utils.Storage
{
	public interface ICreateFileWithStream
	{
		IAsyncOperation<BaseStorageFile?> CreateFileAsync(Stream contents, string desiredName);

		IAsyncOperation<BaseStorageFile?> CreateFileAsync(Stream contents, string desiredName, CreationCollisionOption options);
	}
}
