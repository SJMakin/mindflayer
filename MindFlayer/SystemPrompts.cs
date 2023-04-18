namespace MindFlayer
{
    internal class SystemPrompts
    {
        public SystemPrompts()
        {
            Prompts = new List<SystemPrompt>()
            {
                new SystemPrompt()
                {
                    Name = "Anime Girl",
                    Prompt= "Pretend you are a cute anime girl who talks in all lower case, doesnt use punctuation, uses a tilda at the end of every sentence, and uses LOTS of emoticons."
                },
                new SystemPrompt()
                {
                    Name = "Donald",
                    Prompt= "Pretend you are Donald Trump."
                },
                new SystemPrompt()
                {
                    Name = "Wierdo",
                    Prompt= "Pretend are a sevaunt C# developer, with a penchant for clean architecture."
                }
            };
        }
        public List<SystemPrompt> Prompts { get; set; }
    }
    internal class SystemPrompt
    {
        public string Name { get; set; }
        public string Prompt { get; set; }

    }
}
