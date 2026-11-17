// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.Helpers
{
	/// <summary>
	/// State data for crash guard persistence.
	/// </summary>
	public sealed class CrashGuardState
	{
		public List<DateTimeOffset> RecentCrashes { get; set; } = new();
		public bool FallbackTriggered { get; set; }
		public bool PromptOnNextManualLaunch { get; set; }
		public string? LastCrashReason { get; set; }
		public int LastExitCode { get; set; }
	}

	[JsonSerializable(typeof(CrashGuardState))]
	internal sealed partial class CrashGuardJsonContext : JsonSerializerContext
	{
	}

	/// <summary>
	/// Service that monitors premature crashes and routes folder requests to Windows Explorer
	/// if consecutive crashes occur within a 60-second window (F069).
	/// </summary>
	public static class CrashGuardService
	{
		public const int CrashThreshold = 2;
		public static readonly TimeSpan CrashWindow = TimeSpan.FromSeconds(60);
		public static readonly TimeSpan StabilityThreshold = TimeSpan.FromSeconds(10);

		private static readonly object _syncLock = new();
		private static DateTimeOffset _processStartTime = DateTimeOffset.UtcNow;
		private static bool _isStable;

		public static bool IsStable => _isStable;

		/// <summary>
		/// Gets the default path for the crash guard state file.
		/// </summary>
		public static string GetDefaultStateFilePath()
		{
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var targetDir = Path.Combine(localAppData, "KyraFiles");
			return Path.Combine(targetDir, "crash_guard.json");
		}

		/// <summary>
		/// Gets the default path for the crash guard log file.
		/// </summary>
		public static string GetDefaultLogFilePath()
		{
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			var targetDir = Path.Combine(localAppData, "KyraFiles");
			return Path.Combine(targetDir, "crash_guard.log");
		}

		/// <summary>
		/// Records application start time.
		/// </summary>
		public static void RecordStart()
		{
			_processStartTime = DateTimeOffset.UtcNow;
			_isStable = false;
		}

		/// <summary>
		/// Loads the state from disk.
		/// </summary>
		public static CrashGuardState LoadState(string? customFilePath = null)
		{
			lock (_syncLock)
			{
				var filePath = customFilePath ?? GetDefaultStateFilePath();
				if (!File.Exists(filePath))
					return new CrashGuardState();

				try
				{
					var json = File.ReadAllText(filePath);
					var state = JsonSerializer.Deserialize(json, CrashGuardJsonContext.Default.CrashGuardState);
					return state ?? new CrashGuardState();
				}
				catch
				{
					return new CrashGuardState();
				}
			}
		}

		/// <summary>
		/// Saves the state to disk.
		/// </summary>
		public static void SaveState(CrashGuardState state, string? customFilePath = null)
		{
			lock (_syncLock)
			{
				var filePath = customFilePath ?? GetDefaultStateFilePath();
				try
				{
					var dir = Path.GetDirectoryName(filePath);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
						Directory.CreateDirectory(dir);

					var json = JsonSerializer.Serialize(state, CrashGuardJsonContext.Default.CrashGuardState);
					File.WriteAllText(filePath, json);
				}
				catch
				{
					// Avoid throwing inside crash handling
				}
			}
		}

		/// <summary>
		/// Appends an entry to the crash guard log.
		/// </summary>
		public static void LogCrashEvent(string message, string? customLogPath = null)
		{
			lock (_syncLock)
			{
				var logPath = customLogPath ?? GetDefaultLogFilePath();
				try
				{
					var dir = Path.GetDirectoryName(logPath);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
						Directory.CreateDirectory(dir);

					var entry = $"[{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz}] {message}{Environment.NewLine}";
					File.AppendAllText(logPath, entry);
				}
				catch
				{
					// Best-effort logging
				}
			}
		}

		/// <summary>
		/// Evaluates whether the system should fall back to Windows Explorer.
		/// </summary>
		public static bool ShouldFallbackToExplorer(string? customFilePath = null, DateTimeOffset? nowOverride = null)
		{
			lock (_syncLock)
			{
				var state = LoadState(customFilePath);
				var now = nowOverride ?? DateTimeOffset.UtcNow;

				// Prune crashes older than the 60s window
				var recentCount = state.RecentCrashes.Count(t => (now - t) <= CrashWindow);
				return recentCount >= CrashThreshold || state.FallbackTriggered;
			}
		}

		/// <summary>
		/// Records a crash event. If the threshold is reached within 60s, triggers fallback.
		/// </summary>
		public static bool RecordCrash(string reason, int exitCode = -1, string? customFilePath = null, string? customLogPath = null, DateTimeOffset? nowOverride = null)
		{
			lock (_syncLock)
			{
				var now = nowOverride ?? DateTimeOffset.UtcNow;
				var state = LoadState(customFilePath);

				// Remove expired crashes
				state.RecentCrashes = state.RecentCrashes
					.Where(t => (now - t) <= CrashWindow)
					.ToList();

				state.RecentCrashes.Add(now);
				state.LastCrashReason = reason;
				state.LastExitCode = exitCode;

				var fallbackNow = state.RecentCrashes.Count >= CrashThreshold;
				if (fallbackNow)
				{
					state.FallbackTriggered = true;
					state.PromptOnNextManualLaunch = true;
				}

				SaveState(state, customFilePath);

				LogCrashEvent($"CRASH_RECORDED: ExitCode={exitCode}, Reason={reason}, RecentCount={state.RecentCrashes.Count}, FallbackTriggered={state.FallbackTriggered}", customLogPath);

				return fallbackNow;
			}
		}

		/// <summary>
		/// Marks execution as stable (e.g. after running smoothly for >10s), clearing recent crash history.
		/// </summary>
		public static void RecordStable(string? customFilePath = null, string? customLogPath = null)
		{
			lock (_syncLock)
			{
				_isStable = true;
				var state = LoadState(customFilePath);

				if (state.RecentCrashes.Count > 0 || state.FallbackTriggered)
				{
					state.RecentCrashes.Clear();
					state.FallbackTriggered = false;
					SaveState(state, customFilePath);
					LogCrashEvent("STABLE_EXECUTION: Recent crash history and fallback flag cleared.", customLogPath);
				}
			}
		}

		/// <summary>
		/// Resets the manual prompt notice once displayed to the user.
		/// </summary>
		public static void AcknowledgeRecoveryNotice(string? customFilePath = null)
		{
			lock (_syncLock)
			{
				var state = LoadState(customFilePath);
				if (state.PromptOnNextManualLaunch)
				{
					state.PromptOnNextManualLaunch = false;
					SaveState(state, customFilePath);
				}
			}
		}

		/// <summary>
		/// Launches Windows Explorer as fallback on the requested directory.
		/// </summary>
		public static bool LaunchExplorerFallback(string? targetDirectory, string? customLogPath = null)
		{
			try
			{
				var psi = new ProcessStartInfo
				{
					FileName = "explorer.exe",
					UseShellExecute = true,
				};

				if (!string.IsNullOrWhiteSpace(targetDirectory) && (Directory.Exists(targetDirectory) || File.Exists(targetDirectory)))
				{
					psi.Arguments = $"\"{targetDirectory}\"";
				}

				Process.Start(psi);
				LogCrashEvent($"EXPLORER_FALLBACK_LAUNCHED: Target='{targetDirectory ?? "(Home/Computer)"}'", customLogPath);
				return true;
			}
			catch (Exception ex)
			{
				LogCrashEvent($"EXPLORER_FALLBACK_FAILED: Target='{targetDirectory}', Error={ex.Message}", customLogPath);
				return false;
			}
		}
	}
}
