using log4net;
using MindFlayer.saas.tools;
using OpenAI;
using OpenAI.Audio;
using System.Text.Json;

namespace MindFlayer.saas;

public static class ApiWrapper
{
    private static readonly ILog log = LogManager.GetLogger(typeof(ApiWrapper));

    // To set this, simply execute the below...
    // [Environment]::SetEnvironmentVariable('OPENAI_KEY', 'sk-here', 'Machine')
    public static readonly OpenAIClient OpenAiClient = new(OpenAIAuthentication.LoadFromEnv());

    // To set this, simply execute the below...
    // [Environment]::SetEnvironmentVariable('OPENROUTER_KEY', 'sk-here', 'Machine')
    public static readonly OpenAIClient OpenRouterClient = new(new OpenAIAuthentication(Environment.GetEnvironmentVariable("OPENROUTER_KEY"), null), new OpenAIClientSettings("openrouter.ai", "api/v1"));

    public static async Task<string> Chat(ChatContext chat)
    {
        if (chat is null)
        {
            throw new ArgumentNullException(nameof(chat));
        }

        var provider = ChatProviderFactory.CreateProvider(chat.Model);
        return await provider.Chat(chat).ConfigureAwait(false);
    }

    public static async Task ChatStream(ChatContext chat)
    {
        if (chat is null)
        {
            throw new ArgumentNullException(nameof(chat));
        }

        var provider = ChatProviderFactory.CreateProvider(chat.Model);
        await provider.ChatStream(chat).ConfigureAwait(false);
    }

    public static string Transcribe(string file)
    {
        var request = new AudioTranscriptionRequest(audioPath: file, responseFormat: AudioResponseFormat.Text);
        var result = OpenAiClient.AudioEndpoint.CreateTranscriptionTextAsync(request).Result;
        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request=AudioRequest result={JsonSerializer.Serialize(result)}");
        return result.Trim();
    }
}


public class ChatContext
{
    public IEnumerable<ChatMessage> Messages { get; set; }
    public double? Temperature { get; set; }
    public Action<string> Callback { get; set; }
    public string Model { get; set; }
    public Action<tools.ToolCall> ToolCallback { get; set; }
}
