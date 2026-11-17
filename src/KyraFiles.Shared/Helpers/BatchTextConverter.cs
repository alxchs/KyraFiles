// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KyraFiles.Shared.Helpers
{
	public readonly record struct ConversionSummary(
		int ConvertedCount,
		int SkippedCount,
		int FailedCount,
		IReadOnlyList<string> SkippedReasons);

	public static class BatchTextConverter
	{
		private static readonly Stack<List<(string FilePath, byte[] OriginalBytes)>> UndoHistory = new();

		public static bool CanUndo => UndoHistory.Count > 0;

		public static Task<ConversionSummary> ConvertToCrlfAsync(IEnumerable<string> filePaths)
		{
			return ConvertLineEndingsAsync(filePaths, toCrlf: true);
		}

		public static Task<ConversionSummary> ConvertToLfAsync(IEnumerable<string> filePaths)
		{
			return ConvertLineEndingsAsync(filePaths, toCrlf: false);
		}

		public static Task<int> UndoLastConversionAsync()
		{
			if (UndoHistory.Count == 0)
				return Task.FromResult(0);

			var lastBatch = UndoHistory.Pop();
			int restored = 0;

			foreach (var (path, bytes) in lastBatch)
			{
				try
				{
					if (File.Exists(path))
					{
						// Atomic rollback
						string tempFile = Path.Combine(Path.GetTempPath(), $"kyrafiles_undo_{Guid.NewGuid():N}.tmp");
						File.WriteAllBytes(tempFile, bytes);
						File.Move(tempFile, path, overwrite: true);
						TextEncodingHelper.InvalidateCache(path);
						restored++;
					}
				}
				catch
				{
					// Ignore individual undo failures
				}
			}

			return Task.FromResult(restored);
		}

		private static Task<ConversionSummary> ConvertLineEndingsAsync(IEnumerable<string> filePaths, bool toCrlf)
		{
			return Task.Run(() =>
			{
				int converted = 0;
				int skipped = 0;
				int failed = 0;
				var reasons = new List<string>();
				var undoItems = new List<(string FilePath, byte[] OriginalBytes)>();

				foreach (var path in filePaths)
				{
					if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
					{
						skipped++;
						continue;
					}

					// AC-4: Skip and continue for files in use / locked
					byte[] originalBytes;
					try
					{
						originalBytes = File.ReadAllBytes(path);
					}
					catch (IOException)
					{
						failed++;
						reasons.Add($"{Path.GetFileName(path)}: arquivo bloqueado ou em uso.");
						continue;
					}
					catch (UnauthorizedAccessException)
					{
						failed++;
						reasons.Add($"{Path.GetFileName(path)}: acesso negado.");
						continue;
					}

					// AC-7: Reject and explain for non-text files
					var info = TextEncodingHelper.GetFileInfo(path);
					if (!info.IsText)
					{
						skipped++;
						reasons.Add($"{Path.GetFileName(path)}: não é arquivo de texto.");
						continue;
					}

					try
					{
						// Detect encoding to preserve it
						Encoding encoding = info.Encoding.StartsWith("UTF-8 BOM", StringComparison.OrdinalIgnoreCase)
							? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
							: (info.Encoding.StartsWith("UTF-16 LE", StringComparison.OrdinalIgnoreCase)
								? Encoding.Unicode
								: (info.Encoding.StartsWith("UTF-16 BE", StringComparison.OrdinalIgnoreCase)
									? Encoding.BigEndianUnicode
									: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)));

						string text;
						using (var reader = new StreamReader(new MemoryStream(originalBytes), encoding, detectEncodingFromByteOrderMarks: true))
						{
							text = reader.ReadToEnd();
						}

						// Normalize line endings
						string convertedText;
						if (toCrlf)
						{
							// Replace all existing line endings with \r\n
							convertedText = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
						}
						else
						{
							// Replace all existing line endings with \n
							convertedText = text.Replace("\r\n", "\n").Replace("\r", "\n");
						}

						byte[] newBytes = encoding.GetBytes(convertedText);

						// FR-002: Atomic write via temporary file
						string tempFile = Path.Combine(Path.GetTempPath(), $"kyrafiles_conv_{Guid.NewGuid():N}.tmp");
						File.WriteAllBytes(tempFile, newBytes);
						File.Move(tempFile, path, overwrite: true);

						TextEncodingHelper.InvalidateCache(path);
						undoItems.Add((path, originalBytes));
						converted++;
					}
					catch (Exception ex)
					{
						failed++;
						reasons.Add($"{Path.GetFileName(path)}: erro ao converter ({ex.Message}).");
					}
				}

				if (undoItems.Count > 0)
				{
					UndoHistory.Push(undoItems);
				}

				return new ConversionSummary(converted, skipped, failed, reasons);
			});
		}
	}
}
