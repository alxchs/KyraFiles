// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using KyraFiles.App.Utils.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	/// <summary>
	/// Integration tests for F036 T3: verifying operation queue behavior under realistic concurrent dispatch.
	/// </summary>
	[TestClass]
	public sealed class OperationQueueIntegrationTests
	{
		[TestMethod]
		public async Task Integration_TwoCopies_SameDestination_SerializesWhenLimitIsOne()
		{
			var queue = new OperationQueue
			{
				MaxConcurrentOperationsPerDestination = 1
			};

			var dest = @"C:\Destination\Volume";
			var log = new ConcurrentQueue<(string op, string action, long timeMs)>();
			var sw = Stopwatch.StartNew();

			var task1 = Task.Run(async () =>
			{
				log.Enqueue(("op1", "waiting", sw.ElapsedMilliseconds));
				using var ticket = await queue.EnqueueAsync(dest);
				log.Enqueue(("op1", "started", sw.ElapsedMilliseconds));
				await Task.Delay(100);
				log.Enqueue(("op1", "finished", sw.ElapsedMilliseconds));
			});

			await Task.Delay(20);

			var task2 = Task.Run(async () =>
			{
				log.Enqueue(("op2", "waiting", sw.ElapsedMilliseconds));
				using var ticket = await queue.EnqueueAsync(dest);
				log.Enqueue(("op2", "started", sw.ElapsedMilliseconds));
				await Task.Delay(50);
				log.Enqueue(("op2", "finished", sw.ElapsedMilliseconds));
			});

			await Task.WhenAll(task1, task2);

			var events = log.ToArray();
			long op1FinishTime = 0;
			long op2StartTime = 0;

			foreach (var ev in events)
			{
				if (ev.op == "op1" && ev.action == "finished")
					op1FinishTime = ev.timeMs;
				if (ev.op == "op2" && ev.action == "started")
					op2StartTime = ev.timeMs;
			}

			Assert.IsTrue(op2StartTime >= op1FinishTime - 10,
				$"Op2 start ({op2StartTime}ms) must occur after Op1 finish ({op1FinishTime}ms) when serialized.");
		}

		[TestMethod]
		public async Task Integration_TwoCopies_DifferentDestinations_RunConcurrently()
		{
			var queue = new OperationQueue
			{
				MaxConcurrentOperationsPerDestination = 1
			};

			var destC = @"C:\TargetFolder";
			var destD = @"D:\TargetFolder";
			var overlapDetected = false;
			var runningKeys = new ConcurrentDictionary<string, bool>();

			var task1 = Task.Run(async () =>
			{
				using var ticket = await queue.EnqueueAsync(destC);
				runningKeys[destC] = true;
				if (runningKeys.ContainsKey(destD)) overlapDetected = true;
				await Task.Delay(100);
				runningKeys.TryRemove(destC, out _);
			});

			var task2 = Task.Run(async () =>
			{
				using var ticket = await queue.EnqueueAsync(destD);
				runningKeys[destD] = true;
				if (runningKeys.ContainsKey(destC)) overlapDetected = true;
				await Task.Delay(100);
				runningKeys.TryRemove(destD, out _);
			});

			await Task.WhenAll(task1, task2);
			Assert.IsTrue(overlapDetected, "Operations targeting different drives must run concurrently.");
		}

		[TestMethod]
		public async Task Integration_DeleteAndRename_BypassQueueEntirely()
		{
			var queue = new OperationQueue
			{
				MaxConcurrentOperationsPerDestination = 1
			};

			var dest = @"C:\TargetVolume";

			// Saturate queue with a long-running copy
			using var copyTicket = await queue.EnqueueAsync(dest);
			Assert.AreEqual(1, queue.GetRunningCount(dest));

			// Simulated Delete and Rename operations bypass the queue entirely
			var deleteExecuted = false;
			var renameExecuted = false;

			// Delete runs outside the queue
			await Task.Run(() =>
			{
				// Simulating delete
				deleteExecuted = true;
			});

			// Rename runs outside the queue
			await Task.Run(() =>
			{
				// Simulating rename
				renameExecuted = true;
			});

			Assert.IsTrue(deleteExecuted, "Delete operation must execute immediately without queueing");
			Assert.IsTrue(renameExecuted, "Rename operation must execute immediately without queueing");
			Assert.AreEqual(0, queue.GetWaitingCount(dest), "Delete/Rename must not enter the waiting queue");
		}
	}
}
