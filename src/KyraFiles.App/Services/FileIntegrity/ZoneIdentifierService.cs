// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KyraFiles.App.Services.FileIntegrity
{
	/// <summary>
	/// Service for detecting and unblocking downloaded files by removing the Mark of the Web (Zone.Identifier) NTFS Alternate Data Stream.
	/// </summary>
	public sealed class ZoneIdentifierService : IZoneIdentifierService
	{
		public const string ZoneStreamSuffix = ":Zone.Identifier";

		/// <inheritdoc />
		public bool HasZoneIdentifier(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
				return false;

			var streamPath = filePath + ZoneStreamSuffix;
			try
			{
				using var fs = new FileStream(streamPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return fs.Length > 0;
			}
			catch
			{
				return false;
			}
		}

		/// <inheritdoc />
		public bool UnblockFile(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				return false;

			var streamPath = filePath + ZoneStreamSuffix;
			try
			{
				if (!HasZoneIdentifier(filePath))
					return false;

				bool deleted = Windows.Win32.PInvoke.DeleteFileFromApp(streamPath);
				return deleted || !HasZoneIdentifier(filePath);
			}
			catch
			{
				return false;
			}
		}

		/// <inheritdoc />
		public Task<BatchUnblockResult> UnblockFilesAsync(
			IEnumerable<string> filePaths,
			IProgress<int>? progress = null,
			CancellationToken cancellationToken = default)
		{
			return Task.Run(() =>
			{
				var unblocked = 0;
				var unchanged = 0;
				var failed = 0;
				var totalProcessed = 0;

				foreach (var file in filePaths)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
					{
						unchanged++;
						totalProcessed++;
						progress?.Report(totalProcessed);
						continue;
					}

					if (HasZoneIdentifier(file))
					{
						if (UnblockFile(file))
							unblocked++;
						else
							failed++;
					}
					else
					{
						unchanged++;
					}

					totalProcessed++;
					progress?.Report(totalProcessed);
				}

				return new BatchUnblockResult(unblocked, unchanged, failed);
			}, cancellationToken);
		}
	}
}
