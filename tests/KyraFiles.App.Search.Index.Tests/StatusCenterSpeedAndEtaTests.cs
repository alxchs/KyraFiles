// Copyright (c) Files Community
// Licensed under the MIT License.

using System.ComponentModel;
using KyraFiles.App.Data.Enums;
using KyraFiles.App.Utils.StatusCenter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class StatusCenterSpeedAndEtaTests
	{
		[TestMethod]
		public void SpeedAndEta_ExponentialMovingAverage_SmoothsSpeedAndCalculatesEta()
		{
			// Arrange: total size 100 MB, sampler interval 0 to capture every report
			var progressModel = new StatusCenterItemProgressModel(null, enumerationCompleted: true, itemsCount: 10, samplerInterval: 0);
			progressModel.TotalSize = 100 * 1024 * 1024; // 100 MB

			// Act 1: Initial progress after 100ms
			Thread.Sleep(150);
			progressModel.SetProcessedSize(10 * 1024 * 1024); // 10 MB in ~0.15s
			progressModel.Report();

			var speed1 = progressModel.ProcessingSizeSpeed;
			Assert.IsTrue(speed1 > 0, "Speed should be positive after first report");
			Assert.IsNotNull(progressModel.EstimatedRemainingTime, "Estimated remaining time should be calculated");
			Assert.IsNotNull(progressModel.EstimatedRemainingTimeText, "Estimated remaining text should not be null");

			// Act 2: Simulate second chunk with higher burst
			Thread.Sleep(150);
			progressModel.SetProcessedSize(30 * 1024 * 1024); // +20 MB
			progressModel.Report();

			var speed2 = progressModel.ProcessingSizeSpeed;
			Assert.IsTrue(speed2 > 0, "Speed should remain positive");

			// Verify remaining time tolerance within 15% of expected (remaining 70MB / speed2)
			var remainingBytes = progressModel.TotalSize - progressModel.ProcessedSize;
			var expectedSeconds = (double)remainingBytes / speed2;
			var actualSeconds = progressModel.EstimatedRemainingTime!.Value.TotalSeconds;

			var diff = Math.Abs(actualSeconds - expectedSeconds);
			var tolerance = expectedSeconds * 0.15;
			Assert.IsTrue(diff <= Math.Max(1.0, tolerance), $"ETA {actualSeconds}s was not within 15% of expected {expectedSeconds}s (diff: {diff})");
		}

		[TestMethod]
		public void SpeedAndEta_Completion_ClearsEstimatedTime()
		{
			var progressModel = new StatusCenterItemProgressModel(null, enumerationCompleted: true, itemsCount: 1, samplerInterval: 0);
			progressModel.TotalSize = 1024 * 1024;

			Thread.Sleep(120);
			progressModel.SetProcessedSize(512 * 1024);
			progressModel.Report();

			Assert.IsNotNull(progressModel.EstimatedRemainingTime);

			Thread.Sleep(120);
			progressModel.SetProcessedSize(1024 * 1024);
			progressModel.AddProcessedItemsCount(1);
			progressModel.Report();

			Assert.AreEqual(TimeSpan.Zero, progressModel.EstimatedRemainingTime);
			Assert.IsNull(progressModel.EstimatedRemainingTimeText);
		}

		[TestMethod]
		public void SpeedAndEta_Formatting_ReturnsHumanReadableUnits()
		{
			var progressModel = new StatusCenterItemProgressModel(null, enumerationCompleted: true, itemsCount: 10, samplerInterval: 0);
			
			// Total size: 50 MB, at ~5 MB/s -> ~10s remaining (seconds format)
			progressModel.TotalSize = 50 * 1024 * 1024;
			Thread.Sleep(150);
			progressModel.SetProcessedSize(5 * 1024 * 1024);
			progressModel.Report();

			var text = progressModel.EstimatedRemainingTimeText;
			Assert.IsNotNull(text);
			Assert.IsTrue(text.EndsWith("restantes", StringComparison.Ordinal), $"Expected 'restantes' suffix in '{text}'");
		}
	}
}
