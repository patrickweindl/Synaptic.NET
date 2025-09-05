namespace Synaptic.NET.Domain.Resources.Configuration;

public class OAuthSettings
{
    public required bool Enabled { get; set; }

    public required string ClientId { get; set; } = string.Empty;

    public required string ClientSecret { get; set; } = string.Empty;

    public required string OAuthUrl { get; set; } = string.Empty;
}
