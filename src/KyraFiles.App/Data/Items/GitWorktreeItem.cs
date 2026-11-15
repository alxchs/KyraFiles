// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Items
{
	public record GitWorktreeItem(
		string Name,
		string Path,
		string? BranchName,
		bool IsCurrent,
		bool IsLocked = false
	);
}
