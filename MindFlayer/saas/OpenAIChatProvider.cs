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

    public override async Task<string> Chat(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        var request = CreateChatRequest(messages, temp, model);
        var result = await client.ChatEndpoint.GetCompletionAsync(request).ConfigureAwait(false);
        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
        return result.FirstChoice.Message.Content.ToString().Trim();
    }

    public override async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<string> callback, string model, Action<tools.ToolCall> toolCallback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));
        if (toolCallback is null) throw new ArgumentNullException(nameof(toolCallback));

        try
        {
            var request = CreateChatRequest(messages, temp, model);

            log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)}");

            var fullResponse = new StringBuilder();
            var toolCall = new ToolCall();

            await foreach (var chatResponse in ApiWrapper.OpenAiClient.ChatEndpoint.StreamCompletionEnumerableAsync(request))
            {
                Debug.WriteLine(JsonSerializer.Serialize(chatResponse));

                var call = chatResponse?.FirstChoice?.Delta?.ToolCalls?.FirstOrDefault();

                if (call is not null)
                {        
                    Debug.WriteLine($"Call part: id:{call.Id} name:{call.Function.Name} args:{call.Function.Arguments}");
                    if (call.Function.Name is not null && call.Id != toolCall.ID)
                    {
                        Debug.WriteLine($"Created new tool call.");
                        toolCall = new ToolCall() { ID = call.Id, Name = call.Function.Name, Parameters = call.Function.Arguments.ToString() };
                        toolCallback(toolCall);
                    }
                    else if (!string.IsNullOrWhiteSpace(call.Function.Arguments.ToString()))
                    {
                        toolCall.Parameters += call.Function.Arguments.ToString();
                        Debug.WriteLine($"Call args:{toolCall.Parameters}");
                    }
                    else
                    {
                        Debug.WriteLine($"Tool call complete.");
                        toolCall.IsLoaded = true;
                        toolCall = new ToolCall();
                    }
                }

                if (chatResponse?.FirstChoice?.Delta?.Content is null) continue;
                fullResponse.Append(chatResponse.FirstChoice.Delta.Content);
                callback(chatResponse.FirstChoice.Delta.Content);
            }

            log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} result={fullResponse}");
        }
        catch (Exception ex)
        {
            callback.Invoke($"\nError: {ex.Message}");
            log.Error($"{nameof(ApiWrapper)}.{nameof(Chat)} {ex}");
        }

    }

    private static ChatRequest CreateChatRequest(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        var prompt = messages.SelectMany(CreateMessages).ToList();
        var tools = ToolMapper.OpenAiTools();

        // Handicap o1 for the time being.
        if (model.StartsWith("o1", StringComparison.OrdinalIgnoreCase))
        {
            prompt = prompt.Where(m => m.Role != Role.System).ToList();
            tools = null;
        } 

        return new ChatRequest(messages: prompt, model: model, temperature: temp, tools: tools);
    }

    private static IEnumerable<Message> CreateMessages(ChatMessage msg)
    {
        var tools = ToolMapper.OpenAiTools();

        var toolCalls = msg.ToolCalls
            .Select(t =>
            {
                var tool = tools.First(oit => oit.Function.Name == t.Name);
                return new Tool(new Function(
                    tool.Function.Name, 
                    tool.Function.Description, 
                    tool.Function.Parameters,
                    t.Parameters)) { Id = t.ID };
            })
            .ToList();

        var message = new Message(
            msg.Role,
            CreateContent(msg));

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

    private static IEnumerable<Content> CreateContent(ChatMessage message)
    {
        yield return new Content(ContentType.Text, message.Content);

        if (message.Images is null) yield break;

        foreach (var image in message.Images)
            yield return new Content(ContentType.ImageUrl, $"data:image/jpeg;base64,{image}");
    }
}
