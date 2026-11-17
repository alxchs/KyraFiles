// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.App.Services.FileIntegrity
{
	public readonly record struct BatchUnblockResult(int UnblockedCount, int UnchangedCount, int FailedCount);

	/// <summary>
	/// Service contract for detecting and stripping Windows Mark of the Web (Zone.Identifier) ADS streams.
	/// </summary>
	public interface IZoneIdentifierService
	{
		/// <summary>
		/// Checks whether the specified file possesses an alternate data stream of Zone.Identifier.
		/// </summary>
		bool HasZoneIdentifier(string filePath);

		/// <summary>
		/// Removes the Zone.Identifier stream from a single file.
		/// </summary>
		bool UnblockFile(string filePath);

		/// <summary>
		/// Removes Zone.Identifier streams across a batch of files asynchronously.
		/// </summary>
		Task<BatchUnblockResult> UnblockFilesAsync(
			IEnumerable<string> filePaths,
			IProgress<int>? progress = null,
			CancellationToken cancellationToken = default);
	}
}

