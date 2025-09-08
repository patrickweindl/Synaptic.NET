namespace Synaptic.NET.Domain.Attributes;

/// <summary>
/// An attribute defining an example for an AI assistant on usage of a method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AssistantExample : Attribute
{
    /// <summary>
    /// The example value as text.
    /// </summary>
    public string Value { get; set; }

    public AssistantExample(string value)
    {
        Value = value;
    }
}
