namespace MindFlayer.saas;

public static class ChatProviderFactory
{
    public static ChatProvider CreateProvider(string model)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (model.StartsWith("deepseek", StringComparison.OrdinalIgnoreCase)) return new OpenAIChatProvider(ApiWrapper.DeepseekClient);
        if (model.StartsWith("gemini", StringComparison.OrdinalIgnoreCase)) return new OpenAIChatProvider(ApiWrapper.GeminiClient);
        if (model.StartsWith("gpt", StringComparison.OrdinalIgnoreCase)) return new OpenAIChatProvider(ApiWrapper.OpenAiClient);
        if (model.StartsWith("o1", StringComparison.OrdinalIgnoreCase)) return new OpenAIChatProvider(ApiWrapper.OpenAiClient);
        if (model.StartsWith("o3", StringComparison.OrdinalIgnoreCase)) return new OpenAIChatProvider(ApiWrapper.OpenAiClient);
        if (model.StartsWith("claude", StringComparison.OrdinalIgnoreCase)) return new AnthropicChatProvider();
        throw new InvalidOperationException("Unexpected model.");
    }
}