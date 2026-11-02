// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Services.FileIntegrity
{
	public enum ChecksumAlgorithm
	{
		CRC32,
		MD5,
		SHA1,
		SHA256,
		SHA384,
		SHA512
	}

	public readonly record struct FileHashProgress(long ProcessedBytes, long TotalBytes, double Percentage);

	/// <summary>
	/// Service contract for streaming file hash calculation and candidate hash comparison.
	/// </summary>
	public interface IFileIntegrityService
	{
		/// <summary>
		/// Calculates checksum/hash for the specified file in streaming chunks without loading the entire file into memory.
		/// </summary>
		Task<string> CalculateHashAsync(
			string filePath,
			ChecksumAlgorithm algorithm,
			IProgress<FileHashProgress>? progress = null,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Compares a computed hash against an input candidate, ignoring case, prefixes, dashes, and whitespace.
		/// </summary>
		bool CompareHash(string? computedHash, string? candidateHash);
	}
}

