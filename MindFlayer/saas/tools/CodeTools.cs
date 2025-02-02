using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MindFlayer.saas.tools;

public static class CodeTools
{
    const int MaxChars = 10000; // Output budget for map function  // Output budget
    const int MaxDepth = 5;     // Directory depth limit

    [Tool("map", "Analyze directory/file structure and code elements. Shows classes/methods/properties for code files using BFS traversal.")]
    public static string Map(
        [ToolParameter("path", "Path to analyze (file or directory)")] string path)
    {
        var sb = new StringBuilder();
        var skipped = 0;
        var queue = new Queue<(string path, int depth)>();
        queue.Enqueue((path, 0));

        while (queue.Count > 0 && sb.Length < MaxChars)
        {
            var (current, depth) = queue.Dequeue();
            var indent = new string(' ', depth * 2);

            try
            {
                if (Directory.Exists(current))
                {
                    sb.AppendLine($"{indent} {current}/");

                    if (depth < MaxDepth)
                    {
                        var entries = Directory.EnumerateFileSystemEntries(current)
                        .Where(e => !e.Contains("node_modules", StringComparison.OrdinalIgnoreCase))
                        .Where(e => !e.Contains(".git", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(e => Path.GetFileName(e))
                        .ToList();

                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (queue.Count > 100) // Prevent queue explosion
                        {
                            skipped += entries.Count - i;
                            break;
                        }
                        queue.Enqueue((entries[i], depth + 1));
                    }
                    }
                    else skipped++;
                }
                else if (File.Exists(current))
                {
                    var fi = new FileInfo(current);
                    var ext = fi.Extension.ToLowerInvariant();

                    sb.AppendLine($"{indent} {fi.FullName} ({FormatFileSize(fi.Length)})");

                    if ((ext == ".cs" || ext == ".go") && sb.Length < MaxChars - 500)
                    {
                        try
                        {
                            string fileContent = File.ReadAllText(current); 
                            if (ext == ".cs")
                            {
                                MapCSharp(sb, indent, fileContent);
                            }
                            else // Go file
                            {
                                MapGo(sb, indent, fileContent);
                            }
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"{indent} Error processing file: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"{indent} Error: {ex.Message}");
            }

            if (sb.Length >= MaxChars)
            {
                skipped += queue.Count;
                sb.AppendLine($"\n...output truncated, {skipped} items not shown. Narrow your search!");
                break;
            }
        }

        return sb.ToString();
    }

    private static void MapGo(StringBuilder sb, string indent, string fileContent)
    {
        // Match type declarations
        var typeMatches = System.Text.RegularExpressions.Regex.Matches(fileContent, @"type\s+([^\s{]+)\s*{?");
        foreach (System.Text.RegularExpressions.Match typeMatch in typeMatches)
        {
            if (sb.Length >= MaxChars - 100) break;
            string typeName = typeMatch.Groups[1].Value;
            sb.AppendLine($"{indent}  {typeName}");

            // Match methods for this type
            var methodMatches = System.Text.RegularExpressions.Regex.Matches(fileContent,
                $@"func\s+\([^)]+\s*{typeName}\)\s*([^(\s]+)\s*\([^)]*\)");
            foreach (System.Text.RegularExpressions.Match methodMatch in methodMatches)
            {
                if (sb.Length >= MaxChars - 100) break;
                sb.AppendLine($"{indent}    {methodMatch.Groups[1].Value}");
            }
        }

        // Match standalone functions
        var funcMatches = System.Text.RegularExpressions.Regex.Matches(fileContent, @"func\s+([^(\s]+)\s*\([^)]*\)");
        foreach (System.Text.RegularExpressions.Match funcMatch in funcMatches)
        {
            if (sb.Length >= MaxChars - 100) break;
            string funcName = funcMatch.Groups[1].Value;
            if (!funcName.Contains(")")) // Exclude method matches
                sb.AppendLine($"{indent}  {funcName}");
        }
    }

    private static void MapCSharp(StringBuilder sb, string indent, string fileContent)
    {
        var tree = CSharpSyntaxTree.ParseText(fileContent);
        var types = tree.GetRoot().DescendantNodes()
            .OfType<TypeDeclarationSyntax>();

        foreach (var type in types)
        {
            if (sb.Length >= MaxChars - 100) break;
            sb.AppendLine($"{indent}  {type.Modifiers} {type.Keyword} {type.Identifier}{TypeParams(type.TypeParameterList)} [{GetPosition(type)}]");

            foreach (var member in type.Members)
            {
                if (sb.Length >= MaxChars - 100)
                {
                    sb.AppendLine($"{indent}    + more members truncated due to size limit...");
                    break;
                }
                var memberText = member switch
                {
                    MethodDeclarationSyntax method => $"{GetMemberSignature(method)} [{GetPosition(method)}]",
                    PropertyDeclarationSyntax prop => $"{prop.Modifiers} {prop.Type} {prop.Identifier} [{GetPosition(prop)}]",
                    FieldDeclarationSyntax field => $"{field.Modifiers} {field.Declaration.Type} {field.Declaration.Variables.First().Identifier} [{GetPosition(field)}]",
                    _ => null
                };
                if (memberText != null)
                    sb.AppendLine($"{indent}    {memberText}");
            }
        }
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1}KB";
        return $"{bytes / (1024.0 * 1024):F1}MB";
    }

    // Code Reading
    public static string ReadNode(string path, string identifier)
    {
        try
        {
            var code = File.ReadAllText(path);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            // Try to find by exact match first
            var node = root.DescendantNodes()
                .FirstOrDefault(n => n.ToString() == identifier ||
                                   (n is MemberDeclarationSyntax m && GetIdentifier(m).ToString() == identifier));

            // If not found, try fuzzy match
            if (node == null)
            {
                node = root.DescendantNodes()
                    .FirstOrDefault(n => n.ToString().Contains(identifier) ||
                                       (n is MemberDeclarationSyntax m && GetIdentifier(m).ToString().Contains(identifier)));
            }

            if (node == null)
                return $"Node '{identifier}' not found in {path}";

            return $"Found node at {GetPosition(node)}:\n{node}";
        }
        catch (Exception ex)
        {
            return $"Error reading node from {path}: {ex.Message}";
        }
    }

    // Code Writing
    public static string ReplaceNode(string path, string identifier, string newCode)
    {
        try
        {
            var code = File.ReadAllText(path);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var node = root.DescendantNodes()
                .FirstOrDefault(n => n.ToString() == identifier ||
                                   (n is MemberDeclarationSyntax m && GetIdentifier(m).ToString() == identifier));

            if (node == null)
                return $"Node '{identifier}' not found in {path}";

            var newNode = CSharpSyntaxTree.ParseText(newCode).GetRoot().DescendantNodes().First();
            var newRoot = root.ReplaceNode(node, newNode);

            File.WriteAllText(path, newRoot.ToString());
            return $"Replaced node '{identifier}' in {path}";
        }
        catch (Exception ex)
        {
            return $"Error replacing node in {path}: {ex.Message}";
        }
    }

    private static string GetIdentifier(MemberDeclarationSyntax member) => member switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        PropertyDeclarationSyntax p => p.Identifier.Text,
        ClassDeclarationSyntax c => c.Identifier.Text,
        FieldDeclarationSyntax f => f.Declaration.Variables.First().Identifier.Text,
        InterfaceDeclarationSyntax i => i.Identifier.Text,
        EnumDeclarationSyntax e => e.Identifier.Text,
        _ => ""
    };

    // Execution
    [Tool("run_command", "Execute shell command with timeout")]
    public static string RunCommand(
        [ToolParameter("cmd", "Command to execute")] string command,
        [ToolParameter("timeout", "Timeout in milliseconds", "integer", 30000)] int timeoutMs = 30000)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var output = new StringBuilder();
            process.OutputDataReceived += (s, e) => output.AppendLine(e.Data);
            process.ErrorDataReceived += (s, e) => output.AppendLine(e.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(timeoutMs))
            {
                process.Kill();
                return $"Command timed out after {timeoutMs}ms: {command}";
            }

            return $"Command completed (exit code {process.ExitCode}):\n{output}";
        }
        catch (Exception ex)
        {
            return $"Error executing command: {ex.Message}";
        }
    }

