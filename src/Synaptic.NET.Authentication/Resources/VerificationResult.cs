namespace Synaptic.NET.Authentication.Resources;

public class VerificationResult
{
    public required bool Success { get; set; }
    public required string UserName { get; set; }
    public required string UserId { get; set; }
}

