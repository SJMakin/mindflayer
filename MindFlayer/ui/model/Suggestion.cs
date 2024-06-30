using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace MindFlayer.ui.model;

public class Suggestion
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [JsonPropertyName("text")]
    public Func<string, (string literal, List<ChatMessage> question)> Query { get; set; }

    public static Suggestion Summarise => new()
    {
        Summary = "Summarise",
        Query = (convo) => QuestionFactory("You are a helpful assistant, who is an expert at summerisation.",
                              $"Please summerise this conversation down to just the facts:\n{convo}")
    };

    public static Suggestion Reply => new()
    {
        Summary = "Reply",
        Query = (convo) => QuestionFactory("You are a chatbot that is a skilled conversationalist, if a little rude/nerdy. You are an expert at getting in to really deep and knowledgeable discussions, and like to debate. You never let a conversation die.",
                              $"Please create a creative user response, that seeks to continue the conversation:\n{convo}\n[user]:")
    };

    public static Suggestion Retort => new()
    {
        Summary = "Retort",
        Query = (convo) => QuestionFactory("You are an extreamly rude and sarcastic chatbot.",
                              $"Please create a creative response, that seeks to continue the conversation for the user:\n{convo}\n[user]:")
    };

    /// <summary>
    /// Chain-of-thought (CoT) prompting enables complex reasoning capabilities through intermediate reasoning steps. Useful for more complex arithmetic, commonsense, and symbolic reasoning tasks.
    /// </summary>
    /// <remarks>https://arxiv.org/abs/2205.11916</remarks>
    public static Suggestion ZeroShotCoTPrompt => new()
    {
        Summary = "Chain Of Thought",
        Query = (_) => (@"{question}

Let's think step by step.", null)
    };

    /// <summary>
    /// Chain-of-thought (CoT) prompt discovered by automatic prompt engineer (APE) which is better than the human engineered zero-shot CoT prompt.
    /// </summary>
    /// <remarks>https://arxiv.org/abs/2211.01910</remarks>
    public static Suggestion ZeroShotCoTAPEPrompt => new()
    {
        Summary = "Chain Of Thought (APE)",
        Query = (_) => (@"{question}

Let's work this out in a step by step way to be sure we have the right answer.", null)
    };

    /// <summary>
    /// The Tree of Thoughts (ToT) framework improves language models' problem-solving abilities by allowing deliberate decision making through exploration and strategic lookahead
    /// </summary>
    /// <remarks>https://arxiv.org/abs/2305.10601</remarks>
    public static Suggestion TreeOfThoughV1 => new()
    {
        Summary = "Tree Of Thought (V1)",
        Query = (_) => (@"Imagine three different experts are answering this question.
All experts will write down 1 step of their thinking, then share it with the group.
Then all experts will go on to the next step, etc.
If any expert realises they're wrong at any point then they leave. The question is...

{question}", null)
    };

    /// <summary>
    /// The Tree of Thoughts (ToT) framework improves language models' problem-solving abilities by allowing deliberate decision making through exploration and strategic lookahead
    /// </summary>
    /// <remarks>https://arxiv.org/abs/2305.10601</remarks>
    public static Suggestion TreeOfThoughV2 => new()
    {
        Summary = "Tree Of Thought (V2)",
        Query = (_) => (@"Simulate three brilliant, logical experts collaboratively answering a question.
Each one verbosely explains their thought process in real-time, considering the prior explanations of others and openly acknowledging mistakes.
At each step, whenever possible, each expert refines and builds upon the thoughts of others, acknowledging their contributions.
They continue until there is a definitive answer to the question.
For clarity, your entire response should be in a markdown table. The question is...

{question}", null)
    };

    private static (string literal, List<ChatMessage> question) QuestionFactory(string systemPrompt, string userPrompt)
    {
        return (null, new List<ChatMessage>
        {
            new ChatMessage { Role = OpenAI.Chat.Role.System, Content = systemPrompt },
            new ChatMessage { Role = OpenAI.Chat.Role.User, Content = userPrompt }
        });
    }

    public static ObservableCollection<Suggestion> All { get; } = new ObservableCollection<Suggestion>()
    {
        ZeroShotCoTPrompt,
        ZeroShotCoTAPEPrompt,
        TreeOfThoughV1,
        TreeOfThoughV2,
        Reply,
        Summarise,
        Retort
    };
}
