using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Homunculus.Commands;

public static class JsonSchemaGenerator
{
    public static string FromType(Type type)
    {
        var sb = new StringBuilder("{\"type\":\"object\",");
        sb.Append("\"properties\": {");

        var properties = type.GetProperties();
        var requiredProperties = new List<string>();

        foreach (var property in properties)
        {
            var jsonType = GetTypeAsString(property);
            var description = GetPropertyDescription(property);

            var propertyName = property.Name.ToLower();

            if (IsRequiredProperty(property))
                requiredProperties.Add($"\"{propertyName}\"");

            sb.Append($"\"{propertyName}\":{{\"type\":\"{jsonType}\"");

            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                sb.Append($",\"items\":{FromType(property.PropertyType.GetGenericArguments()[0])}");
            }

            if (!string.IsNullOrEmpty(description))
            {
                sb.Append($",\"description\":\"{description}\"");
            }

            sb.Append("},");
        }

        sb.Remove(sb.Length - 1, 1);
        sb.Append("}");

        if (requiredProperties.Count > 0)
        {
            sb.Append(",\"required\": [ ");
            sb.Append(string.Join(",", requiredProperties));
            sb.Append(" ]");
        }

        sb.Append("}");

        return sb.ToString();
    }

    private static string GetTypeAsString(PropertyInfo property)
    {
        string jsonType;
        var propertyType = property.PropertyType;

        if (propertyType == typeof(string))
        {
            jsonType = "string";
        }
        else if (propertyType == typeof(int))
        {
            jsonType = "int";
        }
        else if (propertyType == typeof(bool))
        {
            jsonType = "boolean";
        }
        else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            jsonType = "array";
        }
        else
        {
            throw new InvalidOperationException("Unsupported type: " + propertyType.Name);
        }

        return jsonType;
    }

    private static string GetPropertyDescription(PropertyInfo property)
    {
        return property.GetCustomAttribute(typeof(DescriptionAttribute)) is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : "";
    }

    private static bool IsRequiredProperty(PropertyInfo property)
    {
        var requiredAttribute = property.GetCustomAttribute(typeof(RequiredAttribute));
        return requiredAttribute != null;
    }
}