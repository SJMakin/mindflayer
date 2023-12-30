
using System.Windows;

namespace MindFlayer
{
    public class Family : DependencyObject, IParent<object>
    {
        public string Name { get; set; }
        public List<Person> Members { get; set; }

        IEnumerable<object> IParent<object>.GetChildren()
        {
            return Members;
        }
    }

    public class Person : DependencyObject
    {
        public string Name { get; set; }
    }
}
