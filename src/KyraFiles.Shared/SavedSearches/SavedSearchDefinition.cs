// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.SavedSearches
{
	public enum SavedSearchRelativeDateKind
	{
		None,
		Today,
		Yesterday,
		LastNDays,
		ThisWeek,
		ThisMonth,
	}

	public sealed class SavedSearchDefinition
	{
		public string Id { get; set; } = Guid.NewGuid().ToString("N");

		public string Name { get; set; } = string.Empty;

		public string Query { get; set; } = string.Empty;

		public string ScopePath { get; set; } = string.Empty;

		public bool IncludeSubfolders { get; set; } = true;

		public List<string> Extensions { get; set; } = [];

		public long? MinSizeBytes { get; set; }

		public long? MaxSizeBytes { get; set; }

		public SavedSearchRelativeDateKind RelativeDateKind { get; set; } = SavedSearchRelativeDateKind.None;

		public int RelativeDaysCount { get; set; } = 7;

		public DateTimeOffset? AbsoluteDateFrom { get; set; }

		public DateTimeOffset? AbsoluteDateTo { get; set; }

		public int Order { get; set; }
	}

	public sealed class SavedSearchesFileModel
	{
		public int SchemaVersion { get; set; } = 1;

		public List<SavedSearchDefinition> Items { get; set; } = [];
	}

	[JsonSourceGenerationOptions(
		WriteIndented = true,
		PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
		UseStringEnumConverter = true)]
	[JsonSerializable(typeof(SavedSearchesFileModel))]
	[JsonSerializable(typeof(SavedSearchDefinition))]
	[JsonSerializable(typeof(List<SavedSearchDefinition>))]
	[JsonSerializable(typeof(List<string>))]
	public sealed partial class SavedSearchesJsonContext : JsonSerializerContext
	{
	}
}
