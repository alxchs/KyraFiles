using System;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.Workspaces
{
    public sealed class WorkspaceTabSnapshot
    {
        [JsonPropertyName("leftPath")]
        public string LeftPath { get; set; } = string.Empty;

        [JsonPropertyName("rightPath")]
        public string? RightPath { get; set; }

        [JsonPropertyName("isDualPane")]
        public bool IsDualPane { get; set; }

        [JsonPropertyName("isPinned")]
        public bool IsPinned { get; set; }

        [JsonPropertyName("layoutMode")]
        public string LayoutMode { get; set; } = "Details";

        [JsonPropertyName("sortOption")]
        public string SortOption { get; set; } = "Name";

        public WorkspaceTabSnapshot() { }

        public WorkspaceTabSnapshot(string leftPath, string? rightPath = null, bool isDualPane = false, bool isPinned = false)
        {
            LeftPath = leftPath ?? string.Empty;
            RightPath = rightPath;
            IsDualPane = isDualPane;
            IsPinned = isPinned;
        }
    }
}
