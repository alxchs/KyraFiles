using System;
using System.Text.Json.Serialization;

namespace KyraFiles.Shared.FileDiff
{
    public sealed class DiffToolDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("argumentsTemplate")]
        public string ArgumentsTemplate { get; set; } = "\"{left}\" \"{right}\"";

        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }

        public DiffToolDefinition() { }

        public DiffToolDefinition(string id, string name, string command, string argumentsTemplate, bool isAvailable = true)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Command = command ?? string.Empty;
            ArgumentsTemplate = argumentsTemplate ?? "\"{left}\" \"{right}\"";
            IsAvailable = isAvailable;
        }
    }
}
