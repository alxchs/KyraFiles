// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Diagnostics;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal partial class OpenCmdAction : OpenTerminalAction
	{
		public override string Label
			=> "Prompt de Comando";

		public override string Description
			=> "Abre o Prompt de Comando (CMD) na pasta atual";

		public override HotKey HotKey
			=> default;

		protected override ProcessStartInfo? GetProcessStartInfo(string[] paths)
		{
			if (paths.Length is 0)
				return null;

			return new ProcessStartInfo
			{
				FileName = "cmd.exe",
				WorkingDirectory = paths[0],
				UseShellExecute = true
			};
		}
	}
}

