// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class PowerShellHelpersTests
	{
		[TestMethod]
		public void PreferredExecutable_IsNotNullOrEmpty()
		{
			PowerShellHelpers.ResetCache();
			var exe = PowerShellHelpers.PreferredExecutable;

			Assert.IsFalse(string.IsNullOrWhiteSpace(exe), "PreferredExecutable should not be null or whitespace.");
		}

		[TestMethod]
		public void PreferredExecutable_ResolvesPwshWhenAvailable()
		{
			PowerShellHelpers.ResetCache();
			var exe = PowerShellHelpers.PreferredExecutable;

			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			var pwshStandard = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");

			if (File.Exists(pwshStandard))
			{
				Assert.IsTrue(
					exe.EndsWith("pwsh.exe", StringComparison.OrdinalIgnoreCase),
					$"Expected PreferredExecutable to resolve pwsh.exe when installed at {pwshStandard}, but got {exe}");
			}
			else
			{
				Assert.IsTrue(
					exe.EndsWith("powershell.exe", StringComparison.OrdinalIgnoreCase) || exe.EndsWith("pwsh.exe", StringComparison.OrdinalIgnoreCase),
					$"Unexpected executable resolution: {exe}");
			}
		}

		[TestMethod]
		public void PreferredExecutable_CanLaunchAndExecuteCommand()
		{
			PowerShellHelpers.ResetCache();
			var exe = PowerShellHelpers.PreferredExecutable;

			var startInfo = new ProcessStartInfo
			{
				FileName = exe,
				Arguments = "-NoProfile -Command \"Write-Output 'PWSHTEST_SUCCESS'\"",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = Process.Start(startInfo);
			Assert.IsNotNull(process, "Process should have started successfully.");

			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit(5000);

			Assert.AreEqual(0, process.ExitCode, $"PowerShell exited with code {process.ExitCode}");
			StringAssert.Contains(output, "PWSHTEST_SUCCESS", "Output should contain the expected string.");
		}
	}
}
