// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading;

namespace KyraFiles.Shared.BuildFolder;

/// <summary>
/// Scans directory trees for rebuildable build folders based on rules, pruning discovered trees (NFR-001, FR-001, FR-002).
/// </summary>
public sealed class BuildFolderScanner
{
	private static readonly HashSet<string> _ignoredFolderNames = new(StringComparer.OrdinalIgnoreCase)
	{
		".git",
		".vs",
		".idea",
		"$RECYCLE.BIN",
		"System Volume Information",
	};

	private readonly IReadOnlyList<BuildFolderRule> _rules;
	private readonly Dictionary<string, List<BuildFolderRule>> _rulesByFolderName;
	private readonly IGitTrackingChecker? _gitTracker;

	public BuildFolderScanner(IEnumerable<BuildFolderRule>? rules = null, IGitTrackingChecker? gitTracker = null)
	{
		_rules = rules?.ToList() ?? BuildFolderRules.DefaultRules;
		_rulesByFolderName = new Dictionary<string, List<BuildFolderRule>>(StringComparer.OrdinalIgnoreCase);

		foreach (var rule in _rules)
		{
			if (!_rulesByFolderName.TryGetValue(rule.FolderName, out var list))
			{
				list = [];
				_rulesByFolderName[rule.FolderName] = list;
			}
			list.Add(rule);
		}

		_gitTracker = gitTracker;
	}

	/// <summary>
	/// Scans the specified root path for build folder candidates.
	/// </summary>
	public List<BuildFolderCandidate> Scan(string rootPath, CancellationToken cancellationToken = default)
	{
		var results = new List<BuildFolderCandidate>();

		if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
			return results;

		var stack = new Stack<string>();
		stack.Push(rootPath);

		while (stack.Count > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var currentDir = stack.Pop();

			string[] subDirectories;
			try
			{
				subDirectories = Directory.GetDirectories(currentDir);
			}
			catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
			{
				continue;
			}

			if (subDirectories.Length == 0)
				continue;

			string[]? currentFiles = null;

			foreach (var subDir in subDirectories)
			{
				cancellationToken.ThrowIfCancellationRequested();

				string subDirName = Path.GetFileName(subDir);
				if (string.IsNullOrEmpty(subDirName) || _ignoredFolderNames.Contains(subDirName))
					continue;

				try
				{
					var dirInfo = new DirectoryInfo(subDir);
					if ((dirInfo.Attributes & FileAttributes.ReparsePoint) != 0)
						continue;
				}
				catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
				{
					continue;
				}

				if (_rulesByFolderName.TryGetValue(subDirName, out var matchingRules))
				{
					currentFiles ??= SafeGetFileNames(currentDir);

					var matchedRule = FindMatchingRule(matchingRules, currentFiles);
					if (matchedRule is not null)
					{
						// Check git safety: never offer if directory contains git-tracked files (FR-002, AC-5)
						if (_gitTracker is not null && _gitTracker.HasTrackedFiles(subDir))
						{
							continue;
						}

						results.Add(new BuildFolderCandidate
						{
							FolderPath = subDir,
							FolderName = subDirName,
							ParentFolderPath = currentDir,
							ProjectName = Path.GetFileName(currentDir),
							ProjectType = matchedRule.ProjectType,
							MatchedRule = matchedRule,
							LastModified = Directory.GetLastWriteTime(subDir),
						});

						// Pruning: do NOT recurse into matched build folders (NFR-001)
						continue;
					}
				}

				// Not a build folder: push to stack to continue searching
				stack.Push(subDir);
			}
		}

		return results;
	}

	/// <summary>
	/// Calculates the sum of sizes for all provided candidates.
	/// </summary>
	public static long CalculateTotalSize(IEnumerable<BuildFolderCandidate> candidates)
	{
		long total = 0;
		foreach (var candidate in candidates)
		{
			long size = candidate.SizeBytes >= 0 ? candidate.SizeBytes : candidate.CalculateSize();
			total += size;
		}
		return total;
	}

	private static string[] SafeGetFileNames(string directoryPath)
	{
		try
		{
			return Directory.GetFiles(directoryPath).Select(Path.GetFileName).Where(f => !string.IsNullOrEmpty(f)).Select(f => f!).ToArray();
		}
		catch
		{
			return [];
		}
	}

	private static BuildFolderRule? FindMatchingRule(List<BuildFolderRule> rules, string[] fileNamesInParent)
	{
		if (fileNamesInParent.Length == 0)
			return null;

		foreach (var rule in rules)
		{
			foreach (var pattern in rule.MarkerPatterns)
			{
				foreach (var fileName in fileNamesInParent)
				{
					if (FileSystemName.MatchesSimpleExpression(pattern, fileName, ignoreCase: true))
					{
						return rule;
					}
				}
			}
		}

		return null;
	}
}
