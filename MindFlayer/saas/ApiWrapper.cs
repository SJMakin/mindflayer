using Anthropic.SDK;
using log4net;
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

    // [Environment]::SetEnvironmentVariable('ANTHROPIC_API_KEY', 'sk-here', 'Machine')
    public static readonly AnthropicClient AnthropicClient = new();

    public static async Task<string> Chat(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        var provider = ChatProviderFactory.CreateProvider(model);
        return await provider.Chat(messages, temp, model).ConfigureAwait(false);
    }

    public static async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<string> callback, string model)
    {
        var provider = ChatProviderFactory.CreateProvider(model);
        await provider.ChatStream(messages, temp, callback, model).ConfigureAwait(false);
    }

    public static string Transcribe(string file)
    {
        var request = new AudioTranscriptionRequest(audioPath: file, responseFormat: AudioResponseFormat.Text);
        var result = OpenAiClient.AudioEndpoint.CreateTranscriptionAsync(request).Result;
        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request=AudioRequest result={JsonSerializer.Serialize(result)}");
        return result.Trim();
    }
}
