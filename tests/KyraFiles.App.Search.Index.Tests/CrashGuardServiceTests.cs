// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.IO;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class CrashGuardServiceTests
	{
		private string? _tempTestDir;
		private string? _tempStateFile;
		private string? _tempLogFile;

		[TestInitialize]
		public void Setup()
		{
			_tempTestDir = Path.Combine(Path.GetTempPath(), "agy", Guid.NewGuid().ToString(), "CrashGuardTests");
			Directory.CreateDirectory(_tempTestDir);
			_tempStateFile = Path.Combine(_tempTestDir, "crash_guard.json");
			_tempLogFile = Path.Combine(_tempTestDir, "crash_guard.log");
		}

		[TestCleanup]
		public void Cleanup()
		{
			if (_tempTestDir is not null && Directory.Exists(_tempTestDir))
			{
				try
				{
					Directory.Delete(_tempTestDir, true);
				}
				catch
				{
					// Ignore cleanup errors
				}
			}
		}

		[TestMethod]
		public void SingleCrash_DoesNotTriggerFallback_AC3()
		{
			var now = DateTimeOffset.UtcNow;
			var triggered = CrashGuardService.RecordCrash("Test crash 1", -1, _tempStateFile, _tempLogFile, now);

			Assert.IsFalse(triggered, "Single crash must not trigger fallback.");
			Assert.IsFalse(CrashGuardService.ShouldFallbackToExplorer(_tempStateFile, now), "ShouldFallbackToExplorer must be false for single crash.");

			var state = CrashGuardService.LoadState(_tempStateFile);
			Assert.AreEqual(1, state.RecentCrashes.Count);
			Assert.IsFalse(state.FallbackTriggered);
			Assert.IsFalse(state.PromptOnNextManualLaunch);
		}

		[TestMethod]
		public void TwoCrashesWithin60Seconds_TriggersFallback_AC1()
		{
			var baseTime = DateTimeOffset.UtcNow;

			// First crash
			var trigger1 = CrashGuardService.RecordCrash("Test crash 1", -1, _tempStateFile, _tempLogFile, baseTime);
			Assert.IsFalse(trigger1);

			// Second crash 20 seconds later
			var secondCrashTime = baseTime.AddSeconds(20);
			var trigger2 = CrashGuardService.RecordCrash("Test crash 2", -1073741819, _tempStateFile, _tempLogFile, secondCrashTime);

			Assert.IsTrue(trigger2, "Two crashes within 60s must trigger fallback.");
			Assert.IsTrue(CrashGuardService.ShouldFallbackToExplorer(_tempStateFile, secondCrashTime), "ShouldFallbackToExplorer must return true.");

			var state = CrashGuardService.LoadState(_tempStateFile);
			Assert.AreEqual(2, state.RecentCrashes.Count);
			Assert.IsTrue(state.FallbackTriggered);
			Assert.IsTrue(state.PromptOnNextManualLaunch);
			Assert.AreEqual(-1073741819, state.LastExitCode);
		}

		[TestMethod]
		public void CrashesSeparatedByMoreThan60Seconds_DoNotTriggerFallback_AC4()
		{
			var baseTime = DateTimeOffset.UtcNow;

			// First crash
			CrashGuardService.RecordCrash("Test crash 1", -1, _tempStateFile, _tempLogFile, baseTime);

			// Second crash 70 seconds later (window expired)
			var secondCrashTime = baseTime.AddSeconds(70);
			var trigger2 = CrashGuardService.RecordCrash("Test crash 2", -1, _tempStateFile, _tempLogFile, secondCrashTime);

			Assert.IsFalse(trigger2, "Crashes separated by >60s must not trigger fallback.");
			Assert.IsFalse(CrashGuardService.ShouldFallbackToExplorer(_tempStateFile, secondCrashTime));

			var state = CrashGuardService.LoadState(_tempStateFile);
			Assert.AreEqual(1, state.RecentCrashes.Count, "Expired crash should have been pruned.");
		}

		[TestMethod]
		public void StableExecution_ClearsCrashHistory_AC6()
		{
			var baseTime = DateTimeOffset.UtcNow;

			// One crash occurred
			CrashGuardService.RecordCrash("Test crash 1", -1, _tempStateFile, _tempLogFile, baseTime);
			Assert.AreEqual(1, CrashGuardService.LoadState(_tempStateFile).RecentCrashes.Count);

			// After running stably
			CrashGuardService.RecordStable(_tempStateFile, _tempLogFile);

			var stateAfter = CrashGuardService.LoadState(_tempStateFile);
			Assert.AreEqual(0, stateAfter.RecentCrashes.Count, "Stable execution must reset recent crashes list.");
			Assert.IsFalse(stateAfter.FallbackTriggered);
		}

		[TestMethod]
		public void AcknowledgeRecoveryNotice_ResetsPromptFlag_AC2()
		{
			var state = new CrashGuardState
			{
				FallbackTriggered = true,
				PromptOnNextManualLaunch = true,
			};
			CrashGuardService.SaveState(state, _tempStateFile);

			Assert.IsTrue(CrashGuardService.LoadState(_tempStateFile).PromptOnNextManualLaunch);

			CrashGuardService.AcknowledgeRecoveryNotice(_tempStateFile);

			Assert.IsFalse(CrashGuardService.LoadState(_tempStateFile).PromptOnNextManualLaunch);
		}

		[TestMethod]
		public void LogFile_ContainsRecordedEvents_AC5()
		{
			var baseTime = DateTimeOffset.UtcNow;
			CrashGuardService.RecordCrash("Log test reason", 42, _tempStateFile, _tempLogFile, baseTime);

			Assert.IsTrue(File.Exists(_tempLogFile), "Log file should be created.");
			var logContent = File.ReadAllText(_tempLogFile);
			StringAssert.Contains(logContent, "CRASH_RECORDED");
			StringAssert.Contains(logContent, "Log test reason");
			StringAssert.Contains(logContent, "ExitCode=42");
		}
	}
}

