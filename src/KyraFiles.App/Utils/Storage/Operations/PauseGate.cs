// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.App.Utils.Storage
{
	/// <summary>
	/// Coordinates pausing and resuming of multi-item file operations between files.
	/// Implements Phase B of F036 (AC-1 Level 1).
	/// </summary>
	public sealed partial class PauseGate : IDisposable
	{
		private readonly Lock _sync = new();
		private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private bool _isPaused;
		private bool _isPausePending;

		public bool IsPaused
		{
			get
			{
				lock (_sync)
					return _isPaused;
			}
		}

		public bool IsPausePending
		{
			get
			{
				lock (_sync)
					return _isPausePending;
			}
		}

		public PauseGate()
		{
			// Initially unpaused
			_tcs.TrySetResult(true);
		}

		public void RequestPause()
		{
			lock (_sync)
			{
				if (_isPaused)
					return;

				_isPausePending = true;
				_isPaused = true;
				_tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			}
		}

		public void Resume()
		{
			TaskCompletionSource<bool> tcsToComplete;
			lock (_sync)
			{
				if (!_isPaused && !_isPausePending)
					return;

				_isPaused = false;
				_isPausePending = false;
				tcsToComplete = _tcs;
			}

			tcsToComplete.TrySetResult(true);
		}

		public async Task WaitIfPausedAsync(CancellationToken cancellationToken = default)
		{
			Task pauseTask;
			lock (_sync)
			{
				_isPausePending = false;
				if (!_isPaused)
					return;

				pauseTask = _tcs.Task;
			}

			await pauseTask.WaitAsync(cancellationToken);
		}

		public void Dispose()
		{
			Resume();
		}
	}
}
