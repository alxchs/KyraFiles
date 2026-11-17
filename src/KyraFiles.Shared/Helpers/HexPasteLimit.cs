// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Text;

namespace KyraFiles.Shared.Helpers
{
	/// <summary>
	/// Enforces paste size limits in the hex editor (F060 M2 / NFR-002 / M2-T1 risk note).
	/// Limits pasted data to at most 1 MiB per operation to prevent unbounded memory growth from edit records.
	/// </summary>
	public static class HexPasteLimit
	{
		/// <summary>
		/// Maximum allowed paste size in bytes (1 MiB = 1,048,576 bytes).
		/// </summary>
		public const int MaxPasteSizeBytes = 1024 * 1024;

		/// <summary>
		/// Validates whether a requested paste byte count is within the safe limit.
		/// </summary>
		/// <param name="byteCount">Number of bytes to paste.</param>
		/// <param name="errorMessage">Error message in en-US if the paste exceeds the limit or is empty; null otherwise.</param>
		/// <returns>True if within the allowed limit; false if refused.</returns>
		public static bool ValidatePasteSize(long byteCount, out string? errorMessage)
		{
			if (byteCount <= 0)
			{
				errorMessage = "No data to paste.";
				return false;
			}

			if (byteCount > MaxPasteSizeBytes)
			{
				errorMessage = $"Cannot paste more than 1 MiB (1,048,576 bytes) at once. The requested size was {byteCount:N0} bytes.";
				return false;
			}

			errorMessage = null;
			return true;
		}

		/// <summary>
		/// Attempts to parse a hexadecimal string (with optional spaces/dashes) into bytes,
		/// ensuring the total parsed size does not exceed the 1 MiB limit.
		/// </summary>
		/// <param name="hexText">Hex string input (e.g. "48 65 6C 6C 6F" or "48656C6C6F").</param>
		/// <param name="bytes">Parsed bytes if successful.</param>
		/// <param name="errorMessage">Error message if invalid or exceeding the limit.</param>
		/// <returns>True if successfully parsed and within limit; false otherwise.</returns>
		public static bool TryParseHexBytes(string? hexText, out byte[]? bytes, out string? errorMessage)
		{
			bytes = null;
			if (string.IsNullOrWhiteSpace(hexText))
			{
				errorMessage = "No data to paste.";
				return false;
			}

			// Clean whitespace, separators, and '0x' prefixes
			var clean = new StringBuilder(hexText.Length);
			for (int i = 0; i < hexText.Length; i++)
			{
				char c = hexText[i];
				if (char.IsWhiteSpace(c) || c == '-' || c == ',' || c == ':')
					continue;

				if ((c == 'x' || c == 'X') && clean.Length > 0 && clean[^1] == '0')
				{
					clean.Length--; // Remove preceding '0' in '0x'
					continue;
				}

				if (!HexNibbleEditor.IsHexDigit(c))
				{
					errorMessage = $"Invalid hexadecimal character '{c}'.";
					return false;
				}

				clean.Append(c);
			}

			if (clean.Length == 0)
			{
				errorMessage = "No data to paste.";
				return false;
			}

			if (clean.Length % 2 != 0)
			{
				errorMessage = "Hexadecimal string must have an even number of digits.";
				return false;
			}

			long byteCount = clean.Length / 2;
			if (!ValidatePasteSize(byteCount, out errorMessage))
			{
				return false;
			}

			byte[] result = new byte[byteCount];
			for (int i = 0; i < byteCount; i++)
			{
				int high = HexNibbleEditor.HexToNibble(clean[i * 2]);
				int low = HexNibbleEditor.HexToNibble(clean[i * 2 + 1]);
				result[i] = (byte)((high << 4) | low);
			}

			bytes = result;
			errorMessage = null;
			return true;
		}

		/// <summary>
		/// Validates and extracts UTF-8 bytes from raw text, enforcing the 1 MiB paste limit.
		/// </summary>
		public static bool TryGetRawTextBytes(string? text, out byte[]? bytes, out string? errorMessage)
		{
			bytes = null;
			if (string.IsNullOrEmpty(text))
			{
				errorMessage = "No data to paste.";
				return false;
			}

			long byteCount = Encoding.UTF8.GetByteCount(text);
			if (!ValidatePasteSize(byteCount, out errorMessage))
			{
				return false;
			}

			bytes = Encoding.UTF8.GetBytes(text);
			errorMessage = null;
			return true;
		}
	}
}
