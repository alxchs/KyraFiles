// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace KyraFiles.Shared.BuildFolder;

/// <summary>
/// Checks whether a folder contains git-tracked files by executing git commands with fail-closed safety (FR-002, AC-5).
/// </summary>
public sealed class CliGitTrackingChecker : IGitTrackingChecker
{
	private readonly string _gitBinary;
	private readonly IDictionary<string, string?>? _environment;
	private readonly int _timeoutMs;

	public CliGitTrackingChecker(
		string? gitBinary = null,
		IDictionary<string, string?>? environment = null,
		int timeoutMs = 3000)
	{
		_gitBinary = string.IsNullOrWhiteSpace(gitBinary) ? "git" : gitBinary;
		_environment = environment;
		_timeoutMs = timeoutMs > 0 ? timeoutMs : 3000;
	}

	/// <summary>
	/// Returns true if the directory contains tracked files or if tracking status could not be verified (fail-closed).
	/// </summary>
	public bool HasTrackedFiles(string directoryPath)
	{
		var status = CheckDirectory(directoryPath);
		return status != GitTrackingResult.Clean;
	}

	/// <summary>
	/// Inspects the directory using git and returns the tracking result.
	/// </summary>
	public GitTrackingResult CheckDirectory(string directoryPath)
	{
		if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
			return GitTrackingResult.Clean;

		// Step 1: Check if inside a git repository
		var revParse = RunGit(directoryPath, ["rev-parse", "--is-inside-work-tree"], maxChars: 1024);
		if (revParse.TimedOut || revParse.StartFailed)
		{
			return GitTrackingResult.CannotVerify;
		}

		if (revParse.ExitCode != 0)
		{
			// Distinguish "not a git repository" from errors/cannot verify
			if (revParse.StdErr.Contains("not a git repository", StringComparison.OrdinalIgnoreCase) ||
			    revParse.StdOut.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
			{
				return GitTrackingResult.Clean;
			}

			return GitTrackingResult.CannotVerify;
		}

		if (!revParse.StdOut.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			return GitTrackingResult.CannotVerify;
		}

		// Step 2: Inside a git repository. Check if any files under directoryPath are tracked.
		var lsFiles = RunGit(directoryPath, ["ls-files"], maxChars: 2048);
		if (lsFiles.TimedOut || lsFiles.StartFailed || lsFiles.ExitCode != 0)
		{
			return GitTrackingResult.CannotVerify;
		}

		if (!string.IsNullOrWhiteSpace(lsFiles.StdOut.Trim()))
		{
			return GitTrackingResult.HasTrackedFiles;
		}

		return GitTrackingResult.Clean;
	}

	private (int ExitCode, string StdOut, string StdErr, bool TimedOut, bool StartFailed) RunGit(
		string directoryPath,
		string[] arguments,
		int maxChars)
	{
		string? executable = ResolveBinary();
		if (executable is null)
		{
			return (-1, string.Empty, "Git executable not found in PATH", false, true);
		}

		var psi = new ProcessStartInfo
		{
			FileName = executable,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};

		psi.ArgumentList.Add("-C");
		psi.ArgumentList.Add(directoryPath);
		foreach (var arg in arguments)
		{
			psi.ArgumentList.Add(arg);
		}

		if (_environment != null)
		{
			foreach (var kvp in _environment)
			{
				psi.Environment[kvp.Key] = kvp.Value;
			}
		}

		Process? process;
		try
		{
			process = Process.Start(psi);
		}
		catch
		{
			return (-1, string.Empty, "Failed to start git process", false, true);
		}

		if (process is null)
		{
			return (-1, string.Empty, "Process.Start returned null", false, true);
		}

		using (process)
		{
			var stdoutSb = new StringBuilder();
			var stderrSb = new StringBuilder();
			object sync = new();

			process.OutputDataReceived += (_, e) =>
			{
				if (e.Data is null) return;
				lock (sync)
				{
					if (stdoutSb.Length < maxChars)
						stdoutSb.AppendLine(e.Data);
				}
			};

			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data is null) return;
				lock (sync)
				{
					if (stderrSb.Length < maxChars)
						stderrSb.AppendLine(e.Data);
				}
			};

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			bool exited = process.WaitForExit(_timeoutMs);
			if (!exited)
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch
				{
				}

				return (-1, string.Empty, "Process timed out", true, false);
			}

			try
			{
				process.WaitForExit(500);
			}
			catch
			{
			}

			lock (sync)
			{
				return (process.ExitCode, stdoutSb.ToString(), stderrSb.ToString(), false, false);
			}
		}
	}

	private string? ResolveBinary()
	{
		if (_environment != null && _environment.TryGetValue("PATH", out var customPath))
		{
			if (string.IsNullOrEmpty(customPath))
				return null;

			var paths = customPath.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
			foreach (var p in paths)
			{
				var candidate = Path.Combine(p, _gitBinary.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? _gitBinary : _gitBinary + ".exe");
				if (File.Exists(candidate))
					return candidate;
			}

			return null;
		}

		return _gitBinary;
	}
}
