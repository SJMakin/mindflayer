using Anthropic.SDK.Messaging;
using log4net;
using OpenAI.Chat;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MindFlayer.intermutatio;
using MindFlayer.saas.tools;
using System.Reflection;

namespace MindFlayer.saas;

public class AnthropicChatProvider : ChatProvider
{
    private static readonly ILog log = LogManager.GetLogger(typeof(AnthropicChatProvider));

    public override async Task<string> Chat(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        var request = CreateMessageParameters(messages, temp, model, false);
        var result = await ApiWrapper.AnthropicClient.Messages.GetClaudeMessageAsync(request).ConfigureAwait(false);
        return result.Content.Last(c => c.Text is not null).Text.Trim();
    }

    public override async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<string> callback, string model, Action<tools.ToolCall> toolCallback)
    {
        if (callback is null) throw new ArgumentNullException(nameof(callback));

        var response = new StringBuilder();
        var request = CreateMessageParameters(messages, temp, model);

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
            }
            else if (chatResponse.Delta?.PartialJson != null && inToolCall)
            {
                currentToolJson.Append(chatResponse.Delta.PartialJson);
            }
            else if (chatResponse.Type == "content_block_stop" && inToolCall)
            {
                inToolCall = false;
                // The assembled JSON string is the parameters
                toolCall.Parameters = currentToolJson.ToString();
                toolCallback(toolCall);
            }
            else if (chatResponse?.Delta?.Text is not null)
            {
                response.Append(chatResponse.Delta.Text);
                callback(chatResponse.Delta.Text);
            }
        }

        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={response}");
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
            request.Tools = ToolMapper.MapToolsFromAssembly().ToArray();
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
            yield return new ToolUseContent() { Id = toolCall.ID, Name = toolCall.Name, Input = JsonSerializer.Deserialize<JsonDocument>(toolCall.Parameters).RootElement};
    }

    private static IEnumerable<object> CreateToolResultContent(ChatMessage message)
    {
        foreach (var toolCall in message.ToolCalls)
            yield return new ToolResultContent() { ToolUseId = toolCall.ID, Content = toolCall.Result };

        yield return new TextContent() { Text = "Tool call results:" };
    }
}
public static class ToolMapper
{
    public static List<Anthropic.SDK.Messaging.Tool> MapToolsFromAssembly()
    {
        var tools = new List<Anthropic.SDK.Messaging.Tool>();

        var methods = typeof(IOTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Concat(typeof(CodeTools).GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);

        foreach (var method in methods)
        {
            var toolAttr = method.GetCustomAttribute<ToolAttribute>();
            var parameters = method.GetParameters()
                .Select(p =>
                {
                    var paramAttr = p.GetCustomAttribute<ToolParameterAttribute>();
                    if (paramAttr == null) return null;

                    return new
                    {
                        Parameter = paramAttr,
                        IsArray = paramAttr.Type == "array",
                        ArrayItemType = p.ParameterType.IsGenericType ?
                            p.ParameterType.GetGenericArguments().FirstOrDefault() : null
                    };
                })
                .Where(p => p != null)
                .ToList();

            var functionSchema = new
            {
                name = toolAttr.Name,
                description = toolAttr.Description,

                type = "object",
                parameters = new
                {
                    type = "object",
                    properties = parameters.ToDictionary(
                        keySelector: p => p.Parameter.Name,
                        elementSelector: p =>
                        {
                            if (p.IsArray && p.ArrayItemType != null)
                            {
                                var itemProperties = p.ArrayItemType.GetProperties()
                                    .Select(prop => prop.GetCustomAttribute<ToolParameterAttribute>())
                                    .Where(attr => attr != null)
                                    .ToDictionary(
                                        attr => attr.Name,
                                        attr => new Dictionary<string, object>
                                        {
                                            { "type", attr.Type },
                                            { "description", attr.Description },
                                            { "default", attr.Default }
                                        }
                                    );

                                return new Dictionary<string, object>
                                {
                                    { "type", "array" },
                                    { "items", new Dictionary<string, object>
                                        {
                                            { "type", "object" },
                                            { "properties", itemProperties },
                                            { "required", p.ArrayItemType.GetProperties()
                                                .Select(prop => prop.GetCustomAttribute<ToolParameterAttribute>())
                                                .Where(attr => attr != null && attr.IsRequired)
                                                .Select(attr => attr.Name)
                                                .ToList()
                                            }
                                        }
                                    },
                                    { "description", p.Parameter.Description }
                                };
                            }
                            return new Dictionary<string, object>
                            {
                                { "type", p.Parameter.Type },
                                { "description", p.Parameter.Description },
                                { "default", p.Parameter.Default }
                            };
                        }
                    ),
                    required = parameters.Where(p => p.Parameter.IsRequired)
                                       .Select(p => p.Parameter.Name)
                                       .ToList()
                }
            };

            var tool = new Anthropic.SDK.Messaging.Tool()
            {
                Name = toolAttr.Name,
                Description = toolAttr.Description,
                InputSchema = functionSchema
            };

            tools.Add(tool);
        }

        return tools;
    }
}