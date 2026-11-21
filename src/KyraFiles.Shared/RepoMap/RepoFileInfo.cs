// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;

namespace KyraFiles.Shared.RepoMap
{
	/// <summary>
	/// Represents metadata and metrics for an individual file within a repository.
	/// </summary>
	public sealed class RepoFileInfo
	{
		public string FilePath { get; set; } = string.Empty;
		public string RelativePath { get; set; } = string.Empty;
		public string Language { get; set; } = string.Empty;
		public long FileSizeBytes { get; set; }
		public long LineCount { get; set; }
		public int CommitCount12Months { get; set; }

		public double HotSpotScore => (double)CommitCount12Months * FileSizeBytes;

		public string FormattedSize
		{
			get
			{
				if (FileSizeBytes < 1024)
					return $"{FileSizeBytes} B";
				if (FileSizeBytes < 1024 * 1024)
					return $"{FileSizeBytes / 1024.0:F1} KB";
				return $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
			}
		}
	}
}
