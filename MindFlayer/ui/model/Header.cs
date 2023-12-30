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
        public List<object> Children { get; set; } = new List<object>();

        public IEnumerable<object> GetChildren() => Children;
    }

}
