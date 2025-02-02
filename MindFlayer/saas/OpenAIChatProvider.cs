using System.Diagnostics;
using System.Reflection.Metadata;
using log4net;
using MindFlayer.saas.tools;
using OpenAI;
using OpenAI.Chat;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MindFlayer.saas;

public class OpenAIChatProvider : ChatProvider
{
    private static readonly ILog log = LogManager.GetLogger(typeof(OpenAIChatProvider));

    private readonly OpenAIClient client;

    public OpenAIChatProvider(OpenAIClient client)
    {
        this.client = client;
    }

    public override async Task<string> Chat(ChatContext chat)
    {
        if (chat is null)
        {
            throw new ArgumentNullException(nameof(chat));
        }

        var request = CreateChatRequest(chat);
        var result = await client.ChatEndpoint.GetCompletionAsync(request).ConfigureAwait(false);
        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
        return result.FirstChoice.Message.Content.ToString().Trim();
    }

    public override async Task ChatStream(ChatContext chat)
    {
        try
        {
            var request = CreateChatRequest(chat);

            log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)}");

            if (chat.ToolCallback != null)
            {
                var result = await client.ChatEndpoint.GetCompletionAsync(request);
                log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
                chat.Callback.Invoke(result.FirstChoice.Message.Content?.ToString().Trim());
                var toolCalls = result.FirstChoice.Message.ToolCalls;
                if (toolCalls is null) return;
                foreach (var toolCall in toolCalls)
                {
                    chat.ToolCallback.Invoke(new tools.ToolCall()
                    {
                        ID = toolCall.Id,
                        Name = toolCall.Function.Name,
                        Parameters = toolCall.Function.Arguments.ToString()
                    });
                }
                return;
            }


            var fullResponse = new StringBuilder();

            await foreach (var chatResponse in client.ChatEndpoint.StreamCompletionEnumerableAsync(request))
            {
                log.Debug($"{nameof(ApiWrapper)}.{nameof(Chat)} response={JsonSerializer.Serialize(chatResponse)}");

                // Gemini returns the whole message at the end, with a delta, so need to bail if it says its done.
                if (chatResponse.FirstChoice?.FinishReason is not null)
                {
                    break;
                }

                if (chatResponse?.FirstChoice?.Delta?.Content is null) continue;
                fullResponse.Append(chatResponse.FirstChoice.Delta.Content);
                chat.Callback(chatResponse.FirstChoice.Delta.Content);                
            }

            log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} result={fullResponse}");
        }
        catch (Exception ex)
        {
            chat.Callback.Invoke($"\nError: {ex.Message}");
            log.Error($"{nameof(ApiWrapper)}.{nameof(Chat)} {ex}");
        }

    }

    private static ChatRequest CreateChatRequest(ChatContext context)
    {
        var prompt = context.Messages.SelectMany(CreateMessages).ToList();
        var tools = context.ToolCallback is null ? null : ToolMapper.OpenAiTools();

        return new ChatRequest(messages: prompt, model: context.Model, temperature: context.Temperature, tools: tools, reasoningEffort: context.Model.StartsWith("o") ? ReasoningEffort.High : null);
    }

    private static IEnumerable<Message> CreateMessages(ChatMessage msg)
    {
        var tools = ToolMapper.OpenAiTools();

        var toolCalls = msg.ToolCalls
            .Select(t =>
            {
                var tool = tools.First(oit => oit.Function.Name == t.Name);
                return new OpenAI.ToolCall(t.ID, t.Name, t.Parameters);
            }).ToList();

        Message message;
        if (msg.Role == Role.Assistant) // Deepseek doesnt work with 
        {
            message = new Message(msg.Role, msg.Content);
        }
        else
        {
            message = new Message(msg.Role, CreateContent(msg));
        }        

        if (toolCalls.Count > 0) message.ToolCalls = toolCalls;

        yield return message;

        foreach (var call in msg.ToolCalls)
        {
            if (call.Result is null) continue;

            var toolMsg = new Message(Role.Tool, call.Result, call.Name);
            toolMsg.ToolCallId = call.ID;

            yield return toolMsg;
        }
    }

    private static List<Content> CreateContent(ChatMessage message)
    {
        var result = new List<Content>();

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            result.Add(new Content(ContentType.Text, message.Content));
        }

        if (message.Images is not null)
        {
            foreach (var image in message.Images)
                result.Add(new Content(ContentType.ImageUrl, $"data:image/jpeg;base64,{image}"));
        }

        return result;        
    }
}
