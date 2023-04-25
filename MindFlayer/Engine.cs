﻿using log4net;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Completions;
using OpenAI.Edits;
using OpenAI.Models;
using System.Text.Json;
using OpenAI.Audio;

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

        public static string Chat(string input, Operation op)
        {
            var prompt = op.Messages.Select(prompt => new ChatPrompt(prompt.Role, ReplacePlaceholders(prompt.Content, input))).ToList();
            return Chat(prompt, 1);
        }

        public static string Chat(string input, IEnumerable<ChatMessage> messages)
        {
            var prompt = messages.Select(prompt => new ChatPrompt(prompt.Role, ReplacePlaceholders(prompt.Content, input))).ToList();
            return Chat(prompt, 1);
        }

        public static string Chat(IEnumerable<ChatMessage> messages, double? temp = null)
        {
            var prompt = messages.Select(prompt => new ChatPrompt(prompt.Role, prompt.Content)).ToList();
            return Chat(prompt, temp);
        }

        private static string Chat(IEnumerable<ChatPrompt> prompts, double? temp)
        {
            var request = new ChatRequest(messages: prompts, model: Model.GPT3_5_Turbo, temperature: temp );
            var result = Client.ChatEndpoint.GetCompletionAsync(request).Result;
            log.Info($"{nameof(Engine)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
            return result.FirstChoice.Message.Content.Trim();
        }

        public static async Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<ChatResponse> callback)
        {
            var prompt = messages.Select(prompt => new ChatPrompt(prompt.Role, prompt.Content)).ToList();
            await ChatStream(prompt, temp, callback).ConfigureAwait(false);
        }

        public static async Task ChatStream(IEnumerable<ChatPrompt> prompts, double? temp, Action<ChatResponse> callback)
        {
            var request = new ChatRequest(messages: prompts, model: Model.GPT3_5_Turbo, temperature: temp);
            await Client.ChatEndpoint.StreamCompletionAsync(request, callback).ConfigureAwait(false);          
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
            log.Info($"{nameof(Engine)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
            return result.Trim();
        }
    }
}