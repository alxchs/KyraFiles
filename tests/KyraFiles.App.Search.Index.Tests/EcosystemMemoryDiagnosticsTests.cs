// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using KyraFiles.Shared.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KyraFiles.App.Search.Index.Tests
{
	[TestClass]
	public class EcosystemMemoryDiagnosticsTests
	{
		[TestMethod]
		public void FormatBytes_FormatsZero_ReturnsZeroMB()
		{
			Assert.AreEqual("0 MB", EcosystemMemoryDiagnostics.FormatBytes(0));
			Assert.AreEqual("0 MB", EcosystemMemoryDiagnostics.FormatBytes(-100));
		}

		[TestMethod]
		public void FormatBytes_FormatsKilobytes_ReturnsExpectedString()
		{
			long bytes = 500 * 1024;
			Assert.AreEqual("500.0 KB", EcosystemMemoryDiagnostics.FormatBytes(bytes));
		}

		[TestMethod]
		public void FormatBytes_FormatsMegabytes_ReturnsExpectedString()
		{
			long bytes = 184 * 1024 * 1024;
			Assert.AreEqual("184.0 MB", EcosystemMemoryDiagnostics.FormatBytes(bytes));
		}

		[TestMethod]
		public void FormatBytes_FormatsGigabytes_ReturnsExpectedString()
		{
			long bytes = (long)(2.5 * 1024 * 1024 * 1024);
			Assert.AreEqual("2.50 GB", EcosystemMemoryDiagnostics.FormatBytes(bytes));
		}

		[TestMethod]
		public void EcosystemMemoryReport_CalculatesTotalsAndClipboardText()
		{
			var items = new List<ProcessMemoryItem>
			{
				new ("KyraFiles", 1000, 150 * 1024 * 1024, 120 * 1024 * 1024, true),
				new ("KyraFiles.App.Server", 1001, 20 * 1024 * 1024, 15 * 1024 * 1024, true),
				new ("KyraFiles.HotkeyService", 1002, 5 * 1024 * 1024, 3 * 1024 * 1024, true),
				new ("KyraFiles.Watchdog", 1003, 4 * 1024 * 1024, 2 * 1024 * 1024, true),
				new ("KyraFiles.App.IndexService", 0, 0, 0, false)
			};

			long totalWs = items.Where(x => x.IsRunning).Sum(x => x.WorkingSet64);
			long totalPriv = items.Where(x => x.IsRunning).Sum(x => x.PrivateMemorySize64);

			var report = new EcosystemMemoryReport(
				new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero),
				items,
				totalWs,
				totalPriv,
				4);

			Assert.AreEqual("179.0 MB", report.FormattedTotalWorkingSet);
			Assert.AreEqual("140.0 MB", report.FormattedTotalPrivateMemory);
			Assert.AreEqual(4, report.ActiveProcessCount);

			string text = report.ToClipboardText();
			StringAssert.Contains(text, "KyraFiles — Ecosystem Memory Report");
			StringAssert.Contains(text, "Total Working Set (RAM): 179.0 MB");
			StringAssert.Contains(text, "Files (PID 1000)");
			StringAssert.Contains(text, "KyraFiles.App.IndexService: Inactive (0 MB)");
		}

		[TestMethod]
		public void CollectSnapshot_RunsSafelyAndReturnsAllMonitoredProcesses()
		{
			var snapshot = EcosystemMemoryDiagnostics.CollectSnapshot();

			Assert.IsNotNull(snapshot);
			Assert.IsTrue(snapshot.Items.Count >= EcosystemMemoryDiagnostics.MonitoredProcessNames.Length);

			// Cada nome monitorado deve estar presente nos itens
			foreach (var name in EcosystemMemoryDiagnostics.MonitoredProcessNames)
			{
				Assert.IsTrue(snapshot.Items.Any(item => item.ProcessName == name));
			}
		}
		[TestMethod]
		public void EcosystemMemoryReport_FormatSummary_IncludesBothWorkingSetAndPrivateMemory()
		{
			var report = new EcosystemMemoryReport(
				DateTimeOffset.UtcNow,
				Array.Empty<ProcessMemoryItem>(),
				150 * 1024 * 1024,
				200 * 1024 * 1024,
				3);

			string summary = report.FormatSummary("RAM: {0} | Private: {1} ({2} active process(es))");
			Assert.AreEqual("RAM: 150.0 MB | Private: 200.0 MB (3 active process(es))", summary);
		}
	}
}

