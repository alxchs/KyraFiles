// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.App.Data.EventArguments
{
	public sealed class PathNavigationEventArgs
	{
		public string? ItemPath { get; set; }

		public string? ItemName { get; set; }

		public bool IsFile { get; set; }
	}
}
