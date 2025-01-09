using MindFlayer.saas.tools;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace MindFlayer.saas;

public static class ToolMapper
{
    public static List<Anthropic.SDK.Messaging.Tool> AnthropicTools()
    {
        return MapToolsFromAssembly((name, desc, schema) => new Anthropic.SDK.Messaging.Tool() { Name = name, Description = desc, InputSchema = schema });
    }

    public static List<OpenAI.Chat.Tool> OpenAiTools()
    {
        return MapToolsFromAssembly((name, desc, schema) => new OpenAI.Chat.Tool(new OpenAI.Chat.Function(name, desc, ConvertObjectToJsonNode(schema.parameters))));
    }

    private static JsonNode ConvertObjectToJsonNode(object obj)
    {
        var jsonString = JsonSerializer.Serialize(obj);

        return JsonNode.Parse(jsonString);
    }

    private static List<T> MapToolsFromAssembly<T>(Func<string, string, dynamic, T> generator)
    {
        if (generator is null) throw new ArgumentNullException(nameof(generator));

        var tools = new List<T>();

        var methods = typeof(IOTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Concat(typeof(CodeTools).GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);

        foreach (var method in methods)
        {
            var toolAttr = method.GetCustomAttribute<ToolAttribute>();
            var parameters = method.GetParameters()
                .Select(p =>
                {
                    var paramAttr = p.GetCustomAttribute<ToolParameterAttribute>();
                    if (paramAttr == null) return null;

                    return new
                    {
                        Parameter = paramAttr,
                        IsArray = paramAttr.Type == "array",
                        ArrayItemType = p.ParameterType.IsGenericType ?
                            p.ParameterType.GetGenericArguments().FirstOrDefault() : null
                    };
                })
                .Where(p => p != null)
                .ToList();

            var functionSchema = new
            {
                name = toolAttr.Name,
                description = toolAttr.Description,

                type = "object",
                parameters = new
                {
                    type = "object",
                    properties = parameters.ToDictionary(
                        keySelector: p => p.Parameter.Name,
                        elementSelector: p =>
                        {
                            if (p.IsArray && p.ArrayItemType != null)
                            {
                                var itemProperties = p.ArrayItemType.GetProperties()
                                    .Select(prop => prop.GetCustomAttribute<ToolParameterAttribute>())
                                    .Where(attr => attr != null)
                                    .ToDictionary(
                                        attr => attr.Name,
                                        attr => new Dictionary<string, object>
                                        {
                                            { "type", attr.Type },
                                            { "description", attr.Description },
                                            { "default", attr.Default }
                                        }
                                    );

                                return new Dictionary<string, object>
                                {
                                    { "type", "array" },
                                    { "items", new Dictionary<string, object>
                                        {
                                            { "type", "object" },
                                            { "properties", itemProperties },
                                            { "required", p.ArrayItemType.GetProperties()
                                                .Select(prop => prop.GetCustomAttribute<ToolParameterAttribute>())
                                                .Where(attr => attr != null && attr.IsRequired)
                                                .Select(attr => attr.Name)
                                                .ToList()
                                            }
                                        }
                                    },
                                    { "description", p.Parameter.Description }
                                };
                            }
                            return new Dictionary<string, object>
                            {
                                { "type", p.Parameter.Type },
                                { "description", p.Parameter.Description },
                                { "default", p.Parameter.Default }
                            };
                        }
                    ),
                    required = parameters.Where(p => p.Parameter.IsRequired)
                                       .Select(p => p.Parameter.Name)
                                       .ToList()
                }
            };

            var tool = generator(toolAttr.Name, toolAttr.Description, functionSchema);
                
            tools.Add(tool);
        }

        return tools;
    }
}