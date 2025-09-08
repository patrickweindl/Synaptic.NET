namespace Synaptic.NET.Domain.Attributes;

/// <summary>
/// An attribute defining how an AI assistant should handle a class, method or parameter.
/// </summary>
public class AssistantInstruction : Attribute
{
    /// <summary>
    /// The instruction text.
    /// </summary>
    public string Instruction { get; set; }

    public AssistantInstruction(string instruction)
    {
        Instruction = instruction;
    }
}
