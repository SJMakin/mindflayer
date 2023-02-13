using System.Text.Json.Serialization;

namespace MindFlayer;

public class Operation
{
    [JsonConstructor]
    public Operation(string endpoint, string prompt, string name)
    {
        Endpoint = endpoint;
        Prompt = prompt;
        Name = name;
    }

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; }

    [JsonPropertyName("name")]
    public string Name { get; }
}
