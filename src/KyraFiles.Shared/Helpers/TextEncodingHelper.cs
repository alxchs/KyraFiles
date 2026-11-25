// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace KyraFiles.Shared.Helpers
{
	public readonly record struct TextFileInfo(bool IsText, string Encoding, string LineEnding);

	public static class TextEncodingHelper
	{
		private static readonly ConcurrentDictionary<string, (long LastWriteTicks, long Length, long CachedTimestamp, TextFileInfo Info)> Cache =
			new(StringComparer.OrdinalIgnoreCase);

		private const int SampleBufferSize = 4096;
		private const long CacheTtlMs = 3000;

		public static TextFileInfo GetFileInfo(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				return new TextFileInfo(false, string.Empty, string.Empty);

			long now = Environment.TickCount64;
			if (Cache.TryGetValue(filePath, out var cached) && (now - cached.CachedTimestamp < CacheTtlMs))
			{
				return cached.Info;
			}

			try
			{
				if (!File.Exists(filePath))
					return new TextFileInfo(false, string.Empty, string.Empty);

				var fileInfo = new FileInfo(filePath);
				long writeTicks = fileInfo.LastWriteTimeUtc.Ticks;
				long length = fileInfo.Length;

				if (cached.LastWriteTicks == writeTicks && cached.Length == length && cached.Info.IsText)
				{
					Cache[filePath] = (writeTicks, length, now, cached.Info);
					return cached.Info;
				}

				var analyzed = AnalyzeFile(filePath, length);
				Cache[filePath] = (writeTicks, length, now, analyzed);
				return analyzed;
			}
			catch
			{
				return new TextFileInfo(false, string.Empty, string.Empty);
			}
		}

		public static void InvalidateCache(string filePath)
		{
			Cache.TryRemove(filePath, out _);
		}

		private static TextFileInfo AnalyzeFile(string filePath, long fileLength)
		{
			if (fileLength == 0)
				return new TextFileInfo(true, "UTF-8", "-");

			byte[] buffer;
			int bytesRead;

			using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				int toRead = (int)Math.Min(SampleBufferSize, fileLength);
				buffer = new byte[toRead];
				bytesRead = stream.Read(buffer, 0, toRead);
			}

			if (bytesRead == 0)
				return new TextFileInfo(true, "UTF-8", "-");

			// 1. Detect Encoding via BOM
			string encodingName;
			int contentOffset = 0;

			if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
			{
				encodingName = "UTF-8 BOM";
				contentOffset = 3;
			}
			else if (bytesRead >= 4 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
			{
				encodingName = "UTF-32 LE";
				contentOffset = 4;
			}
			else if (bytesRead >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF)
			{
				encodingName = "UTF-32 BE";
				contentOffset = 4;
			}
			else if (bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
			{
				encodingName = "UTF-16 LE";
				contentOffset = 2;
			}
			else if (bytesRead >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
			{
				encodingName = "UTF-16 BE";
				contentOffset = 2;
			}
			else
			{
				// Check for binary null bytes
				bool hasNulls = false;
				for (int i = 0; i < bytesRead; i++)
				{
					if (buffer[i] == 0)
					{
						hasNulls = true;
						break;
					}
				}

				if (hasNulls)
				{
					// Known binary
					return new TextFileInfo(false, string.Empty, string.Empty);
				}

				// Check if valid UTF-8
				if (IsValidUtf8(buffer, 0, bytesRead))
					encodingName = "UTF-8";
				else
					encodingName = "ANSI / Latin-1";
			}

			// 2. Detect Line Endings
			int crlfCount = 0;
			int lfCount = 0;
			int crCount = 0;

			for (int i = contentOffset; i < bytesRead; i++)
			{
				if (buffer[i] == (byte)'\r')
				{
					if (i + 1 < bytesRead && buffer[i + 1] == (byte)'\n')
					{
						crlfCount++;
						i++; // Skip \n
					}
					else
					{
						crCount++;
					}
				}
				else if (buffer[i] == (byte)'\n')
				{
					lfCount++;
				}
			}

			string lineEndingName;
			if (crlfCount > 0 && (lfCount > 0 || crCount > 0))
			{
				lineEndingName = "Misto"; // AC-2
			}
			else if (lfCount > 0 && crCount > 0)
			{
				lineEndingName = "Misto";
			}
			else if (crlfCount > 0)
			{
				lineEndingName = "CRLF";
			}
			else if (lfCount > 0)
			{
				lineEndingName = "LF";
			}
			else if (crCount > 0)
			{
				lineEndingName = "CR";
			}
			else
			{
				lineEndingName = "-";
			}

			return new TextFileInfo(true, encodingName, lineEndingName);
		}

		public static Encoding DetectEncoding(byte[] buffer, int bytesRead)
		{
			if (bytesRead >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
				return Encoding.UTF8;

			if (bytesRead >= 4 && buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
				return Encoding.UTF32;

			if (bytesRead >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0xFE && buffer[3] == 0xFF)
				return new UTF32Encoding(true, true);

			if (bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
				return Encoding.Unicode;

			if (bytesRead >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
				return Encoding.BigEndianUnicode;

			if (IsValidUtf8(buffer, 0, bytesRead))
				return Encoding.UTF8;

			try
			{
				// No Windows brasileiro/ocidental (e padrão ANSI para Delphi .pas e arquivos legados), CodePage 1252 / Western European
				return Encoding.GetEncoding(1252);
			}
			catch
			{
				return Encoding.Latin1;
			}
		}

		private static bool IsValidUtf8(byte[] bytes, int offset, int length)
		{
			int i = offset;
			int end = offset + length;

			while (i < end)
			{
				byte b = bytes[i++];
				if (b <= 0x7F)
					continue;

				int extraBytes;
				if ((b & 0xE0) == 0xC0) extraBytes = 1;
				else if ((b & 0xF0) == 0xE0) extraBytes = 2;
				else if ((b & 0xF8) == 0xF0) extraBytes = 3;
				else return false;

				if (i + extraBytes > end)
					return true; // Truncated at end of buffer, treat as valid

				for (int j = 0; j < extraBytes; j++)
				{
					byte next = bytes[i++];
					if ((next & 0xC0) != 0x80)
						return false;
				}
			}

			return true;
		}
	}
}
