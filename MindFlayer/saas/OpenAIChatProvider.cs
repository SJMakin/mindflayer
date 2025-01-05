using log4net;
using OpenAI.Chat;
using System.Text.Json;

namespace MindFlayer.saas;

public class OpenAIChatProvider : ChatProvider
{
    private static readonly ILog log = LogManager.GetLogger(typeof(OpenAIChatProvider));

    public override async Task<string> Chat(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        var request = CreateChatRequest(messages, temp, model);
        var result = await ApiWrapper.OpenAiClient.ChatEndpoint.GetCompletionAsync(request).ConfigureAwait(false);
        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
        return result.FirstChoice.Message.Content.ToString().Trim();
    }

    public override async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<string> callback, string model)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        var request = CreateChatRequest(messages, temp, model);
        var fullResponse = new StringBuilder();

        await foreach (var chatResponse in ApiWrapper.OpenAiClient.ChatEndpoint.StreamCompletionEnumerableAsync(request))
        {
            if (chatResponse?.FirstChoice?.Delta?.Content is null) continue;
            fullResponse.Append(chatResponse.FirstChoice.Delta.Content);
            callback(chatResponse.FirstChoice.Delta.Content);
        }

        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={fullResponse}");
    }

    private static ChatRequest CreateChatRequest(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        var prompt = messages.Select(prompt => new Message(prompt.Role, CreateContent(prompt))).ToList();
        if (model.StartsWith("o1", StringComparison.OrdinalIgnoreCase)) prompt = prompt.Where(m => m.Role != Role.System).ToList();
        return new ChatRequest(messages: prompt, model: model, temperature: temp);
    }

    private static IEnumerable<Content> CreateContent(ChatMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.Content))
            yield return new Content(ContentType.Text, message.Content);

        if (message.Images is null) yield break;

        foreach (var image in message.Images)
            yield return new Content(ContentType.ImageUrl, $"data:image/jpeg;base64,{image}");
    }
}
