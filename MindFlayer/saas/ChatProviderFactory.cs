namespace MindFlayer.saas;

public static class ChatProviderFactory
{
    public static ChatProvider CreateProvider(string model)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (model.StartsWith("gpt", StringComparison.OrdinalIgnoreCase)) return new OpenAIChatProvider();        
        if (model.StartsWith("claude", StringComparison.OrdinalIgnoreCase)) return new AnthropicChatProvider();
        throw new InvalidOperationException("Unexpected model.");
    }
}