using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MindFlayer.saas.tools;

public static class CodeTools
{
    const int MaxChars = 3000;  // Output budget
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
                    sb.AppendLine($"{indent} {Path.GetFileName(current)}/");

                    if (depth < MaxDepth)
                    {
                        foreach (var entry in Directory.EnumerateFileSystemEntries(current))
                        {
                            queue.Enqueue((entry, depth + 1));
                            if (queue.Count > 100) // Prevent queue explosion
                            {
                                skipped += Directory.GetFileSystemEntries(current).Length - 100;
                                break;
                            }
                        }
                    }
                    else skipped++;
                }
                else if (File.Exists(current))
                {
                    var fi = new FileInfo(current);
                    if (fi.Extension.ToLower() == ".cs" && sb.Length < MaxChars - 500)
                    {
                        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(current));
                        var types = tree.GetRoot().DescendantNodes()
                            .OfType<TypeDeclarationSyntax>()
                            .Take(5);

                        sb.AppendLine($"{indent} {fi.Name} ({fi.Length}b)");
                        foreach (var type in types)
                        {
                            sb.AppendLine($"{indent}  {type.Identifier}");
                            var members = type.Members.Take(3)
                                .Select(m => m switch
                                {
                                    MethodDeclarationSyntax method => $"{GetMemberSignature(method)}",
                                    PropertyDeclarationSyntax prop => prop.Identifier.ToString(),
                                    _ => null
                                })
                                .Where(m => m != null);
                            foreach (var m in members)
                                sb.AppendLine($"{indent}    {m}");

                            if (type.Members.Count > 3)
                                sb.AppendLine($"{indent}    + {type.Members.Count - 3} more members...");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"{indent} {fi.Name} ({fi.Length}b)");
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

            return $"Found node at line {GetLineNumber(node)}:\n{node}";
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
    [Tool("runCommand", "Execute shell command with timeout")]
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
    private static int GetLineNumber(SyntaxNode node)
    {
        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
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