    public static string RunCSharp(string code)
    {
        var assemblyName = Path.GetRandomFileName();
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.ConsoleApplication))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var failures = result.Diagnostics
                .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

            return $"Compilation failed:\n{string.Join("\n", failures)}";
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var entryPoint = assembly.EntryPoint;

        try
        {
            var output = new StringWriter();
            Console.SetOut(output);

            entryPoint?.Invoke(null, new[] { new string[0] });

            return $"Code executed successfully:\n{output}";
        }
        catch (Exception ex)
        {
            return $"Runtime error: {ex.Message}";
        }
    }

    // Helper methods
    private static string GetPosition(SyntaxNode node)
    {
        var pos = node.GetLocation().SourceSpan.Start;
        return $"@{pos}";
    }

    private static string GetMemberSignature(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax m =>
                $"{m.Modifiers} {m.ReturnType} {m.Identifier}({string.Join(", ", m.ParameterList.Parameters)})",

            PropertyDeclarationSyntax p =>
                $"{p.Modifiers} {p.Type} {p.Identifier}{(p.ExpressionBody != null ? " => ..." : "")}",

            ClassDeclarationSyntax c =>
                $"{c.Modifiers} class {c.Identifier}{TypeParams(c.TypeParameterList)}",

            FieldDeclarationSyntax f =>
                $"{f.Modifiers} {f.Declaration.Type} {f.Declaration.Variables.First().Identifier}",

            InterfaceDeclarationSyntax i =>
                $"{i.Modifiers} interface {i.Identifier}{TypeParams(i.TypeParameterList)}",

            _ => member.ToString().Split('\n')[0].Trim()
        };
    }

    private static string TypeParams(TypeParameterListSyntax? list) =>
        list == null ? "" : $"<{string.Join(", ", list.Parameters)}>";
}
