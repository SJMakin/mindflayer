using System.Text.Json.Serialization;

namespace MindFlayer.ui.model
{
    public class Suggestion
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
