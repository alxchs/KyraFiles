// Copyright (c) Files Community
// Licensed under the MIT License.

namespace KyraFiles.Shared.BuildFolder;

/// <summary>
/// Status of git tracking check for a directory.
/// </summary>
public enum GitTrackingResult
{
	Clean,
	HasTrackedFiles,
	CannotVerify,
}

/// <summary>
/// Interface for checking if a directory contains git-tracked files (FR-002, AC-5).
/// </summary>
public interface IGitTrackingChecker
{
	/// <summary>
	/// Returns true if the directory contains any files tracked by git,
	/// or if git tracking status cannot be verified (fail-closed).
	/// </summary>
	bool HasTrackedFiles(string directoryPath);

	/// <summary>
	/// Returns the detailed git tracking status of the directory.
	/// </summary>
	GitTrackingResult CheckDirectory(string directoryPath) =>
		HasTrackedFiles(directoryPath) ? GitTrackingResult.HasTrackedFiles : GitTrackingResult.Clean;
}
