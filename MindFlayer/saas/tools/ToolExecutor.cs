using System.Reflection;
using System.Text.Json;

namespace MindFlayer.saas.tools;

public class ToolExecutor
{
    public static string ExecuteTool(string toolName, string jsonArgs)
    {
        // Get all tool methods from both classes
        var toolMethods = typeof(IOTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Concat(typeof(CodeTools).GetMethods(BindingFlags.Public | BindingFlags.Static));

        // Find the method with matching Tool attribute name
        var method = toolMethods.FirstOrDefault(m =>
            m.GetCustomAttribute<ToolAttribute>()?.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase) == true);

        if (method == null)
            return $"Error: Tool '{toolName}' not found";

        try
        {
            // Parse JSON args
            using var document = JsonDocument.Parse(jsonArgs);
            var root = document.RootElement;

            // Get method parameters
            var parameters = method.GetParameters();
            var args = new object[parameters.Length];

            // Map JSON properties to parameters
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramAttr = param.GetCustomAttribute<ToolParameterAttribute>();
                if (paramAttr == null) continue;

                // Try get value from JSON or use default
                if (root.TryGetProperty(paramAttr.Name, out var jsonValue))
                {
                    args[i] = ConvertJsonValue(jsonValue, param.ParameterType);
                }
                else
                {
                    // Use parameter default value or attribute default
                    args[i] = param.HasDefaultValue ? param.DefaultValue :
                             paramAttr.Default ?? GetDefaultValue(param.ParameterType);
                }
            }

            // Invoke the method
            return (string)method.Invoke(null, args);
        }
        catch (Exception ex)
        {
            return $"Error executing tool: {ex.Message}";
        }
    }

    private static object ConvertJsonValue(JsonElement element, Type targetType)
    {
        return targetType.Name switch
        {
            "String" => element.GetString(),
            "Int32" => element.GetInt32(),
            "Boolean" => element.GetBoolean(),
            "Double" => element.GetDouble(),
            // Add other types as needed
            _ => throw new NotSupportedException($"Conversion for type {targetType.Name} not supported")
        };
    }

    private static object GetDefaultValue(Type t)
    {
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }
}