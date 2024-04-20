using log4net;
using OpenAI.Chat;
using System.Text.Json;
using System.Text;
using Anthropic.SDK.Messaging;
using System.Diagnostics.CodeAnalysis;

namespace MindFlayer.saas;

public class AnthropicChatProvider : ChatProvider
{
    private static readonly ILog log = LogManager.GetLogger(typeof(AnthropicChatProvider));

    public override async Task<string> Chat(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        var request = CreateMessageParameters(messages, temp, model);
        var result = await ApiWrapper.AnthropicClient.Messages.GetClaudeMessageAsync(request).ConfigureAwait(false);
        return result.Content.Last().Text.Trim();
    }

    public override async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<string> callback, string model)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        var response = new StringBuilder();
        var request = CreateMessageParameters(messages, temp, model);
        await foreach (var chatResponse in ApiWrapper.AnthropicClient.Messages.StreamClaudeMessageAsync(request))
        {
            if (chatResponse?.Delta?.Text is null) continue;
            response.Append(chatResponse.Delta.Text);
            callback(chatResponse.Delta.Text);
        }

        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={response}");
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Required to meet api specification.")]
    private static MessageParameters CreateMessageParameters(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        var request = new MessageParameters
        {
            Model = model,
            Temperature = Convert.ToDecimal(temp.GetValueOrDefault() / 2),
            SystemMessage = messages.FirstOrDefault(m => m.Role == Role.System).Content,
            Messages = messages.SkipWhile(m => m.Role == Role.System).Select(prompt => new Anthropic.SDK.Messaging.Message() { Role = prompt.Role.ToString().ToLowerInvariant(), Content = prompt.Content }).ToList(),
            MaxTokens = 4096
        };
        return request;
    }
}
