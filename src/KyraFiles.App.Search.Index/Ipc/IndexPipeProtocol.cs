// Copyright (c) KyraFiles contributors. Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace KyraFiles.App.Search.Index.Ipc;

/// <summary>
/// Protocolo versionado de comunicação por Named Pipe entre o Files e o processo do motor de índice
/// (<c>KyraFiles.App.IndexService.exe</c>) nos modos Básico e Completo (F015 T10 — FR-040, FR-042).
/// </summary>
public static class IndexPipeProtocol
{
	public const int CurrentVersion = 1;
	public const string FullServicePipeName = "KyraFiles.IndexService.Full";

	public static string GetBasicPipeName(int? sessionId = null)
	{
		int sid = sessionId ?? Process.GetCurrentProcess().SessionId;
		return $"KyraFiles.IndexService.Basic.S{sid}";
	}
}

public sealed class IndexPipeRequest
{
	public int Version { get; set; } = IndexPipeProtocol.CurrentVersion;
	public string Command { get; set; } = string.Empty; // "ping", "status", "query", "cancel", "configure", "reindex", "shutdown"
	public string? RequestId { get; set; }
	public string? Query { get; set; }
	public string? ScopePath { get; set; }
	public int MaxResults { get; set; } = 100_000;
	public Guid? VolumeGuid { get; set; }
	public string[]? ExcludedPaths { get; set; }
}

public sealed class IndexPipeResponse
{
	public int Version { get; set; } = IndexPipeProtocol.CurrentVersion;
	public string Type { get; set; } = string.Empty; // "pong", "status", "batch", "completed", "configured", "error"
	public string? RequestId { get; set; }
	public string? ErrorMessage { get; set; }
	public string? Mode { get; set; } // "Basic" | "Full"
	public int ServerProcessId { get; set; }
	public VolumeStatusDto[]? Volumes { get; set; }
	public string[]? ExcludedPaths { get; set; }
	public SearchResultDto[]? Items { get; set; }
	public int TotalMatches { get; set; }
	public long ElapsedMs { get; set; }
	public bool IsTruncated { get; set; }
	public bool IsCancelled { get; set; }
	public string? FallbackReason { get; set; }
}

public sealed class VolumeStatusDto
{
	public Guid VolumeGuid { get; set; }
	public string DisplayRoot { get; set; } = string.Empty;
	public string FileSystem { get; set; } = string.Empty;
	public string Mode { get; set; } = "Basic";
	public string State { get; set; } = "Ready"; // "Indexing", "Ready", "Excluded", "BitLockerLocked", "Error"
	public int ItemCount { get; set; }
	public long LastIndexedUtcTicks { get; set; }
	public string? StatusDetail { get; set; }

	/// <summary>Estouros do watcher desde que o volume ficou pronto (T17c).</summary>
	public int OverflowCount { get; set; }

	/// <summary>Reconferência pedida ou em andamento (T17c).</summary>
	public bool ReconcilePending { get; set; }
}

public sealed class SearchResultDto
{
	public ulong FileReference { get; set; }
	public string Name { get; set; } = string.Empty;
	public string FullPath { get; set; } = string.Empty;
	public long FileSize { get; set; }
	public long CreationTimeUtcTicks { get; set; }
	public long LastWriteTimeUtcTicks { get; set; }
	public uint FileAttributes { get; set; }
	public bool IsDirectory { get; set; }
}

[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	WriteIndented = false)]
[JsonSerializable(typeof(IndexPipeRequest))]
[JsonSerializable(typeof(IndexPipeResponse))]
[JsonSerializable(typeof(VolumeStatusDto))]
[JsonSerializable(typeof(VolumeStatusDto[]))]
[JsonSerializable(typeof(SearchResultDto))]
[JsonSerializable(typeof(SearchResultDto[]))]
public partial class IndexPipeJsonContext : JsonSerializerContext
{
}
