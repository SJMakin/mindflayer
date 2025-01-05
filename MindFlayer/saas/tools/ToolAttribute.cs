namespace MindFlayer.saas.tools
{
    public class ToolAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsRequired { get; set; } = true;

        public ToolAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
