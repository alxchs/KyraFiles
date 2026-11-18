// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KyraFiles.Shared.Helpers;

namespace KyraFiles.Shared.SavedSearches
{
	public sealed record EvaluatedSavedSearch(
		string EffectiveQuery,
		string ScopePath,
		bool IsScopeMissing,
		string? ScopeWarning,
		SearchIntent? Intent);

	public static class SavedSearchQueryEvaluator
	{
		public const string VirtualPathPrefix = "savedsearch:";

		public static string GetVirtualPath(string id) => VirtualPathPrefix + id;

		public static bool TryParseVirtualPath(string? path, out string id)
		{
			if (!string.IsNullOrWhiteSpace(path) &&
				path.StartsWith(VirtualPathPrefix, StringComparison.OrdinalIgnoreCase))
			{
				id = path[VirtualPathPrefix.Length..];
				return !string.IsNullOrWhiteSpace(id);
			}

			id = string.Empty;
			return false;
		}

		public static EvaluatedSavedSearch Evaluate(
			SavedSearchDefinition definition,
			DateTimeOffset referenceNow,
			Func<string, bool>? directoryExistsOverride = null)
		{
			ArgumentNullException.ThrowIfNull(definition);

			var dirExists = directoryExistsOverride ?? Directory.Exists;
			var scope = definition.ScopePath?.Trim() ?? string.Empty;
			var isGlobalScope = string.IsNullOrEmpty(scope) || string.Equals(scope, "Home", StringComparison.OrdinalIgnoreCase);

			if (!isGlobalScope && !dirExists(scope))
			{
				return new EvaluatedSavedSearch(
					EffectiveQuery: definition.Query ?? string.Empty,
					ScopePath: scope,
					IsScopeMissing: true,
					ScopeWarning: $"A pasta de escopo nao existe mais: {scope}",
					Intent: null);
			}

			var dateRange = ResolveDateRange(definition, referenceNow);
			SizeRange? sizeRange = (definition.MinSizeBytes is not null || definition.MaxSizeBytes is not null)
				? new SizeRange(definition.MinSizeBytes, definition.MaxSizeBytes)
				: null;

			var normalizedExtensions = (definition.Extensions ?? [])
				.Where(e => !string.IsNullOrWhiteSpace(e))
				.Select(e => e.StartsWith('.') ? e.Trim().ToLowerInvariant() : "." + e.Trim().ToLowerInvariant())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			var rawQuery = definition.Query?.Trim() ?? string.Empty;
			var hasExplicitFilters = dateRange is not null || sizeRange is not null || normalizedExtensions.Count > 0;

			SearchIntent? intent = null;
			string effectiveQuery = rawQuery;

			if (hasExplicitFilters)
			{
				intent = new SearchIntent
				{
					SearchText = rawQuery,
					FileExtensions = normalizedExtensions,
					DateModifiedRange = dateRange,
					SizeRange = sizeRange,
				};

				// NativeFilesSearchProvider retorna vazio se Query estiver em branco; usar "*" quando ha filtros estruturados.
				if (string.IsNullOrWhiteSpace(effectiveQuery))
					effectiveQuery = "*";
			}
			else if (!string.IsNullOrWhiteSpace(rawQuery))
			{
				var parsed = NaturalSearchParser.Parse(rawQuery, referenceNow);
				if (parsed.HasStructuredFilters)
				{
					intent = parsed;
					effectiveQuery = string.IsNullOrWhiteSpace(parsed.SearchText) ? "*" : parsed.SearchText;
				}
			}

			return new EvaluatedSavedSearch(
				EffectiveQuery: effectiveQuery,
				ScopePath: isGlobalScope ? "Home" : scope,
				IsScopeMissing: false,
				ScopeWarning: null,
				Intent: intent);
		}

		private static DateRange? ResolveDateRange(SavedSearchDefinition definition, DateTimeOffset now)
		{
			var startOfToday = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);

			return definition.RelativeDateKind switch
			{
				SavedSearchRelativeDateKind.Today => new DateRange(startOfToday, now),
				SavedSearchRelativeDateKind.Yesterday => new DateRange(startOfToday.AddDays(-1), startOfToday.AddTicks(-1)),
				SavedSearchRelativeDateKind.LastNDays => new DateRange(startOfToday.AddDays(-Math.Max(1, definition.RelativeDaysCount)), now),
				SavedSearchRelativeDateKind.ThisWeek => new DateRange(startOfToday.AddDays(-(int)startOfToday.DayOfWeek), now),
				SavedSearchRelativeDateKind.ThisMonth => new DateRange(new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset), now),
				SavedSearchRelativeDateKind.None when definition.AbsoluteDateFrom is not null || definition.AbsoluteDateTo is not null
					=> new DateRange(definition.AbsoluteDateFrom, definition.AbsoluteDateTo),
				_ => null,
			};
		}
	}
}
