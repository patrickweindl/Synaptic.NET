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
    public string GetUserIdentifier()
    {
        return GetCurrentUser().Identifier;
    }

    /// <summary>
    /// Retrieves the currently authenticated user from the service.
    /// </summary>
    /// <returns>A <see cref="User"/> object representing the current user.</returns>
    User GetCurrentUser();
}
