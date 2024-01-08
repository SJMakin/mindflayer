using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MindFlayer
{

    public class Header : DependencyObject, IParent<object>
    {
        public string Title { get; set; }
        public int Level { get; set; }
        public List<Header> Children { get; set; } = new List<Header>();
        public string Content { get; internal set; }

        public IEnumerable<object> GetChildren() => Children;

        public override string ToString()
        {
            return $"{("".PadLeft(Level, '#'))} {Title}\n{Content}";
        }
    }

}
