using log4net;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System.Text.Json;
using OpenAI.Audio;
using System.Text;

namespace MindFlayer;

public static class ModelToOpenAi
{
    private static readonly ILog log = LogManager.GetLogger(typeof(ModelToOpenAi));

    // To set this, simply exevute the below...
    // [Environment]::SetEnvironmentVariable('OPENAI_KEY', 'sk-here', 'Machine')
    public static readonly OpenAIClient Client = new(OpenAIAuthentication.LoadFromEnv());

    public static async Task<string> Chat(IEnumerable<ChatMessage> messages, double? temp, Model model)
    {
        var prompt = messages.Select(prompt => new OpenAI.Chat.Message(prompt.Role, prompt.Content)).ToList();
        var request = new ChatRequest(messages: prompt, model: model, temperature: temp );
        var result = await Client.ChatEndpoint.GetCompletionAsync(request);
        log.Info($"{nameof(ModelToOpenAi)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
        return result.FirstChoice.Message.Content.ToString().Trim();
    }

    public static async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<ChatResponse> callback, Model model)
    {
        var prompt = messages.Select(prompt => new OpenAI.Chat.Message(prompt.Role, prompt.Content)).ToList();
        var request = new ChatRequest(messages: prompt, model: model, temperature: temp);
        var response = new StringBuilder();
        var callbackWrapper = new Action<ChatResponse>((c) =>
        {
            response.Append(c.FirstChoice.Delta.Content);
            callback(c);
        });
        await Client.ChatEndpoint.StreamCompletionAsync(request, callbackWrapper)
            .ContinueWith(_ => log.Info($"{nameof(ModelToOpenAi)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={response}"), TaskScheduler.Current)
            .ConfigureAwait(false);          
    }

    public static string Transcribe(string file)
    {
        var request = new AudioTranscriptionRequest(audioPath: file, responseFormat: AudioResponseFormat.Text);
        var result = Client.AudioEndpoint.CreateTranscriptionAsync(request).Result;
        log.Info($"{nameof(ModelToOpenAi)}.{nameof(Chat)} request=AudioRequest result={JsonSerializer.Serialize(result)}");
        return result.Trim();
    }
}
