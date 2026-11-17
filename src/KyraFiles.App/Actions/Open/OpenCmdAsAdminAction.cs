// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Diagnostics;

namespace KyraFiles.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class OpenCmdAsAdminAction : OpenCmdAction
	{
		public override string Label
			=> "Prompt de Comando (Admin)";

		public override string Description
			=> "Abre o Prompt de Comando (CMD) como Administrador na pasta atual";

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

