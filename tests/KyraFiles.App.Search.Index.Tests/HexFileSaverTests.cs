// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public sealed class HexFileSaverTests
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

		private static string ComputeFileSha256(string filePath)
		{
			using var sha256 = SHA256.Create();
			using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024);
			byte[] hash = sha256.ComputeHash(stream);
			return Convert.ToHexStringLower(hash);
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

		private sealed class ThrowingWriteStream : Stream
		{
			private readonly Stream _inner;
			private readonly int _failAfterWrites;
			private int _writeCount;

			public ThrowingWriteStream(Stream inner, int failAfterWrites = 1)
			{
				_inner = inner;
				_failAfterWrites = failAfterWrites;
			}

			public override bool CanRead => _inner.CanRead;
			public override bool CanSeek => _inner.CanSeek;
			public override bool CanWrite => _inner.CanWrite;
			public override long Length => _inner.Length;
			public override long Position
			{
				get => _inner.Position;
				set => _inner.Position = value;
			}

			public override void Flush() => _inner.Flush();
			public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
			public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
			public override void SetLength(long value) => _inner.SetLength(value);

			public override void Write(byte[] buffer, int offset, int count)
			{
				_writeCount++;
				if (_writeCount > _failAfterWrites)
				{
					throw new IOException("There is not enough space on the disk. (Simulated ERROR_DISK_FULL)");
				}
				_inner.Write(buffer, offset, count);
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_inner.Dispose();
				}
				base.Dispose(disposing);
			}
		}

		[TestMethod]
		public void M2_AC4_Crc32_KnownVector_Verified()
		{
			// Known standard vector: CRC-32 of ASCII "123456789" = 0xCBF43926
			byte[] asciiBytes = Encoding.ASCII.GetBytes("123456789");
			var crc = new Crc32();
			crc.Append(asciiBytes);
			byte[] hashBytes = crc.GetCurrentHash();
			uint crcValue = BinaryPrimitives.ReadUInt32LittleEndian(hashBytes);

			Assert.AreEqual(0xCBF43926u, crcValue, "Standard CRC-32 known vector for '123456789' must equal 0xCBF43926");

			Array.Reverse(hashBytes);
			string hexString = Convert.ToHexString(hashBytes);
			Assert.AreEqual("CBF43926", hexString);

			// ComputeEntryCrc32 test with known vector
			long blockIndex = 5L;
			int blockLength = asciiBytes.Length;
			byte[] newBytes = Encoding.ASCII.GetBytes("987654321");
			uint entryCrc = HexFileSaver.ComputeEntryCrc32(blockIndex, blockLength, asciiBytes, newBytes);
			Assert.IsTrue(entryCrc != 0, "ComputeEntryCrc32 must produce valid non-zero CRC32");

			// Repeat calculation independently to ensure deterministic output
			uint entryCrc2 = HexFileSaver.ComputeEntryCrc32(blockIndex, blockLength, asciiBytes, newBytes);
			Assert.AreEqual(entryCrc, entryCrc2);
		}

		[TestMethod]
		public void M2_AC4_NoChanges_SaveReturnsSuccessNoChanges()
		{
			string testFile = Path.Combine(TestTempDir, "no_changes.bin");
			File.WriteAllBytes(testFile, new byte[] { 0x01, 0x02, 0x03, 0x04 });

			using var buffer = new HexFileBuffer(testFile, blockSize: 2);
			var result = HexFileSaver.Save(buffer);

			Assert.IsTrue(result.Success);
			Assert.AreEqual(HexSaveStatus.NoChanges, result.Status);
			Assert.AreEqual(0, result.ModifiedBlocksWritten);
			Assert.AreEqual(0, result.ModifiedBytesCount);
			Assert.IsFalse(HexFileSaver.HasPendingJournal(testFile));
		}

		[TestMethod]
		public void M2_AC4_Save_BeginningMiddleEnd_64MB_AssertsOnlyThoseBytesChanged_ViaSha256()
		{
			const long fileSize = 64 * 1024 * 1024; // 64 MB
			const int blockSize = 64 * 1024; // 64 KB

			string filePath = CreateDeterministicFile("save_64mb_test.bin", fileSize);
			string initialSha256 = ComputeFileSha256(filePath);

			using var buffer = new HexFileBuffer(filePath, blockSize: blockSize, maxCachedBlocks: 16);

			long offsetBeginning = 0;
			long offsetMiddle = 32 * 1024 * 1024; // block 512
			long offsetEnd = fileSize - 1; // block 1023

			byte originalBeg = buffer.GetByte(offsetBeginning);
			byte originalMid = buffer.GetByte(offsetMiddle);
			byte originalEnd = buffer.GetByte(offsetEnd);

			byte newBeg = (byte)(originalBeg ^ 0xFF);
			byte newMid = (byte)(originalMid ^ 0xAA);
			byte newEnd = (byte)(originalEnd ^ 0x55);

			buffer.SetByte(offsetBeginning, newBeg);
			buffer.SetByte(offsetMiddle, newMid);
			buffer.SetByte(offsetEnd, newEnd);

			Assert.AreEqual(3, buffer.ModifiedBytesCount);
			CollectionAssert.AreEqual(new long[] { 0, 512, 1023 }, buffer.ModifiedBlockIndices.ToArray());

			// Save to the original file
			var saveResult = HexFileSaver.Save(buffer);

			Assert.IsTrue(saveResult.Success, $"Save failed: {saveResult.ErrorMessage}");
			Assert.AreEqual(HexSaveStatus.Success, saveResult.Status);
			Assert.AreEqual(3, saveResult.ModifiedBlocksWritten);
			Assert.AreEqual(3, saveResult.ModifiedBytesCount);

			// Assert journal was deleted
			string journalPath = HexFileSaver.GetDefaultJournalPath(filePath);
			Assert.IsFalse(File.Exists(journalPath), "Recovery journal must be deleted after successful save.");

			// Assert SHA-256 changed from initial
			string savedSha256 = ComputeFileSha256(filePath);
			Assert.AreNotEqual(initialSha256, savedSha256, "File SHA-256 must reflect the modified bytes.");

			// Verify byte-by-byte independently: ONLY the 3 target bytes changed; all other 67,108,861 bytes match deterministic pattern
			using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024))
			{
				byte[] chunk = new byte[64 * 1024];
				long position = 0;
				int bytesRead;

				while ((bytesRead = stream.Read(chunk, 0, chunk.Length)) > 0)
				{
					for (int i = 0; i < bytesRead; i++)
					{
						long currentOffset = position + i;
						byte actualByte = chunk[i];

						if (currentOffset == offsetBeginning)
						{
							Assert.AreEqual(newBeg, actualByte, $"Offset {currentOffset} must match edited beginning byte.");
						}
						else if (currentOffset == offsetMiddle)
						{
							Assert.AreEqual(newMid, actualByte, $"Offset {currentOffset} must match edited middle byte.");
						}
						else if (currentOffset == offsetEnd)
						{
							Assert.AreEqual(newEnd, actualByte, $"Offset {currentOffset} must match edited end byte.");
						}
						else
						{
							byte expected = ExpectedByteAt(currentOffset);
							if (actualByte != expected)
							{
								Assert.Fail($"Corruption detected at offset {currentOffset}: expected {expected:X2}, got {actualByte:X2}");
							}
						}
					}
					position += bytesRead;
				}

				Assert.AreEqual(fileSize, position, "Saved file length must remain exactly 64 MB.");
			}
		}

		[TestMethod]
		public void M2_AC6_Save_IdentityChanged_SizeOrDate_RefusesWithoutWriting()
		{
			// Part A: Modification date changed
			string testFileDate = Path.Combine(TestTempDir, "identity_date.bin");
			File.WriteAllBytes(testFileDate, new byte[] { 0x10, 0x20, 0x30, 0x40 });
			string initialShaDate = ComputeFileSha256(testFileDate);

			using (var buffer = new HexFileBuffer(testFileDate, blockSize: 2))
			{
				buffer.SetByte(1, 0x99);

				// Simulate external process modifying the file's timestamp
				File.SetLastWriteTimeUtc(testFileDate, DateTime.UtcNow.AddHours(2));

				var result = HexFileSaver.Save(buffer);

				Assert.IsFalse(result.Success);
				Assert.AreEqual(HexSaveStatus.FileIdentityChanged, result.Status);
				StringAssert.Contains(result.ErrorMessage, "data de modificação");

				// Ensure no journal and original file untouched
				Assert.IsFalse(HexFileSaver.HasPendingJournal(testFileDate));
				Assert.AreEqual(initialShaDate, ComputeFileSha256(testFileDate));
				CollectionAssert.AreEqual(new byte[] { 0x10, 0x20, 0x30, 0x40 }, File.ReadAllBytes(testFileDate));
			}

			// Part B: File size changed
			string testFileSize = Path.Combine(TestTempDir, "identity_size.bin");
			File.WriteAllBytes(testFileSize, new byte[] { 0x10, 0x20, 0x30, 0x40 });

			using (var buffer = new HexFileBuffer(testFileSize, blockSize: 2))
			{
				buffer.SetByte(1, 0x99);

				// Simulate external process appending a byte to the file
				using (var appendFs = new FileStream(testFileSize, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				{
					appendFs.WriteByte(0x50);
				}

				var result = HexFileSaver.Save(buffer);

				Assert.IsFalse(result.Success);
				Assert.AreEqual(HexSaveStatus.FileIdentityChanged, result.Status);
				StringAssert.Contains(result.ErrorMessage, "tamanho");

				Assert.IsFalse(HexFileSaver.HasPendingJournal(testFileSize));
			}
		}

		[TestMethod]
		public void M2_AC7_Save_ReadOnlyFile_RefusesAndLeavesOriginalIntact()
		{
			string testFile = Path.Combine(TestTempDir, "readonly_test.bin");
			byte[] original = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
			File.WriteAllBytes(testFile, original);
			string initialSha = ComputeFileSha256(testFile);

			using (var buffer = new HexFileBuffer(testFile, blockSize: 2))
			{
				buffer.SetByte(0, 0x11);

				// Set ReadOnly attribute
				File.SetAttributes(testFile, FileAttributes.ReadOnly);

				try
				{
					var result = HexFileSaver.Save(buffer);

					Assert.IsFalse(result.Success);
					Assert.AreEqual(HexSaveStatus.ReadOnly, result.Status);
					StringAssert.Contains(result.ErrorMessage, "somente leitura");

					Assert.IsFalse(HexFileSaver.HasPendingJournal(testFile));
					Assert.AreEqual(initialSha, ComputeFileSha256(testFile));
				}
				finally
				{
					File.SetAttributes(testFile, FileAttributes.Normal);
				}
			}
		}

		[TestMethod]
		public void M2_AC7_Save_FileInUse_RefusesAndLeavesOriginalIntact()
		{
			string testFile = Path.Combine(TestTempDir, "inuse_test.bin");
			byte[] original = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
			File.WriteAllBytes(testFile, original);
			string initialSha = ComputeFileSha256(testFile);

			// 1. Another process has opened the file exclusively with FileShare.None
			using (var memStream = new MemoryStream((byte[])original.Clone()))
			using (var buffer = new HexFileBuffer(memStream, blockSize: 2))
			{
				buffer.SetByte(0, 0x11);

				using (var lockStream = new FileStream(testFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
				{
					var result = HexFileSaver.Save(buffer, testFile);

					Assert.IsFalse(result.Success);
					Assert.AreEqual(HexSaveStatus.FileInUse, result.Status);
					StringAssert.Contains(result.ErrorMessage, "em uso");

					Assert.IsFalse(HexFileSaver.HasPendingJournal(testFile));
				}
			}

			// 2. Buffer was opened on file, and another process locks with FileShare.Read (disallowing write handles)
			using (var bufferOnFile = new HexFileBuffer(testFile, blockSize: 2))
			{
				bufferOnFile.SetByte(1, 0x22);

				using (var lockStream = new FileStream(testFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
				{
					var result = HexFileSaver.Save(bufferOnFile);

					Assert.IsFalse(result.Success);
					Assert.AreEqual(HexSaveStatus.FileInUse, result.Status);
					StringAssert.Contains(result.ErrorMessage, "em uso");

					Assert.IsFalse(HexFileSaver.HasPendingJournal(testFile));
				}
			}

			// Verify original content was never modified
			Assert.AreEqual(initialSha, ComputeFileSha256(testFile));
			CollectionAssert.AreEqual(original, File.ReadAllBytes(testFile));
		}

		[TestMethod]
		public void M2_AC7_Save_SimulatedDiskFull_RollsBackAndLeavesOriginalIntactWithoutJournal()
		{
			string testFile = Path.Combine(TestTempDir, "simulated_disk_full.bin");
			byte[] original = new byte[128 * 1024]; // 128 KB = 2 blocks of 64 KB
			for (int i = 0; i < original.Length; i++)
			{
				original[i] = (byte)(i % 199);
			}
			File.WriteAllBytes(testFile, original);
			string initialSha = ComputeFileSha256(testFile);

			using var buffer = new HexFileBuffer(testFile, blockSize: 64 * 1024);

			// Edit both block 0 and block 1
			buffer.SetByte(10, 0xFE);
			buffer.SetByte(64 * 1024 + 10, 0xCA);

			Assert.AreEqual(2, buffer.ModifiedBlockIndices.Count);

			// Configure DestinationStreamFactory to throw on the 2nd block write
			var options = new HexSaveOptions
			{
				DestinationStreamFactory = (path, mode, access, share, bufferSize, fileOptions) =>
				{
					var realStream = new FileStream(path, mode, access, share, bufferSize, fileOptions);
					return new ThrowingWriteStream(realStream, failAfterWrites: 1);
				}
			};

			var result = HexFileSaver.Save(buffer, options);

			Assert.IsFalse(result.Success);
			Assert.AreEqual(HexSaveStatus.DiskFullOrIoError, result.Status);
			StringAssert.Contains(result.ErrorMessage, "space");

			// Original file must be rolled back to intact original state
			Assert.AreEqual(initialSha, ComputeFileSha256(testFile), "File content must be 100% intact after disk full rollback.");
			CollectionAssert.AreEqual(original, File.ReadAllBytes(testFile));

			// Journal must be cleaned up (no garbage left behind)
			Assert.IsFalse(HexFileSaver.HasPendingJournal(testFile), "Journal must be deleted after rollback.");
		}

		[TestMethod]
		public void M2_AC7_CrashDuringSave_RecoveryRestoresOriginalContentAndDeletesJournal()
		{
			string testFile = Path.Combine(TestTempDir, "crash_recovery_test.bin");
			byte[] original = new byte[128 * 1024]; // 128 KB = 2 blocks of 64 KB
			for (int i = 0; i < original.Length; i++)
			{
				original[i] = (byte)(i % 173);
			}
			File.WriteAllBytes(testFile, original);
			string initialSha = ComputeFileSha256(testFile);

			using (var buffer = new HexFileBuffer(testFile, blockSize: 64 * 1024))
			{
				// Edit block 0 and block 1
				buffer.SetByte(5, 0xAA);
				buffer.SetByte(64 * 1024 + 5, 0xBB);

				var options = new HexSaveOptions
				{
					// Simulate process crash after writing block 0
					SimulateCrashAfterBlocksWritten = 1
				};

				Throws<HexCrashSimulatedException>(() => HexFileSaver.Save(buffer, options));
			}

			// Assert that journal DOES exist on disk (because process crashed before cleanup)
			string journalPath = HexFileSaver.GetDefaultJournalPath(testFile);
			Assert.IsTrue(File.Exists(journalPath), "Recovery journal must exist after simulated crash.");

			// Assert that file on disk is in a partially modified state
			string crashedSha = ComputeFileSha256(testFile);
			Assert.AreNotEqual(initialSha, crashedSha, "File on disk must have partial modifications before recovery.");

			// Perform recovery
			var recoveryResult = HexFileSaver.Recover(testFile);

			Assert.IsTrue(recoveryResult.Recovered, $"Recovery failed: {recoveryResult.ErrorMessage}");
			Assert.AreEqual(2, recoveryResult.BlocksRestored, "Both blocks from journal must be restored.");
			Assert.AreEqual(0, recoveryResult.IncompleteEntriesIgnored);

			// Journal must now be deleted
			Assert.IsFalse(File.Exists(journalPath), "Journal must be deleted after recovery.");

			// File content must match initial state exactly
			string recoveredSha = ComputeFileSha256(testFile);
			Assert.AreEqual(initialSha, recoveredSha, "File SHA-256 after recovery must equal initial SHA-256.");
			CollectionAssert.AreEqual(original, File.ReadAllBytes(testFile));
		}

		[TestMethod]
		public void M2_AC7_Recovery_ForgedJournal_BlockOutsideFileOrWrongLength_TouchesNothingAndKeepsJournal()
		{
			byte[] original = new byte[4096];
			for (int i = 0; i < original.Length; i++)
				original[i] = (byte)(i % 199);

			// Caso 1: bloco muito além do fim do arquivo. Caso 2: tamanho do diário diferente do arquivo
			// (a recuperação NÃO pode esticar nem encolher o arquivo).
			foreach (var (blockIndex, journalLength) in new[] { (1_000_000L, (long)original.Length), (0L, 10_000_000_000L) })
			{
				string testFile = Path.Combine(TestTempDir, $"forged_journal_{blockIndex}_{journalLength}.bin");
				File.WriteAllBytes(testFile, original);
				string journalPath = HexFileSaver.GetDefaultJournalPath(testFile);

				const int blockSize = 64;
				byte[] orig = new byte[blockSize];
				byte[] mod = new byte[blockSize];
				Array.Fill(orig, (byte)0x11);
				Array.Fill(mod, (byte)0x22);

				using (var journal = new FileStream(journalPath, FileMode.Create, FileAccess.Write))
				{
					Span<byte> header = new byte[40];
					System.Text.Encoding.ASCII.GetBytes("HEXJRNL1").CopyTo(header[..8]);
					System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header[8..12], 1);
					System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(header[12..20], journalLength);
					System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(header[20..28], 0L);
					System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header[28..32], blockSize);
					System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header[32..36], 1);
					System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(header[36..40],
						HexFileSaver.ComputeHeaderCrc32(1, journalLength, 0L, blockSize, 1));
					journal.Write(header);

					Span<byte> prefix = new byte[12];
					System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(prefix[..8], blockIndex);
					System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(prefix[8..12], blockSize);
					journal.Write(prefix);
					journal.Write(orig);
					journal.Write(mod);

					Span<byte> crc = new byte[4];
					System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(crc,
						HexFileSaver.ComputeEntryCrc32(blockIndex, blockSize, orig, mod));
					journal.Write(crc);
				}

				var result = HexFileSaver.Recover(testFile);

				Assert.IsFalse(result.Recovered, "Diário que não bate com o arquivo não pode ser aplicado.");
				Assert.IsTrue(File.Exists(journalPath), "O diário deve ser mantido para o usuário decidir.");
				CollectionAssert.AreEqual(original, File.ReadAllBytes(testFile), "O arquivo não pode mudar de conteúdo nem de tamanho.");
			}
		}

		[TestMethod]
		public void M2_AC7_Recovery_TruncatedJournal_RestoresValidEntriesAndIgnoresIncomplete()
		{
			string testFile = Path.Combine(TestTempDir, "truncated_journal_test.bin");
			byte[] original = new byte[128 * 1024]; // 2 blocks of 64 KB
			for (int i = 0; i < original.Length; i++)
			{
				original[i] = (byte)(i % 151);
			}
			File.WriteAllBytes(testFile, original);
			string initialSha = ComputeFileSha256(testFile);

			string journalPath = HexFileSaver.GetDefaultJournalPath(testFile);

			// Manually construct a journal with 1 complete valid entry and 1 truncated entry
			using (var buffer = new HexFileBuffer(testFile, blockSize: 64 * 1024))
			{
				byte[] origBlock0 = buffer.ReadOriginalBlock(0);
				byte[] origBlock1 = buffer.ReadOriginalBlock(1);

				byte[] modBlock0 = (byte[])origBlock0.Clone();
				modBlock0[0] = 0x77;

				byte[] modBlock1 = (byte[])origBlock1.Clone();
				modBlock1[0] = 0x88;

				using (var jStream = new FileStream(journalPath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					// Header
					int version = 1;
					long length = buffer.Length;
					long ticks = buffer.InitialLastWriteTimeUtc?.Ticks ?? 0L;
					int blockSize = buffer.BlockSize;
					int entryCount = 2;
					uint headerCrc = HexFileSaver.ComputeHeaderCrc32(version, length, ticks, blockSize, entryCount);

					Span<byte> header = new byte[40];
					Encoding.ASCII.GetBytes("HEXJRNL1").CopyTo(header[..8]);
					BinaryPrimitives.WriteInt32LittleEndian(header[8..12], version);
					BinaryPrimitives.WriteInt64LittleEndian(header[12..20], length);
					BinaryPrimitives.WriteInt64LittleEndian(header[20..28], ticks);
					BinaryPrimitives.WriteInt32LittleEndian(header[28..32], blockSize);
					BinaryPrimitives.WriteInt32LittleEndian(header[32..36], entryCount);
					BinaryPrimitives.WriteUInt32LittleEndian(header[36..40], headerCrc);
					jStream.Write(header);

					// Entry 1 (complete and valid for block 0)
					Span<byte> prefix0 = stackalloc byte[12];
					BinaryPrimitives.WriteInt64LittleEndian(prefix0[..8], 0L);
					BinaryPrimitives.WriteInt32LittleEndian(prefix0[8..12], origBlock0.Length);
					jStream.Write(prefix0);
					jStream.Write(origBlock0);
					jStream.Write(modBlock0);
					uint crc0 = HexFileSaver.ComputeEntryCrc32(0L, origBlock0.Length, origBlock0, modBlock0);
					Span<byte> crcBuf0 = stackalloc byte[4];
					BinaryPrimitives.WriteUInt32LittleEndian(crcBuf0, crc0);
					jStream.Write(crcBuf0);

					// Entry 2 (TRUNCATED - only 8 bytes written instead of 16 + 2 * 64KB)
					Span<byte> prefix1 = stackalloc byte[8];
					BinaryPrimitives.WriteInt64LittleEndian(prefix1, 1L);
					jStream.Write(prefix1);

					jStream.Flush();
				}
			}

			// Simulate that block 0 on disk was modified before crash
			using (var destStream = new FileStream(testFile, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
			{
				destStream.Seek(0, SeekOrigin.Begin);
				destStream.WriteByte(0x77);
			}

			Assert.AreNotEqual(initialSha, ComputeFileSha256(testFile));

			// Recover with truncated journal
			var recoveryResult = HexFileSaver.Recover(testFile);

			Assert.IsTrue(recoveryResult.Recovered, "Recovery must succeed on truncated journal.");
			Assert.AreEqual(1, recoveryResult.BlocksRestored, "Block 0 must be restored from valid Entry 1.");
			Assert.AreEqual(1, recoveryResult.IncompleteEntriesIgnored, "Incomplete Entry 2 must be ignored.");

			Assert.IsFalse(File.Exists(journalPath), "Journal must be deleted after recovery.");
			Assert.AreEqual(initialSha, ComputeFileSha256(testFile), "File content must be restored to initial state.");
		}
	}
}
