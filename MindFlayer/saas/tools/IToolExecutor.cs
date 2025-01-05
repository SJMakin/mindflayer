namespace MindFlayer.saas.tools
{
    // Interface for execution (if needed)
    public interface IToolExecutor
    {
        string Execute(string toolName, IDictionary<string, object> parameters);
        IEnumerable<ToolInfo> GetAvailableTools();
    }
}
