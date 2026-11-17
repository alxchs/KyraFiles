// Copyright (c) KyraFiles contributors
// SPDX-License-Identifier: MPL-2.0

namespace KyraFiles.App.Data.Contracts
{
	public interface IWindowsJumpListService
	{
		Task InitializeAsync();

		Task AddFolderAsync(string path);

		Task RefreshPinnedFoldersAsync();

		Task RemoveFolderAsync(string path);

		Task<IEnumerable<string>> GetFoldersAsync();
	}
}
