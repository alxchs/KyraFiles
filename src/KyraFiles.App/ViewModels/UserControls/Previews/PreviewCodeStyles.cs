// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using ColorCode.Styling;

namespace KyraFiles.App.ViewModels.Previews
{
	/// <summary>
	/// ColorCode default palettes with every red tone replaced (project color rule: no red in the UI).
	/// </summary>
	internal static class PreviewCodeStyles
	{
		private static readonly Dictionary<string, string> LightReplacements = new(StringComparer.OrdinalIgnoreCase)
		{
			["#FFA31515"] = "#FF8A5A00",
			["#FFFF0000"] = "#FF6F42C1",
			["#FFFF4500"] = "#FF0E7490",
			["#FFFF8700"] = "#FF9A6700",
		};

		private static readonly Dictionary<string, string> DarkReplacements = new(StringComparer.OrdinalIgnoreCase)
		{
			["#FFA31515"] = "#FF4EC9B0",
			["#FFFF0000"] = "#FFB392F0",
			["#FFFF4500"] = "#FF67E8F9",
			["#FFD69D85"] = "#FFE2C08D",
			["#FFFF8700"] = "#FFE5B567",
		};

		public static StyleDictionary Light { get; } = Recolor(StyleDictionary.DefaultLight, LightReplacements);

		public static StyleDictionary Dark { get; } = Recolor(StyleDictionary.DefaultDark, DarkReplacements);

		private static StyleDictionary Recolor(StyleDictionary source, Dictionary<string, string> replacements)
		{
			var result = new StyleDictionary();
			foreach (var style in source)
			{
				result.Add(new Style(style.ScopeName)
				{
					Foreground = Replace(style.Foreground, replacements),
					Background = Replace(style.Background, replacements),
					ReferenceName = style.ReferenceName,
					Italic = style.Italic,
					Bold = style.Bold,
				});
			}

			return result;
		}

		private static string Replace(string? color, Dictionary<string, string> replacements)
			=> color is not null && replacements.TryGetValue(color, out var replacement) ? replacement : color!;
	}
}
