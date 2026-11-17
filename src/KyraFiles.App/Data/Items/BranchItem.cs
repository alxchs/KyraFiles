// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.App.Data.Items
{
	public record BranchItem(string Name, bool IsHead, bool IsRemote, int? AheadBy, int? BehindBy, int PendingChanges = 0);
}
