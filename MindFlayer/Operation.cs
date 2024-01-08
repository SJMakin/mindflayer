using System.Text.Json.Serialization;

namespace MindFlayer;

public class Operation
{
    [JsonConstructor]
    public Operation(string prompt, string name, List<ChatMessage> messages)
    {
        Prompt = prompt;
        Name = name;
        Messages = messages;
    }

    [JsonPropertyName("prompt")]
    public string Prompt { get; }

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; }

    [JsonPropertyName("name")]
    public string Name { get; }
}
