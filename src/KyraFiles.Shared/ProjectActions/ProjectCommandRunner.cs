// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.Shared.ProjectActions
{
	public sealed record ProjectActionExecutionContext(
		string FolderPath,
		IReadOnlyList<string> SelectedPaths,
		string? GitRepoRoot = null,
		string? SolutionPath = null);

	public sealed record ProjectCommandResult(
		int ExitCode,
		bool Succeeded,
		bool Cancelled,
		bool OutputTruncated,
		IReadOnlyList<string> OutputLines,
		IReadOnlyList<string> ErrorLines,
		TimeSpan Duration);

	public static class PlaceholderExpander
	{
		public static string ToPowerShellLiteral(string? value)
			=> $"'{(value ?? string.Empty).Replace("'", "''", StringComparison.Ordinal)}'";

		public static IReadOnlyList<string> ExpandArgumentTemplates(
			IEnumerable<string> argumentTemplates,
			ProjectActionExecutionContext context,
			string? singleSelectedPath = null)
		{
			ArgumentNullException.ThrowIfNull(argumentTemplates);
			ArgumentNullException.ThrowIfNull(context);

			var result = new List<string>();
			var activeItem = singleSelectedPath ?? context.SelectedPaths.FirstOrDefault() ?? context.FolderPath;
			var itemName = string.IsNullOrEmpty(activeItem) ? string.Empty : Path.GetFileName(activeItem);
			var itemExt = string.IsNullOrEmpty(activeItem) ? string.Empty : Path.GetExtension(activeItem);

			foreach (var template in argumentTemplates)
			{
				if (string.Equals(template, "{selecao}", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(template, "{seleção}", StringComparison.OrdinalIgnoreCase))
				{
					if (singleSelectedPath is not null)
					{
						result.Add(singleSelectedPath);
					}
					else if (context.SelectedPaths.Count > 0)
					{
						result.AddRange(context.SelectedPaths);
					}
					continue;
				}

				var expanded = template
					.Replace("{caminho}", context.FolderPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("{raiz do repositorio}", context.GitRepoRoot ?? context.FolderPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("{raiz do repositório}", context.GitRepoRoot ?? context.FolderPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("{nome}", itemName, StringComparison.OrdinalIgnoreCase)
					.Replace("{extensao}", itemExt, StringComparison.OrdinalIgnoreCase)
					.Replace("{extensão}", itemExt, StringComparison.OrdinalIgnoreCase)
					.Replace("{solucao}", context.SolutionPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("{solução}", context.SolutionPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("{selecao}", activeItem ?? string.Empty, StringComparison.OrdinalIgnoreCase)
					.Replace("{seleção}", activeItem ?? string.Empty, StringComparison.OrdinalIgnoreCase);

				result.Add(expanded);
			}

			return result;
		}

		public static string BuildSafePowerShellCommand(
			string commandName,
			IReadOnlyList<string> literalArguments)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(commandName);

			var sb = new StringBuilder();
			sb.Append("& ");
			sb.Append(ToPowerShellLiteral(commandName));

			foreach (var arg in literalArguments)
			{
				sb.Append(' ');
				sb.Append(ToPowerShellLiteral(arg));
			}

			return sb.ToString();
		}
	}

	public sealed class ProjectCommandRunner
	{
		public const int DefaultMaxLines = 500;
		public const int DefaultMaxBytes = 256 * 1024;

		public async Task<ProjectCommandResult> RunAsync(
			string executable,
			IReadOnlyList<string> arguments,
			string workingDirectory,
			int maxLines = DefaultMaxLines,
			int maxBytes = DefaultMaxBytes,
			CancellationToken cancellationToken = default)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(executable);

			var sw = Stopwatch.StartNew();
			var stdoutLines = new Queue<string>();
			var stderrLines = new Queue<string>();
			var totalBytes = 0;
			var truncated = false;
			var lineLimit = Math.Max(1, maxLines);
			var byteLimit = Math.Max(1024, maxBytes);
			var sync = new object();

			var psi = new ProcessStartInfo
			{
				FileName = executable,
				WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8,
			};

			foreach (var arg in arguments)
				psi.ArgumentList.Add(arg);

			using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

			void AppendLine(Queue<string> target, string? line)
			{
				if (line is null)
					return;

				lock (sync)
				{
					var bytes = Encoding.UTF8.GetByteCount(line);
					if (totalBytes + bytes > byteLimit)
					{
						truncated = true;
						return;
					}

					if (target.Count >= lineLimit)
					{
						var removed = target.Dequeue();
						totalBytes = Math.Max(0, totalBytes - Encoding.UTF8.GetByteCount(removed));
						truncated = true;
					}

					target.Enqueue(line);
					totalBytes += bytes;
				}
			}

			process.OutputDataReceived += (_, e) => AppendLine(stdoutLines, e.Data);
			process.ErrorDataReceived += (_, e) => AppendLine(stderrLines, e.Data);

			if (!process.Start())
			{
				sw.Stop();
				return new ProjectCommandResult(-1, false, false, false, [], ["Falha ao iniciar processo."], sw.Elapsed);
			}

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			var cancelled = false;
			try
			{
				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				cancelled = true;
				try
				{
					if (!process.HasExited)
						process.Kill(entireProcessTree: true);
				}
				catch
				{
					// Process may have exited between check and Kill.
				}

				try
				{
					await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromMilliseconds(800)).ConfigureAwait(false);
				}
				catch
				{
					// Best-effort wait for clean teardown.
				}
			}

			sw.Stop();
			var exitCode = cancelled ? -1 : process.ExitCode;

			List<string> outCopy;
			List<string> errCopy;
			bool wasTruncated;
			lock (sync)
			{
				outCopy = [.. stdoutLines];
				errCopy = [.. stderrLines];
				wasTruncated = truncated;
			}

			return new ProjectCommandResult(
				exitCode,
				!cancelled && exitCode == 0,
				cancelled,
				wasTruncated,
				outCopy,
				errCopy,
				sw.Elapsed);
		}
	}
}
