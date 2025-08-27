using System.Text.Json.Serialization;

namespace Synaptic.NET.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Guest,
    Group,
    User,
    Admin
}
