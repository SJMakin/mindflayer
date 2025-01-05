using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MindFlayer.saas.tools
{
    public static class IOTools
    {
        const int MaxChars = 4000;
        const int MaxLines = 100;

        // Core IO

        [Tool("read", "Read file content with position and length limits")]
        public static string Read(
            [ToolParameter("path", "Path to the file to read")] string path,
            [ToolParameter("start", "Starting position in file", "integer", 0)] int start = 0,
            [ToolParameter("maxChars", "Maximum characters to read", "integer", MaxChars)] int maxChars = MaxChars)
        {
            if (!File.Exists(path))
                return $"Error: File not found: {path}";

            var text = File.ReadAllText(path);
            var available = text.Length - start;
            if (available <= 0)
                return $"Error: Start position {start} beyond file length {text.Length}";

            var chunk = text.Substring(start, Math.Min(maxChars, available));
            return $"Read {chunk.Length} chars from {path} (position {start}, {available} remaining):\n{chunk}";
        }

        [Tool("readLines", "Read file lines with start/limit control")]
        public static string ReadLines(
            [ToolParameter("path", "Path to the file to read")] string path,
            [ToolParameter("start", "Starting line number", "integer", 0)] int start = 0,
            [ToolParameter("maxLines", "Maximum lines to read", "integer", MaxLines)] int maxLines = MaxLines)
        {
            if (!File.Exists(path))
                return $"Error: File not found: {path}";

            var lines = File.ReadAllLines(path);
            if (start >= lines.Length)
                return $"Error: Start line {start} beyond file length {lines.Length}";

            var chunk = lines.Skip(start).Take(maxLines);
            return $"Read {chunk.Count()} lines from {path} (starting line {start}, {lines.Length - start} remaining):\n{string.Join("\n", chunk)}";
        }

        [Tool("write", "Write content to file, overwriting existing")]
        public static string Write(
            [ToolParameter("path", "Path to write to")] string path,
            [ToolParameter("content", "Content to write")] string content)
        {
            try
            {
                File.WriteAllText(path, content);
                return $"Wrote {content.Length} chars to {path}";
            }
            catch (Exception ex)
            {
                return $"Error writing to {path}: {ex.Message}";
            }
        }

        [Tool("append", "Write content to file")]
        public static string Append(
            [ToolParameter("path", "Path to write to")] string path,
            [ToolParameter("content", "Content to write")] string content)
        {
            try
            {
                File.AppendAllText(path, content);
                return $"Appended {content.Length} chars to {path}";
            }
            catch (Exception ex)
            {
                return $"Error appending to {path}: {ex.Message}";
            }
        }

        // File System
        [Tool("find", "Find files using glob patterns")]
        public static string Find(
            [ToolParameter("pattern", "Glob pattern (eg: *.cs)")] string pattern,
            [ToolParameter("maxResults", "Maximum results to return", "integer", MaxLines)] int max = MaxLines)
        {
            try
            {
                var files = Directory.GetFiles(".", pattern, SearchOption.AllDirectories)
                    .Take(max)
                    .Select(f => new FileInfo(f))
                    .Select(f => $"{f.FullName} ({f.Length} bytes, modified {f.LastWriteTime})");

                return $"Found {files.Count()} files matching '{pattern}':\n{string.Join("\n", files)}";
            }
            catch (Exception ex)
            {
                return $"Error searching for {pattern}: {ex.Message}";
            }
        }

        // Text Search
        [Tool("grep", "Search file contents using regex")]
        public static string Grep(
           [ToolParameter("pattern", "Regex pattern to search for")] string pattern,
           [ToolParameter("maxResults", "Maximum results to return", "integer", MaxLines)] int max = MaxLines)
        {
            try
            {
                var matches = Directory.GetFiles(".", "*.cs", SearchOption.AllDirectories)
                    .SelectMany(file => File.ReadLines(file)
                        .Select((line, i) => new { file, line, lineNo = i + 1 })
                        .Where(x => Regex.IsMatch(x.line, pattern))
                        .Take(max)
                        .Select(x => $"{x.file}:{x.lineNo}: {x.line.Trim()}"));

                return $"Found {matches.Count()} matches for '{pattern}':\n{string.Join("\n", matches)}";
            }
            catch (Exception ex)
            {
                return $"Error searching for {pattern}: {ex.Message}";
            }
        }

        // Text Replace
        [Tool("replace", "Regex-based file content replacement")]
        public static string Replace(
            [ToolParameter("path", "Path to the file")] string path,
            [ToolParameter("pattern", "Regex pattern to match")] string pattern,
            [ToolParameter("replacement", "Replacement text")] string replacement)
        {
            try
            {
                var text = File.ReadAllText(path);
                var result = Regex.Replace(text, pattern, replacement);
                File.WriteAllText(path, result);
                return $"Replaced pattern '{pattern}' in {path}";
            }
            catch (Exception ex)
            {
                return $"Error replacing in {path}: {ex.Message}";
            }
        }

        [Tool("replaceText", "Literal string replacement in file")]
        public static string ReplaceText(
            [ToolParameter("path", "Path to the file")] string path,
            [ToolParameter("oldText", "Text to replace")] string oldText,
            [ToolParameter("newText", "New text")] string newText)
        {
            try
            {
                var text = File.ReadAllText(path);
                var result = text.Replace(oldText, newText);
                File.WriteAllText(path, result);
                return $"Replaced '{oldText}' with '{newText}' in {path}";
            }
            catch (Exception ex)
            {
                return $"Error replacing in {path}: {ex.Message}";
            }
        }
    }
}
