using System.Text.Json.Serialization;
using OpenAI.Chat;
using Message = OpenAI.Chat.Message;

namespace MindFlayer;

public class Operation
{
    [JsonConstructor]
    public Operation(string endpoint, string prompt, string name, List<Message> messages)
    {
        Endpoint = endpoint;
        Prompt = prompt;
        Name = name;
        Messages = messages;
    }

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; }

    [JsonPropertyName("prompt")]
    public string Prompt { get; }

    [JsonPropertyName("messages")]
    public List<ChatPrompt> Messages { get; }

    [JsonPropertyName("name")]
    public string Name { get; }
}
