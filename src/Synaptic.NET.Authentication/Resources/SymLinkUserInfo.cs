using System.Text.Json.Serialization;

namespace Synaptic.NET.Authentication.Resources;

public class SymLinkUserInfo
{
    [JsonPropertyName("main_user_identifier")]
    public string MainUserIdentifier { get; set; } = string.Empty;
    [JsonPropertyName("sym_link_user_identifiers")]
    public List<string> SymLinkUserIdentifiers { get; set; } = new();
}
