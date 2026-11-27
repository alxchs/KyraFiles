// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using KyraFiles.App.Utils.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class PauseGateTests
	{
		[TestMethod]
		public async Task PauseGate_InitiallyUnpaused_CompletesImmediately()
		{
			using var gate = new PauseGate();
			Assert.IsFalse(gate.IsPaused);
			Assert.IsFalse(gate.IsPausePending);

			var waitTask = gate.WaitIfPausedAsync();
			Assert.IsTrue(waitTask.IsCompletedSuccessfully, "Unpaused gate must complete immediately");
			await waitTask;
		}

		[TestMethod]
		public async Task PauseGate_PauseAndResume_TransitionsStateCorrectly()
		{
			using var gate = new PauseGate();

			// 1. Request pause while operation is running
			gate.RequestPause();
			Assert.IsTrue(gate.IsPaused);
			Assert.IsTrue(gate.IsPausePending);

			// 2. Start waiting between items
			var waitTask = gate.WaitIfPausedAsync();
			Assert.IsFalse(waitTask.IsCompleted, "WaitTask must be pending while paused");
			Assert.IsFalse(gate.IsPausePending, "PausePending transitions to false once waiting begins");
			Assert.IsTrue(gate.IsPaused, "Still paused while waiting");

			// 3. Resume
			gate.Resume();
			await waitTask;
			Assert.IsFalse(gate.IsPaused);
			Assert.IsFalse(gate.IsPausePending);
		}

		[TestMethod]
		public async Task PauseGate_CancellationDuringPause_AbortsWait()
		{
			using var gate = new PauseGate();
			gate.RequestPause();

			using var cts = new CancellationTokenSource();
			var waitTask = gate.WaitIfPausedAsync(cts.Token);
			Assert.IsFalse(waitTask.IsCompleted);

			cts.Cancel();
			var threw = false;
			try
			{
				await waitTask;
			}
			catch (OperationCanceledException)
			{
				threw = true;
			}

			Assert.IsTrue(threw, "WaitIfPausedAsync must throw OperationCanceledException when token is cancelled");
		}

		[TestMethod]
		public async Task PauseGate_MultiItemCopy_PausesBetweenItemsAndPreservesSourceSafety()
		{
			var testGuid = Guid.NewGuid().ToString("N");
			var tempBase = Path.Combine(Path.GetTempPath(), "agy", testGuid, "desenv-utils-KyraFiles", "KyraFilesApp", "T7PauseTest");
			var srcDir = Path.Combine(tempBase, "Source");
			var dstDir = Path.Combine(tempBase, "Destination");

			Directory.CreateDirectory(srcDir);
			Directory.CreateDirectory(dstDir);

			try
			{
				const int totalFiles = 5;
				var sourceFiles = new List<string>();
				var destFiles = new List<string>();
				for (int i = 0; i < totalFiles; i++)
				{
					var s = Path.Combine(srcDir, $"item_{i}.txt");
					var d = Path.Combine(dstDir, $"item_{i}.txt");
					File.WriteAllText(s, $"Content {i}");
					sourceFiles.Add(s);
					destFiles.Add(d);
				}

				using var gate = new PauseGate();
				int itemsCopied = 0;

				var copyTask = Task.Run(async () =>
				{
					for (int i = 0; i < totalFiles; i++)
					{
						await gate.WaitIfPausedAsync();

						File.Copy(sourceFiles[i], destFiles[i]);
						Interlocked.Increment(ref itemsCopied);
						await Task.Delay(30);
					}
				});

				// Wait for at least 1 file to be copied, then pause
				while (Volatile.Read(ref itemsCopied) < 1)
					await Task.Delay(10);

				gate.RequestPause();
				await Task.Delay(100);

				var snapshot = Volatile.Read(ref itemsCopied);
				Assert.IsTrue(snapshot < totalFiles, "Operation should be paused before all files finish");
				await Task.Delay(100);
				Assert.AreEqual(snapshot, Volatile.Read(ref itemsCopied), "No more files should be copied while paused");

				// Resume and finish
				gate.Resume();
				await copyTask;

				Assert.AreEqual(totalFiles, itemsCopied, "All files should finish after resume");
				for (int i = 0; i < totalFiles; i++)
					Assert.IsTrue(File.Exists(destFiles[i]));
			}
			finally
			{
				if (Directory.Exists(tempBase))
				{
					try { Directory.Delete(tempBase, true); } catch { }
				}
			}
		}

		[TestMethod]
		public async Task PauseGate_OverheadBenchmark_OneThousandSmallFiles_ThreeRounds_GeneratesCsv()
		{
			var testGuid = Guid.NewGuid().ToString("N");
			var tempBase = Path.Combine(Path.GetTempPath(), "agy", testGuid, "desenv-utils-KyraFiles", "KyraFilesApp", "T7OverheadTest");
			var srcDir = Path.Combine(tempBase, "Source");
			var dstDirBatch = Path.Combine(tempBase, "DstBatch");
			var dstDirGate = Path.Combine(tempBase, "DstGate");

			Directory.CreateDirectory(srcDir);
			Directory.CreateDirectory(dstDirBatch);
			Directory.CreateDirectory(dstDirGate);

			var csvPath = TestEvidence.GetCsvPath("F036", "medicoes_t7_sobrecarga.csv", tempBase);

			var csvBuilder = new StringBuilder();
			csvBuilder.AppendLine("Rodada,Arquivos,TamanhoTotalKB,TempoLoteMs,TempoItemPorItemMs,SobrecargaPct,Observacao");

			const int fileCount = 1000;
			var dummyData = new byte[1024]; // 1 KB
			Array.Fill(dummyData, (byte)0x42);

			var files = new List<string>(fileCount);
			for (int i = 0; i < fileCount; i++)
			{
				var p = Path.Combine(srcDir, $"small_{i:D4}.bin");
				File.WriteAllBytes(p, dummyData);
				files.Add(p);
			}

			try
			{
				using var gate = new PauseGate();

				for (int round = 1; round <= 3; round++)
				{
					// Clean destinations
					foreach (var f in Directory.GetFiles(dstDirBatch)) File.Delete(f);
					foreach (var f in Directory.GetFiles(dstDirGate)) File.Delete(f);

					// Measure Batch (direct loop without PauseGate check)
					var swBatch = Stopwatch.StartNew();
					for (int i = 0; i < fileCount; i++)
					{
						File.Copy(files[i], Path.Combine(dstDirBatch, Path.GetFileName(files[i])));
					}
					swBatch.Stop();
					var timeBatchMs = swBatch.ElapsedMilliseconds;

					// Measure Item-by-item with PauseGate check
					var swGate = Stopwatch.StartNew();
					for (int i = 0; i < fileCount; i++)
					{
						await gate.WaitIfPausedAsync();
						File.Copy(files[i], Path.Combine(dstDirGate, Path.GetFileName(files[i])));
					}
					swGate.Stop();
					var timeGateMs = swGate.ElapsedMilliseconds;

					var overheadPct = timeBatchMs > 0
						? ((timeGateMs - timeBatchMs) / (double)timeBatchMs) * 100.0
						: 0.0;

					var obs = overheadPct < 15.0 ? "Sobrecarga negligivel (< 15%)" : "Sobrecarga aceitavel";
					csvBuilder.AppendLine(CultureInfo.InvariantCulture, $"{round},{fileCount},1000,{timeBatchMs},{timeGateMs},{overheadPct:F1}%,{obs}");
				}

				File.WriteAllText(csvPath, csvBuilder.ToString());
			}
			finally
			{
				if (Directory.Exists(tempBase))
				{
					try { Directory.Delete(tempBase, true); } catch { }
				}
			}
		}
	}
}
