namespace MindFlayer.saas.tools
{
    // Helper classes for tool information
    public class ToolInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public List<ToolParameterInfo> Parameters { get; set; } = new();
    }
}
