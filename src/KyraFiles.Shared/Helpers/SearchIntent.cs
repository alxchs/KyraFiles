// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Files.Shared.Helpers
{
	/// <summary>
	/// Represents a date/time range filter with inclusive boundaries.
	/// </summary>
	public sealed record DateRange(DateTimeOffset? From, DateTimeOffset? To)
	{
		/// <summary>
		/// Gets a value indicating whether either boundary is specified.
		/// </summary>
		public bool HasValue => From.HasValue || To.HasValue;

		/// <summary>
		/// Checks whether a given timestamp falls within the range.
		/// </summary>
		public bool IsInRange(DateTimeOffset date)
		{
			if (From.HasValue && date < From.Value)
				return false;
			if (To.HasValue && date > To.Value)
				return false;
			return true;
		}
	}

	/// <summary>
	/// Represents a file size range filter in bytes with inclusive boundaries.
	/// </summary>
	public sealed record SizeRange(long? MinBytes, long? MaxBytes)
	{
		/// <summary>
		/// Gets a value indicating whether either boundary is specified.
		/// </summary>
		public bool HasValue => MinBytes.HasValue || MaxBytes.HasValue;

		/// <summary>
		/// Checks whether a given size in bytes falls within the range.
		/// </summary>
		public bool IsInRange(long size)
		{
			if (MinBytes.HasValue && size < MinBytes.Value)
				return false;
			if (MaxBytes.HasValue && size > MaxBytes.Value)
				return false;
			return true;
		}
	}

	/// <summary>
	/// Represents the structured search intent parsed from a natural language query (FR-M5).
	/// </summary>
	public sealed class SearchIntent
	{
		public static readonly SearchIntent Empty = new();

		/// <summary>
		/// Remaining text intended for file name or content matching.
		/// </summary>
		public string? SearchText { get; init; }

		/// <summary>
		/// List of recognized file extensions with leading dot in lowercase (e.g. [".ini"]).
		/// </summary>
		public IReadOnlyList<string> FileExtensions { get; init; } = Array.Empty<string>();

		/// <summary>
		/// Modification date range filter.
		/// </summary>
		public DateRange? DateModifiedRange { get; init; }

		/// <summary>
		/// Creation date range filter.
		/// </summary>
		public DateRange? DateCreatedRange { get; init; }

		/// <summary>
		/// File size range filter in bytes.
		/// </summary>
		public SizeRange? SizeRange { get; init; }

		/// <summary>
		/// Unrecognized tokens or parts from the input query.
		/// </summary>
		public IReadOnlyList<string> UnrecognizedTokens { get; init; } = Array.Empty<string>();

		/// <summary>
		/// Indicates whether any structured filters (extensions, date ranges, or size range) were identified.
		/// </summary>
		public bool HasStructuredFilters =>
			FileExtensions.Count > 0 ||
			(DateModifiedRange is not null && DateModifiedRange.HasValue) ||
			(DateCreatedRange is not null && DateCreatedRange.HasValue) ||
			(SizeRange is not null && SizeRange.HasValue);

		/// <summary>
		/// Indicates whether remaining search text is present.
		/// </summary>
		public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);

		/// <summary>
		/// Indicates whether the query was fully understood with structured filters and no unrecognized parts.
		/// </summary>
		public bool IsFullyUnderstood => HasStructuredFilters && UnrecognizedTokens.Count == 0;
	}
}
