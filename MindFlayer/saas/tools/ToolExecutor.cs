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
        // Handle List<T>
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listType = targetType.GetGenericArguments()[0];
            var list = Activator.CreateInstance(targetType);
            var add = targetType.GetMethod("Add");

            // Handle string that should be an array
            if (element.ValueKind == JsonValueKind.String)
            {
                try
                {
                    // Try parse as JSON array
                    using var arrayDoc = JsonDocument.Parse(element.GetString());
                    if (arrayDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in arrayDoc.RootElement.EnumerateArray())
                        {
                            add.Invoke(list, new[] { ConvertJsonValue(item, listType) });
                        }
                        return list;
                    }
                }
                catch
                {
                    // If parsing fails, treat as single item
                    add.Invoke(list, new[] { ConvertJsonValue(element, listType) });
                    return list;
                }
            }

            foreach (var item in element.EnumerateArray())
            {
                add.Invoke(list, new[] { ConvertJsonValue(item, listType) });
            }
            return list;
        }

        // Handle primitive types
        if (element.ValueKind == JsonValueKind.Null) return null;
        if (element.ValueKind == JsonValueKind.String) return element.GetString();
        if (element.ValueKind == JsonValueKind.Number)
        {
            if (targetType == typeof(int)) return element.GetInt32();
            if (targetType == typeof(double)) return element.GetDouble();
        }
        if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
            return element.GetBoolean();

        // Handle complex objects
        if (element.ValueKind == JsonValueKind.Object)
        {
            var instance = Activator.CreateInstance(targetType);
            var properties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                var paramAttr = property.GetCustomAttribute<ToolParameterAttribute>();
                if (paramAttr == null) continue;

                if (element.TryGetProperty(paramAttr.Name, out var jsonValue) && property.CanWrite)
                {
                    property.SetValue(instance, ConvertJsonValue(jsonValue, property.PropertyType));
                }
            }
            return instance;
        }

        throw new NotSupportedException($"Conversion for type {targetType.Name} not supported");
    }

    private static object GetDefaultValue(Type t)
    {
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }
}