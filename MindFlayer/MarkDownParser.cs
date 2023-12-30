using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindFlayer
{
    internal class MarkDownParser
    {
        public static Header Parse(string mdText)
        {
            var lines = mdText.Split('\n');

            var root = new Header { Level = 0, Title = "Root" };
            var stack = new Stack<Header>();
            stack.Push(root);

            foreach (var line in lines)
            {
                var level = line.TakeWhile(c => c == '#').Count();

                if (level > 0)
                {
                    var title = line.Substring(level).Trim();
                    var header = new Header { Level = level, Title = title };
                    var parent = stack.Peek();

                    while (stack.Peek().Level >= level)
                    {
                        stack.Pop();
                    }

                    stack.Peek().Children.Add(header);
                    stack.Push(header);
                }
            }
            return root;
        }
    }
}
