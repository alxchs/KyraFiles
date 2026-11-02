// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Services.FileIntegrity
{
	/// <summary>
	/// Default implementation of file integrity verification using streaming buffers (128 KB) to guarantee minimal memory footprint.
	/// </summary>
	public sealed class FileIntegrityService : IFileIntegrityService
	{
		public const int DefaultBufferSize = 128 * 1024; // 128 KB buffer

		/// <inheritdoc />
		public async Task<string> CalculateHashAsync(
			string filePath,
			ChecksumAlgorithm algorithm,
			IProgress<FileHashProgress>? progress = null,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentException("File path must not be null or empty.", nameof(filePath));

			if (!File.Exists(filePath))
				throw new FileNotFoundException("Target file not found.", filePath);

			await using var stream = new FileStream(
				filePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				DefaultBufferSize,
				FileOptions.SequentialScan | FileOptions.Asynchronous);

			var totalBytes = stream.Length;
			long processedBytes = 0;
			var buffer = new byte[DefaultBufferSize];

			if (algorithm == ChecksumAlgorithm.CRC32)
			{
				var crc = new Crc32();
				int read;
				while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
				{
					cancellationToken.ThrowIfCancellationRequested();
					crc.Append(buffer.AsSpan(0, read));
					processedBytes += read;

					var pct = totalBytes > 0 ? (double)processedBytes / totalBytes * 100 : 100;
					progress?.Report(new FileHashProgress(processedBytes, totalBytes, pct));
				}

				var hashBytes = crc.GetCurrentHash();
				Array.Reverse(hashBytes);
				return Convert.ToHexStringLower(hashBytes);
			}

			var hashAlgorithmName = algorithm switch
			{
				ChecksumAlgorithm.MD5 => HashAlgorithmName.MD5,
				ChecksumAlgorithm.SHA1 => HashAlgorithmName.SHA1,
				ChecksumAlgorithm.SHA256 => HashAlgorithmName.SHA256,
				ChecksumAlgorithm.SHA384 => HashAlgorithmName.SHA384,
				ChecksumAlgorithm.SHA512 => HashAlgorithmName.SHA512,
				_ => throw new NotSupportedException($"Algorithm {algorithm} is not supported.")
			};

			using var incHash = IncrementalHash.CreateHash(hashAlgorithmName);
			int bytesRead;
			while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
				incHash.AppendData(buffer, 0, bytesRead);
				processedBytes += bytesRead;

				var pct = totalBytes > 0 ? (double)processedBytes / totalBytes * 100 : 100;
				progress?.Report(new FileHashProgress(processedBytes, totalBytes, pct));
			}

			var resultBytes = incHash.GetHashAndReset();
			return Convert.ToHexStringLower(resultBytes);
		}

		/// <inheritdoc />
		public bool CompareHash(string? computedHash, string? candidateHash)
		{
			if (string.IsNullOrWhiteSpace(computedHash) || string.IsNullOrWhiteSpace(candidateHash))
				return false;

			var cleanComputed = NormalizeHashString(computedHash);
			var cleanCandidate = NormalizeHashString(candidateHash);

			return string.Equals(cleanComputed, cleanCandidate, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Normalizes a hash string by stripping common prefixes (e.g. SHA256:, 0x), spaces, and separators.
		/// </summary>
		public static string NormalizeHashString(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return string.Empty;

			var text = input.Trim();

			var colonIdx = text.IndexOf(':');
			if (colonIdx > 0 && colonIdx < 10)
			{
				text = text.Substring(colonIdx + 1).Trim();
			}

			if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(2).Trim();
			}

			return text.Replace(" ", string.Empty).Replace("-", string.Empty);
		}
	}
}

