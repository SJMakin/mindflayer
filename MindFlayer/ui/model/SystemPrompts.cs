using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace MindFlayer;

public static class SystemPrompts
{
    private static Random random = new();

    public static SystemPrompt RandomPrompt()
    {
        return All[random.Next(0, All.Count)];
    }

    public static List<SystemPrompt> Load()
    {
        return JsonSerializer.Deserialize<List<SystemPrompt>>(File.ReadAllText("system-prompts.json"));
    }

    public static ObservableCollection<SystemPrompt> All { get; } = new ObservableCollection<SystemPrompt>(Load());
}
