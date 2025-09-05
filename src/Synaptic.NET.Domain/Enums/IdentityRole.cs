using System.Text.Json.Serialization;

namespace Synaptic.NET.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IdentityRole
{
    Guest,
    Group,
    User,
    Admin
}
