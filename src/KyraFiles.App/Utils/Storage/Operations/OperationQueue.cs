// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.App.Utils.Storage
{
	/// <summary>
	/// Implementation of <see cref="IOperationQueue"/> controlling concurrent copy and move operations per destination.
	/// Satisfies requirements of F036 (FR-002, AC-2, AC-3) and decision D-F036-3 (option b).
	/// </summary>
	public sealed class OperationQueue : IOperationQueue
	{
		private const int DefaultMaxConcurrentOperations = 2;
		private int _maxConcurrentOperations = DefaultMaxConcurrentOperations;
		private bool _isEnabled = true;

		private readonly ConcurrentDictionary<string, DestinationQueueEntry> _queues = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets or sets the maximum concurrent copy/move operations allowed per destination volume/server.
		/// Default is 2. Clamped between 1 and 16.
		/// </summary>
		public int MaxConcurrentOperationsPerDestination
		{
			get => _maxConcurrentOperations;
			set
			{
				var clamped = Math.Clamp(value, 1, 16);
				if (_maxConcurrentOperations != clamped)
				{
					_maxConcurrentOperations = clamped;
					// Update existing semaphores where possible
					foreach (var entry in _queues.Values)
					{
						entry.UpdateCapacity(clamped);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets whether queue throttling is active. If false, tickets are acquired immediately without blocking.
		/// </summary>
		public bool IsEnabled
		{
			get => _isEnabled;
			set => _isEnabled = value;
		}

		/// <inheritdoc/>
		public string GetDestinationKey(string? destinationPath)
		{
			if (string.IsNullOrWhiteSpace(destinationPath))
				return "DEFAULT";

			var trimmed = destinationPath.Trim();

			// UNC network path: \\server\share or //server/share
			if (trimmed.StartsWith(@"\\", StringComparison.Ordinal) || trimmed.StartsWith("//", StringComparison.Ordinal))
			{
				var slashIndex = trimmed.IndexOfAny(['\\', '/'], 2);
				if (slashIndex > 2)
				{
					var serverName = trimmed.Substring(2, slashIndex - 2);
					return @"\\" + serverName.ToUpperInvariant();
				}
				return trimmed.ToUpperInvariant();
			}

			// Local drive letter: e.g. C:\ or C:
			if (trimmed.Length >= 2 && char.IsLetter(trimmed[0]) && trimmed[1] == ':')
			{
				return char.ToUpperInvariant(trimmed[0]) + ":";
			}

			// Path root fallback
			try
			{
				var root = Path.GetPathRoot(trimmed);
				if (!string.IsNullOrEmpty(root))
				{
					return root.TrimEnd('\\', '/').ToUpperInvariant();
				}
			}
			catch
			{
				// In case of invalid path format
			}

			return trimmed.ToUpperInvariant();
		}

		/// <inheritdoc/>
		public async Task<IOperationQueueTicket> EnqueueAsync(string? destinationPath, CancellationToken cancellationToken = default)
		{
			var key = GetDestinationKey(destinationPath);

			if (!_isEnabled)
			{
				return new OperationQueueTicket(this, key, Guid.NewGuid(), isThrottled: false);
			}

			var entry = _queues.GetOrAdd(key, k => new DestinationQueueEntry(k, _maxConcurrentOperations));
			return await entry.AcquireAsync(this, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public int GetRunningCount(string? destinationPath)
		{
			var key = GetDestinationKey(destinationPath);
			return _queues.TryGetValue(key, out var entry) ? entry.RunningCount : 0;
		}

		/// <inheritdoc/>
		public int GetWaitingCount(string? destinationPath)
		{
			var key = GetDestinationKey(destinationPath);
			return _queues.TryGetValue(key, out var entry) ? entry.WaitingCount : 0;
		}

		internal void ReleaseSlot(string destinationKey)
		{
			if (_queues.TryGetValue(destinationKey, out var entry))
			{
				entry.Release();
			}
		}

		private sealed class DestinationQueueEntry
		{
			private readonly string _key;
			private readonly SemaphoreSlim _semaphore;
			private int _currentCapacity;
			private int _runningCount;
			private int _waitingCount;
			private readonly object _lock = new();

			public int RunningCount => Volatile.Read(ref _runningCount);
			public int WaitingCount => Volatile.Read(ref _waitingCount);

			public DestinationQueueEntry(string key, int capacity)
			{
				_key = key;
				_currentCapacity = capacity;
				_semaphore = new SemaphoreSlim(capacity, int.MaxValue);
			}

			public void UpdateCapacity(int newCapacity)
			{
				lock (_lock)
				{
					if (newCapacity > _currentCapacity)
					{
						var diff = newCapacity - _currentCapacity;
						_currentCapacity = newCapacity;
						_semaphore.Release(diff);
					}
					else if (newCapacity < _currentCapacity)
					{
						// In case capacity shrinks, new tasks will naturally queue as slots are released
						_currentCapacity = newCapacity;
					}
				}
			}

			public async Task<IOperationQueueTicket> AcquireAsync(OperationQueue parentQueue, CancellationToken cancellationToken)
			{
				Interlocked.Increment(ref _waitingCount);
				try
				{
					await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
					Interlocked.Decrement(ref _waitingCount);
					Interlocked.Increment(ref _runningCount);
					return new OperationQueueTicket(parentQueue, _key, Guid.NewGuid(), isThrottled: true);
				}
				catch
				{
					Interlocked.Decrement(ref _waitingCount);
					throw;
				}
			}

			public void Release()
			{
				Interlocked.Decrement(ref _runningCount);
				_semaphore.Release();
			}
		}

		private sealed partial class OperationQueueTicket : IOperationQueueTicket
		{
			private readonly OperationQueue _queue;
			private readonly bool _isThrottled;
			private int _isDisposed;

			public string DestinationKey { get; }
			public Guid OperationId { get; }

			public OperationQueueTicket(OperationQueue queue, string destinationKey, Guid operationId, bool isThrottled)
			{
				_queue = queue;
				DestinationKey = destinationKey;
				OperationId = operationId;
				_isThrottled = isThrottled;
			}

			public void Dispose()
			{
				if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
				{
					if (_isThrottled)
					{
						_queue.ReleaseSlot(DestinationKey);
					}
				}
			}
		}
	}
}
