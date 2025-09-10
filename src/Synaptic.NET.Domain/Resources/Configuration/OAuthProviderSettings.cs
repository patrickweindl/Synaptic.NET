using Microsoft.Extensions.Configuration;
using Synaptic.NET.Domain.Helpers;

namespace Synaptic.NET.Domain.Resources.Configuration;

public class OAuthProviderSettings
{
    private readonly string _providerName;
    private string _enabledEnvVar => $"OAUTH__{_providerName.ToUpper()}__ENABLED";
    private string _clientIdEnvVar => $"OAUTH__{_providerName.ToUpper()}__CLIENTID";
    private string _clientSecretEnvVar => $"OAUTH__{_providerName.ToUpper()}__CLIENTSECRET";
    private string _oAuthUrlEnvVar => $"OAUTH__{_providerName.ToUpper()}__OAUTHURL";
    public OAuthProviderSettings(string providerName)
    {
        _providerName = providerName;
        _enabledEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => Enabled = bool.Parse(s));
        _clientIdEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => ClientId = s);
        _clientSecretEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => ClientSecret = s);
        _oAuthUrlEnvVar.AssignValueFromEnvironmentVariableIfAvailable(s => OAuthUrl = s);
        AssignProviderUrlIfMissing();
    }

    public OAuthProviderSettings(string providerName, IConfiguration configuration)
        : this(providerName)
    {
        if (configuration.GetSection("OAuth").Exists())
        {
            if (configuration.GetSection("OAuth").GetSection(providerName).Exists())
            {
                var oauthSection = configuration.GetSection("OAuth").GetSection(providerName);
                oauthSection.AssignValueIfAvailable(s => Enabled = bool.Parse(s), "Enabled");
                oauthSection.AssignValueIfAvailable(s => ClientId = s, "ClientId");
                oauthSection.AssignValueIfAvailable(s => ClientSecret = s, "ClientSecret");
                oauthSection.AssignValueIfAvailable(s => OAuthUrl = s, "OAuthUrl");
            }
        }
        AssignProviderUrlIfMissing();
    }

    private void AssignProviderUrlIfMissing()
    {
        if (string.IsNullOrEmpty(OAuthUrl))
        {
            OAuthUrl = _providerName.ToLowerInvariant() switch
            {
                "microsoft" => "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize",
                "google" => "https://accounts.google.com/o/oauth2/v2/auth",
                "github" => "https://github.com/login/oauth/authorize",
                _ => throw new NotImplementedException($"OAuth provider '{_providerName}' is not implemented.")
            };
        }
    }

    public bool Enabled { get; set; }

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string OAuthUrl { get; set; } = string.Empty;
}
