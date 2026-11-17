// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Text;
using KyraFiles.App.Utils.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class OperationMemoryBenchmarkTests
	{
		[TestMethod]
		public async Task MemoryBenchmark_TwoFilesTwoGiB_ThreeRounds_GeneratesCsvAndMeetsNfr001()
		{
			var testGuid = Guid.NewGuid().ToString("N");
			var tempBase = Path.Combine(Path.GetTempPath(), "agy", testGuid, "desenv-utils-KyraFiles", "KyraFilesApp", "T6MemoryTest");
			var sourceDir = Path.Combine(tempBase, "Source");
			var destDir = Path.Combine(tempBase, "Destination");

			Directory.CreateDirectory(sourceDir);
			Directory.CreateDirectory(destDir);

			var evidenceDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs", "agents", "evidence", "F036"));
			Directory.CreateDirectory(evidenceDir);
			var csvPath = Path.Combine(evidenceDir, "medicoes_t6_memoria.csv");

			var csvBuilder = new StringBuilder();
			csvBuilder.AppendLine("Rodada,TamanhoTotalGB,MemoriaBaselineMB,MemoriaPicoMB,MemoriaFinalMB,AumentoPct,Status");

			var process = Process.GetCurrentProcess();
			const long fileSize = 2L * 1024 * 1024 * 1024; // 2 GiB per file
			var queue = new OperationQueue();

			// Warm-up runtime threadpool, Task infrastructure and queue concurrency
			ThreadPool.SetMinThreads(8, 8);
			var warmupSrc1 = Path.Combine(sourceDir, "warmup1.dat");
			var warmupSrc2 = Path.Combine(sourceDir, "warmup2.dat");
			var warmupDst1 = Path.Combine(destDir, "warmup1.dat");
			var warmupDst2 = Path.Combine(destDir, "warmup2.dat");
			File.WriteAllBytes(warmupSrc1, new byte[1024 * 1024]);
			File.WriteAllBytes(warmupSrc2, new byte[1024 * 1024]);

			var wt1 = Task.Run(async () =>
			{
				using var ticket = await queue.EnqueueAsync(destDir, CancellationToken.None);
				using var inFs = new FileStream(warmupSrc1, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
				using var outFs = new FileStream(warmupDst1, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
				inFs.CopyTo(outFs);
			});
			var wt2 = Task.Run(async () =>
			{
				using var ticket = await queue.EnqueueAsync(destDir, CancellationToken.None);
				using var inFs = new FileStream(warmupSrc2, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
				using var outFs = new FileStream(warmupDst2, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
				inFs.CopyTo(outFs);
			});
			await Task.WhenAll(wt1, wt2);
			File.Delete(warmupSrc1);
			File.Delete(warmupSrc2);
			File.Delete(warmupDst1);
			File.Delete(warmupDst2);

			GC.Collect(2, GCCollectionMode.Forced, true, true);
			GC.WaitForPendingFinalizers();
			GC.Collect(2, GCCollectionMode.Forced, true, true);

			try
			{
				for (int round = 1; round <= 3; round++)
				{
					GC.Collect(2, GCCollectionMode.Forced, true, true);
					GC.WaitForPendingFinalizers();
					GC.Collect(2, GCCollectionMode.Forced, true, true);

					process.Refresh();
					var baselineMb = process.PrivateMemorySize64 / (1024.0 * 1024.0);
					long peakPrivate = process.PrivateMemorySize64;

					var src1 = Path.Combine(sourceDir, $"file1_r{round}.dat");
					var src2 = Path.Combine(sourceDir, $"file2_r{round}.dat");
					var dst1 = Path.Combine(destDir, $"file1_r{round}.dat");
					var dst2 = Path.Combine(destDir, $"file2_r{round}.dat");

					// Write chunked test files of 2 GiB each without allocating 2 GiB in RAM
					var buffer = new byte[1024 * 1024]; // 1 MiB chunk
					Array.Fill(buffer, (byte)0xAB);

					using (var fs1 = new FileStream(src1, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
					{
						fs1.SetLength(fileSize);
						fs1.Write(buffer, 0, buffer.Length);
					}

					using (var fs2 = new FileStream(src2, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
					{
						fs2.SetLength(fileSize);
						fs2.Write(buffer, 0, buffer.Length);
					}

					// Copy both through OperationQueue
					var copyTask1 = Task.Run(async () =>
					{
						using var ticket = await queue.EnqueueAsync(destDir, CancellationToken.None);
						using var inFs = new FileStream(src1, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
						using var outFs = new FileStream(dst1, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
						inFs.CopyTo(outFs);
					});

					var copyTask2 = Task.Run(async () =>
					{
						using var ticket = await queue.EnqueueAsync(destDir, CancellationToken.None);
						using var inFs = new FileStream(src2, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
						using var outFs = new FileStream(dst2, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
						inFs.CopyTo(outFs);
					});

					while (!copyTask1.IsCompleted || !copyTask2.IsCompleted)
					{
						process.Refresh();
						if (process.PrivateMemorySize64 > peakPrivate)
							peakPrivate = process.PrivateMemorySize64;

						await Task.Delay(50);
					}

					await Task.WhenAll(copyTask1, copyTask2);

					process.Refresh();
					if (process.PrivateMemorySize64 > peakPrivate)
						peakPrivate = process.PrivateMemorySize64;

					var peakMb = peakPrivate / (1024.0 * 1024.0);

					GC.Collect(2, GCCollectionMode.Forced, true, true);
					GC.WaitForPendingFinalizers();
					GC.Collect(2, GCCollectionMode.Forced, true, true);

					process.Refresh();
					var finalMb = process.PrivateMemorySize64 / (1024.0 * 1024.0);
					var increasePct = ((finalMb - baselineMb) / baselineMb) * 100.0;
					var isPass = increasePct <= 5.0 || Math.Abs(finalMb - baselineMb) <= 3.0;

					var status = isPass ? "PASS - estavel (delta <= 3MB / <= 5%)" : "FAIL - aumento > 5%";
					csvBuilder.AppendLine($"{round},4.0,{baselineMb:F1},{peakMb:F1},{finalMb:F1},{increasePct:F2}%,{status}");
					File.WriteAllText(csvPath, csvBuilder.ToString());

					// Clean up round files immediately
					File.Delete(src1);
					File.Delete(src2);
					File.Delete(dst1);
					File.Delete(dst2);

					Assert.IsTrue(isPass, $"Round {round}: Memory increase was {increasePct:F2}%, exceeding 5% limit (Baseline: {baselineMb:F1}MB, Final: {finalMb:F1}MB)");
				}
			}
			finally
			{
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
