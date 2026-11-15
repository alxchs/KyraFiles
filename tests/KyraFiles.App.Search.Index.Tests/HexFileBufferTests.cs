// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class HexFileBufferTests
	{
		private static readonly string TestTempDir = Path.Combine(
			Path.GetTempPath(),
			"agy",
			Guid.NewGuid().ToString("N"),
			"desenv",
			"utils",
			"FilesApp-f060-m2",
			"Files");

		[ClassInitialize]
		public static void ClassInit(TestContext context)
		{
			Directory.CreateDirectory(TestTempDir);
		}

		[ClassCleanup]
		public static void ClassCleanup()
		{
			if (Directory.Exists(TestTempDir))
			{
				try
				{
					Directory.Delete(TestTempDir, recursive: true);
				}
				catch
				{
					// Ignore cleanup errors on tear down
				}
			}
		}

		private static string CreateDeterministicFile(string fileName, long sizeInBytes)
		{
			string filePath = Path.Combine(TestTempDir, fileName);
			byte[] chunk = new byte[64 * 1024];
			for (int i = 0; i < chunk.Length; i++)
			{
				chunk[i] = (byte)(i % 251);
			}

			using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024);
			long written = 0;
			while (written < sizeInBytes)
			{
				int toWrite = (int)Math.Min(chunk.Length, sizeInBytes - written);
				fs.Write(chunk, 0, toWrite);
				written += toWrite;
			}

			return filePath;
		}

		private static byte ExpectedByteAt(long offset)
		{
			return (byte)((offset % 65536) % 251);
		}

		private static T Throws<T>(Action action) where T : Exception
		{
			try
			{
				action();
				Assert.Fail($"Expected {typeof(T).Name} was not thrown.");
				return null!;
			}
			catch (T ex)
			{
				return ex;
			}
		}

		[TestMethod]
		public void M2_AC2_EmptyFile_ZeroLength_And_RejectsReadPastEnd()
		{
			string emptyFilePath = Path.Combine(TestTempDir, "empty.bin");
			File.WriteAllBytes(emptyFilePath, Array.Empty<byte>());

			using var buffer = new HexFileBuffer(emptyFilePath, blockSize: 1024, maxCachedBlocks: 4);

			Assert.AreEqual(0, buffer.Length);
			Assert.AreEqual(0, buffer.CachedBlockCount);
			Assert.AreEqual(0, buffer.ModifiedBytesCount);
			Assert.IsFalse(buffer.CanUndo);
			Assert.IsFalse(buffer.CanRedo);

			// Read count 0 at offset 0 succeeds
			byte[] zeroRead = buffer.Read(0, 0);
			Assert.AreEqual(0, zeroRead.Length);

			// Reading beyond length throws ArgumentOutOfRangeException
			Throws<ArgumentOutOfRangeException>(() => buffer.Read(0, 1));
			Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(0));
			Throws<ArgumentOutOfRangeException>(() => buffer.SetByte(0, 0xFF));
		}

		[TestMethod]
		public void M2_AC2_InvalidOffset_ThrowsArgumentOutOfRangeException()
		{
			string testFile = Path.Combine(TestTempDir, "invalid_offset.bin");
			File.WriteAllBytes(testFile, new byte[] { 10, 20, 30, 40 });

			using var buffer = new HexFileBuffer(testFile, blockSize: 2, maxCachedBlocks: 2);

			Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(-1));
			Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(4));
			Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(100));

			Throws<ArgumentOutOfRangeException>(() => buffer.SetByte(-1, 0xFF));
			Throws<ArgumentOutOfRangeException>(() => buffer.SetByte(4, 0xFF));

			Throws<ArgumentOutOfRangeException>(() => buffer.Read(-1, 1));
			Throws<ArgumentOutOfRangeException>(() => buffer.Read(0, 5));
			Throws<ArgumentOutOfRangeException>(() => buffer.Read(2, 3));
			Throws<ArgumentOutOfRangeException>(() => buffer.Read(0, -1));
		}

		[TestMethod]
		public void M2_AC2_Read_BeginningMiddleEnd_64MB_AssertsCacheLimit()
		{
			const long fileSize = 64 * 1024 * 1024; // 64 MB
			const int blockSize = 64 * 1024; // 64 KB
			const int maxCachedBlocks = 8;

			string filePath = CreateDeterministicFile("large_64mb_read.bin", fileSize);

			using var buffer = new HexFileBuffer(filePath, blockSize, maxCachedBlocks);

			Assert.AreEqual(fileSize, buffer.Length);
			Assert.AreEqual(0, buffer.CachedBlockCount);
			Assert.AreEqual(maxCachedBlocks, buffer.MaxCachedBlocks);

			// 1. Read at beginning (offset 0, 16 bytes)
			byte[] startBytes = buffer.Read(0, 16);
			Assert.AreEqual(16, startBytes.Length);
			for (int i = 0; i < 16; i++)
			{
				Assert.AreEqual(ExpectedByteAt(i), startBytes[i], $"Mismatch at offset {i}");
			}
			Assert.AreEqual(1, buffer.CachedBlockCount);

			// 2. Read in the middle (offset 32 MB = 33,554,432, 32 bytes)
			long middleOffset = 32 * 1024 * 1024;
			byte[] middleBytes = buffer.Read(middleOffset, 32);
			Assert.AreEqual(32, middleBytes.Length);
			for (int i = 0; i < 32; i++)
			{
				Assert.AreEqual(ExpectedByteAt(middleOffset + i), middleBytes[i], $"Mismatch at middle offset {middleOffset + i}");
			}
			Assert.AreEqual(2, buffer.CachedBlockCount);

			// 3. Read at the end (last 16 bytes)
			long endOffset = fileSize - 16;
			byte[] endBytes = buffer.Read(endOffset, 16);
			Assert.AreEqual(16, endBytes.Length);
			for (int i = 0; i < 16; i++)
			{
				Assert.AreEqual(ExpectedByteAt(endOffset + i), endBytes[i], $"Mismatch at end offset {endOffset + i}");
			}
			Assert.AreEqual(3, buffer.CachedBlockCount);

			// 4. Read spanning across block boundaries in the middle (e.g. block 511 to block 512)
			long crossOffset = (32 * 1024 * 1024) - 10;
			byte[] crossBytes = buffer.Read(crossOffset, 20);
			Assert.AreEqual(20, crossBytes.Length);
			for (int i = 0; i < 20; i++)
			{
				Assert.AreEqual(ExpectedByteAt(crossOffset + i), crossBytes[i], $"Mismatch at cross offset {crossOffset + i}");
			}

			// 5. Read across 100 scattered blocks to verify the cache limit is strictly asserted
			for (int b = 0; b < 100; b++)
			{
				long off = (long)b * blockSize;
				byte bVal = buffer.GetByte(off);
				Assert.AreEqual(ExpectedByteAt(off), bVal);

				Assert.IsTrue(
					buffer.CachedBlockCount <= maxCachedBlocks,
					$"Cache limit violated! CachedBlockCount {buffer.CachedBlockCount} > maxCachedBlocks {maxCachedBlocks}");
			}

			Assert.AreEqual(maxCachedBlocks, buffer.CachedBlockCount, "Cache should be full at maxCachedBlocks");
		}

		[TestMethod]
		public void M2_AC4_KnownVector_ContentVerified()
		{
			string testFile = Path.Combine(TestTempDir, "known_vector.bin");
			byte[] vector = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04 };
			File.WriteAllBytes(testFile, vector);

			using var buffer = new HexFileBuffer(testFile, blockSize: 4, maxCachedBlocks: 2);

			Assert.AreEqual(8, buffer.Length);
			CollectionAssert.AreEqual(vector, buffer.Read(0, 8));

			// Edit byte at offset 2 from 0xBE to 0x77
			buffer.SetByte(2, 0x77);

			Assert.AreEqual(1, buffer.ModifiedBytesCount);
			Assert.AreEqual(0x77, buffer.GetByte(2));
			Assert.AreEqual(0xDE, buffer.GetByte(0));

			byte[] expectedAfterEdit = new byte[] { 0xDE, 0xAD, 0x77, 0xEF, 0x01, 0x02, 0x03, 0x04 };
			CollectionAssert.AreEqual(expectedAfterEdit, buffer.Read(0, 8));

			// Verify block 0 is in modified blocks
			var modBlocks = buffer.ModifiedBlockIndices;
			Assert.AreEqual(1, modBlocks.Count);
			Assert.AreEqual(0L, modBlocks[0]);

			// Verify Edits dictionary
			Assert.IsTrue(buffer.Edits.ContainsKey(2));
			Assert.AreEqual((byte)0x77, buffer.Edits[2]);
		}

		[TestMethod]
		public void M2_AC4_Edit_BeginningMiddleEnd_ModifiedCountAndBlockList()
		{
			const long fileSize = 64 * 1024 * 1024; // 64 MB
			const int blockSize = 64 * 1024; // 64 KB
			const int maxCachedBlocks = 16;

			string filePath = CreateDeterministicFile("large_64mb_edit.bin", fileSize);

			using var buffer = new HexFileBuffer(filePath, blockSize, maxCachedBlocks);

			long offsetBeginning = 0;
			long offsetMiddle = 32 * 1024 * 1024; // block 512
			long offsetEnd = fileSize - 1; // block 1023

			byte originalBeg = buffer.GetByte(offsetBeginning);
			byte originalMid = buffer.GetByte(offsetMiddle);
			byte originalEnd = buffer.GetByte(offsetEnd);

			byte newBeg = (byte)(originalBeg ^ 0xFF);
			byte newMid = (byte)(originalMid ^ 0xAA);
			byte newEnd = (byte)(originalEnd ^ 0x55);

			// Edit beginning
			buffer.SetByte(offsetBeginning, newBeg);
			Assert.AreEqual(1, buffer.ModifiedBytesCount);
			CollectionAssert.AreEqual(new long[] { 0 }, buffer.ModifiedBlockIndices.ToArray());

			// Edit middle
			buffer.SetByte(offsetMiddle, newMid);
			Assert.AreEqual(2, buffer.ModifiedBytesCount);
			CollectionAssert.AreEqual(new long[] { 0, 512 }, buffer.ModifiedBlockIndices.ToArray());

			// Edit end
			buffer.SetByte(offsetEnd, newEnd);
			Assert.AreEqual(3, buffer.ModifiedBytesCount);
			CollectionAssert.AreEqual(new long[] { 0, 512, 1023 }, buffer.ModifiedBlockIndices.ToArray());

			// Verify merged read at beginning
			byte[] readBeg = buffer.Read(0, 4);
			Assert.AreEqual(newBeg, readBeg[0]);
			Assert.AreEqual(ExpectedByteAt(1), readBeg[1]);

			// Verify merged read at middle
			byte[] readMid = buffer.Read(offsetMiddle, 4);
			Assert.AreEqual(newMid, readMid[0]);
			Assert.AreEqual(ExpectedByteAt(offsetMiddle + 1), readMid[1]);

			// Verify merged read at end
			byte[] readEnd = buffer.Read(offsetEnd - 3, 4);
			Assert.AreEqual(ExpectedByteAt(offsetEnd - 3), readEnd[0]);
			Assert.AreEqual(newEnd, readEnd[3]);
		}

		[TestMethod]
		public void M2_AC4_RepeatedEditSameOffset_OverwritesAndTracksSingleModifiedByte()
		{
			string testFile = Path.Combine(TestTempDir, "repeated_edit.bin");
			File.WriteAllBytes(testFile, new byte[] { 0x10, 0x20, 0x30 });

			using var buffer = new HexFileBuffer(testFile, blockSize: 2, maxCachedBlocks: 2);

			Assert.AreEqual(0, buffer.ModifiedBytesCount);

			// 1. Edit offset 1 to 0x21
			buffer.SetByte(1, 0x21);
			Assert.AreEqual(1, buffer.ModifiedBytesCount);
			Assert.AreEqual(0x21, buffer.GetByte(1));

			// 2. Repeated edit at same offset 1 to 0x22
			buffer.SetByte(1, 0x22);
			Assert.AreEqual(1, buffer.ModifiedBytesCount, "ModifiedBytesCount must remain 1 for repeated edit at same offset");
			Assert.AreEqual(0x22, buffer.GetByte(1));

			// 3. Repeated edit at same offset 1 to 0x23
			buffer.SetByte(1, 0x23);
			Assert.AreEqual(1, buffer.ModifiedBytesCount);
			Assert.AreEqual(0x23, buffer.GetByte(1));

			// 4. Undo step by step
			Assert.IsTrue(buffer.Undo());
			Assert.AreEqual(0x22, buffer.GetByte(1));
			Assert.AreEqual(1, buffer.ModifiedBytesCount);

			Assert.IsTrue(buffer.Undo());
			Assert.AreEqual(0x21, buffer.GetByte(1));
			Assert.AreEqual(1, buffer.ModifiedBytesCount);

			Assert.IsTrue(buffer.Undo());
			Assert.AreEqual(0x20, buffer.GetByte(1));
			Assert.AreEqual(0, buffer.ModifiedBytesCount, "ModifiedBytesCount must drop to 0 when reverted to original value");

			Assert.IsFalse(buffer.Undo());

			// 5. Redo step by step
			Assert.IsTrue(buffer.Redo());
			Assert.AreEqual(0x21, buffer.GetByte(1));
			Assert.AreEqual(1, buffer.ModifiedBytesCount);

			Assert.IsTrue(buffer.Redo());
			Assert.AreEqual(0x22, buffer.GetByte(1));
			Assert.AreEqual(1, buffer.ModifiedBytesCount);

			Assert.IsTrue(buffer.Redo());
			Assert.AreEqual(0x23, buffer.GetByte(1));
			Assert.AreEqual(1, buffer.ModifiedBytesCount);

			Assert.IsFalse(buffer.Redo());
		}

		[TestMethod]
		public void M2_AC5_UndoRedo_SingleAndMultipleEdits_RestoresOriginalBytes()
		{
			string testFile = Path.Combine(TestTempDir, "undo_redo.bin");
			byte[] original = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
			File.WriteAllBytes(testFile, original);

			using var buffer = new HexFileBuffer(testFile, blockSize: 2, maxCachedBlocks: 4);

			// Edit byte 0 and byte 5
			buffer.SetByte(0, 0xAA);
			buffer.SetByte(5, 0xBB);

			Assert.AreEqual(2, buffer.ModifiedBytesCount);
			CollectionAssert.AreEqual(new long[] { 0, 2 }, buffer.ModifiedBlockIndices.ToArray());
			Assert.AreEqual(0xAA, buffer.GetByte(0));
			Assert.AreEqual(0xBB, buffer.GetByte(5));

			// Undo edit on byte 5
			Assert.IsTrue(buffer.Undo());
			Assert.AreEqual(1, buffer.ModifiedBytesCount);
			Assert.AreEqual(0x06, buffer.GetByte(5));
			Assert.AreEqual(0xAA, buffer.GetByte(0));
			CollectionAssert.AreEqual(new long[] { 0 }, buffer.ModifiedBlockIndices.ToArray());

			// Undo edit on byte 0
			Assert.IsTrue(buffer.Undo());
			Assert.AreEqual(0, buffer.ModifiedBytesCount);
			Assert.AreEqual(0x01, buffer.GetByte(0));
			Assert.AreEqual(0, buffer.ModifiedBlockIndices.Count);
			Assert.IsFalse(buffer.CanUndo);

			// Redo edit on byte 0
			Assert.IsTrue(buffer.Redo());
			Assert.AreEqual(1, buffer.ModifiedBytesCount);
			Assert.AreEqual(0xAA, buffer.GetByte(0));

			// Redo edit on byte 5
			Assert.IsTrue(buffer.Redo());
			Assert.AreEqual(2, buffer.ModifiedBytesCount);
			Assert.AreEqual(0xBB, buffer.GetByte(5));
			Assert.IsFalse(buffer.CanRedo);

			// Batch edit SetBytes
			buffer.SetBytes(2, new byte[] { 0x91, 0x92 });
			Assert.AreEqual(4, buffer.ModifiedBytesCount);
			Assert.AreEqual(0x91, buffer.GetByte(2));
			Assert.AreEqual(0x92, buffer.GetByte(3));

			// Undo batch edit
			Assert.IsTrue(buffer.Undo());
			Assert.AreEqual(2, buffer.ModifiedBytesCount);
			Assert.AreEqual(0x03, buffer.GetByte(2));
			Assert.AreEqual(0x04, buffer.GetByte(3));

			// Redo batch edit
			Assert.IsTrue(buffer.Redo());
			Assert.AreEqual(4, buffer.ModifiedBytesCount);
			Assert.AreEqual(0x91, buffer.GetByte(2));
			Assert.AreEqual(0x92, buffer.GetByte(3));
		}

		[TestMethod]
		public void M2_AC2_AC4_LRUCacheEviction_NeverExceedsMaxBlocks()
		{
			string testFile = Path.Combine(TestTempDir, "lru_eviction.bin");
			byte[] data = new byte[16 * 1024]; // 16 KB
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = (byte)(i & 0xFF);
			}
			File.WriteAllBytes(testFile, data);

			// 16 KB file with 1 KB blocks = 16 blocks (indices 0..15). Max cache = 3.
			using var buffer = new HexFileBuffer(testFile, blockSize: 1024, maxCachedBlocks: 3);

			Assert.AreEqual(0, buffer.CachedBlockCount);

			buffer.GetByte(0); // block 0 loaded
			Assert.AreEqual(1, buffer.CachedBlockCount);

			buffer.GetByte(1024); // block 1 loaded
			Assert.AreEqual(2, buffer.CachedBlockCount);

			buffer.GetByte(2048); // block 2 loaded
			Assert.AreEqual(3, buffer.CachedBlockCount);

			// Access block 0 again -> makes block 0 most recently used (order: 0, 2, 1)
			buffer.GetByte(0);
			Assert.AreEqual(3, buffer.CachedBlockCount);

			// Access block 3 -> should evict block 1 (least recently used)
			buffer.GetByte(3072);
			Assert.AreEqual(3, buffer.CachedBlockCount);

			// Access all 16 blocks sequentially
			for (int b = 0; b < 16; b++)
			{
				buffer.GetByte(b * 1024);
				Assert.IsTrue(buffer.CachedBlockCount <= 3, "Cached block count exceeded MaxCachedBlocks!");
			}

			Assert.AreEqual(3, buffer.CachedBlockCount);
		}
	}
}
