// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.Shared.CodeSearch
{
	/// <summary>
	/// Progress update reporting during code search execution.
	/// </summary>
	public sealed class CodeSearchProgress
	{
		public int FilesScanned { get; init; }

		public int FilesIgnored { get; init; }

		public int MatchesFound { get; init; }

		public string? CurrentFile { get; init; }
	}
}
