// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using KyraFiles.Shared.Helpers;
using System.Diagnostics;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal partial class OpenPowerShellAction : OpenTerminalAction
	{
		public override string Label
			=> "PowerShell";

		public override string Description
			=> "Abre o PowerShell na pasta atual";

		public override HotKey HotKey
			=> default;

		protected override ProcessStartInfo? GetProcessStartInfo(string[] paths)
		{
			if (paths.Length is 0)
				return null;

			return new ProcessStartInfo
			{
				FileName = PowerShellHelpers.PreferredExecutable,
				WorkingDirectory = paths[0],
				UseShellExecute = true
			};
		}
	}
}

