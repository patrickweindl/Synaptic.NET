using System.Security.Claims;

namespace Synaptic.NET.Domain.Abstractions.Management;

/// <summary>
/// A service to create symbolic links between user identities in case they switch their OAuth provider.
/// </summary>
public interface ISymLinkUserService
{
    /// <summary>
    /// Get all available symbolic links for a given user.
    /// </summary>
    /// <param name="claimsIdentity">The user in question.</param>
    /// <returns>All available symbolic links for the user in question.</returns>
    public Task<List<ClaimsIdentity>> GetSymLinkUsersAsync(ClaimsIdentity claimsIdentity);

    /// <summary>
    /// Gets the main identity for the given user identity.
    /// </summary>
    /// <param name="claimsIdentity">The claims identity to try and resolve.</param>
    /// <returns>The main claims identity for the user in question.</returns>
    public Task<ClaimsIdentity> GetMainIdentityAsync(ClaimsIdentity claimsIdentity);

    /// <summary>
    /// Adds a symbolic link between two identities.
    /// </summary>
    /// <param name="mainIdentity">The main identity of the user.</param>
    /// <param name="subIdentity">The sub identity to create a symbolic link for.</param>
    public Task AddSymLinkAsync(ClaimsIdentity mainIdentity, ClaimsIdentity subIdentity);

    /// <summary>
    /// Removes a symbolic link between two identities.
    /// </summary>
    /// <param name="mainIdentity">The main identity of the user.</param>
    /// <param name="subIdentity">The sub identity of the user that should get its symbolic link removed.</param>
    public Task RemoveSymLinkAsync(ClaimsIdentity mainIdentity, ClaimsIdentity subIdentity);
}
