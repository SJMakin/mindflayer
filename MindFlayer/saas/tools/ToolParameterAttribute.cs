namespace MindFlayer.saas.tools
{
    public class ToolParameterAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public string Type { get; }
        public object? Default { get; }
        public bool IsRequired { get; set; } = true;

        public ToolParameterAttribute(
            string name,
            string description,
            string type = "string",
            object? defaultValue = null)
        {
            Name = name;
            Description = description;
            Type = type;
            Default = defaultValue;
        }
    }
}
