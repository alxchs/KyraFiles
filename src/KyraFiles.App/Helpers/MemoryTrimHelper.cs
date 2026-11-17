// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime;
using KyraFiles.App.Utils.Storage.Operations;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace KyraFiles.App.Helpers
{
	/// <summary>
	/// Provides safe, non-blocking working set and garbage collection trimming
	/// when the application is minimized, hidden, or closed to tray.
	/// Guarantees zero UI lag by running strictly on background threads and aborting
	/// immediately if user activity or window restoration occurs.
	/// </summary>
	internal static class MemoryTrimHelper
	{
		private static readonly object lockObj = new();
		private static readonly TimeSpan fileOperationsWaitLimit = TimeSpan.FromSeconds(30);
		private static CancellationTokenSource? currentTrimCts;

		/// <summary>
		/// Schedules a soft working set trim after a debounce delay (default 2000 ms).
		/// If the window is restored before the delay elapses, the trim is cleanly canceled.
		/// </summary>
		public static void ScheduleTrim(int delayMs = 2000, bool forceFullGc = false)
		{
			lock (lockObj)
			{
				currentTrimCts?.Cancel();
				currentTrimCts?.Dispose();

				currentTrimCts = new CancellationTokenSource();
				var token = currentTrimCts.Token;

				_ = Task.Run(async () =>
				{
					try
					{
						if (delayMs > 0)
						{
							await Task.Delay(delayMs, token).ConfigureAwait(false);
						}

						if (token.IsCancellationRequested)
							return;

						// Wait for ongoing file operations, but never past the limit nor after a restore (F063, Q2);
						// if an operation is still running, this trim is skipped
						if (!FileOperationsHelpers.WaitForCompletion(fileOperationsWaitLimit, token) || token.IsCancellationRequested)
							return;

						ExecuteTrim(forceFullGc, token);
					}
					catch (OperationCanceledException)
					{
						// Expected when cancelled by user action / window restore
					}
					catch
					{
						// Fail-safe: memory management should never crash the app
					}
				}, token);
			}
		}

		/// <summary>
		/// Cancels any scheduled trim immediately (e.g. upon window restore/activation).
		/// </summary>
		public static void CancelPendingTrim()
		{
			lock (lockObj)
			{
				currentTrimCts?.Cancel();
				currentTrimCts?.Dispose();
				currentTrimCts = null;
			}
		}

		/// <summary>
		/// Executes the working set trim synchronously on the calling thread (used by background workers).
		/// </summary>
		public static void ExecuteTrim(bool forceFullGc, CancellationToken cancellationToken = default)
		{
			try
			{
				if (cancellationToken.IsCancellationRequested)
					return;

				if (forceFullGc)
				{
					GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
					GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
					GC.WaitForPendingFinalizers();
					GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
				}
				else
				{
					GC.Collect(2, GCCollectionMode.Optimized, false, false);
					GC.WaitForPendingFinalizers();
				}

				if (cancellationToken.IsCancellationRequested)
					return;

				using var process = Process.GetCurrentProcess();
				var handle = new HANDLE(process.Handle);
				PInvoke.K32EmptyWorkingSet(handle);
			}
			catch
			{
				// Fail-safe: empty working set is an optimization
			}
		}
	}
}
