using System.Diagnostics;

namespace MindFlayer
{
    // https://github.com/openai/openai-cookbook/blob/main/examples/How_to_count_tokens_with_tiktoken.ipynb
    internal class MessageTokenCalculator
    {
        readonly int tokensPerMessage;
        readonly int tokensPerName;
        readonly TiktokenSharp.TikToken encoding;

        public MessageTokenCalculator(string model = "gpt-3.5-turbo")
        {
            try
            {
                encoding = TiktokenSharp.TikToken.EncodingForModel(model);
            }
            catch (KeyNotFoundException)
            {
                Debug.WriteLine("Warning: model not found. Using cl100k_base encoding.");
                encoding = TiktokenSharp.TikToken.GetEncoding("cl100k_base");
            }

            if (model == "gpt-3.5-turbo")
            {
                Debug.WriteLine("Warning: gpt-3.5-turbo may change over time. Returning num tokens assuming gpt-3.5-turbo-0301.");
                model = "gpt-3.5-turbo-0301";
            }
            else if (model == "gpt-4")
            {
                Debug.WriteLine("Warning: gpt-4 may change over time. Returning num tokens assuming gpt-4-0314.");
                model = "gpt-4-0314";
            }

            if (model == "gpt-3.5-turbo-0301")
            {
                tokensPerMessage = 4; // every message follows <|start|>{role/name}\n{content}<|end|>\n
                tokensPerName = -1; // if there's a name, the role is omitted
            }
            else if (model == "gpt-4-0314")
            {
                tokensPerMessage = 3;
                tokensPerName = 1;
            }
            else
            {
                throw new NotImplementedException($"NumTokensFromMessages() is not implemented for model {model}. See https://github.com/openai/openai-python/blob/main/chatml.md for information on how messages are converted to tokens.");
            }
        }

        public int NumTokensFromConversation(IEnumerable<ChatMessage> messages)
        {
            int numTokens = 0;

            foreach (var message in messages)
            {
                NumTokensFromMessage(message.Content);
            }

            numTokens += 3; // every reply is primed with <|start|>assistant<|message|>

            return numTokens;
        }


        public int NumTokensFromMessage(string message)
        {
            int numTokens = 0;
            numTokens += tokensPerMessage;
            numTokens += encoding.Encode(message).Count;
            numTokens += tokensPerName;
            return numTokens;
        }
    }
}
