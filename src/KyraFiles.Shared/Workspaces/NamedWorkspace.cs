using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.Workspaces
{
    public sealed class NamedWorkspace
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("tabs")]
        public List<WorkspaceTabSnapshot> Tabs { get; set; } = new();

        [JsonPropertyName("selectedTabIndex")]
        public int SelectedTabIndex { get; set; }

        [JsonPropertyName("createdAtUtc")]
        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        [JsonPropertyName("updatedAtUtc")]
        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public NamedWorkspace() { }

        public NamedWorkspace(string id, string name, List<WorkspaceTabSnapshot>? tabs = null, int selectedTabIndex = 0)
        {
            Id = id ?? Guid.NewGuid().ToString("N");
            Name = name ?? string.Empty;
            Tabs = tabs ?? new List<WorkspaceTabSnapshot>();
            SelectedTabIndex = Math.Max(0, selectedTabIndex);
            CreatedAtUtc = DateTimeOffset.UtcNow;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(List<NamedWorkspace>))]
    [JsonSerializable(typeof(NamedWorkspace))]
    [JsonSerializable(typeof(WorkspaceTabSnapshot))]
    [JsonSerializable(typeof(List<WorkspaceTabSnapshot>))]
    public sealed partial class WorkspacesJsonContext : JsonSerializerContext
    {
    }
}
