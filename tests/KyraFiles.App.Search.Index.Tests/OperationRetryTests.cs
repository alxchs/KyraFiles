// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Files.App.Data.Items;
using Files.App.Utils.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class OperationRetryTests
	{
		[TestMethod]
		public async Task RetryFailedItems_ThreeLockedFilesAndFiveFree_RetriesOnlyFailedThree()
		{
			// Follow Alexandre's strict rule for temporary test folders: %TEMP%\agy\<GUID>\<caminho>\<app>\
			var testGuid = Guid.NewGuid().ToString("N");
			var tempBase = Path.Combine(Path.GetTempPath(), "agy", testGuid, "desenv-utils-KyraFiles", "KyraFilesApp", "T5RetryTest");
			var sourceDir = Path.Combine(tempBase, "Source");
			var destDir = Path.Combine(tempBase, "Destination");

			Directory.CreateDirectory(sourceDir);
			Directory.CreateDirectory(destDir);

			var lockStreams = new List<FileStream>();
			try
			{
				// 1. Create 8 files (5 free, 3 locked)
				var sourceFiles = new List<string>();
				var destinationFiles = new List<string>();
				for (int i = 0; i < 8; i++)
				{
					var filePath = Path.Combine(sourceDir, $"file_{i}.txt");
					File.WriteAllText(filePath, $"Content for file {i}");
					sourceFiles.Add(filePath);
					destinationFiles.Add(Path.Combine(destDir, $"file_{i}.txt"));
				}

				// Lock 3 files: index 1, 3, 5
				var lockedIndices = new HashSet<int> { 1, 3, 5 };
				foreach (var idx in lockedIndices)
				{
					var stream = new FileStream(sourceFiles[idx], FileMode.Open, FileAccess.ReadWrite, FileShare.None);
					lockStreams.Add(stream);
				}

				// 2. Perform copy simulation mimicking ShellFilesystemOperations
				var opResult = new ShellOperationResult();
				var queue = new OperationQueue();

				using (await queue.EnqueueAsync(destDir, CancellationToken.None))
				{
					for (int i = 0; i < 8; i++)
					{
						var src = sourceFiles[i];
						var dst = destinationFiles[i];
						try
						{
							// Attempt copy
							using var inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read);
							using var outStream = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None);
							inStream.CopyTo(outStream);

							opResult.Items.Add(new ShellOperationItemResult
							{
								Source = src,
								Destination = dst,
								Succeeded = true,
								HResult = 0
							});
						}
						catch (IOException ex)
						{
							opResult.Items.Add(new ShellOperationItemResult
							{
								Source = src,
								Destination = dst,
								Succeeded = false,
								HResult = ex.HResult
							});
						}
					}
				}

				// 3. Verify exactly 3 failed
				var failedItems = opResult.Items.Where(x => !x.Succeeded).ToList();
				Assert.AreEqual(3, failedItems.Count, "Exactly 3 files should have failed");
				foreach (var failed in failedItems)
				{
					var fileName = Path.GetFileName(failed.Source);
					Assert.IsTrue(fileName == "file_1.txt" || fileName == "file_3.txt" || fileName == "file_5.txt", $"Unexpected failed file: {fileName}");
				}

				// 4. Verify retry filter logic isolates only the 3 failed files
				var retrySources = sourceFiles.Where(s => failedItems.Any(f => string.Equals(f.Source, s, StringComparison.OrdinalIgnoreCase))).ToList();
				var retryDestinations = destinationFiles.Where(d => failedItems.Any(f => string.Equals(f.Destination, d, StringComparison.OrdinalIgnoreCase))).ToList();

				Assert.AreEqual(3, retrySources.Count, "Retry list should have only 3 items");
				Assert.AreEqual(3, retryDestinations.Count, "Retry destinations should have only 3 items");

				// 5. Release locks on the 3 files
				foreach (var stream in lockStreams)
				{
					stream.Dispose();
				}
				lockStreams.Clear();

				// 6. Execute retry via OperationQueue
				var retryResult = new ShellOperationResult();
				using (await queue.EnqueueAsync(destDir, CancellationToken.None))
				{
					for (int i = 0; i < retrySources.Count; i++)
					{
						var src = retrySources[i];
						var dst = retryDestinations[i];
						using (var inStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read))
						using (var outStream = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None))
						{
							inStream.CopyTo(outStream);
						}

						retryResult.Items.Add(new ShellOperationItemResult
						{
							Source = src,
							Destination = dst,
							Succeeded = true,
							HResult = 0
						});
					}
				}

				// 7. Verify retry processed only 3 items and all 8 files now exist in destination
				Assert.AreEqual(3, retryResult.Items.Count, "Only the 3 failed items should have been processed during retry");
				Assert.IsTrue(retryResult.Items.All(x => x.Succeeded), "All retried items should succeed");

				for (int i = 0; i < 8; i++)
				{
					Assert.IsTrue(File.Exists(destinationFiles[i]), $"Destination file {i} should exist");
					Assert.AreEqual($"Content for file {i}", File.ReadAllText(destinationFiles[i]));
				}
			}
			finally
			{
				foreach (var s in lockStreams)
				{
					s.Dispose();
				}

				if (Directory.Exists(tempBase))
				{
					try
					{
						Directory.Delete(tempBase, true);
					}
					catch
					{
					}
				}
			}
		}
	}
}
