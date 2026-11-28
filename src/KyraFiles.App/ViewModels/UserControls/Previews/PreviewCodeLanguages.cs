// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using ColorCode;
using ColorCode.Common;

namespace KyraFiles.App.ViewModels.Previews
{
	/// <summary>
	/// Syntax rules for formats that ColorCode does not ship (JSON, YAML, INI/TOML and shell scripts).
	/// </summary>
	internal static class PreviewCodeLanguages
	{
		private const string DoubleQuoted = @"""(?:[^""\\\r\n]|\\.)*""";
		private const string SingleQuoted = @"'[^'\r\n]*'";
		private const string NumberPattern = @"(?<![\w.])-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?(?![\w.])";

		public static ILanguage Json { get; } = new RuleLanguage("json", "JSON",
		[
			Rule($@"({DoubleQuoted})(?=\s*:)", 1, ScopeName.JsonKey),
			Rule(DoubleQuoted, 0, ScopeName.JsonString),
			Rule(NumberPattern, 0, ScopeName.JsonNumber),
			Rule(@"\b(?:true|false|null)\b", 0, ScopeName.JsonConst),
		]);

		public static ILanguage Yaml { get; } = new RuleLanguage("yaml", "YAML",
		[
			Rule(@"(?<![^\s])#[^\r\n]*", 0, ScopeName.Comment),
			Rule(@"(?m:^)[ \t]*(?:-[ \t]+)?([^\s#:\-""'][^:#\r\n]*?)(?=[ \t]*:(?:[ \t]|\r?$))", 1, ScopeName.JsonKey),
			Rule($"{DoubleQuoted}|{SingleQuoted}", 0, ScopeName.String),
			Rule(@"(?m:^)(?:---|\.\.\.)(?=\s|$)", 0, ScopeName.Keyword),
			Rule(@"[&*][\w\-]+", 0, ScopeName.PreprocessorKeyword),
			Rule(NumberPattern, 0, ScopeName.Number),
			Rule(@"\b(?:true|false|null|yes|no|on|off)\b|~", 0, ScopeName.JsonConst),
		]);

		public static ILanguage Ini { get; } = new RuleLanguage("ini", "INI/TOML",
		[
			Rule(@"(?m:^)[ \t]*[;#][^\r\n]*", 0, ScopeName.Comment),
			Rule(@"(?m:^)[ \t]*(\[\[?[^\]\r\n]+\]\]?)", 1, ScopeName.Keyword),
			Rule(@"(?m:^)[ \t]*([\w.\-""]+?)(?=[ \t]*[=:])", 1, ScopeName.JsonKey),
			Rule($"{DoubleQuoted}|{SingleQuoted}", 0, ScopeName.String),
			Rule(NumberPattern, 0, ScopeName.Number),
			Rule(@"\b(?:true|false)\b", 0, ScopeName.JsonConst),
		]);

		public static ILanguage Shell { get; } = new RuleLanguage("sh", "Shell",
		[
			Rule(@"(?<![\w$#])#[^\r\n]*", 0, ScopeName.Comment),
			Rule($"{DoubleQuoted}|{SingleQuoted}", 0, ScopeName.String),
			Rule(@"\$(?:\{[^}\r\n]*\}|\w+|[@#?$!*\-0-9])", 0, ScopeName.PowerShellVariable),
			Rule(@"\b(?:if|then|else|elif|fi|for|while|until|do|done|case|esac|in|function|return|exit|local|export|readonly|declare|unset|shift|break|continue|source|select)\b", 0, ScopeName.Keyword),
			Rule(NumberPattern, 0, ScopeName.Number),
		]);

		private static LanguageRule Rule(string pattern, int group, string scope)
			=> new(pattern, new Dictionary<int, string> { [group] = scope });

		private sealed class RuleLanguage(string id, string name, IList<LanguageRule> rules) : ILanguage
		{
			public string Id => id;

			public string Name => name;

			public string CssClassName => id;

			public string? FirstLinePattern => null;

			public IList<LanguageRule> Rules => rules;

			public bool HasAlias(string lang)
				=> string.Equals(lang, id, StringComparison.OrdinalIgnoreCase);
		}
	}
}
