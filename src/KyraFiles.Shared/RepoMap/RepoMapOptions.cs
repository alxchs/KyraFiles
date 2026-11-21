// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace KyraFiles.Shared.RepoMap
{
	/// <summary>
	/// Configuration options for repository map analysis.
	/// </summary>
	public sealed class RepoMapOptions
	{
		public string RootPath { get; set; } = string.Empty;
		public bool RespectGitIgnore { get; set; } = true;
		public bool IncludeGitHotSpots { get; set; } = true;
		public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50 MB
		public Dictionary<string, string> ExtensionToLanguageMap { get; } = new(StringComparer.OrdinalIgnoreCase);

		public RepoMapOptions()
		{
			InitializeDefaultExtensions();
		}

		/// <summary>
		/// Adds or updates an extension-to-language mapping (FR-003).
		/// </summary>
		public void SetLanguageForExtension(string extension, string languageName)
		{
			if (string.IsNullOrWhiteSpace(extension))
				return;

			if (!extension.StartsWith(".", StringComparison.Ordinal))
				extension = "." + extension;

			ExtensionToLanguageMap[extension] = languageName;
		}

		private void InitializeDefaultExtensions()
		{
			ExtensionToLanguageMap[".cs"] = "C#";
			ExtensionToLanguageMap[".xaml"] = "XAML";
			ExtensionToLanguageMap[".xml"] = "XML";
			ExtensionToLanguageMap[".json"] = "JSON";
			ExtensionToLanguageMap[".md"] = "Markdown";
			ExtensionToLanguageMap[".txt"] = "Text";
			ExtensionToLanguageMap[".ts"] = "TypeScript";
			ExtensionToLanguageMap[".tsx"] = "TypeScript React";
			ExtensionToLanguageMap[".js"] = "JavaScript";
			ExtensionToLanguageMap[".jsx"] = "JavaScript React";
			ExtensionToLanguageMap[".html"] = "HTML";
			ExtensionToLanguageMap[".htm"] = "HTML";
			ExtensionToLanguageMap[".css"] = "CSS";
			ExtensionToLanguageMap[".scss"] = "SCSS";
			ExtensionToLanguageMap[".sass"] = "SASS";
			ExtensionToLanguageMap[".less"] = "LESS";
			ExtensionToLanguageMap[".py"] = "Python";
			ExtensionToLanguageMap[".cpp"] = "C++";
			ExtensionToLanguageMap[".c"] = "C";
			ExtensionToLanguageMap[".h"] = "C/C++ Header";
			ExtensionToLanguageMap[".hpp"] = "C++ Header";
			ExtensionToLanguageMap[".rs"] = "Rust";
			ExtensionToLanguageMap[".go"] = "Go";
			ExtensionToLanguageMap[".java"] = "Java";
			ExtensionToLanguageMap[".kt"] = "Kotlin";
			ExtensionToLanguageMap[".dart"] = "Dart";
			ExtensionToLanguageMap[".sql"] = "SQL";
			ExtensionToLanguageMap[".sh"] = "Shell Script";
			ExtensionToLanguageMap[".bash"] = "Bash Script";
			ExtensionToLanguageMap[".ps1"] = "PowerShell";
			ExtensionToLanguageMap[".psm1"] = "PowerShell Module";
			ExtensionToLanguageMap[".psd1"] = "PowerShell Data";
			ExtensionToLanguageMap[".yaml"] = "YAML";
			ExtensionToLanguageMap[".yml"] = "YAML";
			ExtensionToLanguageMap[".toml"] = "TOML";
			ExtensionToLanguageMap[".ini"] = "INI / Config";
			ExtensionToLanguageMap[".cfg"] = "INI / Config";
			ExtensionToLanguageMap[".iss"] = "Inno Setup";
			ExtensionToLanguageMap[".proto"] = "Protobuf";
			ExtensionToLanguageMap[".gradle"] = "Gradle";
			ExtensionToLanguageMap[".sln"] = "Visual Studio Solution";
			ExtensionToLanguageMap[".slnx"] = "Visual Studio Solution (slnx)";
			ExtensionToLanguageMap[".csproj"] = "C# Project";
			ExtensionToLanguageMap[".props"] = "MSBuild Props";
			ExtensionToLanguageMap[".targets"] = "MSBuild Targets";
			ExtensionToLanguageMap[".editorconfig"] = "EditorConfig";
			ExtensionToLanguageMap[".gitignore"] = "Git Configuration";
			ExtensionToLanguageMap[".gitattributes"] = "Git Configuration";
		}
	}
}
