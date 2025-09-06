using System.ComponentModel;

namespace Synaptic.NET.Domain.Resources.Storage;

/// <summary>
/// Represents the specific types of references within the storage domain.
/// This enumeration defines the various categories that can be assigned
/// to distinguish or classify stored items or resources.
/// </summary>
[Description("Represents the specific types of references within the storage domain.")]
public enum ReferenceType
{
    /// <summary>
    /// No specified reference.
    /// </summary>
    [Description("No specified reference.")]
    None,
    /// <summary>
    /// A memory created from a conversation would usually use conversation as reference type.
    /// </summary>
    [Description("A memory from a conversation would usually use conversation as reference type.")]
    Conversation,
    /// <summary>
    /// A memory created from another memory would usually use memory as reference type.
    /// </summary>
    [Description("A memory from another memory would usually use memory as reference type.")]
    Memory,
    /// <summary>
    /// A memory created from a document uses document as reference type.
    /// </summary>
    [Description("A memory from a document uses document as reference type.")]
    Document,
    /// <summary>
    /// A memory created from a codebase uses codebase as reference type.
    /// </summary>
    [Description("A memory from a codebase uses codebase as reference type.")]
    Codebase,
    /// <summary>
    /// A memory created from a homepage uses homepage as reference type.
    /// </summary>
    [Description("A memory from a homepage uses homepage as reference type.")]
    Homepage
}
