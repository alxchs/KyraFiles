// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.Shared.CodeSearch;

namespace KyraFiles.Shared.RepoMap
{
	/// <summary>
	/// High-performance read-only repository structure and metrics analyzer.
	/// Conforms to FR-001, FR-002, FR-003, NFR-001, and AC-1 through AC-7.
	/// </summary>
	public sealed class RepoMapAnalyzer
	{
		private const int ReadBufferSize = 16384;
		private const int BinaryCheckSampleSize = 8192;

		public async Task<RepoMapResult> AnalyzeAsync(
			RepoMapOptions options,
			IProgress<double>? progress = null,
			CancellationToken cancellationToken = default)
		{
			if (options is null)
				throw new ArgumentNullException(nameof(options));

			var stopwatch = Stopwatch.StartNew();
			var result = new RepoMapResult
			{
				RootPath = options.RootPath
			};

			if (string.IsNullOrWhiteSpace(options.RootPath) || !Directory.Exists(options.RootPath))
			{
				result.Elapsed = stopwatch.Elapsed;
				return result;
			}

			GitIgnoreMatcher? gitIgnoreMatcher = null;
			if (options.RespectGitIgnore)
			{
				gitIgnoreMatcher = GitIgnoreMatcher.LoadFromDirectory(options.RootPath, includeDefaults: true);
			}

			var allFiles = new List<RepoFileInfo>();
			var languageGroups = new Dictionary<string, LanguageInfo>(StringComparer.OrdinalIgnoreCase);

			try
			{
				await Task.Run(() =>
				{
					TraverseAndCollectFiles(
						options.RootPath,
						options.RootPath,
						options,
						gitIgnoreMatcher,
						allFiles,
						languageGroups,
						progress,
						cancellationToken);
				}, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				result.WasCancelled = true;
				result.Elapsed = stopwatch.Elapsed;
				return result;
			}

			// Sort files within each language from largest to smallest (AC-2)
			long grandTotalLines = 0;
			long grandTotalBytes = 0;
			int grandTotalFiles = 0;

			foreach (var lang in languageGroups.Values)
			{
				lang.Files = lang.Files.OrderByDescending(f => f.FileSizeBytes).ThenByDescending(f => f.LineCount).ToList();
				grandTotalLines += lang.TotalLines;
				grandTotalBytes += lang.TotalBytes;
				grandTotalFiles += lang.FileCount;
			}

			// Calculate percentages
			foreach (var lang in languageGroups.Values)
			{
				lang.PercentLines = grandTotalLines > 0 ? (double)lang.TotalLines / grandTotalLines * 100.0 : 0.0;
				lang.PercentBytes = grandTotalBytes > 0 ? (double)lang.TotalBytes / grandTotalBytes * 100.0 : 0.0;
			}

			result.Languages = languageGroups.Values
				.OrderByDescending(l => l.TotalLines)
				.ThenByDescending(l => l.TotalBytes)
				.ToList();

			result.TotalFiles = grandTotalFiles;
			result.TotalLines = grandTotalLines;
			result.TotalBytes = grandTotalBytes;
			result.AllFiles = allFiles.OrderByDescending(f => f.FileSizeBytes).ThenByDescending(f => f.LineCount).ToList();

			// Git Hot Spots analysis (AC-3, AC-4, NFR-001)
			if (options.IncludeGitHotSpots && !cancellationToken.IsCancellationRequested)
			{
				await AnalyzeGitHotSpotsAsync(options.RootPath, result, cancellationToken).ConfigureAwait(false);
			}

			stopwatch.Stop();
			result.Elapsed = stopwatch.Elapsed;
			return result;
		}

		private static void TraverseAndCollectFiles(
			string rootPath,
			string currentPath,
			RepoMapOptions options,
			GitIgnoreMatcher? gitIgnoreMatcher,
			List<RepoFileInfo> allFiles,
			Dictionary<string, LanguageInfo> languageGroups,
			IProgress<double>? progress,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Enumerate directories with early pruning of ignored directories
			string[] subDirectories;
			try
			{
				subDirectories = Directory.GetDirectories(currentPath);
			}
			catch
			{
				return;
			}

			foreach (var subDir in subDirectories)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var relDir = GetRelativePath(rootPath, subDir);

				if (gitIgnoreMatcher is not null && gitIgnoreMatcher.IsIgnored(relDir, isDirectory: true))
					continue;

				TraverseAndCollectFiles(
					rootPath,
					subDir,
					options,
					gitIgnoreMatcher,
					allFiles,
					languageGroups,
					progress,
					cancellationToken);
			}

			// Enumerate files
			string[] files;
			try
			{
				files = Directory.GetFiles(currentPath);
			}
			catch
			{
				return;
			}

			var buffer = new byte[ReadBufferSize];

			foreach (var file in files)
			{
				cancellationToken.ThrowIfCancellationRequested();
				var relPath = GetRelativePath(rootPath, file);

				if (gitIgnoreMatcher is not null && gitIgnoreMatcher.IsIgnored(relPath, isDirectory: false))
					continue;

				FileInfo fileInfo;
				try
				{
					fileInfo = new FileInfo(file);
					if (fileInfo.Length > options.MaxFileSizeBytes)
						continue;
				}
				catch
				{
					continue;
				}

				var extension = fileInfo.Extension;
				string language;
				if (!string.IsNullOrEmpty(extension) && options.ExtensionToLanguageMap.TryGetValue(extension, out var mappedLang))
				{
					language = mappedLang;
				}
				else if (string.IsNullOrEmpty(extension))
				{
					language = "No Extension";
				}
				else
				{
					language = extension.ToUpperInvariant().TrimStart('.') + " File";
				}

				long lineCount = 0;
				bool isBinary = false;

				try
				{
					using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
					isBinary = CheckIsBinary(stream, buffer);

					if (!isBinary)
					{
						stream.Seek(0, SeekOrigin.Begin);
						lineCount = CountLines(stream, buffer);
					}
				}
				catch
				{
					// If inaccessible, skip
					continue;
				}

				if (isBinary)
				{
					language = "Binary";
				}

				var repoFile = new RepoFileInfo
				{
					FilePath = file,
					RelativePath = relPath,
					Language = language,
					FileSizeBytes = fileInfo.Length,
					LineCount = lineCount
				};

				allFiles.Add(repoFile);

				if (!languageGroups.TryGetValue(language, out var langInfo))
				{
					langInfo = new LanguageInfo
					{
						LanguageName = language
					};
					languageGroups[language] = langInfo;
				}

				langInfo.FileCount++;
				langInfo.TotalBytes += fileInfo.Length;
				langInfo.TotalLines += lineCount;
				langInfo.Files.Add(repoFile);
			}
		}

