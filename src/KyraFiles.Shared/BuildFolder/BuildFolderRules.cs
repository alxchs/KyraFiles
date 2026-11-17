// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.BuildFolder;

[JsonSourceGenerationOptions(
	PropertyNameCaseInsensitive = true,
	WriteIndented = true)]
[JsonSerializable(typeof(List<BuildFolderRule>))]
[JsonSerializable(typeof(BuildFolderRule))]
internal sealed partial class BuildFolderJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Manages default and custom build folder rules (FR-001, FR-003).
/// </summary>
public static class BuildFolderRules
{
	private static readonly List<BuildFolderRule> _defaultRules =
	[
		// Node.js
		new("node_modules", "Node.js", "package.json"),

		// .NET / Delphi / Visual Studio
		new("bin", ".NET", "*.csproj", "*.fsproj", "*.vbproj", "*.sln", "*.slnx", "*.dpr", "*.vcxproj"),
		new("obj", ".NET", "*.csproj", "*.fsproj", "*.vbproj", "*.sln", "*.slnx", "*.dpr", "*.vcxproj"),

		// Python
		new(".venv", "Python", "pyvenv.cfg", "requirements.txt", "Pipfile", "pyproject.toml"),

		// Rust / Maven
		new("target", "Rust", "Cargo.toml"),
		new("target", "Maven", "pom.xml"),

		// Gradle
		new(".gradle", "Gradle", "build.gradle", "build.gradle.kts", "settings.gradle", "settings.gradle.kts"),
		new("build", "Gradle", "build.gradle", "build.gradle.kts", "settings.gradle", "settings.gradle.kts"),

		// Flutter / Dart
		new(".dart_tool", "Flutter", "pubspec.yaml"),
		new("build", "Flutter", "pubspec.yaml"),
	];

	public static IReadOnlyList<BuildFolderRule> DefaultRules => _defaultRules;

	/// <summary>
	/// Parses JSON containing custom build folder rules (AOT and trimming safe).
	/// </summary>
	public static List<BuildFolderRule> LoadFromJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return [];

		return JsonSerializer.Deserialize(json, BuildFolderJsonContext.Default.ListBuildFolderRule) ?? [];
	}

	/// <summary>
	/// Loads rules from a configuration file, falling back to default rules if file is absent or empty.
	/// </summary>
	public static List<BuildFolderRule> LoadFromFile(string filePath)
	{
		if (!File.Exists(filePath))
			return [.. _defaultRules];

		try
		{
			string json = File.ReadAllText(filePath);
			var custom = LoadFromJson(json);
			if (custom.Count > 0)
				return custom;
		}
		catch
		{
			// Fallback on read error
		}

		return [.. _defaultRules];
	}

	/// <summary>
	/// Merges custom rules with default rules. Custom rules matching folder and project type override defaults.
	/// </summary>
	public static List<BuildFolderRule> Merge(IEnumerable<BuildFolderRule> customRules)
	{
		var dict = new Dictionary<(string, string), BuildFolderRule>();
		foreach (var r in _defaultRules)
			dict[(r.FolderName.ToLowerInvariant(), r.ProjectType.ToLowerInvariant())] = r;

		foreach (var r in customRules)
			dict[(r.FolderName.ToLowerInvariant(), r.ProjectType.ToLowerInvariant())] = r;

		return dict.Values.ToList();
	}
}
