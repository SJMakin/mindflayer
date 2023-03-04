using log4net;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Completions;
using OpenAI.Edits;
using OpenAI.Models;
using System.Text.Json;

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
                model: Model.Davinci,
                max_tokens: 256);
            var result = Client.CompletionsEndpoint.CreateCompletionAsync(request).Result;
            log.Info($"{nameof(Engine)}.{nameof(Completion)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
            return result.Completions[0].Text;
        }

        public static string Chat(string input, Operation op)
        {
            var prompt = op.Messages.Select(prompt => new ChatPrompt(prompt.Role, ReplacePlaceholders(prompt.Content, input))).ToList();
            return Chat(prompt);
        }

        public static string Chat(string input, IEnumerable<ChatMessage> messages)
        {
            var prompt = messages.Select(prompt => new ChatPrompt(prompt.Role, ReplacePlaceholders(prompt.Content, input))).ToList();
            return Chat(prompt);
        }

        private static string Chat(IEnumerable<ChatPrompt> prompts)
        {
            var request = new ChatRequest(messages: prompts, model: Model.GPT3_5_Turbo);
            var result = Client.ChatEndpoint.GetCompletionAsync(request).Result;
            log.Info($"{nameof(Engine)}.{nameof(Chat)} request={JsonSerializer.Serialize(request)} result={JsonSerializer.Serialize(result)}");
            return result.FirstChoice.Message.Content;
        }

        private static string ReplacePlaceholders(string template, string input)
        {
            return template
                .Replace("<{time}>", DateTime.Now.ToString("HH:SS"))
                .Replace("<{input}>", input);
        }
    }
}
