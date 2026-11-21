// Copyright (c) KyraFiles contributors
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace KyraFiles.Shared.CodeSearch
{
	/// <summary>
	/// Final summary result of a code search operation.
	/// </summary>
	public sealed class CodeSearchResult
	{
		public IReadOnlyList<CodeSearchResultItem> Matches { get; }

		public int TotalFilesScanned { get; }

		public int TotalFilesIgnored { get; }

		public TimeSpan Elapsed { get; }

		public bool ReachedMaxResults { get; }

		public bool WasCancelled { get; }

		public string? ErrorMessage { get; }

		public CodeSearchResult(
			IReadOnlyList<CodeSearchResultItem> matches,
			int totalFilesScanned,
			int totalFilesIgnored,
			TimeSpan elapsed,
			bool reachedMaxResults = false,
			bool wasCancelled = false,
			string? errorMessage = null)
		{
			Matches = matches ?? Array.Empty<CodeSearchResultItem>();
			TotalFilesScanned = totalFilesScanned;
			TotalFilesIgnored = totalFilesIgnored;
			Elapsed = elapsed;
			ReachedMaxResults = reachedMaxResults;
			WasCancelled = wasCancelled;
			ErrorMessage = errorMessage;
		}
	}
}
