namespace Synaptic.NET.Domain.Attributes;

/// <summary>
/// Attribute to define JSON schema metadata for structured response classes
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class JsonSchemaAttribute : Attribute
{
    public string SchemaName { get; }
    public string Description { get; }

    public JsonSchemaAttribute(string schemaName, string description = "")
    {
        SchemaName = schemaName;
        Description = description;
    }
}
