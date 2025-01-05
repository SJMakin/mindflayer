using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class MessageParameters
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }
        [JsonPropertyName("system")]
        public string? SystemMessage { get; set; }
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
        [JsonPropertyName("metadata")]
        public dynamic Metadata { get; set; }
        [JsonPropertyName("stop_sequences")]
        public string[] StopSequences { get; set; }
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }
        [JsonPropertyName("temperature")]
        public decimal? Temperature { get; set; }
        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }
        [JsonPropertyName("top_p")]
        public decimal? TopP { get; set; }
        [JsonPropertyName("tools")]
        public Tool[] Tools { get; set; }
        [JsonPropertyName("tool_choice")]
        public ToolChoice ToolChoice { get; set; }
    }

    public class Tool
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("input_schema")]
        public dynamic InputSchema { get; set; }
    }

    public class ToolChoice
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("disable_parallel_tool_use")]
        public bool? DisableParallelToolUse { get; set; }
    }
}
