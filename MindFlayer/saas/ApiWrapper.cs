using Anthropic.SDK;
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
    // [Environment]::SetEnvironmentVariable('DEEPSEEK_KEY', 'sk-here', 'Machine')
    public static readonly OpenAIClient DeepseekClient = new(new OpenAIAuthentication(Environment.GetEnvironmentVariable("DEEPSEEK_KEY"), null), new OpenAIClientSettings("api.deepseek.com", "v1"));

    // To set this, simply execute the below...
    // [Environment]::SetEnvironmentVariable('GEMINI_KEY', 'sk-here', 'Machine')
    public static readonly OpenAIClient GeminiClient = new(new OpenAIAuthentication(Environment.GetEnvironmentVariable("GEMINI_KEY"), null), new OpenAIClientSettings("generativelanguage.googleapis.com", "v1beta/openai"));

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

    public static async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<string> callback, string model, Action<ToolCall> toolCallback)
    {
        var provider = ChatProviderFactory.CreateProvider(model);
        await provider.ChatStream(messages, temp, callback, model, toolCallback).ConfigureAwait(false);
    }

    public static string Transcribe(string file)
    {
        var request = new AudioTranscriptionRequest(audioPath: file, responseFormat: AudioResponseFormat.Text);
        var result = OpenAiClient.AudioEndpoint.CreateTranscriptionAsync(request).Result;
        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request=AudioRequest result={JsonSerializer.Serialize(result)}");
        return result.Trim();
    }
}
