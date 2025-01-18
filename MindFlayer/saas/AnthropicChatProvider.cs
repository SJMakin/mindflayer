using Anthropic.SDK.Messaging;
using log4net;
using OpenAI.Chat;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MindFlayer.intermutatio;
using MindFlayer.saas.tools;

namespace MindFlayer.saas;

public class AnthropicChatProvider : ChatProvider
{
    private static readonly ILog log = LogManager.GetLogger(typeof(AnthropicChatProvider));

    public override async Task<string> Chat(ChatContext chat)
    {
        var request = CreateMessageParameters(chat.Messages, chat.Temperature, chat.Model, false);
        var result = await ApiWrapper.AnthropicClient.Messages.GetClaudeMessageAsync(request).ConfigureAwait(false);
        return result.Content.Last(c => c.Text is not null).Text.Trim();
    }

    public override async Task ChatStream(ChatContext chat)
    {
        try
        {
            var response = new StringBuilder();
            var request = CreateMessageParameters(chat.Messages, chat.Temperature, chat.Model);

            var toolCall = new ToolCall();
            StringBuilder currentToolJson = new StringBuilder();
            bool inToolCall = false;

            await foreach (var chatResponse in ApiWrapper.AnthropicClient.Messages.StreamClaudeMessageAsync(request))
            {
                if (chatResponse.Type == "content_block_start" && chatResponse.ContentBlock?.Type == "tool_use")
                {
                    inToolCall = true;
                    currentToolJson.Clear();
                    // Capture the tool ID and name from the initial block
                    toolCall = new ToolCall
                    {
                        ID = chatResponse.ContentBlock.Id,
                        Name = chatResponse.ContentBlock.Name
                    };

                    chat.ToolCallback(toolCall);
                }
                else if (chatResponse.Delta?.PartialJson != null && inToolCall)
                {
                    currentToolJson.Append(chatResponse.Delta.PartialJson);
                    toolCall.Parameters = currentToolJson.ToString();
                }
                else if (chatResponse.Type == "content_block_stop" && inToolCall)
                {
                    inToolCall = false;
                    toolCall.IsLoaded = true;
                }
                else if (chatResponse?.Delta?.Text is not null)
                {
                    response.Append(chatResponse.Delta.Text);
                    chat.Callback(chatResponse.Delta.Text);
                }
            }

            log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={response}");
        }
        catch (Exception ex)
        {
            chat.Callback.Invoke($"\nError: {ex.Message}");
            log.Error($"{nameof(ApiWrapper)}.{nameof(Chat)} {ex}");
        }
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Required to meet api specification.")]
    private static MessageParameters CreateMessageParameters(IEnumerable<ChatMessage> messages, double? temp, string model, bool withTools = true)
    {
        var request = new MessageParameters
        {
            Model = model,
            Temperature = Convert.ToDecimal(temp.GetValueOrDefault() / 2),
            SystemMessage = messages.FirstOrDefault(m => m.Role == Role.System).Content,
            Messages = messages.SkipWhile(m => m.Role == Role.System).SelectMany(CreateMessages).ToList(),
            MaxTokens = 8192,
        };

        if (withTools)
        {
            request.ToolChoice = new ToolChoice() { Type = "auto" };
            request.Tools = ToolMapper.AnthropicTools().ToArray();
        }

        return request;
    }

    private static IEnumerable<Anthropic.SDK.Messaging.Message> CreateMessages(ChatMessage message)
    {
        yield return new Anthropic.SDK.Messaging.Message()
        {
            Role = message.Role.ToString().ToLowerInvariant(),
            Content = CreateContent(message).ToArray()
        };

        if (message.ToolCalls.Any(tc => tc.Result is not null))
        {
            yield return new Anthropic.SDK.Messaging.Message()
            {
                Role = Role.User.ToString().ToLowerInvariant(),
                Content = CreateToolResultContent(message).ToArray()
            };
        }
    }

    private static IEnumerable<object> CreateContent(ChatMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.Content))
            yield return new TextContent() { Text = message.Content };

        if (message.Images is null) yield break;

        foreach (var image in message.Images)
            yield return new ImageContent() { Source = new ImageSource() { Data = image, MediaType = MediaTypes.FromBase64(image) } };

        foreach (var toolCall in message.ToolCalls)
            yield return new ToolUseContent() { Id = toolCall.ID, Name = toolCall.Name, Input = JsonSerializer.Deserialize<JsonDocument>(toolCall.Parameters).RootElement };
    }

    private static IEnumerable<object> CreateToolResultContent(ChatMessage message)
    {
        foreach (var toolCall in message.ToolCalls)
            yield return new ToolResultContent() { ToolUseId = toolCall.ID, Content = toolCall.Result };

        yield return new TextContent() { Text = "Tool call results:" };
    }
}
