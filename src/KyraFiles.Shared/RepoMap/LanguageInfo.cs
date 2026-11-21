// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace KyraFiles.Shared.RepoMap
{
	/// <summary>
	/// Represents aggregated statistics for a programming language or file category.
	/// </summary>
	public sealed class LanguageInfo
	{
		public string LanguageName { get; set; } = string.Empty;
		public int FileCount { get; set; }
		public long TotalLines { get; set; }
		public long TotalBytes { get; set; }
		public List<RepoFileInfo> Files { get; set; } = new();

		public double PercentLines { get; set; }
		public double PercentBytes { get; set; }

		public string FormattedTotalBytes
		{
			get
			{
				if (TotalBytes < 1024)
					return $"{TotalBytes} B";
				if (TotalBytes < 1024 * 1024)
					return $"{TotalBytes / 1024.0:F1} KB";
				return $"{TotalBytes / (1024.0 * 1024.0):F2} MB";
			}
		}
	}
}
