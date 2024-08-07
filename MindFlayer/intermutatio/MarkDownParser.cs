﻿namespace MindFlayer;

internal class MarkDownParser
{
    public static Header Parse(string mdText)
    {
        var lines = mdText.Split('\n');

        var root = new Header { Level = 0, Title = "Root" };
        var stack = new Stack<Header>();
        stack.Push(root);
        var contentBuffer = new StringBuilder();
        foreach (var line in lines)
        {


            var level = line.TakeWhile(c => c == '#').Count();

            if (level > 0)
            {
                var title = line.Substring(level).Trim();
                var header = new Header { Level = level, Title = title };
                var parent = stack.Peek();

                parent.Content = parent.Content + contentBuffer.ToString();
                contentBuffer.Clear();

                while (stack.Peek().Level >= level)
                {
                    stack.Pop();
                }


                stack.Peek().Children.Add(header);
                stack.Push(header);
            }

            contentBuffer.Append(line);
        }
        return root;
    }
}
