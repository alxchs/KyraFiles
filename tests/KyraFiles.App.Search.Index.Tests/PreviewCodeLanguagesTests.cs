// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using ColorCode;
using ColorCode.Common;
using ColorCode.Parsing;
using KyraFiles.App.ViewModels.Previews;
using System.Globalization;

namespace KyraFiles.App.Search.Index.Tests;

/// <summary>F054 T1 (AC-1, FR-003): syntax rules for JSON, YAML, INI/TOML and shell, and the no-red palette.</summary>
[TestClass]
public sealed class PreviewCodeLanguagesTests
{
	private static List<string> Scopes(string source, ILanguage language)
	{
		var collector = new ScopeCollector();
		collector.Run(source, language);
		return collector.Found;
	}

	private static void HasScope(List<string> found, string scope, string text)
		=> CollectionAssert.Contains(found, $"{scope}={text}", string.Join(" | ", found));

	private sealed class ScopeCollector() : CodeColorizerBase(null, null)
	{
		public List<string> Found { get; } = [];

		public void Run(string source, ILanguage language)
			=> languageParser.Parse(source, language, Write);

		protected override void Write(string parsedSourceCode, IList<Scope> scopes)
		{
			foreach (var scope in scopes)
				Add(parsedSourceCode, scope);
		}

		private void Add(string source, Scope scope)
		{
			Found.Add($"{scope.Name}={source.Substring(scope.Index, scope.Length)}");
			foreach (var child in scope.Children)
				Add(source, child);
		}
	}

	[TestMethod]
	public void Json_HighlightsKeysNumbersAndConstants()
	{
		var found = Scopes("{\"name\": \"x\", \"size\": 42, \"ok\": true}", PreviewCodeLanguages.Json);

		HasScope(found, ScopeName.JsonKey, "\"name\"");
		HasScope(found, ScopeName.JsonString, "\"x\"");
		HasScope(found, ScopeName.JsonNumber, "42");
		HasScope(found, ScopeName.JsonConst, "true");
	}

	[TestMethod]
	public void Yaml_HighlightsKeysCommentsAndStrings()
	{
		var found = Scopes("# config\nserver:\n  port: 8080\n  - name: \"api\"\n", PreviewCodeLanguages.Yaml);

		HasScope(found, ScopeName.Comment, "# config");
		HasScope(found, ScopeName.JsonKey, "server");
		HasScope(found, ScopeName.JsonKey, "port");
		HasScope(found, ScopeName.JsonKey, "name");
		HasScope(found, ScopeName.Number, "8080");
		HasScope(found, ScopeName.String, "\"api\"");
	}

	[TestMethod]
	public void Ini_HighlightsSectionsKeysAndComments()
	{
		var found = Scopes("[core]\nautocrlf = true\n; comment\n[[bin]]\nname = \"k\"\n", PreviewCodeLanguages.Ini);

		HasScope(found, ScopeName.Keyword, "[core]");
		HasScope(found, ScopeName.Keyword, "[[bin]]");
		HasScope(found, ScopeName.JsonKey, "autocrlf");
		HasScope(found, ScopeName.Comment, "; comment");
		HasScope(found, ScopeName.String, "\"k\"");
	}

	[TestMethod]
	public void Shell_HighlightsKeywordsVariablesAndComments()
	{
		var found = Scopes("# build\nif [ -n \"$HOME\" ]; then echo ${USER} $1; fi\n", PreviewCodeLanguages.Shell);

		HasScope(found, ScopeName.Comment, "# build");
		HasScope(found, ScopeName.Keyword, "if");
		HasScope(found, ScopeName.Keyword, "fi");
		HasScope(found, ScopeName.PowerShellVariable, "${USER}");
		HasScope(found, ScopeName.PowerShellVariable, "$1");
	}
	[TestMethod]
	public void Palettes_HaveNoRedTones()
	{
		foreach (var palette in new[] { PreviewCodeStyles.Light, PreviewCodeStyles.Dark })
		{
			foreach (var style in palette)
			{
				foreach (var color in new[] { style.Foreground, style.Background })
				{
					if (string.IsNullOrEmpty(color) || color.Length != 9 || !color.StartsWith('#'))
						continue;

					var r = int.Parse(color.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					var g = int.Parse(color.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					var b = int.Parse(color.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					var isRed = r >= 140 && g <= 110 && b <= 110 && r - Math.Max(g, b) >= 80;

					Assert.IsFalse(isRed, $"{style.ScopeName} uses red tone {color}");
				}
			}
		}
	}
}
