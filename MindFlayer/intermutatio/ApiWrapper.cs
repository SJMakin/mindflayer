using log4net;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System.Text.Json;
using OpenAI.Audio;
using System.Text;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace MindFlayer;

public static class ApiWrapper
{
    private static readonly ILog log = LogManager.GetLogger(typeof(ApiWrapper));

    // To set this, simply exevute the below...
    // [Environment]::SetEnvironmentVariable('OPENAI_KEY', 'sk-here', 'Machine')
    public static readonly OpenAIClient OpenAiClient = new(OpenAIAuthentication.LoadFromEnv());

    // [Environment]::SetEnvironmentVariable('ANTHROPIC_API_KEY', 'sk-here', 'Machine')
    public static readonly AnthropicClient AnthropicClient = new();

    public static async Task<string> Chat(IEnumerable<ChatMessage> messages, double? temp, string model)
    {
        if (model.StartsWith("gpt", StringComparison.OrdinalIgnoreCase))
        {
            var prompt = messages.Select(prompt => new OpenAI.Chat.Message(prompt.Role, prompt.Content)).ToList();
            var request = new ChatRequest(messages: prompt, model: model, temperature: temp);
            var result = await OpenAiClient.ChatEndpoint.GetCompletionAsync(request);
            log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
            return result.FirstChoice.Message.Content.ToString().Trim();
        }
        else
        {
            var request = new MessageParameters();
            request.Model = model;
            request.Temperature = Convert.ToDecimal(temp.GetValueOrDefault() / 2);
            request.Messages = messages.Select(prompt => new Anthropic.SDK.Messaging.Message() { Role = prompt.Role.ToString(), Content = prompt.Content }).ToList();
            var result = await AnthropicClient.Messages.GetClaudeMessageAsync(request);
            return result.Content.Last().Text.Trim();
        }

    }

    public static async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<string> callback, string model)
    {
        if (model.StartsWith("gpt", StringComparison.OrdinalIgnoreCase))
        {
            var prompt = messages.Select(prompt => new OpenAI.Chat.Message(prompt.Role, prompt.Content)).ToList();
            var request = new ChatRequest(messages: prompt, model: model, temperature: temp);
            var response = new StringBuilder();

            await foreach (var chatResponse in OpenAiClient.ChatEndpoint.StreamCompletionEnumerableAsync(request))
            {
                response.Append(chatResponse.FirstChoice.Delta.Content);
                callback(chatResponse.FirstChoice.Delta.Content);
            }

            log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={response}");
        }
        else
        {
            var response = new StringBuilder();
            var request = new MessageParameters();
            request.Model = model;
            request.Temperature = Convert.ToDecimal(temp.GetValueOrDefault() / 2);
            request.Messages = messages.Select(prompt => new Anthropic.SDK.Messaging.Message() { Role = prompt.Role.ToString(), Content = prompt.Content }).ToList();
       
            await foreach (var chatResponse in AnthropicClient.Messages.StreamClaudeMessageAsync(request))
            {
                response.Append(chatResponse.Delta.Text);
                callback(chatResponse.Delta.Text);
            }

            log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={response}");
        }
    }

    public static string Transcribe(string file)
    {
        var request = new AudioTranscriptionRequest(audioPath: file, responseFormat: AudioResponseFormat.Text);
        var result = OpenAiClient.AudioEndpoint.CreateTranscriptionAsync(request).Result;
        log.Info($"{nameof(ApiWrapper)}.{nameof(Chat)} request=AudioRequest result={JsonSerializer.Serialize(result)}");
        return result.Trim();
    }
}
