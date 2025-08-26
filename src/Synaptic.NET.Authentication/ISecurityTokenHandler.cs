using System.Security.Claims;
using Synaptic.NET.Authentication.Resources;
using Synaptic.NET.Domain;

namespace Synaptic.NET.Authentication;

public interface ISecurityTokenHandler
{
    Task<VerificationResult> VerifyMicrosoftAuthentication(SynapticServerSettings settings, string clientId, string clientSecret, string code,
        string redirectUri, string grantType = "", string codeVerifier = "");

    Task<VerificationResult> VerifyGoogleAuthentication(SynapticServerSettings settings, string clientId, string clientSecret, string code,
        string redirectUri, string grantType = "", string codeVerifier = "");

    Task<VerificationResult> VerifyGitHubAuthentication(SynapticServerSettings settings, string clientId, string clientSecret, string code,
        string grantType = "", string codeVerifier = "");

    AccessTokenResult GenerateJwtToken(string jwtSecret, string jwtIssuer, TimeSpan lifetime, ClaimsIdentity? claimsIdentity);
}
