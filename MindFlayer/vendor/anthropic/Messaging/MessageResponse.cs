﻿using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    public class Content
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("input")]
        public dynamic Input { get; set; }

        [JsonPropertyName("tool_use_id")]
        public string ToolUseId { get; set; }

        [JsonPropertyName("content")]
        public string ToolContent { get; set; }
    }

    public class MessageResponse
    {
        [JsonPropertyName("content")]
        public List<Content> Content { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public object StopSequence { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }

        [JsonPropertyName("delta")]
        public Delta Delta { get; set; }

        [JsonPropertyName("message")]
        public StreamMessage StreamStartMessage { get; set; }

        [JsonPropertyName("content_block")]
        public ContentBlock ContentBlock { get; set; }
    }

    public class ContentBlock
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class StreamMessage
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public List<object> Content { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("stop_reason")]
        public object StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public object StopSequence { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }

    public class Delta
    {
        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("partial_json")]
        public string PartialJson { get; set; }

        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}
