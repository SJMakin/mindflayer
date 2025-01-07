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
            [ToolParameter("path", "Path to file or directory to read")] string path,
            [ToolParameter("start_pos", "Starting position in file", "integer", 0)] int startPos = 0,
            [ToolParameter("max_chars", "Maximum characters to read per file", "integer", MaxChars)] int maxChars = MaxChars,
            [ToolParameter("file_pattern", "Optional pattern to match multiple files. Supports * and ?", "string", null)] string filePattern = null,
            [ToolParameter("max_items", "Maximum files to read when using pattern", "integer", 10)] int maxItems = 10)
        {
            try {
                var filesToRead = filePattern != null ?
                    (Directory.Exists(path) ? 
                        Directory.GetFiles(path, filePattern, SearchOption.AllDirectories).Take(maxItems) :
                        throw new Exception("Path must be a directory when using file_pattern")
                    ) :
                    (File.Exists(path) ? 
                        new[] { path } : 
                        throw new Exception("File not found")
                    );

                var results = new List<string>();
                foreach (var file in filesToRead)
                {
                    var text = File.ReadAllText(file);
                    var available = text.Length - startPos;
                    if (available <= 0) continue;

                    var chunk = text.Substring(startPos, Math.Min(maxChars, available));
                    var content = chunk + (available > chunk.Length ? $"\n[Truncated: {available} chars remaining]" : "");
                    
                    if (filePattern != null)
                    {
                        results.Add($"==> {file} <==>\n{content}");
                    }
                    else
                    {
                        return content;
                    }
                }

                return results.Count > 0 ? 
                    string.Join("\n\n", results) : 
                    "Error: No matching files found";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [Tool("write_file", "Write content to file, overwriting existing")]
        public static string Write(
            [ToolParameter("path", "Path to write to")] string path,
            [ToolParameter("content", "Content to write")] string content)
        {
            try
            {
                File.WriteAllText(path, content);
                return "Done";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
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
                return "Done";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // File System
        [Tool("find_file", "Find files using glob patterns")]
        public static string Find(
            [ToolParameter("path", "Absolute path of the directory to search")] string path,
            [ToolParameter("file_pattern", "Glob pattern (eg: *.cs)")] string filePattern,
            [ToolParameter("max_items", "Maximum results to return", "integer", MaxLines)] int maxItems = MaxLines)
        {
            try
            {
                var files = Directory.GetFiles(path, filePattern, SearchOption.AllDirectories)
                    .Take(maxItems)
                    .Select(f => new FileInfo(f))
                    .Select(f => $"{f.FullName} ({f.Length} bytes, modified {f.LastWriteTime})");

                var results = files.ToList();
                var output = $"Found {results.Count} items:\n{string.Join("\n", results)}";
                return results.Count >= maxItems ? $"{output}\n[Truncated]" : output;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // Text Search
        [Tool("grep", "Search file or directory contents using regex")]
        public static string Grep(
           [ToolParameter("path", "Path to file or directory to search")] string path,
           [ToolParameter("file_pattern", "The search string to match against file names. Supports * (any characters) and ? (single character)")] string filePattern,
           [ToolParameter("match_pattern", "Regex pattern to search for")] string matchPattern,
           [ToolParameter("max_items", "Maximum matches to return", "integer", 25)] int maxItems = 25,
           [ToolParameter("start_pos", "Starting position in each file", "integer", 0)] int startPos = 0,
           [ToolParameter("max_chars", "Maximum characters to search in each file", "integer", 50000)] int maxChars = 50000,
           [ToolParameter("context_chars", "Number of characters to show before/after match", "integer", 20)] int contextChars = 20)
        {
            try
            {
                var filesToSearch = File.Exists(path) 
                    ? new[] { path }
                    : Directory.GetFiles(path, filePattern, SearchOption.AllDirectories);
                
                var matches = filesToSearch
                    .SelectMany(file => {
                        var content = File.ReadAllText(file);
                        var searchContent = content.Length <= startPos ? "" :
                            content.Substring(startPos, Math.Min(maxChars, content.Length - startPos));
                        
                        return Regex.Matches(searchContent, matchPattern)
                            .Cast<Match>()
                            .Take(maxItems)
                            .Select(m => {
                                var start = Math.Max(0, m.Index - contextChars);
                                var end = Math.Min(searchContent.Length, m.Index + m.Length + contextChars);
                                var context = searchContent.Substring(start, end - start);
                                if (start > 0) context = "..." + context;
                                if (end < searchContent.Length) context = context + "...";
                                return $"{file}:{startPos + m.Index}: {context}";
                            });
                    });

                var results = matches.ToList();
                var output = $"Found {results.Count} items:\n{string.Join("\n", results)}";
                return results.Count >= maxItems ? $"{output}\n[Truncated]" : output;
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
            [ToolParameter("match_pattern", "Regex pattern to match")] string matchPattern,
            [ToolParameter("replacement", "Replacement text")] string replacement)
        {
            try
            {
                var text = File.ReadAllText(path);
                var result = Regex.Replace(text, matchPattern, replacement);
                File.WriteAllText(path, result);
                return $"Replaced pattern '{matchPattern}' in {path}";
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
            [ToolParameter("path", "Path to the file to edit", "string")] string path,
            [ToolParameter("edits", "Array of {oldText, newText} objects for replacements", "array")] List<FileEdit> edits,
            [ToolParameter("dry_run", "If true, don't write changes to disk", "boolean", "false")] bool dryRun = false)
        {
            var content = NormalizeLineEndings(File.ReadAllText(path));
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
                    throw new Exception("Could not find exact match for edit");
                }
            }

            var diff = CreateUnifiedDiff(content, modifiedContent, path);

            int numBackticks = 3;
            while (diff.Contains(new string('`', numBackticks)))
            {
                numBackticks++;
            }
            var formattedDiff = $"{new string('`', numBackticks)}diff\n{diff}{new string('`', numBackticks)}\n\n";

            if (!dryRun)
            {
                File.WriteAllText(path, modifiedContent);
            }

            return formattedDiff;
        }
    }

    public class FileEdit
    {
        [ToolParameter("old_text", "Text to replace", "string")]
        public string OldText { get; set; }

        [ToolParameter("new_text", "New text to insert", "string")]
        public string NewText { get; set; }
    }
}
