namespace Synaptic.NET.Authentication.Resources;

public struct RedirectSettings
{
    public required string Uri { get; set; }
    public required string Provider { get; set; }
}
