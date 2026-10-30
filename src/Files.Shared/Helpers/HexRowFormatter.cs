// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Text;

namespace Files.Shared.Helpers
{
	/// <summary>
	/// Represents a formatted 16-byte row in the hex editor grid (F060 M2 / M2-AC-1..3).
	/// </summary>
	public sealed class HexRowData
	{
		public long Offset { get; }
		public string OffsetHex { get; }
		public string[] HexCells { get; }
		public string Text { get; }
		public int ByteCount { get; }

		public HexRowData(long offset, string offsetHex, string[] hexCells, string text, int byteCount)
		{
			Offset = offset;
			OffsetHex = offsetHex;
			HexCells = hexCells;
			Text = text;
			ByteCount = byteCount;
		}
	}

	/// <summary>
	/// Pure formatting helper for 16-byte rows in the hex editor (F060 M2 / M2-AC-1..3).
	/// Converts bytes into hexadecimal offset, 16 hex byte cells, and an ASCII decoded text column
	/// where non-printable bytes are represented as '.'.
	/// </summary>
	public static class HexRowFormatter
	{
		public const int BytesPerRow = 16;

		/// <summary>
		/// Formats an offset as a hexadecimal string with at least 8 digits (uppercase).
		/// For offsets > 0xFFFFFFFF, expands to 12 or 16 digits to preserve precision.
		/// </summary>
		public static string FormatOffset(long offset, int minDigits = 8)
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset cannot be negative.");

			if (offset > 0xFFFFFFFFL && minDigits < 12)
				minDigits = (offset > 0xFFFFFFFFFFFFL) ? 16 : 12;

			return offset.ToString($"X{minDigits}");
		}

		/// <summary>
		/// Formats a single byte as a 2-character uppercase hexadecimal string.
		/// </summary>
		public static string FormatByte(byte b)
		{
			return b.ToString("X2");
		}

		/// <summary>
		/// Formats a byte as a displayable ASCII character.
		/// Printable ASCII characters (0x20 through 0x7E) are returned as is;
		/// all other bytes (control chars &lt; 0x20, and high bytes &gt;= 0x7F) are returned as '.'.
		/// </summary>
		public static char FormatAsciiChar(byte b)
		{
			return (b >= 0x20 && b <= 0x7E) ? (char)b : '.';
		}

		/// <summary>
		/// Formats a sequence of bytes into an ASCII string where non-printable bytes become '.'.
		/// </summary>
		public static string FormatAscii(ReadOnlySpan<byte> bytes)
		{
			if (bytes.IsEmpty)
				return string.Empty;

			char[] chars = new char[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
			{
				chars[i] = FormatAsciiChar(bytes[i]);
			}

			return new string(chars);
		}

		/// <summary>
		/// Formats up to 16 bytes into an array of 16 hex cell strings.
		/// Empty slots beyond the length of the span are represented by empty strings.
		/// </summary>
		public static string[] FormatHexCells(ReadOnlySpan<byte> bytes, int bytesPerRow = BytesPerRow)
		{
			if (bytesPerRow <= 0)
				throw new ArgumentOutOfRangeException(nameof(bytesPerRow), bytesPerRow, "Bytes per row must be positive.");

			string[] cells = new string[bytesPerRow];
			int count = Math.Min(bytes.Length, bytesPerRow);

			for (int i = 0; i < count; i++)
			{
				cells[i] = FormatByte(bytes[i]);
			}

			for (int i = count; i < bytesPerRow; i++)
			{
				cells[i] = string.Empty;
			}

			return cells;
		}

		/// <summary>
		/// Formats a complete row of up to 16 bytes.
		/// </summary>
		public static HexRowData FormatRow(long rowOffset, ReadOnlySpan<byte> bytes, int bytesPerRow = BytesPerRow)
		{
			int count = Math.Min(bytes.Length, bytesPerRow);
			string offsetHex = FormatOffset(rowOffset);
			string[] hexCells = FormatHexCells(bytes, bytesPerRow);
			string text = FormatAscii(bytes[..count]);

			return new HexRowData(rowOffset, offsetHex, hexCells, text, count);
		}

		/// <summary>
		/// Formats a full row as a single line of text suitable for copy or export:
		/// Offset | 16 Hex Bytes (split into two groups of 8) | ASCII Text
		/// </summary>
		public static string FormatFullRowString(long rowOffset, ReadOnlySpan<byte> bytes, int bytesPerRow = BytesPerRow)
		{
			var sb = new StringBuilder(80);
			sb.Append(FormatOffset(rowOffset));
			sb.Append("  ");

			int count = Math.Min(bytes.Length, bytesPerRow);
			for (int i = 0; i < bytesPerRow; i++)
			{
				if (i == 8)
					sb.Append(' ');

				if (i < count)
				{
					sb.Append(FormatByte(bytes[i]));
				}
				else
				{
					sb.Append("  ");
				}

				if (i < bytesPerRow - 1)
					sb.Append(' ');
			}

			sb.Append("  |");
			sb.Append(FormatAscii(bytes[..count]).PadRight(bytesPerRow));
			sb.Append('|');

			return sb.ToString();
		}
	}
}
