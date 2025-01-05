namespace MindFlayer.saas.tools
{
    public class ToolParameterInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "string";
        public object? Default { get; set; }
        public bool IsRequired { get; set; }
    }
}
