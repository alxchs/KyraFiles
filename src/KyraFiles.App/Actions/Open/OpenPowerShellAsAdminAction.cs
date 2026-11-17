// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Diagnostics;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenPowerShellAsAdminAction : OpenPowerShellAction
	{
		public override string Label
			=> "PowerShell (Admin)";

		public override string Description
			=> "Abre o PowerShell como Administrador na pasta atual";

		protected override ProcessStartInfo? GetProcessStartInfo(string[] paths)
		{
			var startInfo = base.GetProcessStartInfo(paths);
			if (startInfo is not null)
			{
				startInfo.Verb = "runas";
				startInfo.UseShellExecute = true;
			}

			return startInfo;
		}
	}
}

