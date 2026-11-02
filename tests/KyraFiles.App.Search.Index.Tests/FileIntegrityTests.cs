// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Files.App.Services.FileIntegrity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Files.App.Search.Index.Tests
{
	[TestClass]
	public class FileIntegrityTests
	{
		private string? _tempDir;

		[TestInitialize]
		public void Setup()
		{
			_tempDir = Path.Combine(Path.GetTempPath(), "agy", Guid.NewGuid().ToString(), "FileIntegrityTests");
			Directory.CreateDirectory(_tempDir);
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (_tempDir is not null && Directory.Exists(_tempDir))
			{
				try
				{
					Directory.Delete(_tempDir, true);
				}
				catch
				{
				}
			}
		}

		[TestMethod]
		public async Task CalculateHashAsync_StandardVectors_MatchExpectedValues()
		{
			var service = new FileIntegrityService();
			var testFile = Path.Combine(_tempDir!, "fox.txt");
			var text = "The quick brown fox jumps over the lazy dog";
			File.WriteAllText(testFile, text, new UTF8Encoding(false));

			// Known standard hashes for "The quick brown fox jumps over the lazy dog"
			var md5 = await service.CalculateHashAsync(testFile, ChecksumAlgorithm.MD5);
			Assert.AreEqual("9e107d9d372bb6826bd81d3542a419d6", md5);

			var sha1 = await service.CalculateHashAsync(testFile, ChecksumAlgorithm.SHA1);
			Assert.AreEqual("2fd4e1c67a2d28fced849ee1bb76e7391b93eb12", sha1);

			var sha256 = await service.CalculateHashAsync(testFile, ChecksumAlgorithm.SHA256);
			Assert.AreEqual("d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592", sha256);

			var sha512 = await service.CalculateHashAsync(testFile, ChecksumAlgorithm.SHA512);
			Assert.AreEqual("07e547d9586f6a73f73fbac0435ed76951218fb7d0c8d788a309d785436bbb642e93a252a954f23912547d1e8a3b5ed6e1bfd7097821233fa0538f3db854fee6", sha512);

			var crc32 = await service.CalculateHashAsync(testFile, ChecksumAlgorithm.CRC32);
			Assert.AreEqual("414fa339", crc32);
		}

		[TestMethod]
		public async Task CalculateHashAsync_ReportsProgress()
		{
			var service = new FileIntegrityService();
			var testFile = Path.Combine(_tempDir!, "progress_test.bin");

			// 1 MB file
			var data = new byte[1024 * 1024];
			new Random(42).NextBytes(data);
			await File.WriteAllBytesAsync(testFile, data);

			var progressCount = 0;
			double lastPct = 0;
			var progress = new Progress<FileHashProgress>(p =>
			{
				progressCount++;
				lastPct = p.Percentage;
			});

			var hash = await service.CalculateHashAsync(testFile, ChecksumAlgorithm.SHA256, progress);

			Assert.IsNotNull(hash);
			Assert.IsTrue(progressCount > 0, "Progress should have been reported multiple times.");
			Assert.AreEqual(100.0, lastPct, 0.01, "Final progress percentage should be 100%.");
		}

		[TestMethod]
		public async Task CalculateHashAsync_Cancellation_AbortsImmediately()
		{
			var service = new FileIntegrityService();
			var testFile = Path.Combine(_tempDir!, "cancel_test.bin");

			// 20 MB file
			var data = new byte[1024 * 1024];
			await using (var fs = new FileStream(testFile, FileMode.Create, FileAccess.Write))
			{
				for (int i = 0; i < 20; i++)
				{
					await fs.WriteAsync(data);
				}
			}

			using var cts = new CancellationTokenSource();
			cts.CancelAfter(50); // Cancel after 50 ms

			var sw = Stopwatch.StartNew();
			try
			{
				await service.CalculateHashAsync(testFile, ChecksumAlgorithm.SHA512, null, cts.Token);
				Assert.Fail("Operation should have thrown OperationCanceledException.");
			}
			catch (OperationCanceledException)
			{
				sw.Stop();
				Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"Cancellation took {sw.ElapsedMilliseconds} ms, expected < 500 ms.");
			}
		}

		[TestMethod]
		public void CompareHash_NormalizesFormattingAndCase()
		{
			var service = new FileIntegrityService();
			var expected = "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592";

			// Uppercase
			Assert.IsTrue(service.CompareHash(expected, expected.ToUpperInvariant()));

			// With SHA256: prefix
			Assert.IsTrue(service.CompareHash(expected, "SHA256: " + expected));
			Assert.IsTrue(service.CompareHash(expected, "sha-256: " + expected.ToUpperInvariant()));

			// With 0x prefix
			Assert.IsTrue(service.CompareHash(expected, "0x" + expected));

			// With spaces and hyphens
			var spaced = "d7 a8 fb b3-07 d7 80 94-69 ca 9a bc-b0 08 2e 4f-8d 56 51 e4-6d 3c db 76-2d 02 d0 bf-37 c9 e5 92";
			Assert.IsTrue(service.CompareHash(expected, spaced));

			// Mismatched hash
			Assert.IsFalse(service.CompareHash(expected, "0000000000000000000000000000000000000000000000000000000000000000"));
		}

		[TestMethod]
		public async Task ZoneIdentifierService_BatchUnblock_RemovesStreamsAccurately()
		{
			var zoneService = new ZoneIdentifierService();

			var file1 = Path.Combine(_tempDir!, "downloaded1.txt");
			var file2 = Path.Combine(_tempDir!, "downloaded2.txt");
			var file3 = Path.Combine(_tempDir!, "downloaded3.txt");
			var file4 = Path.Combine(_tempDir!, "local1.txt");
			var file5 = Path.Combine(_tempDir!, "local2.txt");

			File.WriteAllText(file1, "Content 1");
			File.WriteAllText(file2, "Content 2");
			File.WriteAllText(file3, "Content 3");
			File.WriteAllText(file4, "Content 4");
			File.WriteAllText(file5, "Content 5");

			// Attach Zone.Identifier stream to files 1, 2, 3
			var zoneContent = "[ZoneTransfer]\r\nZoneId=3\r\n";
			File.WriteAllText(file1 + ":Zone.Identifier", zoneContent);
			File.WriteAllText(file2 + ":Zone.Identifier", zoneContent);
			File.WriteAllText(file3 + ":Zone.Identifier", zoneContent);

			Assert.IsTrue(zoneService.HasZoneIdentifier(file1));
			Assert.IsTrue(zoneService.HasZoneIdentifier(file2));
			Assert.IsTrue(zoneService.HasZoneIdentifier(file3));
			Assert.IsFalse(zoneService.HasZoneIdentifier(file4));
			Assert.IsFalse(zoneService.HasZoneIdentifier(file5));

			// Run batch unblock
			var files = new[] { file1, file2, file3, file4, file5 };
			var result = await zoneService.UnblockFilesAsync(files);

			Assert.AreEqual(3, result.UnblockedCount, "Expected 3 files to be unblocked.");
			Assert.AreEqual(2, result.UnchangedCount, "Expected 2 files without stream to remain unchanged.");
			Assert.AreEqual(0, result.FailedCount, "Expected 0 failures.");

			// Verify streams are gone
			Assert.IsFalse(zoneService.HasZoneIdentifier(file1));
			Assert.IsFalse(zoneService.HasZoneIdentifier(file2));
			Assert.IsFalse(zoneService.HasZoneIdentifier(file3));
		}
	}
}

