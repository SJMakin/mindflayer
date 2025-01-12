﻿using log4net;
using MindFlayer.saas.tools;
using OpenAI;
using OpenAI.Chat;
using System.Diagnostics;
using System.Text.Json;

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
            ToolCall toolCall = null;
            StringBuilder currentToolJson = new StringBuilder();

            await foreach (var chatResponse in client.ChatEndpoint.StreamCompletionEnumerableAsync(request))
            {
                var call = chatResponse?.FirstChoice?.Delta?.ToolCalls?.FirstOrDefault();

                if (call is not null)
                {
                    Debug.WriteLine(JsonSerializer.Serialize(call));
                    if (call.Function.Name is not null)
                    {
                        if (toolCall is not null)
                        {
                            toolCall = null;
                            return;
                        }

                        toolCall = new ToolCall() { ID = call.Id, Name = call.Function.Name, Parameters = call.Function.Arguments.ToString() };
                        toolCallback(toolCall);
                    }
                    else
                    {
                        toolCall.Parameters += call.Function.Arguments.ToString();
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

    private static IEnumerable<Message> CreateMessages(ChatMessage message)
    {
        yield return new Message(message.Role, CreateContent(message))
        {
            ToolCalls = message.ToolCalls.Any() ? message.ToolCalls.Select(tc => {
                var t = ToolMapper.OpenAiTools().First(ot => ot.Function.Name == tc.Name);
                var result = new Tool() { Id = tc.ID, Type = t.Type, Function = new Function(t.Function) };
                result.Function.Arguments = tc.Parameters;
                return result;
            }).ToList() : null
        };

        foreach (var tc in message.ToolCalls)
        {
            yield return new Message(Role.Tool, [new Content(ContentType.Text, tc.Result)], tc.Name)
            {
                ToolCallId = tc.ID
            };
        }

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
