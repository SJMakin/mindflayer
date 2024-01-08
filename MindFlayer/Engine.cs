﻿using log4net;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Completions;
using OpenAI.Edits;
using OpenAI.Models;
using System.Text.Json;
using OpenAI.Audio;
using System.Text;

namespace MindFlayer
{
    public static class Engine
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Engine));

        // [Environment]::SetEnvironmentVariable('OPENAI_KEY', 'sk-here', 'Machine')
        private static readonly OpenAIClient Client = new(OpenAIAuthentication.LoadFromEnv());

        public static string Edit(string input, Operation op)
        {
            var request = new EditRequest(input, op.Prompt);
            var result = Client.EditsEndpoint.CreateEditAsync(request).Result;
            log.Info($"{nameof(Engine)}.{nameof(Edit)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
            return result.Choices[0].Text;
        }

        public static string Completion(string input, Operation op)
        {
            var request = new CompletionRequest(
                prompt: ReplacePlaceholders(op.Prompt, input),
                temperature: 0.1,
                model: Model.Davinci);
            var result = Client.CompletionsEndpoint.CreateCompletionAsync(request).Result;
            log.Info($"{nameof(Engine)}.{nameof(Completion)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
            return result.Completions[0].Text;
        }

        public static string Chat(string input, Operation op, Model model)
        {
            var prompt = op.Messages.Select(prompt => new OpenAI.Chat.Message(prompt.Role, ReplacePlaceholders(prompt.Content, input))).ToList();
            return Chat(prompt, 1, model);
        }

        public static string Chat(string input, IEnumerable<ChatMessage> messages, Model model)
        {
            var prompt = messages.Select(prompt => new OpenAI.Chat.Message(prompt.Role, ReplacePlaceholders(prompt.Content, input))).ToList();
            return Chat(prompt, 1, model);
        }

        public static string Chat(IEnumerable<ChatMessage> messages, double? temp, Model model)
        {
            var prompt = messages.Select(prompt => new OpenAI.Chat.Message(prompt.Role, prompt.Content)).ToList();
            return Chat(prompt, temp, model);
        }

        private static string Chat(IEnumerable<OpenAI.Chat.Message> prompts, double? temp, Model model)
        {
            var request = new ChatRequest(messages: prompts, model: model, temperature: temp );
            var result = Client.ChatEndpoint.GetCompletionAsync(request).Result;
            log.Info($"{nameof(Engine)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
            return result.FirstChoice.Message.Content.ToString().Trim();
        }

        public static async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<ChatResponse> callback, Model model)
        {
            var prompt = messages.Select(prompt => new OpenAI.Chat.Message(prompt.Role, prompt.Content)).ToList();
            await ChatStream(prompt, temp, callback, model).ConfigureAwait(false);
        }

        public static async Task ChatStream(IEnumerable<OpenAI.Chat.Message> messages, double? temp, Action<ChatResponse> callback, Model model)
        {
            var request = new ChatRequest(messages: messages, model: model, temperature: temp);
            var response = new StringBuilder();
            var callbackWrapper = new Action<ChatResponse>((c) =>
            {
                response.Append(c.FirstChoice.Delta.Content);
                callback(c);
            });
            await Client.ChatEndpoint.StreamCompletionAsync(request, callbackWrapper)
                .ContinueWith(_ => log.Info($"{nameof(Engine)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={response}"))
                .ConfigureAwait(false);          
        }

        private static string ReplacePlaceholders(string template, string input)
        {
            return template.Replace("<{time}>", DateTime.Now.ToString("HH:SS"))
                            .Replace("<{input}>", input);
        }

        public static string Transcribe(string file)
        {
            var request = new AudioTranscriptionRequest(audioPath: file, responseFormat: AudioResponseFormat.Text);
            var result = Client.AudioEndpoint.CreateTranscriptionAsync(request).Result;
            log.Info($"{nameof(Engine)}.{nameof(Chat)} request=AudioRequest result={JsonSerializer.Serialize(result)}");
            return result.Trim();
        }
    }
}
