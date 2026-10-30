// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Files.Shared.BuildFolder;

/// <summary>
/// Defines a rule for identifying build folders by folder name, project type, and marker file patterns (FR-001).
/// </summary>
public sealed class BuildFolderRule
{
	public string FolderName { get; init; } = string.Empty;

	public string ProjectType { get; init; } = string.Empty;

	public IReadOnlyList<string> MarkerPatterns { get; init; } = [];

	public BuildFolderRule() { }

	public BuildFolderRule(string folderName, string projectType, params string[] markerPatterns)
	{
		FolderName = folderName;
		ProjectType = projectType;
		MarkerPatterns = markerPatterns;
	}
}