		private static long CountLines(FileStream stream, byte[] buffer)
		{
			long lines = 0;
			int bytesRead;
			bool hasBytes = false;
			byte lastByte = 0;

			while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				hasBytes = true;
				for (int i = 0; i < bytesRead; i++)
				{
					byte b = buffer[i];
					if (b == 0x0A) // LF
					{
						lines++;
					}
					lastByte = b;
				}
			}

			// If file has content and does not end with a newline, count the last line
			if (hasBytes && lastByte != 0x0A)
			{
				lines++;
			}

			return lines;
		}

		private static bool CheckIsBinary(FileStream stream, byte[] buffer)
		{
			int bytesToRead = (int)Math.Min(stream.Length, BinaryCheckSampleSize);
			if (bytesToRead <= 0)
				return false;

			int read = stream.Read(buffer, 0, bytesToRead);
			if (read <= 0)
				return false;

			// Check common BOMs: UTF-8 BOM, UTF-16 LE, UTF-16 BE
			if (read >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
				return false;

			if (read >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE) // UTF-16 LE
				return false;

			if (read >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF) // UTF-16 BE
				return false;

			for (int i = 0; i < read; i++)
			{
				if (buffer[i] == 0)
				{
					// Check for UTF-16 ASCII alternating pattern
					if (i + 1 < read && (i % 2 == 1 || (i > 0 && buffer[i - 1] != 0)))
					{
						continue;
					}
					return true;
				}
			}

			return false;
		}

		private static async Task AnalyzeGitHotSpotsAsync(
			string rootPath,
			RepoMapResult result,
			CancellationToken cancellationToken)
		{
			// Check if repository has .git
			bool hasGit = HasGitRepository(rootPath);
			if (!hasGit)
			{
				result.IsGitRepository = false;
				result.GitStatusMessage = "Esta pasta não é um repositório Git. O resumo por linguagem permanece disponível.";
				return;
			}

			result.IsGitRepository = true;

			try
			{
				var churnByPath = await FetchGitChurnAsync(rootPath, cancellationToken).ConfigureAwait(false);

				if (churnByPath.Count > 0)
				{
					foreach (var file in result.AllFiles)
					{
						var normalizedRel = file.RelativePath.Replace('\\', '/');
						if (churnByPath.TryGetValue(normalizedRel, out int count))
						{
							file.CommitCount12Months = count;
						}
					}

					result.HotSpots = result.AllFiles
						.Where(f => f.CommitCount12Months > 0)
						.OrderByDescending(f => f.HotSpotScore)
						.ThenByDescending(f => f.CommitCount12Months)
						.Take(20)
						.ToList();
				}
			}
			catch (Exception ex)
			{
				result.GitStatusMessage = $"Não foi possível obter histórico do Git: {ex.Message}";
			}
		}

		private static bool HasGitRepository(string rootPath)
		{
			var current = new DirectoryInfo(rootPath);
			while (current != null && current.Exists)
			{
				if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
					return true;

				current = current.Parent;
			}
			return false;
		}

		private static async Task<Dictionary<string, int>> FetchGitChurnAsync(string rootPath, CancellationToken cancellationToken)
		{
			var churn = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

			var psi = new ProcessStartInfo
			{
				FileName = "git",
				Arguments = "log --name-only --format=\"\" --since=\"12 months ago\"",
				WorkingDirectory = rootPath,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			using var process = new Process { StartInfo = psi };
			if (!process.Start())
				return churn;

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(TimeSpan.FromSeconds(15)); // 15s max timeout

			try
			{
				using (cts.Token.Register(() =>
				{
					try
					{
						if (!process.HasExited)
							process.Kill();
					}
					catch { }
				}))
				{
					string? line;
					while ((line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)) is not null)
					{
						cts.Token.ThrowIfCancellationRequested();
						if (string.IsNullOrWhiteSpace(line))
							continue;

						var normalized = line.Trim().Replace('\\', '/');
						if (churn.TryGetValue(normalized, out int count))
							churn[normalized] = count + 1;
						else
							churn[normalized] = 1;
					}

					await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				// Process timed out or cancelled
			}

			return churn;
		}

		private static string GetRelativePath(string relativeTo, string path)
		{
			if (string.IsNullOrEmpty(relativeTo) || string.IsNullOrEmpty(path))
				return path;

			if (!relativeTo.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) &&
			    !relativeTo.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
			{
				relativeTo += Path.DirectorySeparatorChar;
			}

			var uriRelTo = new Uri(relativeTo, UriKind.Absolute);
			var uriPath = new Uri(path, UriKind.Absolute);
			var relUri = uriRelTo.MakeRelativeUri(uriPath);
			return Uri.UnescapeDataString(relUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
		}
	}
}
