// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.IO;

namespace KyraFiles.Shared.BuildFolder;

/// <summary>
/// Represents a candidate build folder discovered during scanning (US1, AC-1).
/// </summary>
public sealed class BuildFolderCandidate
{
	public string FolderPath { get; init; } = string.Empty;

	public string FolderName { get; init; } = string.Empty;

	public string ParentFolderPath { get; init; } = string.Empty;

	public string ProjectName { get; init; } = string.Empty;

	public string ProjectType { get; init; } = string.Empty;

	public BuildFolderRule MatchedRule { get; init; } = null!;

	public DateTime LastModified { get; init; }

	public long SizeBytes { get; set; } = -1;

	/// <summary>
	/// Calculates and caches the total size in bytes of files contained inside this folder.
	/// </summary>
	public long CalculateSize()
	{
		long size = 0;
		try
		{
			if (Directory.Exists(FolderPath))
			{
				var dirInfo = new DirectoryInfo(FolderPath);
				foreach (var file in dirInfo.EnumerateFiles("*", new EnumerationOptions
				{
					RecurseSubdirectories = true,
					IgnoreInaccessible = true,
					AttributesToSkip = FileAttributes.ReparsePoint
				}))
				{
					size += file.Length;
				}
			}
		}
		catch
		{
			// Inaccessible or deleted during calculation
		}

		SizeBytes = size;
		return size;
	}
}
