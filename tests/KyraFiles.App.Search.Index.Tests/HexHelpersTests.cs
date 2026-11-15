// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Text;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class HexHelpersTests
	{
		#region HexRowFormatter Tests (M2-AC-1, M2-AC-2, M2-AC-3)

		[TestMethod]
		public void M2_AC2_HexRowFormatter_FormatOffset_HexadecimalStringWithMinDigits()
		{
			Assert.AreEqual("00000000", HexRowFormatter.FormatOffset(0));
			Assert.AreEqual("00000010", HexRowFormatter.FormatOffset(16));
			Assert.AreEqual("000000FF", HexRowFormatter.FormatOffset(255));
			Assert.AreEqual("12345678", HexRowFormatter.FormatOffset(0x12345678));
			Assert.AreEqual("000100000000", HexRowFormatter.FormatOffset(0x100000000L)); // > 32-bit expands
		}

		[TestMethod]
		public void M2_AC2_HexRowFormatter_FormatByte_ReturnsUpperHex()
		{
			Assert.AreEqual("00", HexRowFormatter.FormatByte(0));
			Assert.AreEqual("0A", HexRowFormatter.FormatByte(10));
			Assert.AreEqual("7F", HexRowFormatter.FormatByte(127));
			Assert.AreEqual("FF", HexRowFormatter.FormatByte(255));
		}

		[TestMethod]
		public void M2_AC3_HexRowFormatter_FormatAsciiChar_PrintablePreserved_NonPrintableBecomesDot()
		{
			// Control characters < 0x20 become '.'
			Assert.AreEqual('.', HexRowFormatter.FormatAsciiChar(0x00));
			Assert.AreEqual('.', HexRowFormatter.FormatAsciiChar(0x0A)); // newline
			Assert.AreEqual('.', HexRowFormatter.FormatAsciiChar(0x1F));

			// Printable ASCII 0x20..0x7E preserved
			Assert.AreEqual(' ', HexRowFormatter.FormatAsciiChar(0x20));
			Assert.AreEqual('A', HexRowFormatter.FormatAsciiChar((byte)'A'));
			Assert.AreEqual('z', HexRowFormatter.FormatAsciiChar((byte)'z'));
			Assert.AreEqual('0', HexRowFormatter.FormatAsciiChar((byte)'0'));
			Assert.AreEqual('~', HexRowFormatter.FormatAsciiChar(0x7E));

			// High characters >= 0x7F become '.'
			Assert.AreEqual('.', HexRowFormatter.FormatAsciiChar(0x7F)); // DEL
			Assert.AreEqual('.', HexRowFormatter.FormatAsciiChar(0x80));
			Assert.AreEqual('.', HexRowFormatter.FormatAsciiChar(0xFF));
		}

		[TestMethod]
		public void M2_AC3_HexRowFormatter_FormatAscii_Span_ConvertsCorrectly()
		{
			byte[] bytes = new byte[] { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o', 0x00, (byte)'W', (byte)'o', (byte)'r', (byte)'l', (byte)'d', 0xFF };
			string result = HexRowFormatter.FormatAscii(bytes);

			Assert.AreEqual("Hello.World.", result);
		}

		[TestMethod]
		public void M2_AC2_HexRowFormatter_FormatHexCells_16Bytes()
		{
			byte[] bytes = new byte[16];
			for (int i = 0; i < 16; i++)
			{
				bytes[i] = (byte)i;
			}

			string[] cells = HexRowFormatter.FormatHexCells(bytes);
			Assert.AreEqual(16, cells.Length);
			Assert.AreEqual("00", cells[0]);
			Assert.AreEqual("07", cells[7]);
			Assert.AreEqual("08", cells[8]);
			Assert.AreEqual("0F", cells[15]);
		}

		[TestMethod]
		public void M2_AC2_HexRowFormatter_FormatHexCells_FewerThan16Bytes_PaddedWithEmpty()
		{
			byte[] bytes = new byte[] { 0x41, 0x42, 0x43 }; // 3 bytes
			string[] cells = HexRowFormatter.FormatHexCells(bytes);

			Assert.AreEqual(16, cells.Length);
			Assert.AreEqual("41", cells[0]);
			Assert.AreEqual("42", cells[1]);
			Assert.AreEqual("43", cells[2]);
			for (int i = 3; i < 16; i++)
			{
				Assert.AreEqual(string.Empty, cells[i]);
			}
		}

		[TestMethod]
		public void M2_AC1_HexRowFormatter_FormatRow_ProducesRowDataWithCorrectValues()
		{
			byte[] bytes = Encoding.ASCII.GetBytes("FilesCustom.Hex!");
			HexRowData rowData = HexRowFormatter.FormatRow(0x0040, bytes);

			Assert.AreEqual(0x0040L, rowData.Offset);
			Assert.AreEqual("00000040", rowData.OffsetHex);
			Assert.AreEqual(16, rowData.ByteCount);
			Assert.AreEqual(16, rowData.HexCells.Length);
			Assert.AreEqual("46", rowData.HexCells[0]); // 'F'
			Assert.AreEqual("FilesCustom.Hex!", rowData.Text);
		}

		[TestMethod]
		public void M2_AC2_HexRowFormatter_FormatFullRowString_MatchesExpectedLayout()
		{
			byte[] bytes = Encoding.ASCII.GetBytes("ABCDEFGH12345678");
			string rowStr = HexRowFormatter.FormatFullRowString(0x10, bytes);

			Assert.IsTrue(rowStr.StartsWith("00000010  41 42 43 44 45 46 47 48  31 32 33 34 35 36 37 38  |ABCDEFGH12345678|"));
		}

		#endregion

		#region HexNibbleEditor Tests (M2-AC-2)

		[TestMethod]
		public void M2_AC2_HexNibbleEditor_TwoHexDigitsReplaceOneByte()
		{
			var editor = new HexNibbleEditor();
			Assert.IsFalse(editor.HasPendingNibble);

			// First digit 'A' (10)
			bool handled1 = editor.ProcessKey('A', out byte? byte1);
			Assert.IsTrue(handled1);
			Assert.IsNull(byte1);
			Assert.IsTrue(editor.HasPendingNibble);
			Assert.AreEqual(10, editor.PendingHighNibble);
			Assert.AreEqual('A', editor.PendingChar);

			// Second digit '5' (5) -> byte 0xA5 (165)
			bool handled2 = editor.ProcessKey('5', out byte? byte2);
			Assert.IsTrue(handled2);
			Assert.IsNotNull(byte2);
			Assert.AreEqual(0xA5, byte2.Value);
			Assert.IsFalse(editor.HasPendingNibble);
		}

		[TestMethod]
		public void M2_AC2_HexNibbleEditor_LowerCaseHexDigitsAccepted()
		{
			var editor = new HexNibbleEditor();

			// 'f' (15) and 'b' (11) -> 0xFB
			Assert.IsTrue(editor.ProcessKey('f', out byte? b1));
			Assert.IsNull(b1);
			Assert.IsTrue(editor.ProcessKey('b', out byte? b2));
			Assert.IsNotNull(b2);
			Assert.AreEqual(0xFB, b2.Value);
		}

		[TestMethod]
		public void M2_AC2_HexNibbleEditor_InvalidCharacterIgnored()
		{
			var editor = new HexNibbleEditor();

			// 'G', 'Z', '?', space are ignored
			Assert.IsFalse(editor.ProcessKey('G', out byte? b1));
			Assert.IsNull(b1);
			Assert.IsFalse(editor.HasPendingNibble);

			// Type first valid nibble '3'
			Assert.IsTrue(editor.ProcessKey('3', out _));
			Assert.IsTrue(editor.HasPendingNibble);

			// Type invalid char 'X' -> ignored, pending nibble preserved!
			Assert.IsFalse(editor.ProcessKey('X', out byte? b2));
			Assert.IsNull(b2);
			Assert.IsTrue(editor.HasPendingNibble);
			Assert.AreEqual(3, editor.PendingHighNibble);

			// Type valid second nibble 'C' -> byte 0x3C
			Assert.IsTrue(editor.ProcessKey('C', out byte? b3));
			Assert.IsNotNull(b3);
			Assert.AreEqual(0x3C, b3.Value);
		}

		[TestMethod]
		public void M2_AC2_HexNibbleEditor_BackspaceCancelsInProgressEdit()
		{
			var editor = new HexNibbleEditor();
			editor.ProcessKey('7', out _);
			Assert.IsTrue(editor.HasPendingNibble);

			// Backspace cancels pending high nibble
			bool handled = editor.ProcessKey('\b', out byte? completed);
			Assert.IsTrue(handled);
			Assert.IsNull(completed);
			Assert.IsFalse(editor.HasPendingNibble);
		}

		[TestMethod]
		public void M2_AC2_HexNibbleEditor_EscapeCancelsInProgressEdit()
		{
			var editor = new HexNibbleEditor();
			editor.ProcessKey('E', out _);
			Assert.IsTrue(editor.HasPendingNibble);

			// Esc (0x1b) cancels pending high nibble
			bool handled = editor.ProcessKey('\x1b', out byte? completed);
			Assert.IsTrue(handled);
			Assert.IsNull(completed);
			Assert.IsFalse(editor.HasPendingNibble);
		}

		[TestMethod]
		public void M2_AC2_HexNibbleEditor_ResetCancelsInProgressEdit()
		{
			var editor = new HexNibbleEditor();
			editor.ProcessKey('9', out _);
			Assert.IsTrue(editor.HasPendingNibble);

			editor.Reset();
			Assert.IsFalse(editor.HasPendingNibble);
			Assert.IsNull(editor.PendingHighNibble);
			Assert.IsNull(editor.PendingChar);
		}

		#endregion

		#region HexPasteLimit Tests (M2-AC-1)

		[TestMethod]
		public void M2_AC1_HexPasteLimit_UnderOrEqual1MiB_Allowed()
		{
			Assert.IsTrue(HexPasteLimit.ValidatePasteSize(1, out string? err1));
			Assert.IsNull(err1);

			Assert.IsTrue(HexPasteLimit.ValidatePasteSize(1024 * 1024, out string? err2));
			Assert.IsNull(err2);
		}

		[TestMethod]
		public void M2_AC1_HexPasteLimit_Above1MiB_RejectedWithMessage()
		{
			// 1 byte above 1 MiB
			long overLimit = (1024 * 1024) + 1;
			Assert.IsFalse(HexPasteLimit.ValidatePasteSize(overLimit, out string? err));
			Assert.IsNotNull(err);
			Assert.IsTrue(err.Contains("1 MiB", StringComparison.OrdinalIgnoreCase));
			Assert.IsTrue(err.Contains("1,048,576", StringComparison.OrdinalIgnoreCase));
		}

		[TestMethod]
		public void M2_AC1_HexPasteLimit_TryParseHexBytes_FormatsValid_AndRejectsAbove1MiB()
		{
			// Valid hex with spaces
			Assert.IsTrue(HexPasteLimit.TryParseHexBytes("48 65 6C 6C 6F", out byte[]? bytes, out string? err));
			Assert.IsNull(err);
			Assert.IsNotNull(bytes);
			Assert.AreEqual(5, bytes.Length);
			Assert.AreEqual("Hello", Encoding.ASCII.GetString(bytes));

			// Valid hex with '0x' prefixes
			Assert.IsTrue(HexPasteLimit.TryParseHexBytes("0x41, 0x42, 0x43", out byte[]? bytes2, out _));
			Assert.IsNotNull(bytes2);
			Assert.AreEqual(3, bytes2.Length);
			Assert.AreEqual("ABC", Encoding.ASCII.GetString(bytes2));

			// Rejects invalid hex characters
			Assert.IsFalse(HexPasteLimit.TryParseHexBytes("48 65 ZZ 6F", out _, out string? errInvalid));
			Assert.IsNotNull(errInvalid);
			Assert.IsTrue(errInvalid.Contains("Invalid hexadecimal character", StringComparison.OrdinalIgnoreCase));

			// Rejects odd length
			Assert.IsFalse(HexPasteLimit.TryParseHexBytes("ABC", out _, out string? errOdd));
			Assert.IsNotNull(errOdd);
			Assert.IsTrue(errOdd.Contains("even number of digits", StringComparison.OrdinalIgnoreCase));
		}

		[TestMethod]
		public void M2_AC1_HexPasteLimit_TryGetRawTextBytes_EnforcesLimit()
		{
			string shortText = "Sample text for testing hex editor paste limits.";
			Assert.IsTrue(HexPasteLimit.TryGetRawTextBytes(shortText, out byte[]? bytes, out string? err));
			Assert.IsNull(err);
			Assert.IsNotNull(bytes);
			Assert.AreEqual(shortText, Encoding.UTF8.GetString(bytes));

			// Empty string rejected
			Assert.IsFalse(HexPasteLimit.TryGetRawTextBytes("", out _, out string? emptyErr));
			Assert.IsNotNull(emptyErr);
		}

		#endregion
	}
}
