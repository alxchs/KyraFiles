// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.App.Utils.Storage
{
	/// <summary>
	/// Represents an active ticket holding a concurrency slot in the operation queue.
	/// Disposing this ticket releases the slot back to the queue for that destination.
	/// </summary>
	public interface IOperationQueueTicket : IDisposable
	{
		/// <summary>
		/// Gets the normalized destination key for which this ticket was allocated.
		/// </summary>
		string DestinationKey { get; }

		/// <summary>
		/// Gets the unique identifier of this queued operation ticket.
		/// </summary>
		Guid OperationId { get; }
	}

	/// <summary>
	/// Manages queued filesystem transfer operations (Copy and Move) per destination volume/server.
	/// In accordance with D-F036-3 (Option b):
	/// - Limits concurrent copy/move operations to a configurable number (default 2, range 1-4) per destination.
	/// - Delete and Rename operations bypass the queue entirely.
	/// </summary>
	public interface IOperationQueue
	{
		/// <summary>
		/// Gets or sets the maximum concurrent copy/move operations allowed per destination volume/server.
		/// Default is 2. Supported range is 1 to 4 (or int.MaxValue when disabled).
		/// </summary>
		int MaxConcurrentOperationsPerDestination { get; set; }

		/// <summary>
		/// Gets or sets whether queue throttling is enabled. If false, operations acquire tickets immediately without queuing.
		/// </summary>
		bool IsEnabled { get; set; }

		/// <summary>
		/// Derives the canonical destination key (drive volume, UNC server, or device root) from a target folder path.
		/// </summary>
		/// <param name="destinationPath">The destination folder or file path.</param>
		/// <returns>A normalized destination key (e.g., "C:", "\\SERVER", or "DEFAULT").</returns>
		string GetDestinationKey(string? destinationPath);

		/// <summary>
		/// Asynchronously waits for an available operation slot for the given destination path.
		/// </summary>
		/// <param name="destinationPath">The target destination path.</param>
		/// <param name="cancellationToken">Cancellation token to abort waiting.</param>
		/// <returns>An <see cref="IOperationQueueTicket"/> holding the slot. Disposing the ticket releases the slot.</returns>
		Task<IOperationQueueTicket> EnqueueAsync(string? destinationPath, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the number of currently active operations for the given destination key or path.
		/// </summary>
		int GetRunningCount(string? destinationPath);

		/// <summary>
		/// Gets the number of operations currently waiting in line for the given destination key or path.
		/// </summary>
		int GetWaitingCount(string? destinationPath);
	}
}
