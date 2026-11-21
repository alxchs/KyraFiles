// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.Shared.CodeSearch
{
	/// <summary>
	/// High-performance code search engine with streaming, gitignore support, binary filtering, and multi-encoding (F051).
	/// </summary>
	public static class CodeSearchEngine
	{
		private const int BinaryCheckBufferSize = 8192; // 8 KiB buffer to test for null bytes (AC-5)

		public static string? ValidateQuery(CodeSearchOptions options)
		{
			if (options is null)
				return "Opções não informadas.";

			if (string.IsNullOrEmpty(options.Query))
				return "O termo de busca não pode ser vazio.";

			if (options.IsRegex)
			{
				try
				{
					_ = new Regex(options.Query, options.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
				}
				catch (ArgumentException ex)
				{
					return $"Expressão regular inválida: {ex.Message}";
				}
			}

			return null;
		}

		public static async Task<CodeSearchResult> SearchAsync(
			CodeSearchOptions options,
			IProgress<CodeSearchProgress>? progress = null,
			Action<CodeSearchResultItem>? onMatch = null,
			CancellationToken cancellationToken = default)
		{
			if (options is null)
				throw new ArgumentNullException(nameof(options));

			var validationError = ValidateQuery(options);
			if (validationError is not null)
			{
				return new CodeSearchResult(
					Array.Empty<CodeSearchResultItem>(),
					totalFilesScanned: 0,
					totalFilesIgnored: 0,
					TimeSpan.Zero,
					errorMessage: validationError);
			}

			var sw = Stopwatch.StartNew();
			try
			{
				return await Task.Run(() => ExecuteSearch(options, progress, onMatch, sw, cancellationToken), cancellationToken);
			}
			catch (OperationCanceledException)
			{
				return new CodeSearchResult(
					Array.Empty<CodeSearchResultItem>(),
					totalFilesScanned: 0,
					totalFilesIgnored: 0,
					sw.Elapsed,
					wasCancelled: true);
			}
		}

		private static CodeSearchResult ExecuteSearch(
			CodeSearchOptions options,
			IProgress<CodeSearchProgress>? progress,
			Action<CodeSearchResultItem>? onMatch,
			Stopwatch sw,
			CancellationToken cancellationToken)
		{
			var matches = new List<CodeSearchResultItem>();
			int filesScanned = 0;
			int filesIgnored = 0;
			bool reachedMaxResults = false;

			var root = Path.GetFullPath(options.SearchDirectory);
			if (!Directory.Exists(root))
			{
				return new CodeSearchResult(
					matches,
					totalFilesScanned: 0,
					totalFilesIgnored: 0,
					sw.Elapsed,
					errorMessage: $"Diretório não encontrado: {root}");
			}

			GitIgnoreMatcher? gitIgnore = options.RespectGitIgnore ? GitIgnoreMatcher.LoadFromDirectory(root) : null;
			HashSet<string>? allowedExtensions = ParseAllowedExtensions(options.FileExtensions);

			Regex? compiledRegex = null;
			if (options.IsRegex)
			{
				var regexOptions = RegexOptions.CultureInvariant;
				if (!options.MatchCase)
					regexOptions |= RegexOptions.IgnoreCase;
				compiledRegex = new Regex(options.Query, regexOptions);
			}

			var dirsToVisit = new Stack<string>();
			dirsToVisit.Push(root);

			var binaryBuffer = new byte[BinaryCheckBufferSize];

			while (dirsToVisit.Count > 0)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return new CodeSearchResult(matches, filesScanned, filesIgnored, sw.Elapsed, wasCancelled: true);
				}

				var currentDir = dirsToVisit.Pop();

				// Enumerate subdirectories with early pruning based on gitignore
				string[] subDirs;
				try
				{
					subDirs = Directory.GetDirectories(currentDir);
				}
				catch
				{
					continue; // Ignore inaccessible directories
				}

				foreach (var subDir in subDirs)
				{
					var dirName = Path.GetFileName(subDir);
					var relativeDirPath = Path.GetRelativePath(root, subDir);

					if (gitIgnore is not null && gitIgnore.IsIgnored(relativeDirPath, isDirectory: true))
					{
						filesIgnored++;
						continue; // Prune ignored directories (bin, obj, node_modules, etc.)
					}

					dirsToVisit.Push(subDir);
				}

				// Enumerate files in current directory
				string[] files;
				try
				{
					files = Directory.GetFiles(currentDir);
				}
				catch
				{
					continue;
				}

				foreach (var file in files)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						return new CodeSearchResult(matches, filesScanned, filesIgnored, sw.Elapsed, wasCancelled: true);
					}

					var relativeFilePath = Path.GetRelativePath(root, file);

					// Check gitignore for file
					if (gitIgnore is not null && gitIgnore.IsIgnored(relativeFilePath, isDirectory: false))
					{
						filesIgnored++;
						continue;
					}

					// Check extension filter
					if (allowedExtensions is not null)
					{
						var ext = Path.GetExtension(file);
						if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext.ToLowerInvariant()))
							continue;
					}

					// Check file size (AC-5)
					long fileSize = 0;
					try
					{
						var fi = new FileInfo(file);
						fileSize = fi.Length;
					}
					catch
					{
						filesIgnored++;
						continue;
					}

					if (fileSize > options.MaxFileSizeBytes)
					{
						filesIgnored++;
						continue;
					}

					// Open file without locking it (FileShare.ReadWrite | FileShare.Delete) (FR-002)
					FileStream fs;
					try
					{
						fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan);
					}
					catch
					{
						filesIgnored++;
						continue;
					}

					using (fs)
					{
						// Check if binary file (AC-5)
						int bytesRead = fs.Read(binaryBuffer, 0, binaryBuffer.Length);
						if (IsBinaryBuffer(binaryBuffer, bytesRead))
						{
							filesIgnored++;
							continue;
						}

						fs.Seek(0, SeekOrigin.Begin);
						filesScanned++;

						// Read lines with encoding detection (UTF-8, UTF-8 BOM, UTF-16) (AC-7)
						using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 4096, leaveOpen: true);
						int lineNumber = 0;
						string? line;

						while ((line = reader.ReadLine()) is not null)
						{
							lineNumber++;

							if (cancellationToken.IsCancellationRequested)
							{
								return new CodeSearchResult(matches, filesScanned, filesIgnored, sw.Elapsed, wasCancelled: true);
							}

							int colIndex = -1;
							int matchLen = 0;

							if (compiledRegex is not null)
							{
								var match = compiledRegex.Match(line);
								if (match.Success)
								{
									colIndex = match.Index;
									matchLen = match.Length;
								}
							}
							else if (options.MatchWholeWord)
							{
								colIndex = FindWholeWord(line, options.Query, options.MatchCase);
								if (colIndex >= 0)
									matchLen = options.Query.Length;
							}
							else
							{
								var comparison = options.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
								colIndex = line.IndexOf(options.Query, comparison);
								if (colIndex >= 0)
									matchLen = options.Query.Length;
							}

							if (colIndex >= 0)
							{
								var item = new CodeSearchResultItem(
									filePath: file,
									relativePath: relativeFilePath,
									lineNumber: lineNumber,
									columnNumber: colIndex + 1,
									lineText: line,
									matchLength: matchLen);

								matches.Add(item);
								onMatch?.Invoke(item); // Incremental callback (AC-1)

								if (matches.Count >= options.MaxResults)
								{
									reachedMaxResults = true;
									return new CodeSearchResult(matches, filesScanned, filesIgnored, sw.Elapsed, reachedMaxResults: true);
								}
							}
						}
					}

					if (filesScanned % 50 == 0)
					{
						progress?.Report(new CodeSearchProgress
						{
							FilesScanned = filesScanned,
							FilesIgnored = filesIgnored,
							MatchesFound = matches.Count,
							CurrentFile = relativeFilePath
						});
					}
				}
			}

			sw.Stop();
			progress?.Report(new CodeSearchProgress
			{
				FilesScanned = filesScanned,
				FilesIgnored = filesIgnored,
				MatchesFound = matches.Count,
				CurrentFile = null
			});

			return new CodeSearchResult(matches, filesScanned, filesIgnored, sw.Elapsed, reachedMaxResults: reachedMaxResults);
		}

		private static bool IsBinaryBuffer(byte[] buffer, int length)
		{
			if (length <= 0)
				return false;

			// Check standard BOMs (UTF-16 LE, UTF-16 BE, UTF-8) (AC-7)
			if (length >= 2)
			{
				if (buffer[0] == 0xFF && buffer[1] == 0xFE) return false; // UTF-16 LE BOM
				if (buffer[0] == 0xFE && buffer[1] == 0xFF) return false; // UTF-16 BE BOM
			}
			if (length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
			{
				return false; // UTF-8 BOM
			}

			// Heuristic: check if UTF-16 without BOM (alternating zero bytes with ASCII)
			if (length >= 4)
			{
				int zeroEven = 0, zeroOdd = 0;
				int checkLen = Math.Min(length, 512);
				for (int i = 0; i < checkLen; i++)
				{
					if (buffer[i] == 0)
					{
						if (i % 2 == 0) zeroEven++;
						else zeroOdd++;
					}
				}
				// If null bytes are concentrated strictly on odd or even positions, it's UTF-16 text
				if ((zeroOdd > checkLen / 4 && zeroEven == 0) || (zeroEven > checkLen / 4 && zeroOdd == 0))
				{
					return false;
				}
			}

			for (int i = 0; i < length; i++)
			{
				if (buffer[i] == 0)
					return true;
			}
			return false;
		}

		private static int FindWholeWord(string text, string word, bool matchCase)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(word))
				return -1;

			var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			int startIndex = 0;

			while (startIndex <= text.Length - word.Length)
			{
				int index = text.IndexOf(word, startIndex, comparison);
				if (index < 0)
					return -1;

				bool startBoundary = (index == 0) || !char.IsLetterOrDigit(text[index - 1]);
				bool endBoundary = (index + word.Length == text.Length) || !char.IsLetterOrDigit(text[index + word.Length]);

				if (startBoundary && endBoundary)
					return index;

				startIndex = index + 1;
			}

			return -1;
		}

		private static HashSet<string>? ParseAllowedExtensions(string? extensionsString)
		{
			if (string.IsNullOrWhiteSpace(extensionsString))
				return null;

			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var tokens = extensionsString.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var token in tokens)
			{
				var t = token.Trim().TrimStart('*');
				if (!t.StartsWith("."))
					t = "." + t;
				set.Add(t.ToLowerInvariant());
			}

			return set.Count > 0 ? set : null;
		}
	}
}
