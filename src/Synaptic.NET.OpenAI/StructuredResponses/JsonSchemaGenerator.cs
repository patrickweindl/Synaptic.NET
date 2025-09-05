using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synaptic.NET.Domain.Abstractions.Augmentation;
using Synaptic.NET.Domain.Attributes;

namespace Synaptic.NET.OpenAI.StructuredResponses;

/// <summary>
/// Generates JSON schemas from C# classes marked with JsonSchemaAttribute
/// </summary>
internal static class JsonSchemaGenerator
{
    public static string GetSchemaName<T>() where T : IStructuredResponseSchema
    {
        var attribute = typeof(T).GetCustomAttribute<JsonSchemaAttribute>();
        if (attribute == null)
            throw new InvalidOperationException($"Type {typeof(T).Name} must be decorated with JsonSchemaAttribute");

        return attribute.SchemaName;
    }

    public static string GenerateJsonSchema<T>() where T : IStructuredResponseSchema
    {
        var type = typeof(T);
        var attribute = type.GetCustomAttribute<JsonSchemaAttribute>();
        if (attribute == null)
            throw new InvalidOperationException($"Type {type.Name} must be decorated with JsonSchemaAttribute");

        var schema = GenerateSchemaForType(type, attribute.Description);
        return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
    }

    private static object GenerateSchemaForType(Type type, string description = "")
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
            .ToList();

        var schemaProperties = new Dictionary<string, object>();
        var requiredProperties = new List<string>();

        foreach (var property in properties)
        {
            var jsonPropertyName = GetJsonPropertyName(property);
            var propertySchema = GeneratePropertySchema(property);

            schemaProperties[jsonPropertyName] = propertySchema;

            if (IsPropertyRequired(property))
            {
                requiredProperties.Add(jsonPropertyName);
            }
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = schemaProperties,
            ["additionalProperties"] = false
        };

        if (!string.IsNullOrEmpty(description))
        {
            schema["description"] = description;
        }

        if (requiredProperties.Count > 0)
        {
            schema["required"] = requiredProperties;
        }

        return schema;
    }

    private static object GeneratePropertySchema(PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        var description = property.GetCustomAttribute<JsonPropertyDescriptionAttribute>()?.Description ?? "";

        var schema = new Dictionary<string, object>();

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlyingType == typeof(string))
        {
            schema["type"] = "string";
        }
        else if (underlyingType == typeof(int) || underlyingType == typeof(long) || underlyingType == typeof(short))
        {
            schema["type"] = "integer";
        }
        else if (underlyingType == typeof(float) || underlyingType == typeof(double) || underlyingType == typeof(decimal))
        {
            schema["type"] = "number";
        }
        else if (underlyingType == typeof(bool))
        {
            schema["type"] = "boolean";
        }
        else if (typeof(IEnumerable).IsAssignableFrom(underlyingType) && underlyingType != typeof(string))
        {
            schema["type"] = "array";

            var elementType = GetElementType(underlyingType);
            if (elementType != null)
            {
                schema["items"] = GenerateSchemaForPropertyType(elementType);
            }
        }
        else if (underlyingType.IsClass && underlyingType != typeof(string))
        {
            return GenerateSchemaForType(underlyingType);
        }
        else
        {
            schema["type"] = "string";
        }

        if (!string.IsNullOrEmpty(description))
        {
            schema["description"] = description;
        }

        return schema;
    }

    private static object GenerateSchemaForPropertyType(Type type)
    {
        if (type == typeof(string))
            return new Dictionary<string, object> { ["type"] = "string" };
        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            return new Dictionary<string, object> { ["type"] = "integer" };
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return new Dictionary<string, object> { ["type"] = "number" };
        if (type == typeof(bool))
            return new Dictionary<string, object> { ["type"] = "boolean" };
        if (type.IsClass && type != typeof(string))
            return GenerateSchemaForType(type);

        return new Dictionary<string, object> { ["type"] = "string" };
    }

    private static Type? GetElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length > 0)
                return genericArgs[0];
        }

        return typeof(object);
    }

    private static string GetJsonPropertyName(PropertyInfo property)
    {
        var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        return jsonPropertyAttribute?.Name ?? ToCamelCase(property.Name);
    }

    private static bool IsPropertyRequired(PropertyInfo property)
    {
        var requiredModifiers = property.GetRequiredCustomModifiers();
        if (requiredModifiers.Any(m => m.Name.Contains("IsExternalInit") || m.Name.Contains("RequiredMember")))
        {
            return true;
        }

        if (property.CustomAttributes.Any(c => c.AttributeType == typeof(RequiredMemberAttribute)))
        {
            return true;
        }

        var propertyType = property.PropertyType;
        return propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) == null;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        {
            return name.ToLowerInvariant();
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
