using System.Text.Json.Serialization;

namespace Anthropic.SDK.Messaging
{
    /// <summary>
    /// Content Type Definitions
    /// </summary>
    public static class ContentType
    {
        public static string Text => "text";
        public static string Image => "image";
        public static string ToolUse => "tool_use";
        public static string ToolResult => "tool_result";
    }

    /// <summary>
    /// Helper Class for Text Content to Send to Claude
    /// </summary>
    public class TextContent
    {
        /// <summary>
        /// Type of Content (Text, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type => ContentType.Text;

        /// <summary>
        /// Text to send to Claude in a Block
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    /// <summary>
    /// Helper Class for Image Content to Send to Claude
    /// </summary>
    public class ImageContent
    {
        /// <summary>
        /// Type of Content (Image, pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type => ContentType.Image;

        /// <summary>
        /// Source of Image
        /// </summary>
        [JsonPropertyName("source")]
        public ImageSource Source { get; set; }
    }

    /// <summary>
    /// Image Format Types
    /// </summary>
    public static class ImageSourceType
    {
        /// <summary>
        /// Base 64 Image Type
        /// </summary>
        public static string Base64 => "base64";
    }

    /// <summary>
    /// Definition of image to be sent to Claude
    /// </summary>
    public class ImageSource
    {
        /// <summary>
        /// Image data format (pre-set)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type => ImageSourceType.Base64;

        /// <summary>
        /// Image format
        /// </summary>
        [JsonPropertyName("media_type")]
        public string MediaType { get; set; }

        /// <summary>
        /// Base 64 image data
        /// </summary>
        [JsonPropertyName("data")]
        public string Data { get; set; }
    }

    public class ToolUseContent
    {
        [JsonPropertyName("type")]
        public string Type => ContentType.ToolUse;

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("input")]
        public dynamic Input { get; set; }
    }

    public class ToolResultContent
    {
        [JsonPropertyName("type")]
        public string Type => ContentType.ToolResult;

        [JsonPropertyName("tool_use_id")]
        public string ToolUseId { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
