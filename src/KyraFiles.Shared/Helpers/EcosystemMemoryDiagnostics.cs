// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace KyraFiles.Shared.Helpers
{
	/// <summary>
	/// Represents memory statistics of a single ecosystem process.
	/// </summary>
	public sealed class ProcessMemoryItem
	{
		public string ProcessName { get; set; } = string.Empty;
		public int Id { get; set; }
		public long WorkingSet64 { get; set; }
		public long PrivateMemorySize64 { get; set; }
		public bool IsRunning { get; set; }

		public string FormattedWorkingSet => EcosystemMemoryDiagnostics.FormatBytes(WorkingSet64);
		public string FormattedPrivateMemory => EcosystemMemoryDiagnostics.FormatBytes(PrivateMemorySize64);

		public ProcessMemoryItem() { }

		public ProcessMemoryItem(string processName, int id, long workingSet64, long privateMemorySize64, bool isRunning)
		{
			ProcessName = processName;
			Id = id;
			WorkingSet64 = workingSet64;
			PrivateMemorySize64 = privateMemorySize64;
			IsRunning = isRunning;
		}
	}

	/// <summary>
	/// Represents an aggregated snapshot of memory across all KyraFiles processes.
	/// </summary>
	public sealed record EcosystemMemoryReport(
		DateTimeOffset Timestamp,
		IReadOnlyList<ProcessMemoryItem> Items,
		long TotalWorkingSet64,
		long TotalPrivateMemorySize64,
		int ActiveProcessCount)
	{
		public string FormattedTotalWorkingSet => EcosystemMemoryDiagnostics.FormatBytes(TotalWorkingSet64);
		public string FormattedTotalPrivateMemory => EcosystemMemoryDiagnostics.FormatBytes(TotalPrivateMemorySize64);

		public string FormatSummary(string format)
		{
			return string.Format(format, FormattedTotalWorkingSet, FormattedTotalPrivateMemory, ActiveProcessCount);
		}

		public string ToClipboardText()
		{
			var sb = new StringBuilder();
			sb.AppendLine($"KyraFiles — Ecosystem Memory Report");
			sb.AppendLine($"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss zzz}");
			sb.AppendLine($"Total Working Set (RAM): {FormattedTotalWorkingSet}");
			sb.AppendLine($"Total Private Bytes: {FormattedTotalPrivateMemory}");
			sb.AppendLine($"Active Processes: {ActiveProcessCount}");
			sb.AppendLine(new string('-', 50));

			foreach (var item in Items)
			{
				if (item.IsRunning)
				{
					sb.AppendLine($"{item.ProcessName} (PID {item.Id}): WS={item.FormattedWorkingSet}, Private={item.FormattedPrivateMemory}");
				}
				else
				{
					sb.AppendLine($"{item.ProcessName}: Inactive (0 MB)");
				}
			}

			return sb.ToString();
		}
	}

	/// <summary>
	/// Provides memory diagnostic capabilities across all known processes of the KyraFiles ecosystem.
	/// </summary>
	public static class EcosystemMemoryDiagnostics
	{
		public static readonly string[] MonitoredProcessNames =
		[
			"KyraFiles",
			"KyraFiles.App.Server",
			"KyraFiles.HotkeyService",
			"KyraFiles.Watchdog",
			"KyraFiles.App.IndexService"
		];

		/// <summary>
		/// Formats a byte quantity into a human-readable string (B, KB, MB, GB).
		/// </summary>
		public static string FormatBytes(long bytes)
		{
			if (bytes <= 0)
				return "0 MB";

			double b = bytes;
			if (b < 1024)
				return $"{b.ToString("F0", CultureInfo.InvariantCulture)} B";
			if (b < 1024 * 1024)
				return $"{ (b / 1024.0).ToString("F1", CultureInfo.InvariantCulture) } KB";
			if (b < 1024 * 1024 * 1024)
				return $"{ (b / (1024.0 * 1024.0)).ToString("F1", CultureInfo.InvariantCulture) } MB";

			return $"{ (b / (1024.0 * 1024.0 * 1024.0)).ToString("F2", CultureInfo.InvariantCulture) } GB";
		}

		/// <summary>
		/// Collects the current memory snapshot for all monitored ecosystem processes.
		/// Safely handles processes exiting during query.
		/// </summary>
		public static EcosystemMemoryReport CollectSnapshot()
		{
			var items = new List<ProcessMemoryItem>();
			long totalWs = 0;
			long totalPriv = 0;
			int activeCount = 0;

			foreach (var targetName in MonitoredProcessNames)
			{
				Process[] processes;
				try
				{
					processes = Process.GetProcessesByName(targetName);
				}
				catch
				{
					processes = [];
				}

				if (processes.Length == 0)
				{
					items.Add(new ProcessMemoryItem(targetName, 0, 0, 0, false));
					continue;
				}

				foreach (var proc in processes)
				{
					using (proc)
					{
						long ws = 0;
						long priv = 0;
						int pid = 0;
						bool running = false;

						try
						{
							if (!proc.HasExited)
							{
								pid = proc.Id;
								proc.Refresh();
								ws = proc.WorkingSet64;
								priv = proc.PrivateMemorySize64;
								running = true;
							}
						}
						catch
						{
							// Process exited or access restricted
							running = false;
						}

						if (running)
						{
							totalWs += ws;
							totalPriv += priv;
							activeCount++;
							items.Add(new ProcessMemoryItem(targetName, pid, ws, priv, true));
						}
						else
						{
							items.Add(new ProcessMemoryItem(targetName, 0, 0, 0, false));
						}
					}
				}
			}

			return new EcosystemMemoryReport(
				DateTimeOffset.Now,
				items,
				totalWs,
				totalPriv,
				activeCount);
		}
	}
}
