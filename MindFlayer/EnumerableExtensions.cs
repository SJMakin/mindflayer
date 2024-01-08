using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindFlayer
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> DepthFirst<T>(this T node, Func<T, IEnumerable<T>> childSelector) where T : class, new()
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (childSelector == null)
                throw new ArgumentNullException(nameof(childSelector));

            Stack<T> stack = new Stack<T>();
            stack.Push(node);

            while (stack.Any())
            {
                T next = stack.Pop();
                yield return next;

                foreach (T child in childSelector(next))
                    stack.Push(child);
            }
        }
    }
}
