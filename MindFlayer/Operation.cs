using System.Text.Json.Serialization;

namespace MindFlayer;

public class Operation
{
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
