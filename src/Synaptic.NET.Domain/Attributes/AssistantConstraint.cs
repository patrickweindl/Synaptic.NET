namespace Synaptic.NET.Domain.Attributes;

/// <summary>
/// An attribute defining a constraint for an AI assistant on usage of a method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AssistantConstraint : Attribute
{
    /// <summary>
    /// Gets or sets the value representing the specific constraint
    /// applied to an AI assistant regarding the usage of a method.
    /// </summary>
    public string Constraint { get; set; }

    public AssistantConstraint(string constraint)
    {
        Constraint = constraint;
    }
}
