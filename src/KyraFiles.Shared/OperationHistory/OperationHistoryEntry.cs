// Copyright (c) KyraFiles Community. Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.OperationHistory
{
	public enum OperationHistoryKind
	{
		Copy,
		Move,
		Recycle,
		DeletePermanent,
		Rename,
		CreateNew,
		CreateLink,
		Restore,
	}

	public enum OperationHistoryResult
	{
		Success,
		Partial,
		Failed,
	}

	public sealed class OperationHistoryItemPath
	{
		public string Path { get; set; } = string.Empty;

		public bool IsDirectory { get; set; }

		public OperationHistoryItemPath()
		{
		}

		public OperationHistoryItemPath(string path, bool isDirectory)
		{
			Path = path ?? string.Empty;
			IsDirectory = isDirectory;
		}
	}

	public sealed class OperationHistoryEntry
	{
		public string Id { get; set; } = Guid.NewGuid().ToString("N");

		public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

		public OperationHistoryKind Kind { get; set; }

		public OperationHistoryResult Result { get; set; } = OperationHistoryResult.Success;

		public bool Reversible { get; set; }

		public string? IrreversibleReason { get; set; }

		public bool IsUndone { get; set; }

		public DateTimeOffset? UndoneAtUtc { get; set; }

		public int TotalItemCount { get; set; }

		public List<OperationHistoryItemPath> Sources { get; set; } = [];

		public List<OperationHistoryItemPath> Destinations { get; set; } = [];

		/// <summary>
		/// Optional live session reference (e.g., IStorageHistory) kept only in memory and never serialized.
		/// </summary>
		[JsonIgnore]
		public object? LiveHistory { get; set; }
	}

	public sealed class OperationHistoryFileModel
	{
		public int SchemaVersion { get; set; } = 1;

		public List<OperationHistoryEntry> Entries { get; set; } = [];
	}

	[JsonSourceGenerationOptions(
		WriteIndented = true,
		PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
		UseStringEnumConverter = true)]
	[JsonSerializable(typeof(OperationHistoryFileModel))]
	[JsonSerializable(typeof(OperationHistoryEntry))]
	[JsonSerializable(typeof(OperationHistoryItemPath))]
	[JsonSerializable(typeof(List<OperationHistoryEntry>))]
	[JsonSerializable(typeof(List<OperationHistoryItemPath>))]
	public sealed partial class OperationHistoryJsonContext : JsonSerializerContext
	{
	}
}
