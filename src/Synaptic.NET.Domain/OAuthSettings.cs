namespace Synaptic.NET.Domain;

public class OAuthSettings
{
    public required bool Enabled { get; set; }

    public required string ClientId { get; set; } = string.Empty;

    public required string ClientSecret { get; set; } = string.Empty;

    public required string OAuthUrl { get; set; } = string.Empty;
}
