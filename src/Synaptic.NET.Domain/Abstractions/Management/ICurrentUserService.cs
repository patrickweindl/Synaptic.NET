using Synaptic.NET.Domain.Enums;
using Synaptic.NET.Domain.Resources.Management;

namespace Synaptic.NET.Domain.Abstractions.Management;

/// <summary>
/// Provides the functionality to interact with the current user in the application context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Retrieves the unique identifier of the currently authenticated user.
    /// </summary>
    /// <returns>A string representing the user's unique identifier.</returns>
    public async Task<string> GetUserIdentifierAsync()
    {
        return (await GetCurrentUserAsync()).Identifier;
    }

    /// <summary>
    /// Retrieves the currently authenticated user from the service.
    /// </summary>
    /// <returns>A <see cref="User"/> object representing the current user.</returns>
    Task<User> GetCurrentUserAsync();

    Task SetCurrentUserAsync(User user);

    /// <summary>
    ///
    /// </summary>
    /// <exception cref="UnauthorizedAccessException"></exception>
    async Task LockoutUserIfGuestAsync()
    {
        if ((await GetCurrentUserAsync()).Role <= IdentityRole.Guest)
        {
            throw new UnauthorizedAccessException("Guests cannot access this tool.");
        }
    }
}
