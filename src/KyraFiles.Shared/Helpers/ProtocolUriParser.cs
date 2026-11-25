// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;

namespace KyraFiles.Shared.Helpers
{
	public enum ProtocolAction
	{
		Home,
		Folder,
		Tab,
		Cmd,
	}

	public sealed record ProtocolLaunchInfo(
		ProtocolAction Action,
		string TargetValue,
		int? MoveDx = null,
		int? MoveDy = null);

	public static class ProtocolUriParser
	{
		public const string ProtocolScheme = "kyrafiles";
		public const string ProtocolUriPrefix = "kyrafiles:";

		public static ProtocolLaunchInfo Parse(Uri uri)
		{
			ArgumentNullException.ThrowIfNull(uri);

			if (!string.Equals(uri.Scheme, ProtocolScheme, StringComparison.OrdinalIgnoreCase))
			{
				return new ProtocolLaunchInfo(ProtocolAction.Home, string.Empty);
			}

			// Caso padrão: kyrafiles: sem query e sem path ou path root
			if (string.IsNullOrEmpty(uri.Query) &&
				(string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/" || string.IsNullOrEmpty(uri.LocalPath) || uri.LocalPath == "/"))
			{
				return new ProtocolLaunchInfo(ProtocolAction.Home, string.Empty);
			}

			var uriQuery = uri.Query.TrimStart('?');
			var parsedArgs = uriQuery.Contains('=') ? uriQuery.Split('=') : [];
			string actionStr = parsedArgs.Length > 0 ? parsedArgs[0].ToLowerInvariant() : "folder";
			string unescapedValue = string.Empty;
			int? moveDx = null;
			int? moveDy = null;

			if (parsedArgs.Length > 1)
			{
				unescapedValue = Uri.UnescapeDataString(parsedArgs[1].Split('&')[0]);
				if (actionStr == "tab" && parsedArgs.Length > 3 &&
					int.TryParse(parsedArgs[2].Split('&')[0], out var dx) &&
					int.TryParse(parsedArgs[3], out var dy))
				{
					moveDx = dx;
					moveDy = dy;
				}
			}
			else
			{
				var orig = uri.OriginalString;
				int colonIndex = orig.IndexOf(':');
				var rawPath = colonIndex >= 0 && colonIndex + 1 < orig.Length
					? orig.Substring(colonIndex + 1).TrimStart('/')
					: string.Empty;
				unescapedValue = Uri.UnescapeDataString(rawPath);
			}

			if (string.IsNullOrWhiteSpace(unescapedValue))
			{
				return new ProtocolLaunchInfo(ProtocolAction.Home, string.Empty);
			}

			return actionStr switch
			{
				"tab" => new ProtocolLaunchInfo(ProtocolAction.Tab, unescapedValue, moveDx, moveDy),
				"cmd" => new ProtocolLaunchInfo(ProtocolAction.Cmd, unescapedValue),
				_ => new ProtocolLaunchInfo(ProtocolAction.Folder, unescapedValue)
			};
		}
	}
}
