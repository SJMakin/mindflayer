namespace MindFlayer.saas;

public static class ChatProviderFactory
{
    public static ChatProvider CreateProvider(string model)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        
        // Check if it's an OpenRouter model first
        if (OpenRouterModelRepository.IsOpenRouterModel(model))
            return new OpenAIChatProvider(ApiWrapper.OpenRouterClient);
        
        // For any other models, assume they're available through OpenRouter
        // This provides a fallback for models that might not be in the cached list
        return new OpenAIChatProvider(ApiWrapper.OpenAiClient);
    }
}