namespace Synaptic.NET.OpenAI.Attributes;

/// <summary>
/// Attribute to add descriptions to properties for JSON schema generation
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonPropertyDescriptionAttribute : Attribute
{
    public string Description { get; }

    public JsonPropertyDescriptionAttribute(string description)
    {
        Description = description;
    }
}
