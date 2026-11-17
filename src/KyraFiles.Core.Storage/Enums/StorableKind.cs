// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.Core.Storage.Enums
{
	[Flags]
	public enum StorableKind : byte
	{
		None = 0,
		Files = 1,
		Folders = 2,
		All = Files | Folders
	}
}
