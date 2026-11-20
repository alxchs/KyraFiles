// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.Shared.ProjectActions
{
	public enum ProjectKind
	{
		DotNet,
		Delphi,
		Flutter,
		Node,
		Rust,
		Python,
		Go,
		CMake,
	}

	public sealed record BuiltInProjectAction(
		string Id,
		string DisplayName,
		string Command,
		string Arguments,
		bool UsePowerShell,
		ProjectKind Kind);

	public sealed record ProjectDetectionResult(
		string FolderPath,
		IReadOnlyList<ProjectKind> DetectedKinds,
		IReadOnlyList<string> MatchedMarkers,
		IReadOnlyList<BuiltInProjectAction> BuiltInActions)
	{
		public bool HasProject => DetectedKinds.Count > 0;

		public static ProjectDetectionResult Empty(string folderPath)
			=> new(folderPath, [], [], []);
	}

	public sealed class ProjectDetector
	{
		private const int MaxCacheEntries = 256;
		private readonly ConcurrentDictionary<string, ProjectDetectionResult> _cache = new(StringComparer.OrdinalIgnoreCase);

		public ProjectDetectionResult Detect(string? folderPath, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
				return ProjectDetectionResult.Empty(folderPath ?? string.Empty);

			var normalized = NormalizeFolderPath(folderPath);
			if (_cache.TryGetValue(normalized, out var cached))
				return cached;

			var result = DetectCore(normalized, cancellationToken);

			if (_cache.Count >= MaxCacheEntries)
				_cache.Clear();

			_cache[normalized] = result;
			return result;
		}

		public Task<ProjectDetectionResult> DetectAsync(string? folderPath, CancellationToken cancellationToken = default)
			=> Task.Run(() => Detect(folderPath, cancellationToken), cancellationToken);

		public void Invalidate(string? folderPath = null)
		{
			if (string.IsNullOrWhiteSpace(folderPath))
			{
				_cache.Clear();
				return;
			}

			_cache.TryRemove(NormalizeFolderPath(folderPath), out _);
		}

		private static ProjectDetectionResult DetectCore(string folderPath, CancellationToken cancellationToken)
		{
			var kinds = new HashSet<ProjectKind>();
			var markers = new List<string>();
			string? firstSolutionFile = null;

			try
			{
				foreach (var entryPath in Directory.EnumerateFiles(folderPath))
				{
					cancellationToken.ThrowIfCancellationRequested();

					var name = Path.GetFileName(entryPath);
					var ext = Path.GetExtension(name);

					if (ext.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
						ext.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.DotNet);
						markers.Add(name);
						firstSolutionFile ??= entryPath;
					}
					else if (ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
							 ext.Equals(".fsproj", StringComparison.OrdinalIgnoreCase) ||
							 ext.Equals(".vbproj", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.DotNet);
						markers.Add(name);
					}
					else if (ext.Equals(".dpr", StringComparison.OrdinalIgnoreCase) ||
							 ext.Equals(".dpk", StringComparison.OrdinalIgnoreCase) ||
							 ext.Equals(".dproj", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.Delphi);
						markers.Add(name);
					}
					else if (name.Equals("pubspec.yaml", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.Flutter);
						markers.Add(name);
					}
					else if (name.Equals("package.json", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.Node);
						markers.Add(name);
					}
					else if (name.Equals("Cargo.toml", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.Rust);
						markers.Add(name);
					}
					else if (name.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase) ||
							 name.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.Python);
						markers.Add(name);
					}
					else if (name.Equals("go.mod", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.Go);
						markers.Add(name);
					}
					else if (name.Equals("CMakeLists.txt", StringComparison.OrdinalIgnoreCase))
					{
						kinds.Add(ProjectKind.CMake);
						markers.Add(name);
					}
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception)
			{
				return ProjectDetectionResult.Empty(folderPath);
			}

			if (kinds.Count == 0)
				return ProjectDetectionResult.Empty(folderPath);

			var orderedKinds = kinds.OrderBy(k => (int)k).ToList();
			var actions = BuildBuiltInActions(orderedKinds, firstSolutionFile);
			return new ProjectDetectionResult(folderPath, orderedKinds, markers, actions);
		}

		private static List<BuiltInProjectAction> BuildBuiltInActions(
			IReadOnlyList<ProjectKind> kinds,
			string? firstSolutionFile)
		{
			var actions = new List<BuiltInProjectAction>();

			foreach (var kind in kinds)
			{
				switch (kind)
				{
					case ProjectKind.DotNet:
						actions.Add(new BuiltInProjectAction("dotnet-mkfile-d", "Build Debug (mkfile d)", "mkfile", "d", true, kind));
						actions.Add(new BuiltInProjectAction("dotnet-mkfile-r", "Build Release (mkfile r)", "mkfile", "r", true, kind));
						if (!string.IsNullOrEmpty(firstSolutionFile))
							actions.Add(new BuiltInProjectAction("dotnet-open-sln", "Abrir solucao", "explorer.exe", "{solucao}", false, kind));
						actions.Add(new BuiltInProjectAction("dotnet-test", "Rodar testes", "dotnet", "test --nologo", false, kind));
						break;

					case ProjectKind.Delphi:
						actions.Add(new BuiltInProjectAction("delphi-mkfile-d", "Build Debug (mkfile d)", "mkfile", "d", true, kind));
						actions.Add(new BuiltInProjectAction("delphi-mkfile-r", "Build Release (mkfile r)", "mkfile", "r", true, kind));
						break;

					case ProjectKind.Flutter:
						actions.Add(new BuiltInProjectAction("flutter-mkfile-d", "Build Debug (mkfile d)", "mkfile", "d", true, kind));
						actions.Add(new BuiltInProjectAction("flutter-test", "Rodar testes", "flutter", "test", false, kind));
						break;

					case ProjectKind.Node:
						actions.Add(new BuiltInProjectAction("node-test", "Rodar testes (npm test)", "npm", "test", false, kind));
						break;

					case ProjectKind.Rust:
						actions.Add(new BuiltInProjectAction("rust-build", "Cargo Build", "cargo", "build", false, kind));
						actions.Add(new BuiltInProjectAction("rust-test", "Cargo Test", "cargo", "test", false, kind));
						break;

					case ProjectKind.Python:
						actions.Add(new BuiltInProjectAction("python-pytest", "Rodar pytest", "pytest", string.Empty, false, kind));
						break;

					case ProjectKind.Go:
						actions.Add(new BuiltInProjectAction("go-test", "Go Test", "go", "test ./...", false, kind));
						break;

					case ProjectKind.CMake:
						actions.Add(new BuiltInProjectAction("cmake-build", "CMake Build", "cmake", "--build build", false, kind));
						break;
				}
			}

			return actions;
		}

		private static string NormalizeFolderPath(string path)
			=> Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
	}
}
