using System.Windows;

namespace MindFlayer;


public class Header : DependencyObject, IParent<object>
{
    public string Title { get; set; }
    public int Level { get; set; }
    public List<Header> Children { get; set; } = [];
    public string Content { get; internal set; }

    public IEnumerable<object> GetChildren() => Children;

    public override string ToString()
    {
        return $"{("".PadLeft(Level, '#'))} {Title}\n{Content}";
    }
}
