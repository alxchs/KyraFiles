// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

namespace KyraFiles.Shared.CodeSearch
{
	/// <summary>
	/// Configuration options for code search execution (F051).
	/// </summary>
	public sealed class CodeSearchOptions
	{
		public string SearchDirectory { get; set; } = string.Empty;

		public string Query { get; set; } = string.Empty;

		public bool IsRegex { get; set; } = false;

		public bool MatchCase { get; set; } = false;

		public bool MatchWholeWord { get; set; } = false;

		public bool RespectGitIgnore { get; set; } = true;

		public string? FileExtensions { get; set; } = null;

		public long MaxFileSizeBytes { get; set; } = 2 * 1024 * 1024; // 2 MB (AC-5)

		public int MaxResults { get; set; } = 5000;
	}
}
