using DiffMatchPatch;
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

        [Tool("read_file", "Read file content with position and length limits")]
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
            return $"{chunk}{(available > chunk.Length ? $"\n\n[Truncated: {available} chars remaining]" : "")}";
        }

        [Tool("write_file", "Write content to file, overwriting existing")]
        public static string Write(
            [ToolParameter("path", "Path to write to")] string path,
            [ToolParameter("content", "Content to write")] string content)
        {
            try
            {
                File.WriteAllText(path, content);
                return $"Success";
            }
            catch (Exception ex)
            {
                return $"Error writing to {path}: {ex.Message}";
            }
        }

        [Tool("append_file", "Write content to file")]
        public static string Append(
            [ToolParameter("path", "Path to write to")] string path,
            [ToolParameter("content", "Content to write")] string content)
        {
            try
            {
                File.AppendAllText(path, content);
                return $"Success";
            }
            catch (Exception ex)
            {
                return $"Error appending to {path}: {ex.Message}";
            }
        }

        // File System
        [Tool("find_file", "Find files using glob patterns")]
        public static string Find(
            [ToolParameter("path", "Absolute path of the directory to search")] string path,
            [ToolParameter("pattern", "Glob pattern (eg: *.cs)")] string pattern,
            [ToolParameter("maxResults", "Maximum results to return", "integer", MaxLines)] int max = MaxLines)
        {
            try
            {
                var files = Directory.GetFiles(path, pattern, SearchOption.AllDirectories)
                    .Take(max)
                    .Select(f => new FileInfo(f))
                    .Select(f => $"{f.FullName} ({f.Length} bytes, modified {f.LastWriteTime})");

                return $"{files.Count()} results:\n{string.Join("\n", files)}";
            }
            catch (Exception ex)
            {
                return $"Error searching for {pattern}: {ex.Message}";
            }
        }

        // Text Search
        [Tool("grep", "Search file or directory contents using regex")]
        public static string Grep(
           [ToolParameter("path", "Path to file or directory to search")] string path,
           [ToolParameter("searchPattern", "The search string to match against file names. Supports * (any characters) and ? (single character)")] string searchPattern,
           [ToolParameter("content_pattern", "Regex pattern to search for")] string contentPattern,
           [ToolParameter("maxResults", "Maximum results to return", "integer", MaxLines)] int max = MaxLines)
        {
            try
            {
                var filesToSearch = File.Exists(path) 
                    ? new[] { path }
                    : Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
                
                var matches = filesToSearch
                    .SelectMany(file => {
                        var content = File.ReadAllText(file);
                        return Regex.Matches(content, contentPattern)
                            .Cast<Match>()
                            .Take(max)
                            .Select(m => $"{file}:{m.Index}: {m.Value.Trim()}");
                    });

                return $"{matches.Count()} matches:\n{string.Join("\n", matches)}\n{(MaxLines == matches.Count() ? "\n\nResults likely truncated. Consider tweaking args. " : "")}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // Text Replace
        //[Tool("regex_replace", "Regex-based file content replacement")]
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

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n");
        }

        private static string CreateUnifiedDiff(string originalContent, string newContent, string filepath = "file")
        {
            var normalizedOriginal = NormalizeLineEndings(originalContent);
            var normalizedNew = NormalizeLineEndings(newContent);

            var dmp = new diff_match_patch();
            var diffs = dmp.diff_main(normalizedOriginal, normalizedNew);
            dmp.diff_cleanupSemantic(diffs);

            var patches = dmp.patch_make(diffs);
            var diffText = dmp.patch_toText(patches);
            
            // Decode the URL-encoded characters
            return Uri.UnescapeDataString(diffText)
                .Replace("%0A", "\n")  // Handle newlines separately as they might need platform-specific handling
                .Replace("%09", "\t"); // Handle tabs
        }

        [Tool("edit_file", "Make line-based edits to a text file. Each edit replaces exact line sequences with new content. Returns a git-style diff showing the changes made.")]
        public static string ApplyFileEdits(
            [ToolParameter("filePath", "Path to the file to edit", "string")] string filePath,
            [ToolParameter("edits", "Array of {oldText, newText} objects for replacements", "array")] List<FileEdit> edits,
            [ToolParameter("dryRun", "If true, don't write changes to disk", "boolean", "false")] bool dryRun = false)
        {
            var content = NormalizeLineEndings(File.ReadAllText(filePath));
            var modifiedContent = content;

            foreach (var edit in edits)
            {
                var normalizedOld = NormalizeLineEndings(edit.OldText);
                var normalizedNew = NormalizeLineEndings(edit.NewText);

                if (modifiedContent.Contains(normalizedOld))
                {
                    modifiedContent = modifiedContent.Replace(normalizedOld, normalizedNew);
                    continue;
                }

                var oldLines = normalizedOld.Split('\n');
                var contentLines = modifiedContent.Split('\n').ToList();
                bool matchFound = false;

                for (int i = 0; i <= contentLines.Count - oldLines.Length; i++)
                {
                    var potentialMatch = contentLines.Skip(i).Take(oldLines.Length);
                    bool isMatch = oldLines.Zip(potentialMatch, (oldLine, contentLine) =>
                        oldLine.Trim() == contentLine.Trim()).All(x => x);

                    if (isMatch)
                    {
                        var originalIndent = Regex.Match(contentLines[i], @"^\s*").Value;
                        var newLines = normalizedNew.Split('\n').Select((line, j) =>
                        {
                            if (j == 0)
                                return originalIndent + line.TrimStart();

                            var oldIndent = j < oldLines.Length ?
                                Regex.Match(oldLines[j], @"^\s*").Value : "";
                            var newIndent = Regex.Match(line, @"^\s*").Value;

                            if (!string.IsNullOrEmpty(oldIndent) && !string.IsNullOrEmpty(newIndent))
                            {
                                var relativeIndent = newIndent.Length - oldIndent.Length;
                                return originalIndent + new string(' ', Math.Max(0, relativeIndent)) + line.TrimStart();
                            }
                            return line;
                        }).ToList();

                        contentLines.RemoveRange(i, oldLines.Length);
                        contentLines.InsertRange(i, newLines);
                        modifiedContent = string.Join("\n", contentLines);
                        matchFound = true;
                        break;
                    }
                }

                if (!matchFound)
                {
                    throw new Exception($"Could not find exact match for edit:\n{edit.OldText}");
                }
            }

            var diff = CreateUnifiedDiff(content, modifiedContent, filePath);

            int numBackticks = 3;
            while (diff.Contains(new string('`', numBackticks)))
            {
                numBackticks++;
            }
            var formattedDiff = $"{new string('`', numBackticks)}diff\n{diff}{new string('`', numBackticks)}\n\n";

            if (!dryRun)
            {
                File.WriteAllText(filePath, modifiedContent);
            }

            return formattedDiff;
        }
    }

    public class FileEdit
    {
        [ToolParameter("oldText", "Text to replace", "string")]
        public string OldText { get; set; }

        [ToolParameter("newText", "New text to insert", "string")]
        public string NewText { get; set; }
    }
}
