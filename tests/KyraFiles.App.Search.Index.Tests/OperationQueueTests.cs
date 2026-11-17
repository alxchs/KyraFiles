// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.App.Utils.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	/// <summary>
	/// Tests for <see cref="OperationQueue"/> validating F036 T2 (FR-002, AC-2, AC-3 and decision D-F036-3 option b).
	/// </summary>
	[TestClass]
	public sealed class OperationQueueTests
	{
		[TestMethod]
		public void DestinationKey_LocalDrives_NormalizedCorrectly()
		{
			var queue = new OperationQueue();

			Assert.AreEqual("C:", queue.GetDestinationKey(@"C:\Users\User\Downloads"));
			Assert.AreEqual("C:", queue.GetDestinationKey("c:/windows/system32"));
			Assert.AreEqual("D:", queue.GetDestinationKey(@"D:\Backups\Archive.zip"));
			Assert.AreEqual("E:", queue.GetDestinationKey("E:"));
		}

		[TestMethod]
		public void DestinationKey_UNCNetworkPaths_NormalizedByServer()
		{
			var queue = new OperationQueue();

			Assert.AreEqual(@"\\NAS", queue.GetDestinationKey(@"\\nas\share\media"));
			Assert.AreEqual(@"\\SERVER01", queue.GetDestinationKey(@"\\server01\c$\folder"));
			Assert.AreEqual(@"\\MYROUTER", queue.GetDestinationKey("//myrouter/usb_drive/files"));
		}

		[TestMethod]
		public void DestinationKey_NullOrEmpty_ReturnsDefault()
		{
			var queue = new OperationQueue();

			Assert.AreEqual("DEFAULT", queue.GetDestinationKey(null));
			Assert.AreEqual("DEFAULT", queue.GetDestinationKey(""));
			Assert.AreEqual("DEFAULT", queue.GetDestinationKey("   "));
		}

		[TestMethod]
		public async Task ConcurrencyLimit2_TwoSameDestinationRunConcurrently_ThirdWaits()
		{
			var queue = new OperationQueue
			{
				MaxConcurrentOperationsPerDestination = 2
			};

			var dest = @"C:\DestinationFolder";

			// First two slots should acquire immediately
			using var ticket1 = await queue.EnqueueAsync(dest);
			using var ticket2 = await queue.EnqueueAsync(dest);

			Assert.AreEqual(2, queue.GetRunningCount(dest));
			Assert.AreEqual(0, queue.GetWaitingCount(dest));

			// Third operation must wait
			var thirdAcquired = false;
			IOperationQueueTicket? ticket3 = null;
			var thirdTask = Task.Run(async () =>
			{
				ticket3 = await queue.EnqueueAsync(dest);
				thirdAcquired = true;
			});

			await Task.Delay(50);
			Assert.IsFalse(thirdAcquired, "Third task must not acquire while 2 operations are running");
			Assert.AreEqual(2, queue.GetRunningCount(dest));
			Assert.AreEqual(1, queue.GetWaitingCount(dest));

			// Release first ticket: third task should now acquire
			ticket1.Dispose();

			await thirdTask;
			Assert.IsTrue(thirdAcquired, "Third task should acquire once a slot is released");
			Assert.AreEqual(2, queue.GetRunningCount(dest));
			Assert.AreEqual(0, queue.GetWaitingCount(dest));

			ticket2.Dispose();
			ticket3?.Dispose();
			Assert.AreEqual(0, queue.GetRunningCount(dest));
		}

		[TestMethod]
		public async Task ConcurrencyLimit1_SerializesOperationsOnSameDestination()
		{
			var queue = new OperationQueue
			{
				MaxConcurrentOperationsPerDestination = 1
			};

			var dest = @"C:\Target";

			using var ticket1 = await queue.EnqueueAsync(dest);
			Assert.AreEqual(1, queue.GetRunningCount(dest));

			var secondAcquired = false;
			var secondTask = Task.Run(async () =>
			{
				using var ticket2 = await queue.EnqueueAsync(dest);
				secondAcquired = true;
			});

			await Task.Delay(50);
			Assert.IsFalse(secondAcquired, "Second task must wait when limit is 1");

			ticket1.Dispose();
			await secondTask;
			Assert.IsTrue(secondAcquired, "Second task must acquire after first is disposed");
		}

		[TestMethod]
		public async Task DifferentDestinations_RunConcurrentlyWithoutBlocking()
		{
			var queue = new OperationQueue
			{
				MaxConcurrentOperationsPerDestination = 1
			};

			var destC = @"C:\Folder";
			var destD = @"D:\OtherFolder";
			var destNas = @"\\NAS\Share";

			// Operations on different destinations should not block each other even if limit is 1
			using var ticketC = await queue.EnqueueAsync(destC);
			using var ticketD = await queue.EnqueueAsync(destD);
			using var ticketNas = await queue.EnqueueAsync(destNas);

			Assert.AreEqual(1, queue.GetRunningCount(destC));
			Assert.AreEqual(1, queue.GetRunningCount(destD));
			Assert.AreEqual(1, queue.GetRunningCount(destNas));
		}

		[TestMethod]
		public async Task Cancellation_WhileWaiting_AbortsWithoutRetainingSlot()
		{
			var queue = new OperationQueue
			{
				MaxConcurrentOperationsPerDestination = 1
			};

			var dest = @"C:\Folder";
			using var ticket1 = await queue.EnqueueAsync(dest);

			using var cts = new CancellationTokenSource();
			var task = Task.Run(async () =>
			{
				return await queue.EnqueueAsync(dest, cts.Token);
			});

			await Task.Delay(50);
			Assert.AreEqual(1, queue.GetWaitingCount(dest));

			// Cancel waiting task
			cts.Cancel();

			var caught = false;
			try
			{
				await task;
			}
			catch (OperationCanceledException)
			{
				caught = true;
			}

			Assert.IsTrue(caught, "OperationCanceledException should be thrown when token is cancelled");

			Assert.AreEqual(0, queue.GetWaitingCount(dest), "Waiting count must return to 0 after cancellation");
			Assert.AreEqual(1, queue.GetRunningCount(dest), "Running count of original operation remains intact");

			// Ensure subsequent operations can still acquire once slot is freed
			ticket1.Dispose();
			using var nextTicket = await queue.EnqueueAsync(dest);
			Assert.AreEqual(1, queue.GetRunningCount(dest));
		}

		[TestMethod]
		public async Task DisabledQueue_NegativeControl_AllowsUnlimitedConcurrency()
		{
			var queue = new OperationQueue
			{
				IsEnabled = false
			};

			var dest = @"C:\Folder";

			using var ticket1 = await queue.EnqueueAsync(dest);
			using var ticket2 = await queue.EnqueueAsync(dest);
			using var ticket3 = await queue.EnqueueAsync(dest);
			using var ticket4 = await queue.EnqueueAsync(dest);

			// When disabled, all acquire immediately
			Assert.AreEqual(0, queue.GetWaitingCount(dest));
		}

		[TestMethod]
		public async Task DoubleDispose_IsIdempotent()
		{
			var queue = new OperationQueue
			{
				MaxConcurrentOperationsPerDestination = 1
			};

			var dest = @"C:\Folder";
			var ticket = await queue.EnqueueAsync(dest);
			Assert.AreEqual(1, queue.GetRunningCount(dest));

			ticket.Dispose();
			Assert.AreEqual(0, queue.GetRunningCount(dest));

			// Double dispose should not throw or corrupt semaphore count
			ticket.Dispose();
			Assert.AreEqual(0, queue.GetRunningCount(dest));

			// Slot can still be acquired exactly once
			using var nextTicket = await queue.EnqueueAsync(dest);
			Assert.AreEqual(1, queue.GetRunningCount(dest));
		}

		[TestMethod]
		public async Task Benchmark_TenRounds_GeneratesEvidenceCsv()
		{
			var evidenceDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\..\docs\agents\evidence\F036"));
			if (!Directory.Exists(evidenceDir))
			{
				Directory.CreateDirectory(evidenceDir);
			}

			var csvPath = Path.Combine(evidenceDir, "medicoes_t2_concorrencia.csv");
			var tempDir = Path.Combine(Path.GetTempPath(), "agy", Guid.NewGuid().ToString(), "desenv", "utils", "KyraFiles", "BenchmarkF036");
			Directory.CreateDirectory(tempDir);

			try
			{
				var src1 = Path.Combine(tempDir, "origem1.bin");
				var src2 = Path.Combine(tempDir, "origem2.bin");

				// 64 MB per file
				var buffer = new byte[1024 * 1024];
				new Random(42).NextBytes(buffer);

				using (var fs1 = File.Create(src1))
				using (var fs2 = File.Create(src2))
				{
					for (int i = 0; i < 64; i++)
					{
						fs1.Write(buffer, 0, buffer.Length);
						fs2.Write(buffer, 0, buffer.Length);
					}
				}

				var lines = new List<string>
				{
					"Rodada,TamanhoMB,SerialMs,ParaleloMs,CopiaEDeleteMs,VariacaoConcorrentePercent"
				};

				for (int round = 1; round <= 10; round++)
				{
					var dst1Serial = Path.Combine(tempDir, $"dest1_serial_{round}.bin");
					var dst2Serial = Path.Combine(tempDir, $"dest2_serial_{round}.bin");

					var swSerial = Stopwatch.StartNew();
					File.Copy(src1, dst1Serial, true);
					File.Copy(src2, dst2Serial, true);
					swSerial.Stop();
					var serialMs = swSerial.ElapsedMilliseconds;

					File.Delete(dst1Serial);
					File.Delete(dst2Serial);

					var dst1Parallel = Path.Combine(tempDir, $"dest1_parallel_{round}.bin");
					var dst2Parallel = Path.Combine(tempDir, $"dest2_parallel_{round}.bin");

					var swParallel = Stopwatch.StartNew();
					await Task.WhenAll(
						Task.Run(() => File.Copy(src1, dst1Parallel, true)),
						Task.Run(() => File.Copy(src2, dst2Parallel, true))
					);
					swParallel.Stop();
					var parallelMs = swParallel.ElapsedMilliseconds;

					File.Delete(dst1Parallel);
					File.Delete(dst2Parallel);

					var dstCopy = Path.Combine(tempDir, $"dest_copy_{round}.bin");
					var deleteDir = Path.Combine(tempDir, $"del_{round}");
					Directory.CreateDirectory(deleteDir);
					for (int k = 1; k <= 50; k++)
					{
						File.WriteAllBytes(Path.Combine(deleteDir, $"f_{k}.tmp"), buffer[0..4096]);
					}

					var swDelete = Stopwatch.StartNew();
					await Task.WhenAll(
						Task.Run(() => File.Copy(src1, dstCopy, true)),
						Task.Run(() =>
						{
							foreach (var f in Directory.GetFiles(deleteDir, "*.tmp"))
							{
								File.Delete(f);
							}
						})
					);
					swDelete.Stop();
					var withDeleteMs = swDelete.ElapsedMilliseconds;

					File.Delete(dstCopy);
					Directory.Delete(deleteDir, true);

					var diffPercent = Math.Round(((double)(parallelMs - serialMs) / Math.Max(1, serialMs)) * 100, 2);
					lines.Add($"{round},128,{serialMs},{parallelMs},{withDeleteMs},{diffPercent}");
				}

				await File.WriteAllLinesAsync(csvPath, lines);
				Assert.IsTrue(File.Exists(csvPath), "Evidence CSV must be written successfully");
			}
			finally
			{
				if (Directory.Exists(tempDir))
				{
					Directory.Delete(tempDir, true);
				}
			}
		}
	}
}
