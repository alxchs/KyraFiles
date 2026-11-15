// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KyraFiles.Shared.Helpers
{
	/// <summary>
	/// Represents criteria for post-filtering search results (FR-M5).
	/// </summary>
	public sealed record NaturalSearchFilterCriteria
	{
		public IReadOnlyList<string>? FileExtensions { get; init; }
		public DateRange? DateModifiedRange { get; init; }
		public DateRange? DateCreatedRange { get; init; }
		public SizeRange? SizeRange { get; init; }

		public bool HasActiveFilters =>
			(FileExtensions is not null && FileExtensions.Count > 0) ||
			(DateModifiedRange is not null && DateModifiedRange.HasValue) ||
			(DateCreatedRange is not null && DateCreatedRange.HasValue) ||
			(SizeRange is not null && SizeRange.HasValue);

		public static NaturalSearchFilterCriteria FromIntent(SearchIntent? intent)
		{
			if (intent is null)
				return new NaturalSearchFilterCriteria();

			return new NaturalSearchFilterCriteria
			{
				FileExtensions = intent.FileExtensions.Count > 0 ? intent.FileExtensions : null,
				DateModifiedRange = intent.DateModifiedRange,
				DateCreatedRange = intent.DateCreatedRange,
				SizeRange = intent.SizeRange
			};
		}
	}

	/// <summary>
	/// Pure post-filter helper to filter search results by extension, dates, and size (FR-M5, M5-AC-1, M5-AC-2).
	/// </summary>
	public static class NaturalSearchPostFilter
	{
		/// <summary>
		/// Determines whether a single item matches the specified criteria.
		/// </summary>
		public static bool Matches(
			NaturalSearchFilterCriteria? criteria,
			string? itemNameOrPath,
			long? sizeBytes,
			DateTimeOffset? dateModified,
			DateTimeOffset? dateCreated)
		{
			if (criteria is null || !criteria.HasActiveFilters)
				return true;

			// 1. Extension filter
			if (criteria.FileExtensions is not null && criteria.FileExtensions.Count > 0)
			{
				if (string.IsNullOrEmpty(itemNameOrPath))
					return false;

				string ext = Path.GetExtension(itemNameOrPath);
				if (string.IsNullOrEmpty(ext))
					return false;

				bool matchesExt = false;
				foreach (var requiredExt in criteria.FileExtensions)
				{
					string norm = requiredExt.StartsWith('.') ? requiredExt : "." + requiredExt;
					if (string.Equals(ext, norm, StringComparison.OrdinalIgnoreCase))
					{
						matchesExt = true;
						break;
					}
				}

				if (!matchesExt)
					return false;
			}

			// 2. Size filter
			if (criteria.SizeRange is not null && criteria.SizeRange.HasValue)
			{
				if (!sizeBytes.HasValue || !criteria.SizeRange.IsInRange(sizeBytes.Value))
					return false;
			}

			// 3. Date modified filter
			if (criteria.DateModifiedRange is not null && criteria.DateModifiedRange.HasValue)
			{
				if (!dateModified.HasValue || !criteria.DateModifiedRange.IsInRange(dateModified.Value))
					return false;
			}

			// 4. Date created filter
			if (criteria.DateCreatedRange is not null && criteria.DateCreatedRange.HasValue)
			{
				if (!dateCreated.HasValue || !criteria.DateCreatedRange.IsInRange(dateCreated.Value))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Filters a collection of items according to the given criteria.
		/// </summary>
		public static List<T> FilterItems<T>(
			IEnumerable<T>? items,
			NaturalSearchFilterCriteria? criteria,
			Func<T, string?> nameSelector,
			Func<T, long?> sizeSelector,
			Func<T, DateTimeOffset?> modifiedSelector,
			Func<T, DateTimeOffset?> createdSelector)
		{
			if (items is null)
				return new List<T>();

			if (criteria is null || !criteria.HasActiveFilters)
				return items.ToList();

			var result = new List<T>();
			foreach (var item in items)
			{
				if (Matches(
					criteria,
					nameSelector(item),
					sizeSelector(item),
					modifiedSelector(item),
					createdSelector(item)))
				{
					result.Add(item);
				}
			}

			return result;
		}
	}
}
